////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	Events\Event.cs
//
// summary:	Implements the event class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a event. </summary>
    ///
    /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Event : AbstractEvent
    {
        /// <summary>   The hashcode. </summary>
        private readonly int _hashcode;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <param name="alias">            The alias. </param>
        /// <param name="controllability">  The controllability. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Event(string alias, Controllability controllability)
        {
            Alias = alias;
            Controllability = controllability;
            _hashcode = Alias.GetHashCode();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the alias. </summary>
        ///
        /// <value> The alias. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string Alias { get; }

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
            var p = obj as Event;
            if ((object) p == null) return false;

            // Return true if the fields match:
            return Alias == p.Alias && Controllability == p.Controllability;
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
            return _hashcode;
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