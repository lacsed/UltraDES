using System;

namespace UltraDES
{
    [Serializable]
    public struct StatesTuple
    {
        public uint[] m_data
        {
            get;
        }

        public StatesTuple(int[] states, int[] bits, int size)
        {
            m_data = new uint[size];
            Set(states, bits);
        }

        public StatesTuple(int k)
        {
            m_data = new uint[k];
        }

        public void Set(int[] states, int[] bits)
        {
            int j = -1;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i] == 0)
                {
                    ++j;
                    m_data[j] = 0;
                }
                m_data[j] += ((uint)states[i] << bits[i]);
            }
        }

        public void Get(int[] states, int[] bits, int[] maxSize)
        {
            int j = -1;
            for (int i = 0; i < states.Length; i++)
            {
                if (bits[i] == 0)
                    j++;
                states[i] = (int)(m_data[j] >> bits[i]) & maxSize[i];
            }
        }

        public StatesTuple Clone()
        {
            var c = new StatesTuple(m_data.Length);
            for (var i = 0; i < m_data.Length; ++i)
            {
                c.m_data[i] = m_data[i];
            }
            return c;
        }
    }
}