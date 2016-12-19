////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	data structures\adjacencymatrix.cs
//
// summary:	Implements the adjacencymatrix class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a adjacency matrix. </summary>
    ///
    /// <remarks>   Lucas Alves, 18/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class AdjacencyMatrix
    {
        /// <summary>   The internal. </summary>
        private readonly SortedList<int, int>[] m_internal;
        private readonly BitArray[] m_events;
        private readonly int m_numerOfEvents;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Lucas Alves, 18/01/2016. </remarks>
        ///
        /// <param name="states">   The states. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public AdjacencyMatrix(int states, int numEvents)
        {
            m_internal = new SortedList<int, int>[states];
            m_events = new BitArray[states];
            m_numerOfEvents = numEvents;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Indexer to get items within this collection using array index syntax. </summary>
        ///
        /// <param name="s">    The int to process. </param>
        /// <param name="e">    The int to process. </param>
        ///
        /// <returns>   The indexed item. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int this[int s, int e]
        {
            get
            {
                return m_events[s][e] ? m_internal[s][e] : -1;
            }
        }

        public SortedList<int, int> this[uint s]
        {
            get
            {
                return m_internal[s] ?? (m_internal[s] = new SortedList<int, int>());
            }
        }

        public bool hasEvent(uint s, int e)
        {
            return m_events[s] != null ? m_events[s][e] : false;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the length. </summary>
        ///
        /// <value> The length. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int Length
        {
            get { return m_internal.Length; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Adds origin. </summary>
        ///
        /// <remarks>   Lucas Alves, 18/01/2016. </remarks>
        ///
        /// <param name="origin">   The origin. </param>
        /// <param name="values">   The values. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Add(int origin, Tuple<int, int>[] values)
        {
            m_internal[origin] = new SortedList<int, int>(values.Length);
            m_events[origin] = new BitArray(m_numerOfEvents, false);

            foreach (var value in values)
            {
                m_internal[origin].Add(value.Item1, value.Item2);
                m_events[origin][value.Item1] = true;
            }
        }

        public void Add(int origin, int e, int dest)
        {
            if (m_internal[origin] == null)
            {
                m_internal[origin] = new SortedList<int, int>();
                m_events[origin] = new BitArray(m_numerOfEvents, false);
            }
            m_internal[origin].Add(e, dest);
            m_events[origin][e] = true;
        }

        public void Remove(int origin, int e)
        {
            m_events[origin][e] = false;
            m_internal[origin].Remove(e);
        }

        public AdjacencyMatrix Clone()
        {
            var clone = new AdjacencyMatrix(m_internal.Length, m_numerOfEvents);

            for (int i = 0; i < m_internal.Length; i++)
            {
                if (m_events[i] != null)
                    clone.m_events[i] = (BitArray)m_events[i].Clone();

                if (m_internal[i] != null)
                {
                    clone.m_internal[i] = new SortedList<int, int>();
                    foreach (var c in m_internal[i])
                        clone.m_internal[i].Add(c.Key, c.Value);

                }
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
            Parallel.ForEach(m_internal.Where(i => i != null), i => i.TrimExcess());
        }
    }
}
