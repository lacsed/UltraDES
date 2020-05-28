// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 05-20-2020
// ***********************************************************************

using System;

namespace UltraDES
{

    /// <summary>
    /// Class AbstractCompoundState.
    /// Implements the <see cref="UltraDES.AbstractState" />
    /// </summary>
    /// <seealso cref="UltraDES.AbstractState" />
    [Serializable]
    public abstract class AbstractCompoundState : AbstractState
    {
        /// <summary>
        /// Joins this instance.
        /// </summary>
        /// <returns>AbstractState.</returns>
        public AbstractState Join()
        {
            return new State(ToString(), Marking);
        }
    }
}