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
        /// 
        /// </summary>
        public DeterministicFiniteAutomaton Determinize
        {
            get
            {
                var transitions = Transitions.ToList();
                var initial = InitialState;
                while (transitions.Any(t => t.Trigger == Symbol.Epsilon))
                {
                    var transition = transitions.First(t => t.Trigger == Symbol.Epsilon);
                    var (o, _, d) = transition;

                    transitions.Remove(transition);

                    var merged = transition.Origin.MergeWith(transition.Destination, false);
                    if (initial == o || initial == d) initial = merged;

                    foreach (var t in transitions.Where(t => t.Origin == d).ToArray())
                    {
                        //transitions.Remove(t);
                        transitions.Add((merged, t.Trigger, t.Destination));
                    }
                    foreach (var t in transitions.Where(t => t.Origin == o).ToArray())
                    {
                        transitions.Remove(t);
                        transitions.Add((merged, t.Trigger, t.Destination));
                    }
                    foreach (var t in transitions.Where(t => t.Destination == o).ToArray())
                    {
                        transitions.Remove(t);
                        transitions.Add((t.Origin, t.Trigger, merged));
                    }


                }

                var trans = transitions.GroupBy(t => t.Origin).ToDictionary(g => g.Key, g => g.ToList());

                var todo = new HashSet<HashSet<AbstractState>>(HashSet<AbstractState>.CreateSetComparer());
                todo.Add(new HashSet<AbstractState>(new[] { initial }));

                var done = new HashSet<HashSet<AbstractState>>(HashSet<AbstractState>.CreateSetComparer());

                var nTrans = new List<(HashSet<AbstractState> o, AbstractEvent t, HashSet<AbstractState> d)>();

                while (todo.Any())
                {
                    var P = todo.First();

                    foreach (var e in Events.Except(new []{Epsilon.EpsilonEvent }))
                    {
                        var Pl = P.Where(q => trans.ContainsKey(q) && trans[q].Any(t => t.Trigger == e))
                            .SelectMany(q => trans[q].Where(t => t.Trigger == e).Select(t => t.Destination)).ToSet();

                        if (Pl.Count == 0) continue;

                        nTrans.Add((P, e, Pl));
                        if (!done.Contains(Pl)) todo.Add(Pl);
                    }

                    todo.Remove(P);
                    done.Add(P);
                }

                var map = done.ToDictionary(P => P, P => P.Count > 1 ? P.Aggregate((q1, q2) => q1.MergeWith(q2, false)) : P.Single(), HashSet<AbstractState>.CreateSetComparer());

                return new DeterministicFiniteAutomaton(nTrans.Select(t => new Transition(map[t.o], t.t, map[t.d])), initial, $"P({Name})");
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => Name;

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

                foreach (var group in Transitions.GroupBy(t => new {t.Origin, t.Destination}))
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
        public void showAutomaton(string name = "") => ShowAutomaton(name);

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