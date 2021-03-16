// <auto-generated/>
#pragma warning disable 1591
#pragma warning disable 0414
#pragma warning disable 0649
#pragma warning disable 0169

namespace UltraDESWeb.Pages
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
#nullable restore
#line 1 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using System.Net.Http;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using System.Net.Http.Json;

#line default
#line hidden
#nullable disable
#nullable restore
#line 3 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using Microsoft.AspNetCore.Components.Forms;

#line default
#line hidden
#nullable disable
#nullable restore
#line 4 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using Microsoft.AspNetCore.Components.Routing;

#line default
#line hidden
#nullable disable
#nullable restore
#line 5 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using Microsoft.AspNetCore.Components.Web;

#line default
#line hidden
#nullable disable
#nullable restore
#line 6 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using Microsoft.AspNetCore.Components.Web.Virtualization;

#line default
#line hidden
#nullable disable
#nullable restore
#line 7 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using Microsoft.AspNetCore.Components.WebAssembly.Http;

#line default
#line hidden
#nullable disable
#nullable restore
#line 8 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using Microsoft.JSInterop;

#line default
#line hidden
#nullable disable
#nullable restore
#line 9 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using UltraDESWeb;

#line default
#line hidden
#nullable disable
#nullable restore
#line 10 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\_Imports.razor"
using UltraDESWeb.Shared;

#line default
#line hidden
#nullable disable
#nullable restore
#line 1 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\Pages\ApplyUnaryOperation.razor"
using global::UltraDES;

#line default
#line hidden
#nullable disable
    public partial class ApplyUnaryOperation : Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
        }
        #pragma warning restore 1998
#nullable restore
#line 42 "C:\Users\Lucas\source\repos\UltraDESWeb\UltraDESWeb\Pages\ApplyUnaryOperation.razor"
       

    [Parameter]
    public Action<DeterministicFiniteAutomaton> OnSuccess { get; set; }

    [Parameter]
    public Action OnCancel { get; set; }

    [Parameter]
    public Dictionary<string, DeterministicFiniteAutomaton> Automata { get; set; }

    private string G1;
    private string op;
    private bool computing = false;

    public async void Create()
    {
        try
        {
            computing = true;
            await Task.Delay(1);

            var G2 = op switch
            {
                "min" => Automata[G1].Minimal.Rename($"Min({G1})"),
                "ac" => Automata[G1].AccessiblePart.Rename($"Ac({G1})"),
                "coac" => Automata[G1].CoaccessiblePart.Rename($"Coac({G1})"),
                "trim" => Automata[G1].Trim.Rename($"Trim({G1})"),
                "simp" => Automata[G1].SimplifyStatesName().Rename($"Simp({G1})"),
                _ => null

            };

            if (G2 != null) OnSuccess(G2);
        }
        catch (Exception ex)
        {
            computing = false;
            await Task.Delay(1);
            JsRuntime.Alert($"Error: {ex.Message}");
            OnCancel();
        }

    }


#line default
#line hidden
#nullable disable
        [global::Microsoft.AspNetCore.Components.InjectAttribute] private IJSRuntime JsRuntime { get; set; }
    }
}
#pragma warning restore 1591
