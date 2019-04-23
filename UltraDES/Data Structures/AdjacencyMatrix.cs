
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UltraDES
{

    [Serializable]
    class AdjacencyMatrix
    {
        /// <summary>   The internal. </summary>
        private readonly SortedList<int, int>[] _internal;
        private readonly BitArray[] _events;
        private readonly int eventsNum;

        public AdjacencyMatrix(int states, int eventsNum, bool preAllocate = false)
        {
            _internal = new SortedList<int, int>[states];
            _events = new BitArray[states];

            if (preAllocate)
            {
                for(var i = 0; i < states; ++i)
                {
                    _internal[i] = new SortedList<int, int>();
                    _events[i] = new BitArray(eventsNum, false);
                }
            }
            this.eventsNum = eventsNum;
        }

        public int this[int s, int e] => _events[s][e] ? _internal[s][e] : -1;

        public SortedList<int, int> this[int s] => _internal[s] ?? (_internal[s] = new SortedList<int, int>());

        public bool HasEvent(int s, int e)
        {
            return _events[s] != null && _events[s][e];
        }

        public int Length => _internal.Length;

        public void Add(int origin, Tuple<int, int>[] values)
        {
            _internal[origin] = new SortedList<int, int>(values.Length);
            _events[origin] = new BitArray(eventsNum, false);

            foreach (var (item1, item2) in values)
            {
                if (!_events[origin][item1])
                {
                    _internal[origin].Add(item1, item2);
                    _events[origin][item1] = true;
                }
                else if(_internal[origin][item1] != item2)
                {
                    throw new Exception("Automaton is not deterministic.");
                }
            }
        }

        public void Add(int origin, int e, int dest)
        {
            if (_internal[origin] == null)
            {
                _internal[origin] = new SortedList<int, int>();
                _events[origin] = new BitArray(eventsNum, false);
            }
            if (!_events[origin][e])
            {
                _internal[origin].Add(e, dest);
                _events[origin][e] = true;
            }
            else if(_internal[origin][e] != dest)
            {
                throw new Exception("Automaton is not deterministic.");
            }
        }

        public void Remove(int origin, int e)
        {
            _events[origin][e] = false;
            _internal[origin].Remove(e);
        }

        public AdjacencyMatrix Clone()
        {
            var clone = new AdjacencyMatrix(_internal.Length, eventsNum);

            for (int i = 0; i < _internal.Length; i++)
            {
                if (_events[i] != null)
                    clone._events[i] = (BitArray)_events[i].Clone();

                if (_internal[i] == null) continue;
                clone._internal[i] = new SortedList<int, int>();
                foreach (var c in _internal[i])
                    clone._internal[i].Add(c.Key, c.Value);
            }
            return clone;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Trim excess. </summary>
        ///
        /// <remarks>   Lucas Alves, 18/01/2016. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void TrimExcess()
        {
            Parallel.ForEach(_internal.Where(i => i != null), i => i.TrimExcess());
        }
    }
}
