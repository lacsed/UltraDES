# Regular expressions

UltraDES represents regular languages through the `RegularExpression` hierarchy.

## Expression elements

| Type | Meaning |
|---|---|
| `Symbol` | Base class for language symbols; events are symbols |
| `Concatenation` | Words from the first expression followed by words from the second |
| `Union` | Words accepted by either expression |
| `KleeneStar` | Zero or more repetitions of an expression |
| `Epsilon` | The empty word |
| `Empty` | The empty language |

## Building and converting expressions

```csharp
var a = new Event("a", Controllability.Controllable);
var b = new Event("b", Controllability.Controllable);

RegularExpression choice = new Union(a, b);
RegularExpression sequence = new Concatenation(a, b);
RegularExpression repetition = choice.Kleene;

var automaton = repetition.ToDFA;
var equivalentExpression = automaton.ToRegularExpression;
```

Events inherit from `Symbol`, so they can be used directly as expressions. `ToDFA` builds a deterministic automaton that accepts the expression language. `ToRegularExpression` computes an expression that denotes a DFA's language.

## Simplification and projection

```csharp
var simplified = repetition.Simplify;
var observed = repetition.Projection(new HashSet<AbstractEvent> { b });
```

`Simplify` repeatedly applies algebraic simplifications without changing the denoted language. `Projection` hides the supplied events at the language level and is used by language-based opacity checks.

> Do not use `DFA.KleeneClosure` in the current implementation: it throws `NotImplementedException`. Convert the DFA to an expression and use `.Kleene` instead.
