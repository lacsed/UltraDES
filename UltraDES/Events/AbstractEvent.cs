////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	Events\AbstractEvent.cs
//
// summary:	Implements the abstract event class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a abstract event. </summary>
    ///
    /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public abstract class AbstractEvent : Symbol
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the controllability. </summary>
        ///
        /// <value> The controllability. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Controllability Controllability { get; protected set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets a value indicating whether this object is controllable. </summary>
        ///
        /// <value> true if this object is controllable, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool IsControllable
        {
            get { return Controllability == Controllability.Controllable; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns a string that represents the current object. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <returns>   A string that represents the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public abstract override string ToString();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Serves as the default hash function. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <returns>   A hash code for the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public abstract override int GetHashCode();

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

        public abstract override bool Equals(object obj);

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Equality operator. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <param name="a">    The AbstractEvent to process. </param>
        /// <param name="b">    The AbstractEvent to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool operator ==(AbstractEvent a, AbstractEvent b)
        {
            if (!ReferenceEquals(a, null) && !ReferenceEquals(a, null))
            {
                return a.Equals(b);
            }
            return ReferenceEquals(a, b);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Inequality operator. </summary>
        ///
        /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
        ///
        /// <param name="a">    The AbstractEvent to process. </param>
        /// <param name="b">    The AbstractEvent to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool operator !=(AbstractEvent a, AbstractEvent b)
        {
            return !(a == b);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   Values that represent controllabilities. </summary>
    ///
    /// <remarks>   Lucas Alves, 11/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public enum Controllability : byte
    {
        /// <summary>   An enum constant representing the controllable option. </summary>
        Controllable = 1,
        /// <summary>   An enum constant representing the uncontrollable option. </summary>
        Uncontrollable = 0
    }
}