using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace UltraDES
{
    using NFA = NondeterministicFiniteAutomaton;

    [Serializable]
    public class NondeterministicFiniteAutomaton
    {
        public IEnumerable<Transition> Transitions { get; }
        public AbstractState InitialState { get; }
        public string Name { get; }

        public IEnumerable<AbstractState> States =>
            Transitions.SelectMany(t => new[] {t.Origin, t.Destination}).Distinct();

        public IEnumerable<AbstractState> MarkedStates => States.Where(s => s.IsMarked);

        public IEnumerable<AbstractEvent> Events => Transitions.Select(t => t.Trigger).Distinct();


        public override string ToString()
        {
            return Name;
        }

        public string ToDotCode
        {
            get
            {
                var states = States.ToList();

                var dot = new StringBuilder("digraph {\nrankdir=TB;");

                dot.Append("\nnode [shape = doublecircle];");

                foreach (var ms in states.Where(q => q.IsMarked))
                    dot.AppendFormat(" \"{0}\" ", ms);

                dot.Append("\nnode [shape = circle];");

                foreach (var s in states.Where(q => !q.IsMarked))
                    dot.AppendFormat(" \"{0}\" ", s);

                dot.AppendFormat("\nnode [shape = point ]; Initial\nInitial -> \"{0}\";\n", InitialState);

                foreach (
                    var group in Transitions.GroupBy(t => new
                    {
                        t.Origin, t.Destination
                    }))
                {
                    dot.AppendFormat("\"{0}\" -> \"{1}\" [ label = \"{2}\" ];\n", group.Key.Origin,
                        group.Key.Destination,
                        group.Aggregate("", (acc, t) => $"{acc}{t.Trigger},")
                            .Trim(' ', ','));
                }

                dot.Append("}");

                return dot.ToString();
            }
        }

        public void showAutomaton(string name = "")
        {
            if (name == "") name = Name;
           Draw.ShowDotCode(ToDotCode, name);
        }

        public NondeterministicFiniteAutomaton(IEnumerable<Transition> transitions, AbstractState initial, string name)
        {
            Transitions = transitions;
            InitialState = initial;
            Name = name;
        }
    }
}