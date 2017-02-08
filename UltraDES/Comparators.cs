using System;
using System.Collections.Generic;

namespace UltraDES
{
    [Serializable]
    public class StatesTupleComparator : IEqualityComparer<StatesTuple>
    {
        private static StatesTupleComparator m_instance = null;

        private StatesTupleComparator() { }

        public static StatesTupleComparator getInstance()
        {
            if (m_instance == null) m_instance = new StatesTupleComparator();
            return m_instance;
        }

        public bool Equals(StatesTuple a, StatesTuple b)
        {
            for (int i = a.m_data.Length - 1; i >= 0; --i)
            {
                if (a.m_data[i] != b.m_data[i])
                    return false;
            }
            return true;
        }

        public int GetHashCode(StatesTuple obj)
        {
            int p = 0;
            for (int i = obj.m_data.Length - 1; i >= 0; --i)
                p = (p << 1) + (int)obj.m_data[i];
            return p;
        }
    }

    [Serializable]
    public class IntListComparator : IEqualityComparer<List<int>>
    {
        private static IntListComparator m_instance = null;

        private IntListComparator() { }

        public static IntListComparator getInstance()
        {
            if (m_instance == null) m_instance = new IntListComparator();
            return m_instance;
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
            int p = 0;
            for(var i = 0; i < obj.Count; ++i)
            {
                p = (p<<1) + obj[i].GetHashCode();
            }
            return p;
        }
    }
}

[Serializable]
public class IntArrayComparator : IEqualityComparer<int[]>
{
    private static IntArrayComparator m_instance = null;

    private IntArrayComparator() { }

    public static IntArrayComparator getInstance()
    {
        if (m_instance == null) m_instance = new IntArrayComparator();
        return m_instance;
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
        int p = 0;
        for (var i = 0; i < obj.Length; ++i)
        {
            p = (p << 1) + obj[i].GetHashCode();
        }
        return p;
    }
}