////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	States\CompoundState.cs
//
// summary:	Implements the compound state class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Text;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a compound state. </summary>
    /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class CompoundState : AbstractCompoundState
    {
        /// <summary>   The hashcode. </summary>
        private readonly int _hashcode;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="s1">       The s 1. </param>
        /// <param name="s2">       The second AbstractState. </param>
        /// <param name="count">    Number of. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public CompoundState(AbstractState[] s, bool allMarked = true)
        {
            S = s;
            _hashcode = 0;
            bool marked = allMarked;
            for (var i = 0; i < S.Length; ++i)
            {
                if(allMarked) marked &= S[i].IsMarked;
                else marked |= S[i].IsMarked;
                var hash = S[i].GetHashCode();
                _hashcode = (_hashcode ^ hash) + (hash << 1);
            }
            Marking = marked ? Marking.Marked : Marking.Unmarked;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="s1">       The s 1. </param>
        /// <param name="s2">       The second AbstractState. </param>
        /// <param name="marking">  The marking. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public CompoundState(AbstractState[] s, Marking marking)
        {
            S = s;
            _hashcode = 0;
            for (var i = 0; i < S.Length; ++i)
            {
                var hash = S[i].GetHashCode();
                _hashcode = (_hashcode ^ hash) + (hash << 1);
            }
            Marking = marking;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the s 1. </summary>
        /// <value> The s 1. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override AbstractState[] S { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to marked. </summary>
        /// <value> to marked. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override AbstractState ToMarked
        {
            get { return IsMarked ? this : new CompoundState(S, Marking.Marked); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to unmarked. </summary>
        /// <value> to unmarked. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override AbstractState ToUnmarked
        {
            get { return !IsMarked ? this : new CompoundState(S, Marking.Unmarked); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Determines whether the specified object is equal to the current object. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="obj">  The object to compare with the current object. </param>
        /// <returns>
        ///     true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            // If parameter cannot be cast to Point return false.
            var p = obj as CompoundState;
            if ((object) p == null) return false;

            if (_hashcode != p._hashcode || S.Length != p.S.Length) return false;

            for (var i = 0; i < S.Length; ++i)
            {
                if (S[i] != p.S[i]) return false;
            }

            return Marking == p.Marking;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Merge with. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="s2">           The second AbstractState. </param>
        /// <param name="allMarked">    true if all marked. </param>
        /// <returns>   A CompoundState. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override AbstractCompoundState MergeWith(AbstractState s2, bool allMarked = true)
        {
            var marked = allMarked ? IsMarked && s2.IsMarked : IsMarked || s2.IsMarked;
            return new CompoundState(new[] { this, s2 }, marked ? Marking.Marked : Marking.Unmarked);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Serves as the default hash function. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <returns>   A hash code for the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override int GetHashCode()
        {
            return _hashcode;
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns a string that represents the current object. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <returns>   A string that represents the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override string ToString()
        {
            var sb = new StringBuilder(S[0].ToString());
            for (var i = 1; i < S.Length; ++i)
            {
                sb.Append("|");
                sb.Append(S[i].ToString());
            }
            return sb.ToString();
        }
    }
}