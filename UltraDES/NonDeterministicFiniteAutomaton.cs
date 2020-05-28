// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-22-2020
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UltraDES
{
    using NFA = NondeterministicFiniteAutomaton;

    /// <summary>
    /// Class NondeterministicFiniteAutomaton.
    /// </summary>
    [Serializable]
    public class NondeterministicFiniteAutomaton
    {
        /// <summary>
        /// Gets the transitions.
        /// </summary>
        /// <value>The transitions.</value>
        public IEnumerable<Transition> Transitions { get; }
        /// <summary>
        /// Gets the initial state.
        /// </summary>
        /// <value>The initial state.</value>
        public AbstractState InitialState { get; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the states.
        /// </summary>
        /// <value>The states.</value>
        public IEnumerable<AbstractState> States =>
            Transitions.SelectMany(t => new[] {t.Origin, t.Destination}).Distinct();

        /// <summary>
        /// Gets the marked states.
        /// </summary>
        /// <value>The marked states.</value>
        public IEnumerable<AbstractState> MarkedStates => States.Where(s => s.IsMarked);

        /// <summary>
        /// Gets the events.
        /// </summary>
        /// <value>The events.</value>
        public IEnumerable<AbstractEvent> Events => Transitions.Select(t => t.Trigger).Distinct();


        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Converts to dotcode.
        /// </summary>
        /// <value>To dot code.</value>
        public string ToDotCode
        {
            get
            {
                var states = States.ToList();

                var dot = new StringBuilder("digraph {\nrankdir=TB;");

                dot.Append("\nnode [shape = doublecircle];");

                foreach (var ms in states.Where(q => q.IsMarked))
                    dot.Append($" \"{ms}\" ");

                dot.Append("\nnode [shape = circle];");

                foreach (var s in states.Where(q => !q.IsMarked))
                    dot.Append($" \"{s}\" ");

                dot.Append($"\nnode [shape = point ]; Initial\nInitial -> \"{InitialState}\";\n");

                foreach (
                    var group in Transitions.GroupBy(t => new
                    {
                        t.Origin, t.Destination
                    }))
                {
                    dot.Append($"\"{@group.Key.Origin}\" -> \"{@group.Key.Destination}\" [ label = \"{@group.Aggregate("", (acc, t) => $"{acc}{t.Trigger},") .Trim(' ', ',')}\" ];\n");
                }

                dot.Append("}");

                return dot.ToString();
            }
        }

        /// <summary>
        /// Shows the automaton.
        /// </summary>
        /// <param name="name">The name.</param>
        [Obsolete("This method will soon be deprecated. Use ShowAutomaton instead.")]
        public void showAutomaton(string name = "")
        {
           ShowAutomaton(name);
        }

        /// <summary>
        /// Shows the automaton.
        /// </summary>
        /// <param name="name">The name.</param>
        public void ShowAutomaton(string name = "")
        {
            if (name == "") name = Name;
            Draw.ShowDotCode(ToDotCode, name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NondeterministicFiniteAutomaton"/> class.
        /// </summary>
        /// <param name="transitions">The transitions.</param>
        /// <param name="initial">The initial.</param>
        /// <param name="name">The name.</param>
        public NondeterministicFiniteAutomaton(IEnumerable<Transition> transitions, AbstractState initial, string name)
        {
            Transitions = transitions;
            InitialState = initial;
            Name = name;
        }
    }
}