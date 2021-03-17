// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020
// ***********************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UltraDES
{

    /// <summary>
    /// Class AdjacencyMatrix.
    /// </summary>
    [Serializable]
    internal class AdjacencyMatrix
    {
        /// <summary>
        /// The internal.
        /// </summary>
        private readonly SortedList<int, int>[] _internal;
        /// <summary>
        /// The events
        /// </summary>
        private readonly BitArray[] _events;
        /// <summary>
        /// The events number
        /// </summary>
        private readonly int _eventsNum;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdjacencyMatrix"/> class.
        /// </summary>
        /// <param name="states">The states.</param>
        /// <param name="eventsNum">The events number.</param>
        /// <param name="preAllocate">if set to <c>true</c> [pre allocate].</param>
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
            this._eventsNum = eventsNum;
        }

        /// <summary>
        /// Gets the <see cref="System.Int32"/> with the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="e">The e.</param>
        /// <returns>System.Int32.</returns>
        public int this[int s, int e] => _events[s][e] ? _internal[s][e] : -1;

        /// <summary>
        /// Gets the <see cref="SortedList{System.Int32, System.Int32}"/> with the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>SortedList&lt;System.Int32, System.Int32&gt;.</returns>
        public SortedList<int, int> this[int s] => _internal[s] ?? (_internal[s] = new SortedList<int, int>());

        /// <summary>
        /// Determines whether the specified s has event.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="e">The e.</param>
        /// <returns><c>true</c> if the specified s has event; otherwise, <c>false</c>.</returns>
        public bool HasEvent(int s, int e)
        {
            return _events[s] != null && _events[s][e];
        }

        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length => _internal.Length;

        public void Add(int origin, Tuple<int, int>[] values)
        {
            _internal[origin] = new SortedList<int, int>(values.Length);
            _events[origin] = new BitArray(_eventsNum, false);

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
                _events[origin] = new BitArray(_eventsNum, false);
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
            var clone = new AdjacencyMatrix(_internal.Length, _eventsNum);

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


        /// <summary>   Trim excess. </summary>
        ///
        /// <remarks>   Lucas Alves, 18/01/2016. </remarks>
        public void TrimExcess() => _internal
            .AsParallel()
            .WithDegreeOfParallelism(DeterministicFiniteAutomaton.Multicore ? Environment.ProcessorCount : 1)
            .Where(i => i != null)
            .ForAll(i => i.TrimExcess());
    }
}
