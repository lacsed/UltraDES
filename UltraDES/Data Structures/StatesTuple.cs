using System;
using System.Buffers;

namespace UltraDES
{
    /// <summary>
    /// Struct StatesTuple
    /// </summary>
    [Serializable]
    public struct StatesTuple
    {

        /// <summary>
        /// Gets the m data.
        /// </summary>
        /// <value>The m data.</value>
        public uint[] MData { get; private set; }

        public StatesTuple()
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="StatesTuple"/> struct.
        /// </summary>
        /// <param name="states">The states.</param>
        /// <param name="bits">The bits.</param>
        /// <param name="size">The size.</param>
        public StatesTuple(int[] states, int[] bits, int size) : this()
        {
            MData = new uint[size];
            Set(states, bits);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatesTuple"/> struct.
        /// </summary>
        /// <param name="size">The size.</param>
        public StatesTuple(int size) : this() => MData = new uint[size];

        /// <summary>
        /// Sets the specified states.
        /// </summary>
        /// <param name="states">The states.</param>
        /// <param name="bits">The bits.</param>
        public void Set(int[] states, int[] bits)
        {
            var j = -1;
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i] == 0)
                {
                    ++j;
                    MData[j] = 0;
                }
                unchecked
                {
                    MData[j] += ((uint)states[i] << bits[i]);
                }
            }
        }
        /// <summary>
        /// Gets the specified states.
        /// </summary>
        /// <param name="states">The states.</param>
        /// <param name="bits">The bits.</param>
        /// <param name="maxSize">The maximum size.</param>
        public void Get(int[] states, int[] bits, int[] maxSize)
        {
            var j = -1;
            for (var i = 0; i < states.Length; i++)
            {
                if (bits[i] == 0) j++;
                unchecked
                {
                    states[i] = (int)(MData[j] >> bits[i]) & maxSize[i];
                }
                
            }
        }

    }


}