////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	states\state.cs
//
// summary:	Implements the state class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Text;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a state. </summary>
    ///
    /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class State : AbstractState
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <param name="alias">    The alias. </param>
        /// <param name="marking">  The marking. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public State(string alias, Marking marking = Marking.Unmarked)
        {
            Alias = alias;
            Marking = marking;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the alias. </summary>
        ///
        /// <value> The alias. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string Alias { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to marked. </summary>
        ///
        /// <value> to marked. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override AbstractState ToMarked
        {
            get { return IsMarked ? this : new State(Alias, Marking.Marked); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to unmarked. </summary>
        ///
        /// <value> to unmarked. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override AbstractState ToUnmarked
        {
            get { return !IsMarked ? this : new State(Alias, Marking.Unmarked); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Determines whether the specified object is equal to the current object. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <param name="obj">  The object to compare with the current object. </param>
        ///
        /// <returns>
        ///     true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            // If parameter cannot be cast to Point return false.
            var p = obj as State;
            if ((object) p == null) return false;

            // Return true if the fields match:
            return Alias == p.Alias && Marking == p.Marking;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Merge with. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <param name="s2">           The second AbstractState. </param>
        /// <param name="allMarked">    true if all marked. </param>
        ///
        /// <returns>   A CompoundState. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override AbstractCompoundState MergeWith(AbstractState s2, bool allMarked)
        {
            return new CompoundState(new[] { this, s2 }, allMarked);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Serves as the default hash function. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <returns>   A hash code for the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return Alias.GetHashCode();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns a string that represents the current object. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <returns>   A string that represents the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            return Alias;
        }
    }
}