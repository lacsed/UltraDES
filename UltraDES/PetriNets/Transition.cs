// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-21-2020

using System;

namespace UltraDES.PetriNets
{
    /// <summary>
    /// Class Transition.
    /// Implements the <see cref="UltraDES.PetriNets.Node" />
    /// Implements the <see cref="System.IEquatable{UltraDES.PetriNets.Transition}" />
    /// </summary>
    /// <seealso cref="UltraDES.PetriNets.Node" />
    /// <seealso cref="System.IEquatable{UltraDES.PetriNets.Transition}" />
    [Serializable]
    public class Transition: Node, IEquatable<Transition>
    {


        /// <summary>
        /// Initializes a new instance of the <see cref="Transition"/> class.
        /// </summary>
        /// <param name="alias">The alias.</param>
        public Transition(string alias):base(alias)
        { }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Transition) obj);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
        public bool Equals(Transition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ALIAS == other.ALIAS;
        }

        /// <summary>
        /// Implements the == operator.
        /// </summary>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(Transition t1, Transition t2) => ReferenceEquals(t1, t2) || (!ReferenceEquals(t1, null) && t1.Equals(t2));
        /// <summary>
        /// Implements the != operator.
        /// </summary>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(Transition t1, Transition t2) => !(t1 == t2);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return (ALIAS != null ? ALIAS.GetHashCode() : 0);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => ALIAS;

    }


}
