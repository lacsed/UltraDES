using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UltraDES;

namespace UltraDES
{

    [Serializable]
    internal sealed class AdjacencyMatrix
    {
        // Keeps a reference to the selected implementation
        private readonly IAdjacencyMatrixImplementation _impl;

        /// <summary>
        /// Main constructor. If 'eventsNum' <= 64, uses the bitmask (ulong) implementation,
        /// otherwise, uses BitArray.
        /// The 'preAllocate' parameter can be forwarded to both implementations.
        /// </summary>
        public AdjacencyMatrix(int states, int eventsNum, bool preAllocate = false)
        {
            _impl = CreateImplementation(states, eventsNum, preAllocate);
            //_impl = new AdjacencyMatrixBDDImpl(states, eventsNum, preAllocate);
            //_impl = new AdjacencyMatrixBitArrayImpl(states, eventsNum, preAllocate);
        }

        private static IAdjacencyMatrixImplementation CreateImplementation(int states, int eventsNum, bool preAllocate)
        {
            // Static flag on DFA takes highest priority
            if (DeterministicFiniteAutomaton.UseDiskStorage)
                return new AdjacencyMatrixDiskImpl(states, eventsNum, preAllocate);

            var requestedImplementation = Environment.GetEnvironmentVariable("ULTRADES_ADJACENCY_MATRIX_IMPL");

            if (!string.IsNullOrWhiteSpace(requestedImplementation)
                && !requestedImplementation.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                return requestedImplementation.ToUpperInvariant() switch
                {
                    "USHORT" when eventsNum <= 16 => new AdjacencyMatrixUShortImpl(states, eventsNum, preAllocate),
                    "UINT" when eventsNum <= 32 => new AdjacencyMatrixUIntImpl(states, eventsNum, preAllocate),
                    "BITMASK" when eventsNum <= 64 => new AdjacencyMatrixBitMask(states, eventsNum, preAllocate),
                    "BDD" => new AdjacencyMatrixBDDImpl(states, eventsNum, preAllocate),
                    "BITARRAY" => new AdjacencyMatrixBitArrayImpl(states, eventsNum, preAllocate),
                    "BOOLARRAY" => new AdjacencyMatrixBoolArrayImpl(states, eventsNum, preAllocate),
                    "DISK" => new AdjacencyMatrixDiskImpl(states, eventsNum, preAllocate),
                    _ => throw new ArgumentException(
                        $"Unsupported adjacency matrix implementation '{requestedImplementation}' for {eventsNum} events.",
                        nameof(requestedImplementation))
                };
            }

            return eventsNum switch
            {
                <= 16 => new AdjacencyMatrixUShortImpl(states, eventsNum, preAllocate),
                <= 32 => new AdjacencyMatrixUIntImpl(states, eventsNum, preAllocate),
                <= 64 => new AdjacencyMatrixBitMask(states, eventsNum, preAllocate),
                _ => new AdjacencyMatrixBitArrayImpl(states, eventsNum, preAllocate)
            };
        }

        // Constructor used internally for cloning:
        private AdjacencyMatrix(IAdjacencyMatrixImplementation impl) => _impl = impl;

        /// <summary>
        /// Returns the number of states.
        /// </summary>
        public int Length => _impl.Length;

        /// <summary>
        /// Indexer: returns the destination, or -1 when there is no transition.
        /// </summary>
        public int this[int s, int e] => _impl[s, e];

        public bool TryGet(int s, int e, out int value)
        {
            value = this[s, e];
            return value != -1;
        }

        /// <summary>
        /// Indexer: returns the transitions (event -> destination) for state 's'.
        /// Returns an empty list when the state has no transitions.
        /// </summary>
        public List<(int e, int s)> this[int s] => _impl[s];

        /// <summary>
        /// Checks whether state 's' has event 'e'.
        /// </summary>
        public bool HasEvent(int s, int e) => _impl.HasEvent(s, e);

        /// <summary>
        /// Adds multiple (event, destination) pairs to a single state.
        /// </summary>
        public void Add(int origin, (int, int)[] values) => _impl.Add(origin, values);

        /// <summary>
        /// Adds an (event, destination) pair to a state.
        /// </summary>
        public void Add(int origin, int e, int dest) => _impl.Add(origin, e, dest);

        /// <summary>
        /// Removes event 'e' from state 'origin'.
        /// </summary>
        public void Remove(int origin, int e) => _impl.Remove(origin, e);

        /// <summary>
        /// Clones the whole adjacency matrix (deep copy).
        /// </summary>
        public AdjacencyMatrix Clone()
        {
            var clonedImpl = _impl.Clone();
            return new AdjacencyMatrix(clonedImpl);
        }

        /// <summary>
        /// Attempts to reduce the memory used by the internal collections.
        /// This can be useful for large collections.
        /// </summary>
        public void TrimExcess() => _impl.TrimExcess();
    }
}
