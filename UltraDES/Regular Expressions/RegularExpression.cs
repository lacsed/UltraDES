// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020


using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
    
    /// <summary>
    /// (Serializable)a regular expression.
    /// </summary>
    /// <remarks>Lucas Alves, 15/01/2016.</remarks>
    

    [Serializable]
    public abstract class RegularExpression
    {
        protected static int stateNumber = 0;
        private static object obj = new object();

        public abstract RegularExpression Projection(HashSet<AbstractEvent> unobservableEvents);

        /// <summary>
        /// Gets the step simplify.
        /// </summary>
        /// <value>The step simplify.</value>


        public abstract RegularExpression StepSimplify { get; }

        
        /// <summary>
        /// Gets the simplify.
        /// </summary>
        /// <value>The simplify.</value>
        

        public RegularExpression Simplify
        {
            get
            {
                var exp = this;
                var sim = exp.StepSimplify;

                while (sim != exp)
                {
                    exp = sim;
                    sim = exp.StepSimplify;
                }

                return sim;
            }
        }

        
        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public abstract override int GetHashCode();

        
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public abstract override string ToString();

        
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public abstract override bool Equals(object obj);

        
        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">The RegularExpression to process.</param>
        /// <param name="b">The RegularExpression to process.</param>
        /// <returns>The result of the operation.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public static bool operator ==(RegularExpression a, RegularExpression b)
        {
            if (ReferenceEquals(a, b)) return true;
            return !ReferenceEquals(a, null) && a.Equals(b);
        }

        
        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">The RegularExpression to process.</param>
        /// <param name="b">The RegularExpression to process.</param>
        /// <returns>The result of the operation.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public static bool operator !=(RegularExpression a, RegularExpression b)
        {
            return !(a == b);
        }

        
        /// <summary>
        /// Addition operator.
        /// </summary>
        /// <param name="a">The RegularExpression to process.</param>
        /// <param name="b">The RegularExpression to process.</param>
        /// <returns>The result of the operation.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public static RegularExpression operator +(RegularExpression a, RegularExpression b)
        {
            return new Union(a, b);
        }

        
        /// <summary>
        /// Multiplication operator.
        /// </summary>
        /// <param name="a">The RegularExpression to process.</param>
        /// <param name="b">The RegularExpression to process.</param>
        /// <returns>The result of the operation.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public static RegularExpression operator *(RegularExpression a, RegularExpression b)
        {
            return new Concatenation(a, b);
        }

        public RegularExpression Kleene => new KleeneStar(this);

        protected internal abstract (AbstractState initial, AbstractState final, IEnumerable<Transition> trans) AutomatonTransitions { get; }

        public DeterministicFiniteAutomaton ToDFA
        {
            get
            {
                lock (obj)
                {
                    stateNumber = 0;
                    var (initial, final, trans) = AutomatonTransitions;

                    var finalm = final.ToMarked;
                    if (initial == final) initial = finalm;

                    trans = trans.Select(t => new Transition(t.Origin == final ? finalm : t.Origin, t.Trigger,
                        t.Destination == final ? finalm : t.Destination)).ToArray();

                    return new NondeterministicFiniteAutomaton(trans, initial, $"{ToString()}").Determinize;
                }
            }
        }


    }
}