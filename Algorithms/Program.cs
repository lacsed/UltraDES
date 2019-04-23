using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UltraDES;

namespace Algorithms
{
    class Program
    {
        static void Main(string[] args)
        {
            var states = Enumerable.Range(0, 7).Select(n => new State($"{n}", n == 4 ? Marking.Marked : Marking.Unmarked)).ToArray();
            var events = (new[] { "a", "b", "x", "z", "y" }).ToDictionary(n => n, n => new Event(n, Controllability.Controllable));

            var G1 = new DeterministicFiniteAutomaton(new Transition[]
            {
                (states[0], events["a"], states[1]),
                (states[1], events["a"], states[1]),
                (states[1], events["x"], states[2]),
                (states[2], events["y"], states[3]),
                (states[3], events["y"], states[1]),
                (states[3], events["a"], states[4])
            }, states[0], "G");

            G1.showAutomaton("G");

            G1.ObserverPropertyVerify(new[] { events["a"] }, out var Vg1, false);
            ShowRelation(Vg1, "Vg");
        }

        public static void ShowRelation((IEnumerable<Transition> transitions, AbstractState initial) G, string name)
        {
            var states = G.transitions.SelectMany(t => new[] { t.Origin, t.Destination }).Distinct().ToArray();
            var events = G.transitions.Select(t => t.Trigger).Distinct();

            var dot = new StringBuilder("digraph {\nrankdir=TB;");

            dot.Append("\nnode [shape = doublecircle];");

            foreach (var ms in states.Where(q => q.IsMarked))
                dot.AppendFormat(" \"{0}\" ", ms);

            dot.Append("\nnode [shape = circle];");

            foreach (var s in states.Where(q => !q.IsMarked))
                dot.AppendFormat(" \"{0}\" ", s);

            dot.AppendFormat("\nnode [shape = point ]; Initial\nInitial -> \"{0}\";\n", G.initial);

            foreach (
                var group in G.transitions.GroupBy(t => new { t.Origin, t.Destination }))
            {
                dot.AppendFormat("\"{0}\" -> \"{1}\" [ label = \"{2}\" ];\n", group.Key.Origin,
                    group.Key.Destination,
                    group.Aggregate("", (acc, t) => $"{acc}{t.Trigger},")
                        .Trim(' ', ','));
            }

            dot.Append("}");

            string path = $"{name}.html";

            var source = new StringBuilder();
            source.AppendLine("<!DOCTYPE HTML>");
            source.AppendLine("<HTML>");
            source.AppendLine("\t<HEAD>");
            source.AppendFormat("\t\t<TITLE>{0}</TITLE>", name);
            source.AppendLine("\t</HEAD>");
            source.AppendLine("\t<BODY>");
            source.AppendLine("\t\t<script type=\"text/vnd.graphviz\" id=\"cluster\">");
            source.AppendLine(dot.ToString());
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
    }
}
