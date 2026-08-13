# Petri nets

Petri-net types are in `UltraDES.PetriNets`. Since `UltraDES` also defines a `Transition`, aliases avoid ambiguity.

## Construction

```csharp
using UltraDES.PetriNets;
using PNMarking = UltraDES.PetriNets.Marking;
using PNTransition = UltraDES.PetriNets.Transition;

var free = new Place("free");
var busy = new Place("busy");
var enter = new PNTransition("enter");
var leave = new PNTransition("leave");

var net = new PetriNet(new (Node, Node, uint)[]
{
    (free, enter, 1), (enter, busy, 1),
    (busy, leave, 1), (leave, free, 1)
}, "Resource");

var m0 = new PNMarking(new[] { (free, 1u), (busy, 0u) });
```

Each tuple is an `(origin, destination, weight)` arc. Constructors also accept `Arc` objects, unweighted node pairs (weight 1), or separate input/output arc lists.

## Enabling and firing

```csharp
var enabled = net.EnabledTransitions(m0).ToArray();
var m1 = net.Fire(m0, enter);
var final = net.Fire(m0, new[] { enter, leave });
```

`EnabledTransitions` checks which transitions have enough input tokens. `Fire` consumes tokens from input places and adds tokens to output places, returning a new marking. `Marking.Update` also returns a new marking. A `null` token value represents `ω` in coverability analysis; the marking indexer returns zero for an absent place.

## Structure and analysis

```csharp
uint weight = net.Weight(free, enter);
var inputs = net.Inputs(enter);
var outputs = net.Outputs(enter);
bool siphon = net.IsSiphon(new[] { free, busy });
bool trap = net.IsTrap(new[] { free, busy });
var matrix = net.IncidenceMatrix(out var placeIndex, out var transitionIndex);
```

- `Weight`, `Input`, and `Output` query arc weights;
- `Inputs` and `Outputs` enumerate adjacent nodes;
- `IsSiphon` checks whether every transition adding tokens to the place set also consumes from it;
- `IsTrap` checks whether every transition consuming from the set also adds to it;
- `IncidenceMatrix` calculates token change per place/transition and returns index maps;
- the `+` operator combines two nets.

## State-space analysis and drawing

```csharp
var coverability = net.CoverabilityGraph(m0);
var tree = net.ReachabilityTree(m0);
net.ShowPetriNet("Resource", m0);
coverability.ShowGraph("Coverability");
```

`CoverabilityTree` explores firing sequences and introduces `ω` when token growth is unbounded. `CoverabilityGraph` merges equivalent coverability markings. `ReachabilityTree` enumerates reachable markings without the `ω` abstraction and may not terminate for an unbounded net. `ToDotCode` and `ShowPetriNet` export or display the net.
