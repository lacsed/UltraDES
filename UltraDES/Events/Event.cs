// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020

using System;
using System.Collections.Generic;

namespace UltraDES
{
    
    /// <summary>
    /// (Serializable)a event.
    /// </summary>
    /// <remarks>Lucas Alves, 11/01/2016.</remarks>
    

    [Serializable]
    public class Event : AbstractEvent
    {
        /// <summary>
        /// The hashcode.
        /// </summary>
        private readonly int _hashcode;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="alias">The alias.</param>
        /// <param name="controllability">The controllability.</param>
        /// <remarks>Lucas Alves, 11/01/2016.</remarks>
        public Event(string alias, Controllability controllability)
        {
            Alias = alias;
            Controllability = controllability;
            _hashcode = Alias.GetHashCode();
        }

        /// <summary>
        /// Gets the alias.
        /// </summary>
        /// <value>The alias.</value>
        public string Alias { get; private set; }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            // If parameter cannot be cast to Point return false.
            var p = obj as Event;
            if ((object) p == null) return false;

            // Return true if the fields match:
            return Alias == p.Alias && Controllability == p.Controllability;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        
        public override int GetHashCode() => _hashcode;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        /// <remarks>Lucas Alves, 11/01/2016.</remarks>
        public override string ToString() => Alias;

        public static implicit operator Event(int d) => new Event(d.ToString(), d % 2 == 0 ? Controllability.Uncontrollable : Controllability.Controllable);
    }
}