using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
    [Serializable]
    public class StatesTupleComparator : IEqualityComparer<StatesTuple>
    {
        private static StatesTupleComparator _mInstance = null;

        private StatesTupleComparator() { }

        public static StatesTupleComparator GetInstance()
        {
            return _mInstance ?? (_mInstance = new StatesTupleComparator());
        }

        public bool Equals(StatesTuple a, StatesTuple b)
        {
            for (int i = a.MData.Length - 1; i >= 0; --i)
            {
                if (a.MData[i] != b.MData[i])
                    return false;
            }
            return true;
        }

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

    [Serializable]
    public class IntListComparator : IEqualityComparer<List<int>>
    {
        private static IntListComparator _mInstance = null;

        private IntListComparator() { }

        public static IntListComparator getInstance()
        {
            if (_mInstance == null) _mInstance = new IntListComparator();
            return _mInstance;
        }

        public bool Equals(List<int> a, List<int> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a.Count != b.Count) return false;
            for (var i = 0; i < a.Count; ++i)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        public int GetHashCode(List<int> obj)
        {
            return obj.Aggregate(obj.Count, (current, t) => (current << 3) + ~current + t.GetHashCode());
        }
    }
}

[Serializable]
public class IntArrayComparator : IEqualityComparer<int[]>
{
    private static IntArrayComparator _mInstance = null;

    private IntArrayComparator() { }

    public static IntArrayComparator GetInstance()
    {
        if (_mInstance == null) _mInstance = new IntArrayComparator();
        return _mInstance;
    }

    public bool Equals(int[] a, int[] b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; ++i)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    public int GetHashCode(int[] obj)
    {
        return obj.Aggregate(obj.Length, (current, t) => (current << 3) + ~current + t.GetHashCode());
    }
}