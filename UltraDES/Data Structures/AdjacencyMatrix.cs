////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	data structures\adjacencymatrix.cs
//
// summary:	Implements the adjacencymatrix class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
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
        private readonly SortedList<int, int>[] _internal;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Lucas Alves, 18/01/2016. </remarks>
        ///
        /// <param name="states">   The states. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public AdjacencyMatrix(int states)
        {
            _internal = new SortedList<int, int>[states];
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
                if (s >= _internal.Length || s < 0 || _internal[s] == null) return -1;
                return _internal[s].ContainsKey(e) ? _internal[s][e] : -1;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Indexer to get items within this collection using array index syntax. </summary>
        ///
        /// <param name="s">    The int to process. </param>
        ///
        /// <returns>   The indexed item. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public SortedList<int, int> this[int s]
        {
            get { return _internal[s] ?? (_internal[s] = new SortedList<int, int>()); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the length. </summary>
        ///
        /// <value> The length. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public int Length
        {
            get { return _internal.Length; }
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
            _internal[origin] = new SortedList<int, int>(values.Length);
            foreach (var value in values)
                _internal[origin].Add(value.Item1, value.Item2);
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