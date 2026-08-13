# Automata modeling

## States

```csharp
var q0 = new State("q0", Marking.Unmarked);
var q1 = new State("q1", Marking.Marked);
```

`Marking.Marked` identifies an accepting/marked state; `Marking.Unmarked` identifies a nonmarked state. `AbstractState` is the common base type. `CompoundState` represents a state assembled from component states during operations such as composition.

The main DFA state properties are:

- `InitialState`: returns the starting state;
- `States`: enumerates every state in the automaton;
- `MarkedStates`: enumerates only marked states;
- `Size`: returns the number of states.

## Events

```csharp
var start = new Event("start", Controllability.Controllable);
var failure = new Event("failure", Controllability.Uncontrollable);
```

A controllable event may be disabled by a supervisor. An uncontrollable event models an occurrence the supervisor cannot prevent. `Events` returns the DFA alphabet, while `UncontrollableEvents` filters that alphabet by controllability. `Epsilon` and `Empty` are special language symbols used by regular-expression operations.

## Transitions and deterministic automata

```csharp
var transitions = new[]
{
    new Transition(q0, start, q1),
    new Transition(q1, failure, q0)
};
var g = new DeterministicFiniteAutomaton(transitions, q0, "G");
```

A `Transition` exposes `Origin`, `Trigger`, and `Destination`. A DFA can have at most one destination for a given state/event pair.

## Querying a transition

```csharp
var result = g.TransitionFunction(q0, start);
if (result is Some<AbstractState> destination)
    Console.WriteLine(destination.Value);
```

`TransitionFunction` checks whether an event is defined at a state. It returns `Some<AbstractState>` with the destination when found, or `None<AbstractState>` when no transition exists.

## Nondeterministic automata

`NondeterministicFiniteAutomaton` exposes `Transitions`, `InitialState`, `States`, `MarkedStates`, and `Events`. Unlike a DFA, it may represent alternative destinations for the same state/event pair. In UltraDES it is especially useful as the result of observer-property searches.

## Copying and simplifying names

```csharp
var copy = g.Clone();
var simplified = g.SimplifyStatesName(out var stateMap);
```

- `Clone()` copies the DFA;
- `Clone(capacity)` copies it while reserving internal capacity;
- `SimplifyStatesName()` replaces compound/long state names with simpler names;
- `SimplifyStatesName(out map)` also returns the old-to-new state mapping;
- `simplifyName(newName, simplifyStatesName)` is a legacy naming helper that can rename the automaton and simplify state labels.
