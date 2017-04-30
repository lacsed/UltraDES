////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	Events\Epsilon.cs
//
// summary:	Implements the epsilon class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a epsilon. </summary>
    ///
    /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Epsilon : AbstractEvent
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Constructor that prevents a default instance of this class from being created.
        /// </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private Epsilon()
        {
            Controllability = Controllability.Controllable;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the epsilon event. </summary>
        ///
        /// <value> The epsilon event. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static Epsilon EpsilonEvent { get; } = new Epsilon();

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
            var p = obj as Epsilon;
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
            return "epsilon".GetHashCode();
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
            return "\u03B5";
        }
    }
}