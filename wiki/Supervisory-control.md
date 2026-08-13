# Supervisory control

Supervisory-control workflows separate **plants** (physically possible behavior) from **specifications** (allowed behavior).

## Monolithic synthesis

```csharp
var supervisor = DeterministicFiniteAutomaton.MonolithicSupervisor(
    new[] { plant1, plant2 },
    new[] { specification },
    nonBlocking: true);
```

`MonolithicSupervisor` composes the input models and calculates a supervisor that respects uncontrollable behavior. With `nonBlocking: true`, it also removes behavior that cannot reach a marked state. `MonolithicSupervisorLegacy(plant, spec)` retains the older single-plant/single-specification algorithm for compatibility.

## Local modular synthesis

```csharp
var supervisors = DeterministicFiniteAutomaton
    .LocalModularSupervisor(plants, specifications)
    .ToArray();

bool conflicting = DeterministicFiniteAutomaton.IsConflicting(supervisors);
```

`LocalModularSupervisor` builds supervisors from the plants relevant to each specification, which can avoid a single large global state space. `IsConflicting` checks whether nonblocking local supervisors become blocking when used together. Overloads can include conflict-resolution supervisors and algorithm options.

## Controllability and disabled events

```csharp
bool controllable = supervisor.IsControllable(plants);
var status = supervisor.Controllability(plants);
var disabled = supervisor.DisabledEvents(plants);
var complete = supervisor.ControllabilityAndDisabledEvents(plants);
```

- `IsControllable` checks whether the candidate supervisor ever tries to prevent plant behavior caused by an uncontrollable event;
- `Controllability` returns the corresponding controllability classification;
- `DisabledEvents` maps supervisor states to plant events disabled at those states;
- `ControllabilityAndDisabledEvents` obtains the classification and map in one operation.

## Reduction and localization

```csharp
var reduced = DeterministicFiniteAutomaton.ReduceSupervisor(plant, supervisor);
var localControllers = DeterministicFiniteAutomaton.LocalizeSupervisor(
    globalPlant, supervisor, agents);
```

`ReduceSupervisor` merges compatible supervisor states while preserving relevant control decisions. `LocalizeSupervisor` derives one local controller per agent so distributed controllers reproduce the global decisions.

Combined synthesis helpers are also available:

- `MonolithicReducedSupervisor`;
- `MonolithicLocalizedSupervisor`;
- `LocalModularReducedSupervisor`;
- `LocalModularLocalizedSupervisor`.

Their optional `maxIt` argument limits reduction/localization iterations; it does not limit synthesis state-space generation.
