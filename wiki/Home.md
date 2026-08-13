# UltraDES Wiki

**UltraDES** is a .NET library for modeling, analysis, and supervisory control of Discrete Event Systems (DES). This wiki groups the public features by task and illustrates them with small C# examples.

## Suggested reading order

1. [Installation and first steps](Installation-and-first-steps.md)
2. [Automata modeling](Automata-modeling.md)
3. [Automata operations](Automata-operations.md)
4. [Supervisory control](Supervisory-control.md)
5. [Input, output, and visualization](Input-output-and-visualization.md)
6. [Regular expressions](Regular-expressions.md)
7. [Observer, diagnosability, and opacity](Observer-diagnosability-and-opacity.md)
8. [Petri nets](Petri-nets.md)
9. [Graph algorithms](Graph-algorithms.md)

## Conventions used in this wiki

- `DFA` is an alias for `DeterministicFiniteAutomaton`.
- A marked state normally represents successful task completion or an accepted word.
- An uncontrollable event cannot be disabled by a supervisor.
- `AccessiblePart`, `Trim`, and `Minimal` are computed properties, so they are used without parentheses.
- Operations normally return a new automaton. Keep the original reference if the unmodified model is still needed.

> `KleeneClosure` is present in the DFA API but currently throws `NotImplementedException`. To express closure at the language level, use `ToRegularExpression` followed by `RegularExpression.Kleene`.
