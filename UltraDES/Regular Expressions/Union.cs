////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	Regular Expressions\Union.cs
//
// summary:	Implements the union class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a union. </summary>
    ///
    /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Union : RegularExpression
    {
        /// <summary>   a. </summary>
        private readonly RegularExpression _a;
        /// <summary>   The b. </summary>
        private readonly RegularExpression _b;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <param name="a">    The RegularExpression to process. </param>
        /// <param name="b">    The RegularExpression to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Union(RegularExpression a, RegularExpression b)
        {
            _a = a;
            _b = b;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the step simplify. </summary>
        ///
        /// <value> The step simplify. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

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
                if (_b == Symbol.Empty) return _a;
                return new Union(_a.StepSimplify, _b.StepSimplify);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Serves as the default hash function. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <returns>   A hash code for the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return _a.GetHashCode() ^ _b.GetHashCode();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns a string that represents the current object. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <returns>   A string that represents the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            return string.Format("({0} + {1})", _a, _b);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Determines whether the specified object is equal to the current object. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <param name="obj">  The object to compare with the current object. </param>
        ///
        /// <returns>
        ///     true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

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