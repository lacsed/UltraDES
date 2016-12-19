////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	Regular Expressions\RegularExpression.cs
//
// summary:	Implements the regular expression class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a regular expression. </summary>
    ///
    /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public abstract class RegularExpression
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the step simplify. </summary>
        ///
        /// <value> The step simplify. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public abstract RegularExpression StepSimplify { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the simplify. </summary>
        ///
        /// <value> The simplify. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

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

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Serves as the default hash function. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <returns>   A hash code for the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public abstract override int GetHashCode();

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns a string that represents the current object. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <returns>   A string that represents the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public abstract override string ToString();

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

        public abstract override bool Equals(object obj);

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Equality operator. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <param name="a">    The RegularExpression to process. </param>
        /// <param name="b">    The RegularExpression to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool operator ==(RegularExpression a, RegularExpression b)
        {
            if (ReferenceEquals(a, b)) return true;
            return !ReferenceEquals(a, null) && a.Equals(b);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Inequality operator. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <param name="a">    The RegularExpression to process. </param>
        /// <param name="b">    The RegularExpression to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static bool operator !=(RegularExpression a, RegularExpression b)
        {
            return !(a == b);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Addition operator. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <param name="a">    The RegularExpression to process. </param>
        /// <param name="b">    The RegularExpression to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static RegularExpression operator +(RegularExpression a, RegularExpression b)
        {
            return new Union(a, b);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Multiplication operator. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <param name="a">    The RegularExpression to process. </param>
        /// <param name="b">    The RegularExpression to process. </param>
        ///
        /// <returns>   The result of the operation. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static RegularExpression operator *(RegularExpression a, RegularExpression b)
        {
            return new Concatenation(a, b);
        }
    }
}