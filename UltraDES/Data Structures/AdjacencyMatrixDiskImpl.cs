using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace UltraDES
{
    /// <summary>
    /// Disk-backed adjacency matrix implementation using MemoryMappedFile for secondary storage.
    /// Uses a dense layout (int32[states * eventsNum]) with L1 cache and Clock eviction policy.
    /// Designed for very large automata with optimal NVMe performance.
    /// </summary>
    internal sealed class AdjacencyMatrixDiskImpl : IAdjacencyMatrixImplementation, IDisposable
    {
        private readonly string _filePath;
        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _accessor;
        private readonly int _length;
        private readonly CacheSlot[] _cache;
        private readonly Dictionary<int, int> _cacheMap;
        private int _clockHand;
        private readonly int _cacheCapacity;
        private bool _disposed;

        public int Length => _length;
        public int EventsNum { get; }

        private struct CacheSlot
        {
            public int StateIndex;
            public int[] Row;
            public bool IsDirty;
            public bool UseFlag;
        }

        /// <summary>
        /// Constructor for disk-backed adjacency matrix.
        /// </summary>
        /// <param name="states">Number of states</param>
        /// <param name="eventsNum">Number of events</param>
        /// <param name="preAllocate">Ignored for disk implementation (always pre-allocated)</param>
        /// <param name="cacheCapacity">Number of cache slots (default: 512)</param>
        public AdjacencyMatrixDiskImpl(int states, int eventsNum, bool preAllocate = false, int cacheCapacity = 512)
        {
            _length = states;
            EventsNum = eventsNum;
            _cacheCapacity = cacheCapacity;
            _cache = new CacheSlot[_cacheCapacity];
            _cacheMap = new Dictionary<int, int>(_cacheCapacity);
            _clockHand = 0;

            // Initialize cache slots
            for (int i = 0; i < _cacheCapacity; i++)
            {
                _cache[i] = new CacheSlot
                {
                    StateIndex = -1,
                    Row = null,
                    IsDirty = false,
                    UseFlag = false
                };
            }

            // Create temporary file in the configured disk storage path
            var tempPath = DeterministicFiniteAutomaton.DiskStorageTempPath;
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            _filePath = Path.Combine(tempPath, $"UltraDES_AdjMatrix_{Guid.NewGuid():N}.tmp");

            // Calculate file size: states * eventsNum * sizeof(int)
            long fileSize = (long)states * eventsNum * sizeof(int);

            // Create file and initialize with -1 (no transition)
            using (var fs = new FileStream(_filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                fs.SetLength(fileSize);

                // Initialize in chunks of 64 KB to avoid memory explosion
                const int chunkSize = 64 * 1024 / sizeof(int); // 16384 ints
                int[] chunk = new int[chunkSize];
                for (int i = 0; i < chunkSize; i++)
                {
                    chunk[i] = -1;
                }

                long position = 0;
                while (position < fileSize)
                {
                    int bytesToWrite = (int)Math.Min(chunkSize * sizeof(int), fileSize - position);
                    int intsToWrite = bytesToWrite / sizeof(int);

                    for (int i = 0; i < intsToWrite; i++)
                    {
                        fs.Write(BitConverter.GetBytes(-1), 0, sizeof(int));
                    }

                    position += bytesToWrite;
                }
            }

            // Open memory-mapped file
            _mmf = MemoryMappedFile.CreateFromFile(_filePath, FileMode.Open, null, fileSize, MemoryMappedFileAccess.ReadWrite);
            _accessor = _mmf.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.ReadWrite);
        }

        /// <summary>
        /// Gets the destination state for a given state and event.
        /// </summary>
        public int this[int s, int e]
        {
            get
            {
                var row = LoadRow(s);
                return row[e];
            }
        }

        /// <summary>
        /// Gets all transitions for a given state.
        /// </summary>
        public List<(int e, int s)> this[int s]
        {
            get
            {
                var row = LoadRow(s);
                var result = new List<(int, int)>();

                for (int e = 0; e < EventsNum; e++)
                {
                    if (row[e] != -1)
                    {
                        result.Add((e, row[e]));
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Checks whether a state has a specific event.
        /// </summary>
        public bool HasEvent(int s, int e)
        {
            var row = LoadRow(s);
            return row[e] != -1;
        }

        /// <summary>
        /// Adds a single transition.
        /// </summary>
        public void Add(int origin, int e, int dest)
        {
            var row = LoadRow(origin, markDirty: true);

            if (row[e] != -1)
            {
                if (row[e] != dest)
                {
                    throw new Exception("Automaton is not deterministic.");
                }
            }
            else
            {
                row[e] = dest;
            }
        }

        /// <summary>
        /// Adds multiple transitions for a single state.
        /// </summary>
        public void Add(int origin, (int, int)[] values)
        {
            foreach (var (e, dest) in values)
            {
                Add(origin, e, dest);
            }
        }

        /// <summary>
        /// Removes a transition.
        /// </summary>
        public void Remove(int origin, int e)
        {
            var row = LoadRow(origin, markDirty: true);
            row[e] = -1;
        }

        /// <summary>
        /// Loads a row (state) from cache or disk.
        /// </summary>
        private int[] LoadRow(int stateIndex, bool markDirty = false)
        {
            // Check if already in cache
            if (_cacheMap.TryGetValue(stateIndex, out int slotIndex))
            {
                _cache[slotIndex].UseFlag = true;
                if (markDirty)
                {
                    _cache[slotIndex].IsDirty = true;
                }
                return _cache[slotIndex].Row;
            }

            // Cache miss: evict using Clock algorithm
            int victimSlot = EvictSlot();

            // Load row from disk
            int[] row = new int[EventsNum];
            long offset = (long)stateIndex * EventsNum * sizeof(int);

            for (int i = 0; i < EventsNum; i++)
            {
                row[i] = _accessor.ReadInt32(offset + i * sizeof(int));
            }

            // Update cache
            _cache[victimSlot] = new CacheSlot
            {
                StateIndex = stateIndex,
                Row = row,
                IsDirty = markDirty,
                UseFlag = true
            };
            _cacheMap[stateIndex] = victimSlot;

            return row;
        }

        /// <summary>
        /// Evicts a slot using Clock (second chance) algorithm.
        /// </summary>
        private int EvictSlot()
        {
            while (true)
            {
                ref var slot = ref _cache[_clockHand];

                // Empty slot
                if (slot.StateIndex == -1)
                {
                    int victimIndex = _clockHand;
                    _clockHand = (_clockHand + 1) % _cacheCapacity;
                    return victimIndex;
                }

                // Second chance
                if (slot.UseFlag)
                {
                    slot.UseFlag = false;
                    _clockHand = (_clockHand + 1) % _cacheCapacity;
                    continue;
                }

                // Evict this slot
                if (slot.IsDirty)
                {
                    FlushSlot(_clockHand);
                }

                _cacheMap.Remove(slot.StateIndex);
                int victimIndex2 = _clockHand;
                _clockHand = (_clockHand + 1) % _cacheCapacity;
                return victimIndex2;
            }
        }

        /// <summary>
        /// Flushes a dirty slot to disk.
        /// </summary>
        private void FlushSlot(int slotIndex)
        {
            ref var slot = ref _cache[slotIndex];
            if (!slot.IsDirty || slot.Row == null)
            {
                return;
            }

            long offset = (long)slot.StateIndex * EventsNum * sizeof(int);

            for (int i = 0; i < EventsNum; i++)
            {
                _accessor.Write(offset + i * sizeof(int), slot.Row[i]);
            }

            slot.IsDirty = false;
        }

        /// <summary>
        /// Flushes all dirty slots to disk.
        /// </summary>
        private void FlushAll()
        {
            for (int i = 0; i < _cacheCapacity; i++)
            {
                if (_cache[i].IsDirty)
                {
                    FlushSlot(i);
                }
            }
        }

        /// <summary>
        /// Clones the adjacency matrix (deep copy with new file).
        /// </summary>
        public IAdjacencyMatrixImplementation Clone()
        {
            // Flush all dirty cache before cloning
            FlushAll();

            var clone = new AdjacencyMatrixDiskImpl(_length, EventsNum, false, _cacheCapacity);

            // Copy file content in chunks
            const int chunkSize = 64 * 1024 / sizeof(int); // 16384 ints
            int[] buffer = new int[chunkSize];
            long totalInts = (long)_length * EventsNum;
            long position = 0;

            while (position < totalInts)
            {
                int intsToRead = (int)Math.Min(chunkSize, totalInts - position);
                long byteOffset = position * sizeof(int);

                for (int i = 0; i < intsToRead; i++)
                {
                    buffer[i] = _accessor.ReadInt32(byteOffset + i * sizeof(int));
                }

                for (int i = 0; i < intsToRead; i++)
                {
                    clone._accessor.Write(byteOffset + i * sizeof(int), buffer[i]);
                }

                position += intsToRead;
            }

            return clone;
        }

        /// <summary>
        /// Flushes all pending writes to disk.
        /// </summary>
        public void TrimExcess()
        {
            FlushAll();
        }

        /// <summary>
        /// Disposes resources and deletes temporary file.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            FlushAll();
            _accessor?.Dispose();
            _mmf?.Dispose();

            if (File.Exists(_filePath))
            {
                try
                {
                    File.Delete(_filePath);
                }
                catch
                {
                    // Ignore deletion errors
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer to ensure file cleanup.
        /// </summary>
        ~AdjacencyMatrixDiskImpl()
        {
            Dispose();
        }
    }
}
