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
            name = Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c, '_'));

            string path = $"{name}.html";

            var source = new StringBuilder();
            source.AppendLine("<!DOCTYPE HTML>");
            source.AppendLine("<HTML>");
            source.AppendLine("\t<HEAD>");
            source.AppendFormat("\t\t<TITLE>{0}</TITLE>", Name);
            source.AppendLine("\t</HEAD>");
            source.AppendLine("\t<BODY>");
            source.AppendLine("\t\t<script type=\"text/vnd.graphviz\" id=\"cluster\">");
            source.AppendLine(ToDotCode);
            source.AppendLine("\t\t</script>"); //
            //source.AppendLine(@"<script src=""Drawing/viz.js""></script>");
            source.AppendLine(@"<script src=""https://github.com/mdaines/viz.js/releases/download/v1.8.0/viz.js""></script>");
            source.AppendLine(@"        
            <script>
               function inspect(s) {
            return ""<pre>"" + s.replace(/</g, ""&lt;"").replace(/>/g, ""&gt;"").replace(/\""/g, ""&quot;"") + ""</pre>""
               }
               function src(id) {
            return document.getElementById(id).innerHTML;
               }
               function example(id, format, engine) {
            var result;
            try {
               result = Viz(src(id), format, engine);
               if (format === ""svg"")
            return result;
               else
            return inspect(result);
            } catch(e) {
               return inspect(e.toString());
            }
               }
               document.body.innerHTML += example(""cluster"", ""svg"");
        </script>");
            source.AppendLine("\t</BODY>");
            source.AppendLine("</HTML>");

            using (var file = new StreamWriter(path)) file.WriteLine(source.ToString());

            //Process.Start(path);
            Process.Start(@"cmd.exe ", $@"/c {path}");
            Thread.Sleep(1000);
        }

        public NondeterministicFiniteAutomaton(IEnumerable<Transition> transitions, AbstractState initial, string name)
        {
            Transitions = transitions;
            InitialState = initial;
            Name = name;
        }
    }
}