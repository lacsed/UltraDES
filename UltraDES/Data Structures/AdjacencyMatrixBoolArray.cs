using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
    /// <summary>
    /// Implementation that uses bool[] per state to mark which events exist.
    /// It is only used if the number of states is less than 1000 (given criterion).
    /// </summary>
    [Serializable]
    internal sealed class AdjacencyMatrixBoolArrayImpl : IAdjacencyMatrixImplementation
    {
        private readonly SortedList<int, int>[] _internal;
        private readonly bool[][] _events;  // For each state, a bool array with size EventsNum

        public int Length => _internal.Length;
        public int EventsNum { get; }

        public AdjacencyMatrixBoolArrayImpl(int states, int eventsNum, bool preAllocate = false)
        {
            EventsNum = eventsNum;
            _internal = new SortedList<int, int>[states];
            _events = new bool[states][];

            if (preAllocate)
            {
                for (int s = 0; s < states; s++)
                {
                    _internal[s] = new SortedList<int, int>();
                    _events[s] = new bool[eventsNum];
                }
            }
        }

        /// <summary>
        /// Indexer [s, e]: returns the destination or -1 if the transition does not exist
        /// </summary>
        public int this[int s, int e]
            => HasEvent(s, e) ? _internal[s][e] : -1;

        /// <summary>
        /// Indexer [s]: returns the state transitions or an empty list when there are none.
        /// </summary>
        public List<(int, int)> this[int s] => _internal[s] == null
            ? new List<(int, int)>()
            : _internal[s].Select(kvp => (kvp.Key, kvp.Value)).ToList();

        /// <summary>
        /// Checks whether event 'e' exists in state 's'
        /// </summary>
        public bool HasEvent(int s, int e)
        {
            // If _events[s] has not been created yet, there is no event
            if (_events[s] == null) return false;
            return _events[s][e];
        }

        /// <summary>
        /// Adds multiple (event, destination) pairs to state 'origin'
        /// </summary>
        public void Add(int origin, (int, int)[] values)
        {
            if (_internal[origin] == null)
            {
                _internal[origin] = new SortedList<int, int>(values.Length);
                _events[origin] = new bool[EventsNum];
            }

            foreach (var (evt, dest) in values)
            {
                if (!_events[origin][evt])
                {
                    _internal[origin].Add(evt, dest);
                    _events[origin][evt] = true;
                }
                else
                {
                    // If it already exists, checks determinism
                    if (_internal[origin][evt] != dest)
                        throw new Exception("Automaton is not deterministic.");
                }
            }
        }

        /// <summary>
        /// Adds an (event, destination) pair to state 'origin'
        /// </summary>
        public void Add(int origin, int e, int dest)
        {
            if (_internal[origin] == null)
            {
                _internal[origin] = new SortedList<int, int>();
                _events[origin] = new bool[EventsNum];
            }

            if (!_events[origin][e])
            {
                _internal[origin].Add(e, dest);
                _events[origin][e] = true;
            }
            else
            {
                // If it already exists, checks determinism
                if (_internal[origin][e] != dest)
                    throw new Exception("Automaton is not deterministic.");
            }
        }

        /// <summary>
        /// Removes event 'e' from state 'origin'
        /// </summary>
        public void Remove(int origin, int e)
        {
            _events[origin][e] = false;
            _internal[origin]?.Remove(e);
        }

        /// <summary>
        /// Clones the adjacency matrix
        /// </summary>
        public IAdjacencyMatrixImplementation Clone()
        {
            var clone = new AdjacencyMatrixBoolArrayImpl(Length, EventsNum);
            for (int s = 0; s < Length; s++)
            {
                if (_internal[s] != null)
                {
                    // Clones the SortedList
                    clone._internal[s] = new SortedList<int, int>();
                    foreach (var kv in _internal[s])
                    {
                        clone._internal[s].Add(kv.Key, kv.Value);
                    }
                }
                // Clones the bool array
                if (_events[s] != null)
                {
                    clone._events[s] = new bool[EventsNum];
                    Array.Copy(_events[s], clone._events[s], EventsNum);
                }
            }
            return clone;
        }

        /// <summary>
        /// Requests that the internal collections release extra memory
        /// </summary>
        public void TrimExcess()
        {
            foreach (var sl in _internal)
            {
                sl?.TrimExcess();
            }
        }
    }
}
