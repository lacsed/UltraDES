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
using System.Text;

namespace UltraDES
{

    /// <summary>
    /// Class CompoundState.
    /// Implements the <see cref="UltraDES.AbstractCompoundState" />
    /// </summary>
    /// <seealso cref="UltraDES.AbstractCompoundState" />
    [Serializable]
    public class CompoundState : AbstractCompoundState
    {

        /// <summary>
        /// The hashcode
        /// </summary>
        private readonly int _hashcode;


        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundState"/> class.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="allMarked">if set to <c>true</c> [all marked].</param>
        public CompoundState(AbstractState[] s, bool allMarked = true)
        {
            S = s;
            _hashcode = 0;
            var marked = allMarked;
            foreach (var q in S)
            {
                if(allMarked) marked &= q.IsMarked;
                else marked |= q.IsMarked;
                var hash = q.GetHashCode();
                _hashcode = 7 * (_hashcode ^ hash) + 3 * hash;
            }
            Marking = marked ? Marking.Marked : Marking.Unmarked;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundState"/> class.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="marking">The marking.</param>
        public CompoundState(AbstractState[] s, Marking marking)
        {
            S = s;
            _hashcode = 0;
            foreach (var t in S)
            {
                var hash = t.GetHashCode();
                _hashcode = 7 * (_hashcode ^ hash) + 3 * hash;
            }
            Marking = marking;
        }


        /// <summary>
        /// Gets the s.
        /// </summary>
        /// <value>The s.</value>
        public override AbstractState[] S { get; }



        /// <summary>
        /// Gets the flatten.
        /// </summary>
        /// <value>The flatten.</value>
        public override AbstractState Flatten => new State(this.ToString(), this.Marking);



        /// <summary>
        /// Converts to marked.
        /// </summary>
        /// <value>To marked.</value>
        public override AbstractState ToMarked => IsMarked ? this : new CompoundState(S, Marking.Marked);


        /// <summary>
        /// Converts to unmarked.
        /// </summary>
        /// <value>To unmarked.</value>
        public override AbstractState ToUnmarked => !IsMarked ? this : new CompoundState(S, Marking.Unmarked);


        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            // If parameter cannot be cast to Point return false.
            var p = obj as CompoundState;
            if ((object) p == null) return false;

            if (_hashcode != p._hashcode || S.Length != p.S.Length) return false;

            if (S.Where((t, i) => t != p.S[i]).Any())
            {
                return false;
            }

            return Marking == p.Marking;
        }


        /// <summary>
        /// Merges the with.
        /// </summary>
        /// <param name="s2">The s2.</param>
        /// <param name="allMarked">if set to <c>true</c> [all marked].</param>
        /// <returns>AbstractCompoundState.</returns>
        public override AbstractCompoundState MergeWith(AbstractState s2, bool allMarked = true)
        {
            var marked = allMarked ? IsMarked && s2.IsMarked : IsMarked || s2.IsMarked;
            return new CompoundState(S.Concat(s2.S).ToArray(), marked ? Marking.Marked : Marking.Unmarked);
        }



        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => _hashcode;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder(S[0].ToString());
            for (var i = 1; i < S.Length; ++i)
            {
                sb.Append("|");
                sb.Append(S[i]);
            }
            return sb.ToString();
        }
    }
}