# Graph algorithms

`UltraDES.Graph` provides extension methods for graphs represented as tuple sequences. This makes it possible to analyze automata and Petri-net results without creating another graph type.

## Unlabeled graphs

```csharp
var edges = new[]
{
    (o: "A", d: "B"),
    (o: "B", d: "C"),
    (o: "C", d: "A")
};

var visited = edges.BreadthFirstSearch("A");
var reversed = edges.ReverseEdges();
var between = edges.VerticesBetween("A", "C");
var components = edges.StronglyConnectedComponents((x, y) => x == y);
```

- `BreadthFirstSearch` enumerates vertices reachable from a starting vertex in breadth-first order;
- `ReverseEdges` swaps every edge origin and destination;
- `VerticesBetween` finds vertices reachable from the first endpoint that can also reach the second;
- `StronglyConnectedComponents` groups vertices that can mutually reach each other.

Overloads accepting `Func<T,T,bool>` allow domain-specific vertex equality.

## Labeled graphs and Graphviz

```csharp
var labeled = new[]
{
    (o: "A", l: "start", d: "B"),
    (o: "B", l: "finish", d: "C")
};

var simple = labeled.ToUnlabeledEdges();
string dot = labeled.ToDotCode();
labeled.ShowGraph("Flow");
```

`ToUnlabeledEdges` discards edge labels. `ToDotCode` produces a Graphviz document. `ShowGraph` passes that document to the configured viewer; on a headless environment, save the DOT string instead.
