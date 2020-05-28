// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 05-20-2020
// ***********************************************************************

using System;

namespace UltraDES
{

    /// <summary>
    /// Class AbstractState.
    /// </summary>
    [Serializable]
    public abstract class AbstractState
    {

        /// <summary>
        /// Gets or sets the marking.
        /// </summary>
        /// <value>The marking.</value>
        public Marking Marking { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this instance is marked.
        /// </summary>
        /// <value><c>true</c> if this instance is marked; otherwise, <c>false</c>.</value>
        public bool IsMarked => Marking == Marking.Marked;

        /// <summary>
        /// Converts to marked.
        /// </summary>
        /// <value>To marked.</value>
        public abstract AbstractState ToMarked { get; }

        /// <summary>
        /// Converts to unmarked.
        /// </summary>
        /// <value>To unmarked.</value>
        public abstract AbstractState ToUnmarked { get; }

        /// <summary>
        /// Gets the s.
        /// </summary>
        /// <value>The s.</value>
        public abstract AbstractState[] S { get; }

        /// <summary>
        /// Gets the flatten.
        /// </summary>
        /// <value>The flatten.</value>
        public abstract AbstractState Flatten { get; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public abstract override string ToString();

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public abstract override int GetHashCode();

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public abstract override bool Equals(object obj);


        /// <summary>
        /// Merges the with.
        /// </summary>
        /// <param name="s2">The s2.</param>
        /// <param name="allMarked">if set to <c>true</c> [all marked].</param>
        /// <returns>AbstractCompoundState.</returns>
        public abstract AbstractCompoundState MergeWith(AbstractState s2, bool allMarked = true);

        /// <summary>
        /// Implements the == operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(AbstractState a, AbstractState b)
        {
            if (ReferenceEquals(a, b)) return true;
            return !ReferenceEquals(a, null) && a.Equals(b);
        }

        /// <summary>
        /// Implements the != operator.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(AbstractState a, AbstractState b)
        {
            return !(a == b);
        }

        
    }


    /// <summary>
    /// Enum Marking
    /// </summary>
    public enum Marking : byte
    {
        /// <summary>
        /// An enum constant representing the marked option.
        /// </summary>
        Marked = 1,

        /// <summary>
        /// An enum constant representing the unmarked option.
        /// </summary>
        Unmarked = 0
    }
}