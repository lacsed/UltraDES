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
            string path = $"{name}.html";
            path = path.Replace("||", "-");


            var source = new StringBuilder();
            source.AppendLine("<!DOCTYPE HTML>");
            source.AppendLine("<HTML>");
            source.AppendLine("\t<HEAD>");
            source.AppendFormat("\t\t<TITLE>{0}</TITLE>", G.Name);
            source.AppendLine("\t</HEAD>");
            source.AppendLine("\t<BODY>");
            source.AppendLine("\t\t<script type=\"text/vnd.graphviz\" id=\"cluster\">");
            source.AppendLine(G.ToDotCode);
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

            using (var file = new StreamWriter(path))
            {
                file.WriteLine(source.ToString());
            }

            OperatingSystem os_info = System.Environment.OSVersion;

            if (os_info.ToString().Contains("Unix"))
            {
                Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "/bin/bash";
                proc.StartInfo.Arguments = "-c \" " + "xdg-open " + path + " \"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
            }
            else
            {
                Process.Start(@"cmd.exe ", $@"/c {path}");
            }

            Thread.Sleep(1000);
        }
    }
}
