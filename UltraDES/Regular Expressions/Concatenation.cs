////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	Regular Expressions\Concatenation.cs
//
// summary:	Implements the concatenation class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a concatenation. </summary>
    ///
    /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Concatenation : RegularExpression
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

        public Concatenation(RegularExpression a, RegularExpression b)
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
                if (_a is Concatenation)
                {
                    var c = (Concatenation) _a;
                    return new Concatenation(c._a, new Concatenation(c._b, _b)).StepSimplify;
                }
                if (_a == Symbol.Epsilon) return _b.StepSimplify;
                if (_b == Symbol.Epsilon) return _a.StepSimplify;
                if (_a == Symbol.Empty) return Symbol.Empty;
                if (_b == Symbol.Empty) return Symbol.Empty;
                return new Concatenation(_a.StepSimplify, _b.StepSimplify);
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
            return string.Format("{0}.{1}", _a, _b);
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
            if (!(obj is Concatenation)) return false;
            var concat1 = this;
            var concat2 = (Concatenation) obj;

            return concat1._a == concat2._a && concat1._b == concat2._b;
        }
    }
}