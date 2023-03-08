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
#line 1 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using System.Net.Http;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using System.Net.Http.Json;

#line default
#line hidden
#nullable disable
#nullable restore
#line 3 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using Microsoft.AspNetCore.Components.Forms;

#line default
#line hidden
#nullable disable
#nullable restore
#line 4 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using Microsoft.AspNetCore.Components.Routing;

#line default
#line hidden
#nullable disable
#nullable restore
#line 5 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using Microsoft.AspNetCore.Components.Web;

#line default
#line hidden
#nullable disable
#nullable restore
#line 6 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using Microsoft.AspNetCore.Components.Web.Virtualization;

#line default
#line hidden
#nullable disable
#nullable restore
#line 7 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using Microsoft.AspNetCore.Components.WebAssembly.Http;

#line default
#line hidden
#nullable disable
#nullable restore
#line 8 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using Microsoft.JSInterop;

#line default
#line hidden
#nullable disable
#nullable restore
#line 9 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using UltraDESWeb;

#line default
#line hidden
#nullable disable
#nullable restore
#line 10 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\_Imports.razor"
using UltraDESWeb.Shared;

#line default
#line hidden
#nullable disable
#nullable restore
#line 1 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\Pages\ApplyBinaryOperation.razor"
using global::UltraDES;

#line default
#line hidden
#nullable disable
    public partial class ApplyBinaryOperation : global::Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
        }
        #pragma warning restore 1998
#nullable restore
#line 51 "C:\Users\Lucas\OneDrive\Documentos\GitHub\UltraDES\Pages\ApplyBinaryOperation.razor"
       

    [Parameter]
    public Action<DeterministicFiniteAutomaton> OnSuccess { get; set; }

    [Parameter]
    public Action OnCancel { get; set; }

    [Parameter]
    public Dictionary<string, DeterministicFiniteAutomaton> Automata { get; set; }

    private string G1, G2;
    private string op;
    private bool computing = false;

    public void Create()
    {
        var G12 = op switch
        {
            "pc" => Automata[G1].ParallelCompositionWith(Automata[G2]),
            "pd" => Automata[G1].ProductWith(Automata[G2]),
            _ => null
            };

        if (G12 != null) OnSuccess(G12);
    }


#line default
#line hidden
#nullable disable
        [global::Microsoft.AspNetCore.Components.InjectAttribute] private IJSRuntime JsRuntime { get; set; }
    }
}
#pragma warning restore 1591
