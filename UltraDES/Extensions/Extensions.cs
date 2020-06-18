// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020


using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
    
    /// <summary>
    /// Extension Methods.
    /// </summary>
    /// <remarks>Lucas Alves, 18/01/2016.</remarks>
    

    public static class Extensions
    {
        /// <summary>
        /// Enumerates option to value in this collection.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="list">The list to act on.</param>
        /// <returns>An enumerator that allows foreach to be used to process option to value in this
        /// collection.</returns>
        /// <remarks>Lucas Alves, 18/01/2016.</remarks>
        public static IEnumerable<T> OptionToValue<T>(this IEnumerable<Option<T>> list) => list.OfType<Some<T>>().Select(op => op.Value);


        /// <summary>
        /// Enumerates only some in this collection.
        /// </summary>
        /// <typeparam name="T">Generic type parameter.</typeparam>
        /// <param name="list">The list to act on.</param>
        /// <returns>An enumerator that allows foreach to be used to process only some in this collection.</returns>
        /// <remarks>Lucas Alves, 18/01/2016.</remarks>
        public static IEnumerable<Some<T>> OnlySome<T>(this IEnumerable<Option<T>> list) => list.OfType<Some<T>>();
    }
}