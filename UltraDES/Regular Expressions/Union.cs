﻿// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020


using System;

namespace UltraDES
{
    
    /// <summary>
    /// (Serializable)a union.
    /// </summary>
    /// <remarks>Lucas Alves, 15/01/2016.</remarks>
    

    [Serializable]
    public class Union : RegularExpression
    {
        /// <summary>
        /// a.
        /// </summary>
        private readonly RegularExpression _a;
        /// <summary>
        /// The b.
        /// </summary>
        private readonly RegularExpression _b;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="a">The RegularExpression to process.</param>
        /// <param name="b">The RegularExpression to process.</param>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public Union(RegularExpression a, RegularExpression b)
        {
            _a = a;
            _b = b;
        }

        
        /// <summary>
        /// Gets the step simplify.
        /// </summary>
        /// <value>The step simplify.</value>
        

        public override RegularExpression StepSimplify
        {
            get
            {
                if (_a == _b) return _a;
                if (_a is Union)
                {
                    var u = (Union) _a;
                    return new Union(u._a, new Union(u._b, _b)).StepSimplify;
                }
                if (_a == Symbol.Empty) return _b;
                return _b == Symbol.Empty ? _a : new Union(_a.StepSimplify, _b.StepSimplify);
            }
        }

        
        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public override int GetHashCode() => _a.GetHashCode() ^ _b.GetHashCode();


        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public override string ToString() => $"({_a} + {_b})";


        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <remarks>Lucas Alves, 15/01/2016.</remarks>
        

        public override bool Equals(object obj)
        {
            if (!(obj is Union)) return false;
            var union1 = this;
            var union2 = (Union) obj;

            return (union1._a == union2._a && union1._b == union2._b) ||
                   (union1._a == union2._b && union1._b == union2._a);
        }
    }
}