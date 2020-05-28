// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020

namespace UltraDES.PetriNets
{
    /// <summary>
    /// Class Node.
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// The alias
        /// </summary>
        protected readonly string ALIAS;

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="alias">The alias.</param>
        protected Node(string alias) => ALIAS = alias;
    }
}
