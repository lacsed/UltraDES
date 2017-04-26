////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	Events\Empty.cs
//
// summary:	Implements the empty class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a empty. </summary>
    ///
    /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public sealed class Empty : AbstractEvent
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Constructor that prevents a default instance of this class from being created.
        /// </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private Empty()
        {
            Controllability = Controllability.Controllable;
        }

        private static readonly Empty instance = new Empty();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the empty event. </summary>
        ///
        /// <value> The empty event. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static Empty EmptyEvent
        {
            get
            {
                return instance;
            }
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
            var p = obj as Empty;
            if ((object) p == null) return false;

            // Return true if the fields match:
            return true;
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
            return "empty".GetHashCode();
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
            return "\u2205";
        }
    }
}