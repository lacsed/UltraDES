// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 05-20-2020
// ***********************************************************************

using System;
using System.Linq;

namespace UltraDES
{

    /// <summary>
    /// Class State.
    /// Implements the <see cref="UltraDES.AbstractState" />
    /// </summary>
    /// <seealso cref="UltraDES.AbstractState" />
    [Serializable]
    public class State : AbstractState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="State"/> class.
        /// </summary>
        /// <param name="alias">The alias.</param>
        /// <param name="marking">The marking.</param>
        public State(string alias, Marking marking = Marking.Unmarked)
        {
            Alias = alias;
            Marking = marking;
        }


        /// <summary>
        /// Gets the alias.
        /// </summary>
        /// <value>The alias.</value>
        public string Alias { get; }

        /// <summary>
        /// Converts to marked.
        /// </summary>
        /// <value>To marked.</value>
        public override AbstractState ToMarked => IsMarked ? this : new State(Alias, Marking.Marked);


        /// <summary>
        /// Converts to unmarked.
        /// </summary>
        /// <value>To unmarked.</value>
        public override AbstractState ToUnmarked => !IsMarked ? this : new State(Alias, Marking.Unmarked);

        /// <summary>
        /// Gets the s.
        /// </summary>
        /// <value>The s.</value>
        public override AbstractState[] S => new AbstractState[] {this};
        /// <summary>
        /// Gets the flatten.
        /// </summary>
        /// <value>The flatten.</value>
        public override AbstractState Flatten => this;



        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            // If parameter cannot be cast to Point return false.
            var p = obj as State;
            if ((object) p == null) return false;

            // Return true if the fields match:
            return Alias == p.Alias && Marking == p.Marking;
        }



        /// <summary>
        /// Merges the with.
        /// </summary>
        /// <param name="s2">The s2.</param>
        /// <param name="allMarked">if set to <c>true</c> [all marked].</param>
        /// <returns>AbstractCompoundState.</returns>
        //public override AbstractCompoundState MergeWith(AbstractState s2, bool allMarked) => new CompoundState(new[] { this, s2 }, allMarked);
        public override AbstractCompoundState MergeWith(AbstractState s2, bool allMarked) => new CompoundState(S.Concat(s2.S).ToArray(), allMarked);


        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => Alias.GetHashCode();


        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => Alias;
    }
}