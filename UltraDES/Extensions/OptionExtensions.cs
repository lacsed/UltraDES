////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	extensions\optionextensions.cs
//
// summary:	Implements the optionextensions class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   An option extensions. </summary>
    ///
    /// <remarks>   Lucas Alves, 18/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public static class OptionExtensions
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Enumerates option to value in this collection. </summary>
        ///
        /// <remarks>   Lucas Alves, 18/01/2016. </remarks>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="list"> The list to act on. </param>
        ///
        /// <returns>
        ///     An enumerator that allows foreach to be used to process option to value in this
        ///     collection.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IEnumerable<T> OptionToValue<T>(this IEnumerable<Option<T>> list)
        {
            return list.OfType<Some<T>>().Select(op => op.Value);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Enumerates only some in this collection. </summary>
        ///
        /// <remarks>   Lucas Alves, 18/01/2016. </remarks>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="list"> The list to act on. </param>
        ///
        /// <returns>
        ///     An enumerator that allows foreach to be used to process only some in this collection.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IEnumerable<Some<T>> OnlySome<T>(this IEnumerable<Option<T>> list)
        {
            return list.OfType<Some<T>>();
        }
    }
}