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
    /// (Serializable)a symbol.
    /// </summary>
    /// <remarks>Lucas Alves, 15/01/2016.</remarks>
    

    [Serializable]
    public abstract class Symbol : RegularExpression
    {
        
        /// <summary>
        /// Gets the step simplify.
        /// </summary>
        /// <value>The step simplify.</value>
        

        public override RegularExpression StepSimplify => this;


        /// <summary>
        /// Gets the epsilon.
        /// </summary>
        /// <value>The epsilon.</value>
        

        public static Symbol Epsilon => UltraDES.Epsilon.EpsilonEvent;


        /// <summary>
        /// Gets the empty.
        /// </summary>
        /// <value>The empty.</value>
        

        public static Symbol Empty => UltraDES.Empty.EmptyEvent;


        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public abstract override string ToString();

        
        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public abstract override int GetHashCode();

        
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public abstract override bool Equals(object obj);

        public static implicit operator Symbol(char d) => new Event(d.ToString(), Controllability.Controllable);

        protected internal override (AbstractState initial, AbstractState final, IEnumerable<Transition> trans) AutomatonTransitions
        {
            get
            {
                stateNumber++;
                var initial = new State($"I_{stateNumber}");
                var final = new State($"F_{stateNumber}");

                var e = this is AbstractEvent ev
                    ? ev
                    : new Event(ToString(), Controllability.Controllable);

                return (initial, final, new Transition[] {(initial, e, final)});
            }
        }

    }
}