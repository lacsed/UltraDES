# Input, output, and visualization

## Supported formats

| Format | Read | Write | Purpose |
|---|---|---|---|
| ADS | `FromAdsFile`, `FromAdsString` | `ToAdsFile` | Import/export ADS automata |
| FSM | `FromFsmFile`, `FromFsmString` | `ToFsmFile`, `ToFsm` | Exchange textual finite-state machines |
| WMod | `FromWmodFile`, `FromWmodString` | `ToWmodFile`, `ToWmodString` | Exchange plants and specifications with Supremica-style modules |
| XML | `FromXMLFile`, `FromXMLString`, `DeserializeAutomaton` | `ToXMLFile`, `ToXML`, `SerializeAutomaton` | Persist one automaton in XML |
| JSON | `FromJsonFile`, `FromJsonString` | `ToJsonFile`, `ToJsonString` | Persist a collection of automata |
| FM | — | `ToFmFile` | Export the FM representation (`ToFM` is a legacy alias) |

## Reading and writing models

```csharp
using DFA = UltraDES.DeterministicFiniteAutomaton;

DFA.FromWmodFile("model.wmod", out var plants, out var specs);
var supervisor = DFA.MonolithicSupervisor(plants, specs);
supervisor.ToFsmFile("supervisor.fsm");
```

WMod readers separate plant and specification automata. The `From...File` methods read a path, whereas `From...String` methods parse document content.

```csharp
DFA.ToJsonFile("models.json", plants.Concat(specs));
var models = DFA.FromJsonFile("models.json");

string xml = supervisor.ToXML;
var restored = DFA.FromXMLString(xml);
```

JSON methods support multiple automata. XML members are convenient for a single DFA; `SerializeAutomaton` and `DeserializeAutomaton` are file-based alternatives.

## DOT and figures

```csharp
string dot = supervisor.ToDotCode;
supervisor.ShowAutomaton("Supervisor");
supervisor.drawSVGFigure("supervisor.svg", openAfterFinish: false);
supervisor.drawLatexFigure("supervisor.tex", openAfterFinish: false);
```

`ToDotCode` returns a Graphviz description. `ShowAutomaton` opens the available viewer. The figure methods render SVG or LaTeX output; set `openAfterFinish` to `false` on CI servers or other headless environments.

## Highlighting states, events, and transitions

```csharp
var states = new[] { (supervisor.InitialState, SVGColors.LightBlue) };
var dot = supervisor.ToFormattedDotCode(states);
supervisor.ShowFormattedAutomaton(states, name: "Highlighted supervisor");
```

Formatting overloads accept `(Transition, SVGColors, GraphVizStyle)` to style individual transitions, or `(AbstractEvent, SVGColors)` to style every transition triggered by an event. `SVGColors` supplies named colors and `GraphVizStyle` selects line styles.
