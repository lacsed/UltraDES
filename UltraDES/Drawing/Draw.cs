// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-28-2020
// ***********************************************************************

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace UltraDES
{
    /// <summary>
    /// Class Draw.
    /// </summary>
    internal static class Draw
    {
        /// <summary>
        /// Shows the dot code.
        /// </summary>
        /// <param name="dot">The dot.</param>
        /// <param name="name">The name.</param>
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
    <script src=""https://github.com/mdaines/viz.js/releases/download/v1.8.0/viz.js""></script>
</HEAD>
<BODY>
    <script type=""text/vnd.graphviz"" id=""cluster"">
{dot}
    </script>
        <div style=""height: 50px; width: 100 %; "">
            <button type = ""button"" onclick = ""downloadSVG('{name}.svg')"" > Save as SVG</button>
            <button type = ""button"" onclick = ""downloadPNG('{name}.png')"" > Save as PNG</button>
        </div>
         
    <script>
        function example(id) {{
                var result;
            try {{
                let src = document.getElementById(id).innerHTML;
                result = Viz(src, {{ format: 'svg', engine: 'dot', scale: undefined, totalMemory: 1024*1024*1024, files: undefined, images: undefined }});               
                
                return result;
            }} 
            catch(e) {{
                console.log(e);
            }}
            }}

        function downloadSVG(fileName) {{
            var content = example('cluster');
            var a = document.createElement('a');
            var file = new Blob([content], {{type: 'text/plain'}});
            a.href = URL.createObjectURL(file);
            a.download = fileName;
            a.click();
                }}
        function downloadPNG(fileName) {{
            Viz.svgXmlToPngImageElement(example('cluster'), undefined, (err, img) => {{
                    const source = img.src;
                    const a = document.createElement('a');
                    document.body.appendChild(a);

                    a.href = source;
                    a.target = '_self';
                    a.download = fileName;
                    a.click();
                }}); 
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
