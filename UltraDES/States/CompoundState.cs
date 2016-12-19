////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	States\CompoundState.cs
//
// summary:	Implements the compound state class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

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
        public CompoundState(AbstractState s1, AbstractState s2, int count)
        {
            S1 = s1;
            S2 = s2;
            Marking = s1.Marking == s2.Marking ? s1.Marking : Marking.Unmarked;
            _hashcode = s1.GetHashCode()*count + s2.GetHashCode();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="s1">       The s 1. </param>
        /// <param name="s2">       The second AbstractState. </param>
        /// <param name="marking">  The marking. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public CompoundState(AbstractState s1, AbstractState s2, Marking marking)
        {
            S1 = s1;
            S2 = s2;
            Marking = marking;
            _hashcode = s1.GetHashCode() ^ s2.GetHashCode();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the s 1. </summary>
        /// <value> The s 1. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override AbstractState S1 { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the s 2. </summary>
        /// <value> The s 2. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override AbstractState S2 { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to marked. </summary>
        /// <value> to marked. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override AbstractState ToMarked
        {
            get { return IsMarked ? this : new CompoundState(S1, S2, Marking.Marked); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to unmarked. </summary>
        /// <value> to unmarked. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override AbstractState ToUnmarked
        {
            get { return !IsMarked ? this : new CompoundState(S1, S2, Marking.Unmarked); }
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

            if (_hashcode != p._hashcode) return false;

            // Return true if the fields match:
            return S1 == p.S1 && S2 == p.S2 && Marking == p.Marking;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Merge with. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="s2">           The second AbstractState. </param>
        /// <param name="count">        Number of. </param>
        /// <param name="allMarked">    true if all marked. </param>
        /// <returns>   A CompoundState. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override AbstractCompoundState MergeWith(AbstractState s2, int count = 0, bool allMarked = true)
        {
            return (IsMarked || s2.IsMarked) && !allMarked
                ? (AbstractCompoundState) new CompoundState(this, s2, count).ToMarked
                : new CompoundState(this, s2, count);
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
            return string.Format("{0}|{1}", S1, S2);
        }
    }
}