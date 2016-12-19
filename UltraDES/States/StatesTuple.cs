using System;
using System.Collections.Generic;

namespace UltraDES
{
    [Serializable]
    public class StatesTuple
    {
        public uint[] m_data
        {
            get;
        }

        public StatesTuple(uint[] states, int[] bits, int size)
        {
            m_data = new uint[size];
            Set(states, bits);
        }

        public StatesTuple(int k)
        {
            m_data = new uint[k];
        }

        public void Set(uint[] states, int[] bits)
        {
            int j = -1;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i] == 0)
                {
                    ++j;
                    m_data[j] = 0;
                }
                m_data[j] += (states[i] << bits[i]);
            }
        }

        public void Get(uint[] states, int[] bits, uint[] maxSize)
        {
            int j = -1;
            for (int i = 0; i < states.Length; i++)
            {
                if (bits[i] == 0)
                    j++;
                states[i] = (m_data[j] >> bits[i]) & maxSize[i];
            }
        }
    }

    public class StatesTupleComparator : IEqualityComparer<StatesTuple>
    {
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
            uint p = 0;
            for (int i = obj.m_data.Length - 1; i >= 0; --i)
                p = (p << 1) + obj.m_data[i];
            return (int)p;
        }
    }
}