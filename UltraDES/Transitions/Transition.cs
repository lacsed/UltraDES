// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 05-20-2020
// ***********************************************************************

using System;

namespace UltraDES
{

    /// <summary>
    /// Class Transition.
    /// </summary>
    [Serializable]
    public class Transition
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Transition"/> class.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <param name="trigger">The trigger.</param>
        /// <param name="destination">The destination.</param>
        public Transition(AbstractState origin, AbstractEvent trigger, AbstractState destination)
        {
            Origin = origin;
            Destination = destination;
            Trigger = trigger;
        }


        /// <summary>
        /// Gets the origin.
        /// </summary>
        /// <value>The origin.</value>
        public AbstractState Origin { get; }


        /// <summary>
        /// Gets the destination.
        /// </summary>
        /// <value>The destination.</value>
        public AbstractState Destination { get; }


        /// <summary>
        /// Gets the trigger.
        /// </summary>
        /// <value>The trigger.</value>
        public AbstractEvent Trigger { get; }


        /// <summary>
        /// Gets a value indicating whether this instance is controllable transition.
        /// </summary>
        /// <value><c>true</c> if this instance is controllable transition; otherwise, <c>false</c>.</value>
        public bool IsControllableTransition => Trigger.IsControllable;


        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            // If parameter cannot be cast to Point return false.
            var p = obj as Transition;
            if (p == null) return false;

            // Return true if the fields match:
            if (Trigger != p.Trigger) return false;

            return Origin == p.Origin && Destination == p.Destination;
        }


        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return Origin.GetHashCode()*2 + Destination.GetHashCode()*7 + Trigger.GetHashCode();
        }


        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return $"({Origin} --{Trigger}-> {Destination})";
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.ValueTuple{AbstractState, AbstractEvent, AbstractState}"/> to <see cref="Transition"/>.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Transition((AbstractState, AbstractEvent, AbstractState) t)
        {
            return new Transition(t.Item1, t.Item2, t.Item3);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Transition"/> to <see cref="System.ValueTuple{AbstractState, AbstractEvent, AbstractState}"/>.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator (AbstractState, AbstractEvent, AbstractState)(Transition t)
        {
            return (t.Origin, t.Trigger, t.Destination);
        }

        /// <summary>
        /// Deconstructs the specified origin.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <param name="trigger">The trigger.</param>
        /// <param name="destination">The destination.</param>
        public void Deconstruct(out AbstractState origin, out AbstractEvent trigger, out AbstractState destination)
        {
            origin = Origin;
            trigger = Trigger;
            destination = Destination;
        }
    }
}