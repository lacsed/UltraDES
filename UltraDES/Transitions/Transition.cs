////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	Transitions\Transition.cs
//
// summary:	Implements the transition class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a transition. </summary>
    /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class Transition
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="origin">       The origin. </param>
        /// <param name="trigger">      The trigger. </param>
        /// <param name="destination">  The destination. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public Transition(AbstractState origin, AbstractEvent trigger, AbstractState destination)
        {
            Origin = origin;
            Destination = destination;
            Trigger = trigger;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the origin. </summary>
        /// <value> The origin. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public AbstractState Origin { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the Destination for the. </summary>
        /// <value> The destination. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public AbstractState Destination { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the trigger. </summary>
        /// <value> The trigger. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public AbstractEvent Trigger { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     Gets a value indicating whether this object is controllable transition.
        /// </summary>
        /// <value> true if this object is controllable transition, false if not. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool IsControllableTransition
        {
            get { return Trigger.IsControllable; }
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
            if (obj == null) return false;

            // If parameter cannot be cast to Point return false.
            var p = obj as Transition;
            if (p == null) return false;

            // Return true if the fields match:
            if (Trigger != p.Trigger) return false;

            return Origin == p.Origin && Destination == p.Destination;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Serves as the default hash function. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <returns>   A hash code for the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override int GetHashCode()
        {
            return Origin.GetHashCode()*2 + Destination.GetHashCode()*7 + Trigger.GetHashCode();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns a string that represents the current object. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <returns>   A string that represents the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override string ToString()
        {
            return string.Format("({0} --{1}-> {2})", Origin, Trigger, Destination);
        }
    }
}