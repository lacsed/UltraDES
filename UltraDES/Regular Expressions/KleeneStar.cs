////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	Regular Expressions\KleeneStar.cs
//
// summary:	Implements the kleene star class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   (Serializable)a kleene star. </summary>
    ///
    /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class KleeneStar : RegularExpression
    {
        /// <summary>   a. </summary>
        private readonly RegularExpression _a;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Lucas Alves, 15/01/2016. </remarks>
        ///
        /// <param name="a">    The RegularExpression to process. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public KleeneStar(RegularExpression a)
        {
            _a = a;
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
                if (_a == Symbol.Empty) return Symbol.Epsilon;
                if (_a == Symbol.Epsilon) return Symbol.Epsilon;
                return new KleeneStar(_a.StepSimplify);
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
            return _a.GetHashCode();
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
            return string.Format("({0})*", _a);
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
            if (!(obj is KleeneStar)) return false;
            return _a == ((KleeneStar) obj)._a;
        }
    }
}