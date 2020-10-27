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
    /// (Serializable)a kleene star.
    /// </summary>
    /// <remarks>Lucas Alves, 15/01/2016.</remarks>
    

    [Serializable]
    public class KleeneStar : RegularExpression
    {
        /// <summary>
        /// a.
        /// </summary>
        private readonly RegularExpression _a;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="a">The RegularExpression to process.</param>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public KleeneStar(RegularExpression a)
        {
            _a = a;
        }

        
        /// <summary>
        /// Gets the step simplify.
        /// </summary>
        /// <value>The step simplify.</value>
        

        public override RegularExpression StepSimplify
        {
            get
            {
                if (_a == Symbol.Empty) return Symbol.Epsilon;
                if (_a == Symbol.Epsilon) return Symbol.Epsilon;
                return new KleeneStar(_a.StepSimplify);
            }
        }

        
        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public override int GetHashCode()
        {
            return _a.GetHashCode();
        }

        
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public override string ToString()
        {
            return string.Format("({0})*", _a);
        }

        
        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public override bool Equals(object obj)
        {
            if (!(obj is KleeneStar)) return false;
            return _a == ((KleeneStar) obj)._a;
        }

        protected internal override (AbstractState initial, AbstractState final, IEnumerable<Transition> trans) AutomatonTransitions
        {
            get
            {
                var (initial, final, trans) = _a.AutomatonTransitions;
                return (initial, initial, trans.Append((final, Epsilon.EpsilonEvent, initial)));
            }
        }

    }
}