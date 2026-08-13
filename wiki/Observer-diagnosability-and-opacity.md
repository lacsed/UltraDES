# Observer, diagnosability, and opacity

## Observer property

```csharp
AbstractEvent[] relevantEvents = { eventA };
bool holds = system.ObserverPropertyVerify(relevantEvents, out var verifier);
var witness = system.ObserverPropertySearch(relevantEvents);
```

`ObserverPropertyVerify` checks whether projected observations preserve the marked-language information required by the observer property. It returns an auxiliary nondeterministic automaton through `out`; `returnOnDead` can stop the search at the first violation. `ObserverPropertySearch` builds the search automaton used to inspect or witness violations.

The observer module also exposes:

- `TarjanSCC`, which finds strongly connected components in a transition graph;
- `StronglyConnectedComponentsAutomaton`, which creates an automaton representation related to those components while accounting for nonrelevant events.

## Observer construction and diagnosability

```csharp
using UltraDES.Diagnosability;

var unobservable = new HashSet<AbstractEvent> { failure };
var observer = DiagnosticsAlgoritms.CreateObserver(system, unobservable);
bool diagnosable = DiagnosticsAlgoritms.IsDiagnosable(observer);
```

`CreateObserver` groups states that cannot be distinguished after hiding the supplied unobservable events. `IsDiagnosable` checks the observer for indefinitely ambiguous normal/faulty behavior. The system must encode normal and post-failure behavior—commonly by composing it with a labeler—so the observer contains the information needed for that check.

## Language-based opacity

```csharp
using UltraDES.Opacity;

bool opaque = OpacityAlgorithms.LanguageBasedOpacity(
    secretLanguage, nonSecretLanguage, unobservable);
```

`LanguageBasedOpacity` projects the secret and nonsecret languages and checks whether an observer can distinguish them.

## State-based opacity notions

UltraDES also provides:

- `InitialStateOpacity`: checks whether an observation can reveal that execution began in a secret state;
- `InitialFinalStateOpacity`: checks whether an observation can reveal a secret initial/final-state relation;
- `CurrentStepOpacity`: checks whether the current state can be known to be secret;
- `KStepsOpacity`: checks whether a state within the last `K` observed steps can be known to have been secret.

```csharp
bool currentStateOpaque = OpacityAlgorithms.CurrentStepOpacity(
    system, unobservableEvents, secretStates, out var estimator);
```

The exact state-set and event-set parameters differ by opacity notion; use the overload appropriate to the installed version. A `true` result means the secret cannot be inferred under that definition and observation model.
