// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 05-20-2020
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
    /// <summary>
    /// Class StatesTupleComparator.
    /// Implements the <see cref="System.Collections.Generic.IEqualityComparer{UltraDES.StatesTuple}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{UltraDES.StatesTuple}" />
    [Serializable]
    public class StatesTupleComparator : IEqualityComparer<StatesTuple>
    {
        /// <summary>
        /// The m instance
        /// </summary>
        private static StatesTupleComparator _mInstance;

        /// <summary>
        /// Prevents a default instance of the <see cref="StatesTupleComparator"/> class from being created.
        /// </summary>
        private StatesTupleComparator() { }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <returns>StatesTupleComparator.</returns>
        public static StatesTupleComparator GetInstance() => _mInstance ??= new StatesTupleComparator();

        /// <summary>
        /// Equalses the specified a.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool Equals(StatesTuple a, StatesTuple b)
        {
            for (var i = a.MData.Length - 1; i >= 0; --i)
            {
                if (a.MData[i] != b.MData[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> for which a hash code is to be returned.</param>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public int GetHashCode(StatesTuple obj)
        {
            uint p = obj.MData[0];
            for (int i = obj.MData.Length - 1; i > 0; --i)
            {
                p = (p << 3) + ~p + (obj.MData[i] << 2) + ~obj.MData[i];
            }
            return (int)p;
        }
    }

    /// <summary>
    /// Class IntListComparator.
    /// Implements the <see cref="System.Collections.Generic.IEqualityComparer{System.Collections.Generic.List{System.Int32}}" />
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{System.Collections.Generic.List{System.Int32}}" />
    [Serializable]
    public class IntListComparator : IEqualityComparer<List<int>>
    {
        /// <summary>
        /// The m instance
        /// </summary>
        private static IntListComparator _mInstance = null;

        /// <summary>
        /// Prevents a default instance of the <see cref="IntListComparator"/> class from being created.
        /// </summary>
        private IntListComparator() { }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <returns>IntListComparator.</returns>
        public static IntListComparator getInstance()
        {
            if (_mInstance == null) _mInstance = new IntListComparator();
            return _mInstance;
        }

        /// <summary>
        /// Equalses the specified a.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool Equals(List<int> a, List<int> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a.Count != b.Count) return false;
            return !a.Where((t, i) => t != b[i]).Any();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> for which a hash code is to be returned.</param>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public int GetHashCode(List<int> obj)
        {
            return obj.Aggregate(obj.Count, (current, t) => (current << 3) + ~current + t.GetHashCode());
        }
    }
}

/// <summary>
/// Class IntArrayComparator.
/// Implements the <see cref="System.Collections.Generic.IEqualityComparer{System.Int32[]}" />
/// </summary>
/// <seealso cref="System.Collections.Generic.IEqualityComparer{System.Int32[]}" />
[Serializable]
public class IntArrayComparator : IEqualityComparer<int[]>
{
    /// <summary>
    /// The m instance
    /// </summary>
    private static IntArrayComparator _mInstance;

    /// <summary>
    /// Prevents a default instance of the <see cref="IntArrayComparator"/> class from being created.
    /// </summary>
    private IntArrayComparator() { }

    /// <summary>
    /// Gets the instance.
    /// </summary>
    /// <returns>IntArrayComparator.</returns>
    public static IntArrayComparator GetInstance()
    {
        return _mInstance ?? (_mInstance = new IntArrayComparator());
    }

    /// <summary>
    /// Equalses the specified a.
    /// </summary>
    /// <param name="a">a.</param>
    /// <param name="b">The b.</param>
    /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
    public bool Equals(int[] a, int[] b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.Length != b.Length) return false;
        return !a.Where((t, i) => t != b[i]).Any();
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <param name="obj">The <see cref="T:System.Object"></see> for which a hash code is to be returned.</param>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public int GetHashCode(int[] obj)
    {
        return obj.Aggregate(obj.Length, (current, t) => (current << 3) + ~current + t.GetHashCode());
    }
}