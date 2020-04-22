using System;

namespace UltraDES
{
    [Serializable]
    public struct StatesTuple
    {
        public uint[] MData { get; private set; }

        public StatesTuple(int[] states, int[] bits, int size) : this()
        {
            MData = new uint[size];
            Set(states, bits);
        }

        public StatesTuple(int k) : this()
        {
            MData = new uint[k];
        }

        public void Set(int[] states, int[] bits)
        {
            int j = -1;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i] == 0)
                {
                    ++j;
                    MData[j] = 0;
                }
                MData[j] += ((uint)states[i] << bits[i]);
            }
        }

        public void Get(int[] states, int[] bits, int[] maxSize)
        {
            int j = -1;
            for (int i = 0; i < states.Length; i++)
            {
                if (bits[i] == 0)
                    j++;
                states[i] = (int)(MData[j] >> bits[i]) & maxSize[i];
            }
        }

        public StatesTuple Clone()
        {
            var c = new StatesTuple(MData.Length);
            for (var i = 0; i < MData.Length; ++i)
            {
                c.MData[i] = MData[i];
            }
            return c;
        }
    }
}