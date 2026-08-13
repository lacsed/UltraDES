# Installation and first steps

## Requirements

- A .NET SDK compatible with `netstandard2.1`;
- a C# IDE or editor, such as Visual Studio, VS Code, Rider, or LINQPad;
- either the UltraDES NuGet package or a reference to `UltraDES/UltraDES.csproj`.

Install the package in an existing project:

```bash
dotnet add package UltraDES
```

To work from this repository instead:

```bash
dotnet build UltraDES.sln
```

## First automaton

```csharp
using UltraDES;

var idle = new State("Idle", Marking.Marked);
var active = new State("Active", Marking.Unmarked);
var start = new Event("start", Controllability.Controllable);
var stop = new Event("stop", Controllability.Uncontrollable);

var machine = new DeterministicFiniteAutomaton(new[]
{
    new Transition(idle, start, active),
    new Transition(active, stop, idle)
}, idle, "Machine");

Console.WriteLine($"{machine.Name}: {machine.Size} states");
```

The constructor receives the transitions, initial state, and model name. The state set and event alphabet are inferred from the transitions. It also accepts transition tuples of the form `(state, event, state)`.

## Repository examples

| Project | Demonstrates |
|---|---|
| `DFA Functions and Properties` | DFA properties and transformations |
| `Supervisor Synthesis` | Monolithic and local modular synthesis |
| `Reading Files` | WMod model loading |
| `Algorithms` | Observer-property algorithms |
| `Opacity and Diagnosability` | Observers, diagnosability, and opacity |
| `Petri Nets` | Petri-net construction and coverability |
| `linqpad-samples` | Interactive introductory and control examples |

## Large-model settings

```csharp
DeterministicFiniteAutomaton.Multicore = true;
DeterministicFiniteAutomaton.UseDiskStorage = true;
DeterministicFiniteAutomaton.DiskStorageTempPath = "/tmp/ultrades";
```

`Multicore` enables internal parallel processing. `UseDiskStorage` moves some adjacency data to disk, reducing memory pressure at the cost of I/O. `DiskStorageTempPath` selects the writable directory used for that storage.
