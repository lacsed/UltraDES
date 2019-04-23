﻿////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	States\AbstractState.cs
//
// summary:	Implements the abstract state class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a abstract state. </summary>
    /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public abstract class AbstractState
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the marking. </summary>
        /// <value> The marking. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public Marking Marking { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets a value indicating whether this object is marked. </summary>
        /// <value> true if this object is marked, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool IsMarked => Marking == Marking.Marked;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to marked. </summary>
        /// <value> to marked. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public abstract AbstractState ToMarked { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to unmarked. </summary>
        /// <value> to unmarked. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public abstract AbstractState ToUnmarked { get; }

        public abstract AbstractState[] S { get; }

        public abstract AbstractState Flatten { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns a string that represents the current object. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <returns>   A string that represents the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public abstract override string ToString();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Serves as the default hash function. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <returns>   A hash code for the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public abstract override int GetHashCode();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Determines whether the specified object is equal to the current object. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="obj">  The object to compare with the current object. </param>
        /// <returns>
        ///     true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public abstract override bool Equals(object obj);

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Merge with. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="s2">           The second AbstractState. </param>
        /// <param name="allMarked">    true if all marked. </param>
        /// <returns>   A CompoundState. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public abstract AbstractCompoundState MergeWith(AbstractState s2, bool allMarked = true);

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Equality operator. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="a">    The AbstractState to process. </param>
        /// <param name="b">    The AbstractState to process. </param>
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static bool operator ==(AbstractState a, AbstractState b)
        {
            if (ReferenceEquals(a, b)) return true;
            return !ReferenceEquals(a, null) && a.Equals(b);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Inequality operator. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="a">    The AbstractState to process. </param>
        /// <param name="b">    The AbstractState to process. </param>
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static bool operator !=(AbstractState a, AbstractState b)
        {
            return !(a == b);
        }

        
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Values that represent markings. </summary>
    /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public enum Marking : byte
    {
        /// <summary>   An enum constant representing the marked option. </summary>
        Marked = 1,

        /// <summary>   An enum constant representing the unmarked option. </summary>
        Unmarked = 0
    }
}