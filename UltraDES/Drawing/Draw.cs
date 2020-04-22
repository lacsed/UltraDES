using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace UltraDES
{
    internal static class Draw
    {
        public static void ShowDotCode(string dot, string name = "image")
        {
            name = Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c, '_'));
            string path = $"{name}.html";
            path = path.Replace("||", "-");

            var source = $@"
<!DOCTYPE HTML>
<HTML>
<HEAD>
    <TITLE>{name}</TITLE>
</HEAD>
<BODY>
    <script type=""text/vnd.graphviz"" id=""cluster"">
{dot}
    </script>
    <script src=""https://github.com/mdaines/viz.js/releases/download/v1.8.0/viz.js""></script>
    <script>
        function inspect(s) {{
            return ""<pre>"" + s.replace(/</g, ""&lt;"").replace(/>/g, ""&gt;"").replace(/\""/g, ""&quot;"") + ""</pre>""
        }}
        function src(id) {{
            return document.getElementById(id).innerHTML;
        }}
        function example(id, format, engine) {{
            var result;
            try {{
                result = Viz(src(id), format, engine);
                if (format === ""svg"")
                return result;
                else
                return inspect(result);
            }} 
            catch(e) {{
                return inspect(e.toString());
            }}
        }}
        document.body.innerHTML += example(""cluster"", ""svg"");
    </script>
</BODY>
</HTML>
";
            using (var file = new StreamWriter(path))
            {
                file.WriteLine(source);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var proc = new Process
                {
                    StartInfo =
                    {
                        FileName = "/bin/bash",
                        Arguments = "-c \" " + "xdg-open " + path + " \"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                };
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
