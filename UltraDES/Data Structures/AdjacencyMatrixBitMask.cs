using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
    [Serializable]
    internal sealed class AdjacencyMatrixBitMask : IAdjacencyMatrixImplementation
    {
        // Each state has a SortedList<event, destination>
        private readonly SortedList<int, int>[] _internal;

        // Each state has a 'ulong' with marked bits indicating which events exist
        private readonly ulong[] _eventMask;

        public int Length => _internal.Length;
        public int EventsNum { get; }

        public AdjacencyMatrixBitMask(int states, int eventsNum, bool preAllocate = false)
        {
            if (eventsNum > 64)
                throw new ArgumentException("This implementation only supports up to 64 events.", nameof(eventsNum));

            EventsNum = eventsNum;
            _internal = new SortedList<int, int>[states];
            _eventMask = new ulong[states];

            if (preAllocate)
            {
                for (int i = 0; i < states; i++)
                {
                    _internal[i] = new SortedList<int, int>();
                    _eventMask[i] = 0UL;
                }
            }
        }

        public int this[int s, int e]
            => HasEvent(s, e) ? _internal[s][e] : -1;

        public List<(int, int)> this[int s] => _internal[s] == null
            ? new List<(int, int)>()
            : _internal[s].Select(kvp => (kvp.Key, kvp.Value)).ToList();

        public bool HasEvent(int s, int e)
        {
            // Checks whether bit 'e' is marked for state 's'
            ulong mask = 1UL << e;
            return (_eventMask[s] & mask) != 0;
        }

        public void Add(int origin, (int, int)[] values)
        {
            // Creates the row if it has not been allocated yet
            if (_internal[origin] == null)
            {
                _internal[origin] = new SortedList<int, int>(values.Length);
                _eventMask[origin] = 0UL;
            }

            foreach (var tuple in values)
            {
                int e = tuple.Item1;
                int dest = tuple.Item2;
                ulong mask = 1UL << e;

                if ((_eventMask[origin] & mask) == 0UL)
                {
                    _internal[origin].Add(e, dest);
                    _eventMask[origin] |= mask;
                }
                else
                {
                    // If it already exists, checks determinism
                    if (_internal[origin][e] != dest)
                        throw new Exception("Automaton is not deterministic.");
                }
            }
        }

        public void Add(int origin, int e, int dest)
        {
            // Creates the row if it has not been allocated yet
            if (_internal[origin] == null)
            {
                _internal[origin] = new SortedList<int, int>();
                _eventMask[origin] = 0UL;
            }

            ulong mask = 1UL << e;
            if ((_eventMask[origin] & mask) == 0UL)
            {
                _internal[origin].Add(e, dest);
                _eventMask[origin] |= mask;
            }
            else
            {
                if (_internal[origin][e] != dest)
                    throw new Exception("Automaton is not deterministic.");
            }
        }

        public void Remove(int origin, int e)
        {
            if (_internal[origin] != null)
            {
                _internal[origin].Remove(e);
            }

            ulong mask = 1UL << e;
            _eventMask[origin] &= ~mask;
        }

        public IAdjacencyMatrixImplementation Clone()
        {
            var clone = new AdjacencyMatrixBitMask(Length, EventsNum);
            for (int i = 0; i < Length; i++)
            {
                // Clones the bitmask
                clone._eventMask[i] = _eventMask[i];
                if (_internal[i] != null)
                {
                    // Creates a new SortedList and copies values
                    clone._internal[i] = new SortedList<int, int>();
                    foreach (var kv in _internal[i])
                    {
                        clone._internal[i].Add(kv.Key, kv.Value);
                    }
                }
            }

            return clone;
        }

        public void TrimExcess()
        {
            // Calls TrimExcess for each SortedList
            // (if it is too large, evaluate whether parallelization makes sense)
            foreach (var sl in _internal)
            {
                sl?.TrimExcess();
            }
        }

    }
}
