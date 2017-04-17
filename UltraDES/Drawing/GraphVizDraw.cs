using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UltraDES
{
    internal static class GraphVizDraw
    {
        public static void showAutomaton(DeterministicFiniteAutomaton G, string name = "Automaton")
        {
            string path = String.Format("{0}.html", name);//String.Format("automaton_{0}.html", DateTime.Now.Ticks);

            var source = new StringBuilder();
            source.AppendLine("<!DOCTYPE HTML>");
            source.AppendLine("<HTML>");
            source.AppendLine("\t<HEAD>");
            source.AppendFormat("\t\t<TITLE>{0}</TITLE>", G.Name);
            source.AppendLine("\t</HEAD>");
            source.AppendLine("\t<BODY>");
            source.AppendLine("\t\t<script type=\"text/vnd.graphviz\" id=\"cluster\">");
            source.AppendLine(G.ToDotCode);
            source.AppendLine("\t\t</script>");
            source.AppendLine(@"<script src=""Drawing/viz.js""></script>
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

            using (var file = new StreamWriter(path))
            {
                file.WriteLine(source.ToString());
            }

            Process.Start(path);
            Thread.Sleep(1000);
        }
    }
}
