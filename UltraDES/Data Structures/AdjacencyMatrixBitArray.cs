using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UltraDES
{
    internal sealed class AdjacencyMatrixBitArrayImpl : IAdjacencyMatrixImplementation
    {
        private readonly SortedList<int, int>[] _internal;
        private readonly BitArray[] _events; // Cada estado tem um BitArray para marcar eventos

        public int Length => _internal.Length;
        public int EventsNum { get; }

        public AdjacencyMatrixBitArrayImpl(int states, int eventsNum, bool preAllocate = false)
        {
            EventsNum = eventsNum;
            _internal = new SortedList<int, int>[states];
            _events = new BitArray[states];

            if (!preAllocate) return;
            for (int i = 0; i < states; i++)
            {
                _internal[i] = new SortedList<int, int>();
                _events[i] = new BitArray(eventsNum, false);
            }
        }

        public int this[int s, int e]
            => HasEvent(s, e) ? _internal[s][e] : -1;

        public List<(int, int)> this[int s] => _internal[s].Select(kvp => (kvp.Key, kvp.Value)).ToList();

        public bool HasEvent(int s, int e)
        {
            return _events[s] != null && _events[s][e];
        }

        public void Add(int origin, (int, int)[] values)
        {
            if (_internal[origin] == null)
            {
                _internal[origin] = new SortedList<int, int>(values.Length);
                _events[origin] = new BitArray(EventsNum, false);
            }

            foreach (var tuple in values)
            {
                int e = tuple.Item1;
                int dest = tuple.Item2;

                if (!_events[origin][e])
                {
                    _internal[origin].Add(e, dest);
                    _events[origin][e] = true;
                }
                else
                {
                    if (_internal[origin][e] != dest)
                        throw new Exception("Automaton is not deterministic.");
                }
            }
        }

        public void Add(int origin, int e, int dest)
        {
            if (_internal[origin] == null)
            {
                _internal[origin] = new SortedList<int, int>();
                _events[origin] = new BitArray(EventsNum, false);
            }

            if (!_events[origin][e])
            {
                _internal[origin].Add(e, dest);
                _events[origin][e] = true;
            }
            else
            {
                if (_internal[origin][e] != dest)
                    throw new Exception("Automaton is not deterministic.");
            }
        }

        public void Remove(int origin, int e)
        {
            _events[origin][e] = false;
            _internal[origin]?.Remove(e);
        }

        public IAdjacencyMatrixImplementation Clone()
        {
            var clone = new AdjacencyMatrixBitArrayImpl(Length, EventsNum);

            for (int i = 0; i < Length; i++)
            {
                if (_events[i] != null) clone._events[i] = (BitArray)_events[i].Clone();

                if (_internal[i] == null) continue;
                clone._internal[i] = new SortedList<int, int>();
                foreach (var kv in _internal[i])
                {
                    clone._internal[i].Add(kv.Key, kv.Value);
                }
            }

            return clone;
        }

        public void TrimExcess()
        {
            foreach (var sl in _internal) sl?.TrimExcess();
        }
    }
}
