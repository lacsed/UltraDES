# Automata operations

Assume `g1` and `g2` are previously constructed DFAs.

## State-space transformations

```csharp
var accessible = g1.AccessiblePart;
var coaccessible = g1.CoaccessiblePart;
var trim = g1.Trim;
var minimal = g1.Minimal;
var prefixClosed = g1.PrefixClosure;
```

| Member | What it computes or checks |
|---|---|
| `AccessiblePart` | Removes states that cannot be reached from the initial state |
| `CoaccessiblePart` | Keeps states from which a marked state can be reached |
| `Trim` | Keeps states that are both accessible and coaccessible |
| `Minimal` | Merges equivalent states while preserving the accepted language |
| `PrefixClosure` | Produces the prefix-closed behavior by marking reachable behavior appropriately |

## Parallel composition

```csharp
var system = g1.ParallelCompositionWith(g2);
var all = DeterministicFiniteAutomaton.ParallelComposition(new[] { g1, g2 });
```

Parallel composition synchronizes shared events. A private event changes only the component that contains it. The optional `removeNoAccessibleStates` argument controls whether inaccessible composed states are discarded.

## Product

```csharp
var product = g1.ProductWith(g2);
var allProducts = DeterministicFiniteAutomaton.Product(new[] { g1, g2 });
```

`Product`/`ProductWith` construct a synchronized product used by language checks and related algorithms. Unlike parallel composition, product behavior is restricted by the participating alphabets; use parallel composition to model components with private events.

## Projection and inverse projection

```csharp
var observed = g1.Projection(unobservableEvent);
var extended = observed.InverseProjection(g1.Events);
```

`Projection` hides the supplied events from the observed language. `InverseProjection` extends a model to a larger alphabet, allowing events absent from the original alphabet without changing its projected behavior.

## Structural comparison and language conversion

```csharp
bool sameShape = DeterministicFiniteAutomaton.Isomorphism(g1, g2);
var expression = g1.ToRegularExpression;
```

`Isomorphism` verifies whether two automata have the same transition structure up to state renaming. `ToRegularExpression` converts the DFA language into a regular-expression representation. Minimizing both DFAs before an isomorphism check is a practical way to compare deterministic language representations when the expected assumptions hold.

## Complexity note

Composition and product may approach the Cartesian product of component state spaces. Apply `Trim` or `Minimal` first when that preserves the intended analysis, and consider disk storage for large models.
