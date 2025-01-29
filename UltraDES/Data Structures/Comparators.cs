using System;
using System.Collections.Generic;

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
            if (a.MData == null || b.MData == null) return false;
            if (a.MData.Length != b.MData.Length) return false;
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
            if (obj.MData == null) return 0;
            unchecked
            {
                int hash = 17;
                foreach (var x in obj.MData) hash = hash * 31 + (int)x;
                return hash;
            }
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
        private static IntListComparator _mInstance;

        /// <summary>
        /// Prevents a default instance of the <see cref="IntListComparator"/> class from being created.
        /// </summary>
        private IntListComparator() { }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <returns>IntListComparator.</returns>
        public static IntListComparator getInstance() => _mInstance ??= new IntListComparator();

        /// <summary>
        /// Equalses the specified a.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool Equals(List<int> a, List<int> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }


        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> for which a hash code is to be returned.</param>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public int GetHashCode(List<int> obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var x in obj) hash = hash * 31 + x;
                return hash;
            }
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
    public static IntArrayComparator GetInstance() => _mInstance ??= new IntArrayComparator();

    /// <summary>
    /// Equalses the specified a.
    /// </summary>
    /// <param name="a">a.</param>
    /// <param name="b">The b.</param>
    /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
    public bool Equals(int[] a, int[] b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }


    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <param name="obj">The <see cref="T:System.Object"></see> for which a hash code is to be returned.</param>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public int GetHashCode(int[] obj)
    {
        unchecked
        {
            int hash = 17;
            foreach (var x in obj) hash = hash * 31 + x;
            return hash;
        }
    }
}