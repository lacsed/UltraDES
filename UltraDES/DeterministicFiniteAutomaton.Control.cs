using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UltraDES;

using DFA = DeterministicFiniteAutomaton;

/// <summary>
/// Represents a deterministic finite automaton (DFA) for supervisory control.
/// This partial class implements methods for computing controllability, supervisor synthesis,
/// state reduction, and various supervisory operations.
/// </summary>
partial class DeterministicFiniteAutomaton
{
    /// <summary>
    /// Computes the next state arrays for both the plant and the supervisor given specific event triggers.
    /// </summary>
    /// <param name="plant">The plant DFA.</param>
    /// <param name="numPlant">The number of components in the plant.</param>
    /// <param name="numSup">The number of components in the supervisor.</param>
    /// <param name="stateArrPlant">The current state array of the plant.</param>
    /// <param name="stateArrSup">The current state array of the supervisor.</param>
    /// <param name="evPlant">The index of the event in the plant's event list (-1 indicates no change).</param>
    /// <param name="evSup">The index of the event in the supervisor's event list (-1 indicates no change).</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description><c>hasNextPlant</c>: <c>true</c> if a valid next state exists for the plant; otherwise, <c>false</c>.</description></item>
    ///   <item><description><c>hasNextSup</c>: <c>true</c> if a valid next state exists for the supervisor; otherwise, <c>false</c>.</description></item>
    ///   <item><description><c>stateArrPlantNext</c>: The next state array for the plant.</description></item>
    ///   <item><description><c>stateArrSupNext</c>: The next state array for the supervisor.</description></item>
    /// </list>
    /// </returns>
    private (bool hasNextPlant, bool hasNextSup, int[] stateArrPlantNext, int[] stateArrSupNext) CheckState(DFA plant, int numPlant, int numSup, int[] stateArrPlant, int[] stateArrSup, int evPlant, int evSup)
    {
        var stateArrPlantNext = new int[numPlant];
        var stateArrSupNext = new int[numSup];
        bool hasNextPlant = true, hasNextSup = true;
        if (evPlant == -1)
        {
            for (var p = 0; p < numPlant; ++p)
                stateArrPlantNext[p] = stateArrPlant[p];
        }
        else
        {
            for (var p = 0; p < numPlant; ++p)
            {
                if (!plant._eventsList[p][evPlant])
                    stateArrPlantNext[p] = stateArrPlant[p];
                else
                {
                    if (!plant._adjacencyList[p].TryGet(stateArrPlant[p], evPlant, out int val))
                    {
                        hasNextPlant = false;
                        break;
                    }
                    stateArrPlantNext[p] = val;
                }
            }
        }

        if (evSup == -1)
        {
            for (var s = 0; s < numSup; ++s)
                stateArrSupNext[s] = stateArrSup[s];
        }
        else
        {
            for (var s = 0; s < numSup; ++s)
            {
                if (!_eventsList[s][evSup])
                    stateArrSupNext[s] = stateArrSup[s];
                else
                {
                    if (!_adjacencyList[s].TryGet(stateArrSup[s], evSup, out int val))
                    {
                        hasNextSup = false;
                        break;
                    }
                    stateArrSupNext[s] = val;
                }
            }
        }

        return (hasNextPlant, hasNextSup, stateArrPlantNext, stateArrSupNext);
    }

    /// <summary>
    /// Computes the controllability of the given plant DFAs by synthesizing a supervisor.
    /// </summary>
    /// <param name="plants">An array of plant DFAs.</param>
    /// <returns>The controllability result.</returns>
    public Controllability Controllability(params DFA[] plants) =>
        Controllability((IEnumerable<DFA>)plants);

    /// <summary>
    /// Computes the controllability of the given plant DFAs by synthesizing a supervisor.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <returns>The controllability result.</returns>
    public Controllability Controllability(IEnumerable<DFA> plants) =>
        ControllabilityAndDisabledEvents(plants, false).Item1;

    /// <summary>
    /// Computes both the controllability and the set of disabled events for the given plant DFAs.
    /// </summary>
    /// <param name="plants">An array of plant DFAs.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description>The controllability result.</description></item>
    ///   <item><description>A dictionary mapping abstract states to lists of disabled events.</description></item>
    /// </list>
    /// </returns>
    public (Controllability, Dictionary<AbstractState, List<AbstractEvent>>) ControllabilityAndDisabledEvents(params DFA[] plants) =>
        ControllabilityAndDisabledEvents(plants, true);

    /// <summary>
    /// Computes both the controllability and the set of disabled events for the given plant DFAs.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <param name="getDisabledEvents">
    /// If <c>true</c>, the method will also collect the disabled events encountered during supervisor synthesis.
    /// </param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description>The controllability result.</description></item>
    ///   <item><description>A dictionary mapping abstract states to lists of disabled events.</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="Exception">Thrown when the plant is invalid.</exception>
    public (Controllability, Dictionary<AbstractState, List<AbstractEvent>>) ControllabilityAndDisabledEvents(
        IEnumerable<DFA> plants, bool getDisabledEvents = true)
    {
        var plant = ParallelComposition(plants, false);
        var numPlant = plant._statesList.Count;
        var numSup = _statesList.Count;
        var stateArrSup = new int[numSup];
        var evs = _eventsUnion.Union(plant._eventsUnion).OrderBy(i => i.Controllability).ToArray();
        var numUncontEvs = 0;
        var plantStack = new Stack<int[]>();
        var supStack = new Stack<int[]>();
        var filteredStates = _validStates != null;
        var GfilteredStates = plant._validStates != null;
        var evsMapG = new int[evs.Length];
        var evsMapS = new int[evs.Length];
        var statePlant = new StatesTuple(plant._tupleSize);
        var stateSup = new StatesTuple(_tupleSize);
        var controllable = UltraDES.Controllability.Controllable;
        var disabled = new Dictionary<AbstractState, List<AbstractEvent>>((int)Size);
        AbstractState currentState = null;

        if (!filteredStates)
            _validStates = new Dictionary<StatesTuple, bool>((int)Size, StatesTupleComparator.GetInstance());

        for (var e = 0; e < evs.Length; ++e)
        {
            if (!evs[e].IsControllable)
                ++numUncontEvs;
            evsMapG[e] = Array.IndexOf(plant._eventsUnion, evs[e]);
            evsMapS[e] = Array.IndexOf(_eventsUnion, evs[e]);
        }

        plantStack.Push(new int[numPlant]);
        stateSup.Set(stateArrSup, _bits);
        if (!filteredStates || _validStates.ContainsKey(stateSup))
        {
            supStack.Push(stateArrSup);
            if (filteredStates)
                _validStates[stateSup] = true;
            else
                _validStates.Add(new StatesTuple(stateArrSup, _bits, _tupleSize), true);
        }

        var stopSearch = false;

        while (supStack.Count > 0)
        {
            var stateArrPlant = plantStack.Pop();
            stateArrSup = supStack.Pop();

            if (getDisabledEvents)
            {
                currentState = ComposeState(stateArrSup);
                disabled.Add(currentState, new List<AbstractEvent>());
            }

            for (var e = 0; e < evs.Length; ++e)
            {
                var t = CheckState(plant, numPlant, numSup, stateArrPlant, stateArrSup, evsMapG[e], evsMapS[e]);
                var hasNextPlant = t.hasNextPlant;
                var hasNextSup = t.hasNextSup;

                if (hasNextPlant && GfilteredStates)
                {
                    statePlant.Set(t.stateArrPlantNext, plant._bits);
                    hasNextPlant = plant._validStates.ContainsKey(statePlant);
                }

                if (hasNextSup && filteredStates)
                {
                    stateSup.Set(t.stateArrSupNext, _bits);
                    hasNextSup = _validStates.ContainsKey(stateSup);
                }

                if (!hasNextPlant && hasNextSup)
                    throw new Exception("Plant is invalid.");

                if (hasNextPlant && !hasNextSup)
                {
                    if (e < numUncontEvs)
                    {
                        controllable = UltraDES.Controllability.Uncontrollable;
                        if (!getDisabledEvents)
                        {
                            stopSearch = true;
                            break;
                        }
                    }

                    if (getDisabledEvents)
                        disabled[currentState].Add(evs[e]);
                }
                else if (hasNextPlant)
                {
                    if (filteredStates)
                    {
                        if (_validStates[stateSup])
                            continue;
                        _validStates[stateSup] = true;
                        plantStack.Push(t.stateArrPlantNext);
                        supStack.Push(t.stateArrSupNext);
                    }
                    else
                    {
                        stateSup.Set(t.stateArrSupNext, _bits);
                        if (_validStates.ContainsKey(stateSup))
                            continue;
                        _validStates.Add(new StatesTuple(t.stateArrSupNext, _bits, _tupleSize), true);
                        plantStack.Push(t.stateArrPlantNext);
                        supStack.Push(t.stateArrSupNext);
                    }
                }
            }

            if (stopSearch)
                break;
        }

        if (filteredStates)
            foreach (var t in _validStates.Reverse())
                _validStates[t.Key] = false;
        else
            _validStates = null;

        return (controllable, disabled);
    }

    /// <summary>
    /// Retrieves the dictionary of disabled events for the given plant DFAs.
    /// </summary>
    /// <param name="plants">An array of plant DFAs.</param>
    /// <returns>A dictionary mapping abstract states to lists of disabled events.</returns>
    public Dictionary<AbstractState, List<AbstractEvent>> DisabledEvents(params DFA[] plants) =>
        DisabledEvents((IEnumerable<DFA>)plants);

    /// <summary>
    /// Retrieves the dictionary of disabled events for the given plant DFAs.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <returns>A dictionary mapping abstract states to lists of disabled events.</returns>
    public Dictionary<AbstractState, List<AbstractEvent>> DisabledEvents(IEnumerable<DFA> plants) =>
        ControllabilityAndDisabledEvents(plants).Item2;

    /// <summary>
    /// Synthesizes the supervisor for the automaton using the provided number of plant states.
    /// Optionally ensures that the supervisor is nonblocking.
    /// </summary>
    /// <param name="nPlant">The number of states in the plant.</param>
    /// <param name="nonBlocking">
    /// If set to <c>true</c>, the method enforces nonblocking behavior during supervisor synthesis.
    /// </param>
    private void FindSupervisor(int nPlant, bool nonBlocking)
    {
        var statesStack = new Stack<StatesTuple>();
        var removeBadStates = new Stack<bool>();

        _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.GetInstance());

        MakeReverseTransitions();

        var initialIndex = new StatesTuple(new int[_statesList.Count], _bits, _tupleSize);
        statesStack.Push(initialIndex);
        removeBadStates.Push(false);

        var tasks = new Task[NumberOfThreads - 1];
        for (var i = 0; i < NumberOfThreads - 1; ++i)
            tasks[i] = Task.Factory.StartNew(() => FindStates(nPlant, statesStack, removeBadStates));

        FindStates(nPlant, statesStack, removeBadStates);
        Task.WaitAll(tasks);

        foreach (var s in _validStates.Where(s => s.Value))
            _validStates.Remove(s.Key);

        bool vNewBadStates;
        do
        {
            vNewBadStates = DepthFirstSearch(false, true);

            if (nonBlocking)
                vNewBadStates |= RemoveBlockingStates(true);
        } while (vNewBadStates);
    }

    /// <summary>
    /// Determines whether the collection of supervisor DFAs is conflicting.
    /// A conflict exists if the composition leads to blocking.
    /// </summary>
    /// <param name="supervisors">An enumerable collection of supervisor DFAs.</param>
    /// <returns>
    /// <c>true</c> if the supervisors are conflicting (i.e., the composed system is blocking);
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsConflicting(IEnumerable<DFA> supervisors)
    {
        // Parallel.ForEach(supervisors, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, s => s.Simplify());

        var composition = ParallelComposition(supervisors);
        var oldSize = composition.Size;
        composition.RemoveBlockingStates();
        return composition.Size != oldSize;
    }

    /// <summary>
    /// Determines if the given plant DFAs are controllable.
    /// </summary>
    /// <param name="plants">An array of plant DFAs.</param>
    /// <returns><c>true</c> if the plants are controllable; otherwise, <c>false</c>.</returns>
    public bool IsControllable(params DFA[] plants) =>
        IsControllable((IEnumerable<DFA>)plants);

    /// <summary>
    /// Determines if the given plant DFAs are controllable.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <returns><c>true</c> if the plants are controllable; otherwise, <c>false</c>.</returns>
    public bool IsControllable(IEnumerable<DFA> plants) =>
        Controllability(plants) == UltraDES.Controllability.Controllable;

    /// <summary>
    /// Synthesizes a local modular supervisor based on the provided plants and specifications.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <param name="specifications">An enumerable collection of specification DFAs.</param>
    /// <param name="conflictResolvingSupervisor">
    /// An optional enumerable collection of conflict-resolving supervisor DFAs.
    /// </param>
    /// <returns>An enumerable collection of supervisor DFAs.</returns>
    /// <exception cref="Exception">Thrown if the synthesized supervisors conflict.</exception>
    public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, IEnumerable<DFA> conflictResolvingSupervisor = null)
    {
        conflictResolvingSupervisor ??= Array.Empty<DFA>();

        var supervisors = specifications
            // .AsParallel().WithDegreeOfParallelism(NumberOfThreads)
            .Select(e => MonolithicSupervisor(plants.Where(p => p._eventsUnion.Intersect(e._eventsUnion).Any()), new[] { e }))
            .ToList();

        var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

        if (IsConflicting(complete))
            throw new Exception("Conflicting Supervisors");
        GC.Collect();
        return complete;
    }

    /// <summary>
    /// Synthesizes a local modular supervisor from the given plants and specifications, and returns the compound plants.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <param name="specifications">An enumerable collection of specification DFAs.</param>
    /// <param name="compoundPlants">
    /// When the method returns, contains the list of compound plants used in the synthesis.
    /// </param>
    /// <param name="conflictResolvingSupervisor">
    /// An optional enumerable collection of conflict-resolving supervisor DFAs.
    /// </param>
    /// <returns>An enumerable collection of supervisor DFAs.</returns>
    /// <exception cref="Exception">Thrown if the synthesized supervisors conflict.</exception>
    public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications,
        out List<DFA> compoundPlants, IEnumerable<DFA> conflictResolvingSupervisor = null)
    {
        conflictResolvingSupervisor ??= Array.Empty<DFA>();

        var dic = specifications.ToDictionary(e => plants.Where(p => p._eventsUnion.Intersect(e._eventsUnion).Any()).Aggregate((a, b) => a.ParallelCompositionWith(b)));

        var supervisors = dic.AsParallel().WithDegreeOfParallelism(NumberOfThreads)
            .Select(automata => MonolithicSupervisor(new[] { automata.Key }, new[] { automata.Value }))
            .ToList();

        var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

        if (IsConflicting(complete))
            throw new Exception("Conflicting Supervisors");

        compoundPlants = dic.Keys.ToList();
        GC.Collect();
        return complete;
    }

    /// <summary>
    /// Synthesizes a monolithic supervisor from the given plant and specification DFAs.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <param name="specifications">An enumerable collection of specification DFAs.</param>
    /// <param name="nonBlocking">
    /// If <c>true</c>, the resulting supervisor is synthesized to be nonblocking.
    /// </param>
    /// <returns>A DFA representing the synthesized supervisor.</returns>
    public static DFA MonolithicSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, bool nonBlocking = true)
    {
        var plant = plants.Aggregate((a, b) => a.ParallelCompositionWith(b, false));
        var specification = specifications.Aggregate((a, b) => a.ParallelCompositionWith(b, false));
        var result = plant.ParallelCompositionWith(specification, false);
        result.FindSupervisor(plant._statesList.Count, nonBlocking);

        result.Name = $"Sup({result.Name})";
        result._numberOfPlants = plant._statesList.Count;

        return result;
    }

    /// <summary>
    /// [Obsolete] Synthesizes a monolithic supervisor using a legacy algorithm.
    /// </summary>
    /// <param name="plant">The plant DFA.</param>
    /// <param name="spec">The specification DFA.</param>
    /// <returns>A DFA representing the synthesized supervisor.</returns>
    [Obsolete("MonolithicSupervisorLegacy is deprecated, please use MonolithicSupervisor instead.")]
    public static DFA MonolithicSupervisorLegacy(DFA plant, DFA spec)
    {
        var forbidden = new HashSet<AbstractState>();
        var sup = plant.ParallelCompositionWith(spec);
        SupervisorPlantState(sup, plant, forbidden);

        bool controllable = false, nonblocking = false;

        while (!controllable && !nonblocking)
        {
            controllable = VerifyControllability(sup, forbidden);
            nonblocking = VerifyBlocking(sup, forbidden);
        }

        var trans = sup.Transitions.Where(t => !forbidden.Contains(t.Origin) && !forbidden.Contains(t.Destination))
            .ToList();

        return new DFA(trans, sup.InitialState, $"sup({sup.Name})");
    }

    /// <summary>
    /// Removes blocking states from the automaton.
    /// </summary>
    /// <param name="checkForBadStates">
    /// If <c>true</c>, the method also checks for and removes bad states.
    /// </param>
    /// <returns>
    /// <c>true</c> if new bad states were found and removed; otherwise, <c>false</c>.
    /// </returns>
    private bool RemoveBlockingStates(bool checkForBadStates = false)
    {
        MakeReverseTransitions();
        int n = _statesList.Count, i;

        var markedStates = new List<int>[n];
        var pos = new int[n];
        var statePos = new int[n];
        pos[n - 1] = -1;
        var statesStack = new Stack<StatesTuple>();

        var vNotCheckValidState = false;
        if (_validStates == null)
        {
            vNotCheckValidState = true;
            _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.GetInstance());
        }

        for (i = 0; i < n; ++i)
        {
            markedStates[i] = _statesList[i].Where(st => st.IsMarked)
                .Select(st => Array.IndexOf(_statesList[i], st)).ToList();
        }

        while (true)
        {
            for (i = n - 1; i >= 0; --i)
            {
                ++pos[i];
                if (pos[i] < markedStates[i].Count())
                    break;
                pos[i] = 0;
            }

            if (i < 0)
                break;

            for (i = 0; i < n; ++i)
                statePos[i] = markedStates[i][pos[i]];
            var tuple = new StatesTuple(statePos, _bits, _tupleSize);
            if (!_validStates.TryGetValue(tuple, out var vValue) && !vNotCheckValidState || vValue)
                continue;
            _validStates[tuple] = true;
            statesStack.Push(tuple);
        }

        markedStates = null;
        Size = 0;
        var tasks = new Task[NumberOfThreads - 1];

        for (i = 0; i < NumberOfThreads - 1; ++i)
            tasks[i] = Task.Factory.StartNew(() => InverseSearchThread(vNotCheckValidState, statesStack));

        InverseSearchThread(vNotCheckValidState, statesStack);

        Task.WaitAll(tasks);

        statesStack = null;
        var vNewBadStates = false;
        var uncontrollableEventsCount = UncontrollableEvents.Count();

        if (checkForBadStates)
        {
            var removedStates = (_validStates.Where(p => !p.Value).Select(p => p.Key)).ToList();
            vNewBadStates = removedStates.Aggregate(vNewBadStates, (current, p) => current | RemoveBadStates(p, uncontrollableEventsCount, true));
        }

        _reverseTransitionsList = null;

        foreach (var p in _validStates)
            if (!p.Value)
                _validStates.Remove(p.Key);
            else
                _validStates[p.Key] = false;

        return vNewBadStates;
    }

    /// <summary>
    /// Maps supervisor states to corresponding plant states by traversing their transitions.
    /// </summary>
    /// <param name="sup">The supervisor DFA.</param>
    /// <param name="plant">The plant DFA.</param>
    /// <param name="forbidden">
    /// A set of forbidden abstract states that are not allowed in the mapping.
    /// </param>
    /// <returns>
    /// A dictionary mapping each supervisor state to its corresponding plant state.
    /// </returns>
    private static Dictionary<AbstractState, AbstractState> SupervisorPlantState(DFA sup, DFA plant, HashSet<AbstractState> forbidden)
    {
        var transG = plant.Transitions.GroupBy(t => t.Origin)
            .ToDictionary(g => g.Key, g => g.Select(t => (t.Trigger, t.Destination)).ToArray());
        var transS = sup.Transitions.GroupBy(t => t.Origin)
            .ToDictionary(g => g.Key, g => g.Select(t => (t.Trigger, t.Destination)).ToArray());

        var map = new Dictionary<AbstractState, AbstractState>((int)sup.Size);

        var frontier = new HashSet<(AbstractState, AbstractState)> { (sup.InitialState, plant.InitialState) };

        while (frontier.Any())
        {
            var newFrontier = new HashSet<(AbstractState, AbstractState)>();
            foreach (var (qs, qp) in frontier)
                map.Add(qs, qp);

            foreach (var (qs, qp) in frontier)
                foreach (var (e, dp) in transG[qp])
                {
                    var ds = transS[qs].Where(t => t.Trigger == e).DefaultIfEmpty((null, null)).SingleOrDefault().Destination;

                    if (ds == null)
                    {
                        if (!e.IsControllable)
                            forbidden.Add(qs);
                        continue;
                    }

                    if (!map.ContainsKey(ds))
                        newFrontier.Add((ds, dp));
                }

            frontier = newFrontier;
        }

        return map;
    }

    /// <summary>
    /// Verifies whether the supervisor is blocking by traversing reverse transitions.
    /// </summary>
    /// <param name="sup">The supervisor DFA.</param>
    /// <param name="forbidden">A set of forbidden abstract states.</param>
    /// <returns>
    /// <c>true</c> if blocking is detected; otherwise, <c>false</c>.
    /// </returns>
    private static bool VerifyBlocking(DFA sup, HashSet<AbstractState> forbidden)
    {
        var reverseTrans = sup.Transitions
            .Where(t => !forbidden.Contains(t.Origin) && !forbidden.Contains(t.Destination))
            .GroupBy(t => t.Destination).ToDictionary(g => g.Key, g => g.Select(t => t.Origin));

        var frontier = new HashSet<AbstractState>(sup.MarkedStates);
        var visited = new HashSet<AbstractState>();

        while (frontier.Any())
        {
            visited.UnionWith(frontier);
            frontier = new HashSet<AbstractState>(frontier.SelectMany(d =>
                reverseTrans[d].Where(o => !visited.Contains(o))));
        }

        var unvisited = sup.States.Except(forbidden).Except(visited).ToList();
        forbidden.UnionWith(unvisited);

        return unvisited.Any();
    }

    /// <summary>
    /// Verifies the controllability of the supervisor by propagating forbidden states via uncontrollable transitions.
    /// </summary>
    /// <param name="sup">The supervisor DFA.</param>
    /// <param name="forbidden">A set of forbidden abstract states.</param>
    /// <returns>
    /// <c>true</c> if the supervisor is controllable; otherwise, <c>false</c>.
    /// </returns>
    private static bool VerifyControllability(DFA sup, HashSet<AbstractState> forbidden)
    {
        var controllable = true;
        var uncontrollableTrans = sup.Transitions.Where(t => !t.Trigger.IsControllable).ToList();
        while (true)
        {
            var newForbidden = uncontrollableTrans.AsParallel().WithDegreeOfParallelism(NumberOfThreads)
                .Where(t => forbidden.Contains(t.Destination) && !forbidden.Contains(t.Origin))
                .Select(t => t.Origin).ToList();

            if (!newForbidden.Any())
                return controllable;
            controllable = false;

            forbidden.UnionWith(newForbidden);
        }
    }

    /// <summary>
    /// [Experimental] Reduces the supervisor by eliminating redundant states and transitions.
    /// </summary>
    /// <param name="plant">The plant DFA.</param>
    /// <param name="supervisor">The supervisor DFA.</param>
    /// <param name="maxIt">The maximum number of iterations allowed.</param>
    /// <returns>A reduced DFA representing the supervisor.</returns>
    public static DFA ReduceSupervisor(DFA plant, DFA supervisor, long maxIt = long.MaxValue)
    {
        var (trans, initial) = Reduction(plant, supervisor, null, maxIt);
        return new DFA(trans, initial, $"RED({supervisor.Name})");
    }

    /// <summary>
    /// [Experimental] Localizes the supervisor for each agent based on the global plant.
    /// </summary>
    /// <param name="globalPlant">The global plant DFA.</param>
    /// <param name="supervisor">The global supervisor DFA.</param>
    /// <param name="agents">An enumerable collection of agent DFAs.</param>
    /// <param name="maxIt">The maximum number of iterations allowed.</param>
    /// <returns>
    /// An enumerable collection of localized supervisor DFAs, one per agent.
    /// </returns>
    public static IEnumerable<DFA> LocalizeSupervisor(DFA globalPlant, DFA supervisor, IEnumerable<DFA> agents, long maxIt = long.MaxValue)
    {
        return agents.AsParallel().WithDegreeOfParallelism(NumberOfThreads).Select(Gk =>
        {
            var (trans, initial) = Reduction(globalPlant, supervisor, Gk.Events.ToSet(), maxIt);
            return new DFA(trans, initial, $"LOC({supervisor.Name}, ({Gk.Name}))");
        }).ToArray();
    }

    /// <summary>
    /// [Experimental] Synthesizes a monolithic localized supervisor by decomposing the global supervisor.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <param name="specifications">An enumerable collection of specification DFAs.</param>
    /// <param name="maxIt">The maximum number of iterations allowed.</param>
    /// <returns>An enumerable collection of localized supervisor DFAs.</returns>
    public static IEnumerable<DFA> MonolithicLocalizedSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, long maxIt = long.MaxValue)
    {
        var plant = ParallelComposition(plants);
        var sup = MonolithicSupervisor(new[] { plant }, specifications, true);
        return LocalizeSupervisor(plant, sup, plants, maxIt);
    }

    /// <summary>
    /// [Experimental] Synthesizes a monolithic reduced supervisor by reducing the composed supervisor.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <param name="specifications">An enumerable collection of specification DFAs.</param>
    /// <param name="maxIt">The maximum number of iterations allowed.</param>
    /// <returns>A reduced DFA representing the synthesized supervisor.</returns>
    public static DFA MonolithicReducedSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, long maxIt = long.MaxValue)
    {
        var plant = ParallelComposition(plants);
        var sup = MonolithicSupervisor(new[] { plant }, specifications, true);
        return ReduceSupervisor(plant, sup, maxIt);
    }

    /// <summary>
    /// [Experimental] Synthesizes a local modular localized supervisor by decomposing supervisors and localizing them.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <param name="specifications">An enumerable collection of specification DFAs.</param>
    /// <param name="maxIt">The maximum number of iterations allowed.</param>
    /// <returns>An enumerable collection of localized supervisor DFAs.</returns>
    public static IEnumerable<DFA> LocalModularLocalizedSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, long maxIt = long.MaxValue)
    {
        var supervisors = specifications.AsParallel().WithDegreeOfParallelism(NumberOfThreads).Select(spec =>
        {
            var localPlants = plants.Where(p => p._eventsUnion.Intersect(spec._eventsUnion).Any()).ToArray();
            var localPlant = ParallelComposition(localPlants);
            var localSup = MonolithicSupervisor(new[] { localPlant }, new[] { spec }, true);
            return (localPlant, localSup, localPlants);
        }).ToList();

        if (IsConflicting(supervisors.Select(t => t.localSup)))
            throw new Exception("Conflicting Supervisors");
        GC.Collect();
        return supervisors.AsParallel().WithDegreeOfParallelism(NumberOfThreads).SelectMany(t => LocalizeSupervisor(t.localPlant, t.localSup, t.localPlants, maxIt)).ToArray();
    }

    /// <summary>
    /// [Experimental] Synthesizes a local modular reduced supervisor by reducing local supervisors.
    /// </summary>
    /// <param name="plants">An enumerable collection of plant DFAs.</param>
    /// <param name="specifications">An enumerable collection of specification DFAs.</param>
    /// <param name="maxIt">The maximum number of iterations allowed.</param>
    /// <returns>An enumerable collection of reduced supervisor DFAs.</returns>
    public static IEnumerable<DFA> LocalModularReducedSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, long maxIt = long.MaxValue)
    {
        var supervisors = specifications.AsParallel().WithDegreeOfParallelism(NumberOfThreads).Select(spec =>
        {
            var localPlants = plants.Where(p => p._eventsUnion.Intersect(spec._eventsUnion).Any()).ToArray();
            var localPlant = ParallelComposition(localPlants);
            var localSup = MonolithicSupervisor(new[] { localPlant }, new[] { spec }, true);
            return (localPlant, localSup);
        }).ToList();

        if (IsConflicting(supervisors.Select(t => t.localSup)))
            throw new Exception("Conflicting Supervisors");
        GC.Collect();
        return supervisors.AsParallel().WithDegreeOfParallelism(NumberOfThreads).Select(t => ReduceSupervisor(t.localPlant, t.localSup, maxIt)).ToArray();
    }

    /// <summary>
    /// Performs the reduction of the supervisor relative to the plant.
    /// </summary>
    /// <param name="P">The plant DFA.</param>
    /// <param name="S">The supervisor DFA.</param>
    /// <param name="Ek">
    /// A set of events to be localized; if <c>null</c>, all events from the plant are considered.
    /// </param>
    /// <param name="maxIt">The maximum number of iterations allowed.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description>An array of transitions for the reduced DFA.</description></item>
    ///   <item><description>The initial abstract state of the reduced DFA.</description></item>
    /// </list>
    /// </returns>
    private static (Transition[] trans, AbstractState initial) Reduction(DFA P, DFA S, HashSet<AbstractEvent> Ek, long maxIt = long.MaxValue)
    {
        AbstractEvent[] events;
        if (Ek == null)
        {
            Ek = P.Events.ToSet();
            events = Ek.Where(e => e.IsControllable).ToArray();
        }
        else
            events = Ek.ToArray();

        var eventsP = P.Events.Except(S.Events).ToArray();
        var eventsS = S.Events.Except(P.Events).ToArray();

        var transP = P.Transitions.Union(P.States.SelectMany(q => eventsS.Select(e => new Transition(q, e, q)))).GroupBy(t => t.Origin).ToDictionary(g => g.Key, g => g.ToArray());
        var transS = S.Transitions.Union(S.States.SelectMany(q => eventsP.Select(e => new Transition(q, e, q)))).GroupBy(t => t.Origin).ToDictionary(g => g.Key, g => g.ToArray());

        var pairs = MatchingPairs(P, S, transP, transS);

        var E = transS.ToDictionary(k => k.Key, k => new HashSet<AbstractEvent>(k.Value.Select(t => t.Trigger)));
        var D = transS.ToDictionary(k => k.Key, k => events.Where(e => k.Value.All(t => t.Trigger != e) && pairs[k.Key].Any(qp => transP[qp].Any(t => t.Trigger == e))).ToSet());
        var M = transS.ToDictionary(k => k.Key, k => k.Key.IsMarked);
        var T = pairs.ToDictionary(k => k.Key, k => k.Value.Any(qp => qp.IsMarked));

        bool R(AbstractState q1, AbstractState q2) =>
            !E[q1].Intersect(D[q2]).Any() &&
            !E[q2].Intersect(D[q1]).Any() &&
            (T[q1] != T[q2] || M[q1] == M[q2]);

        var states = S.States.OrderBy(q => D[q].Count).ThenByDescending(q => E[q].Count).ToArray();
        var stateIdx = Enumerable.Range(0, states.Length).ToDictionary(i => states[i], i => i);
        var C = Enumerable.Range(0, states.Length).Select(i => new HashSet<int> { i }).ToArray();
        var waitList = new HashSet<(int, int)>();
        int cnode, it = 0;

        bool CheckMergibility(int i, int j)
        {
            var stack = new Stack<Tuple<int, int>>();
            stack.Push(new Tuple<int, int>(i, j));

            while (stack.Count > 0)
            {
                if (it++ > maxIt)
                    return false;

                var pair = stack.Pop();
                i = pair.Item1;
                j = pair.Item2;

                var Xp = waitList.Where(p => p.Item1 == i).Select(p => p.Item2)
                    .Union(waitList.Where(p => p.Item2 == i).Select(p => p.Item1))
                    .Aggregate((IEnumerable<int>)C[i], (acc, e) => acc.Union(C[e]));

                foreach (var xp in Xp)
                {
                    var Xq = waitList.Where(p => p.Item1 == j).Select(p => p.Item2)
                        .Union(waitList.Where(p => p.Item2 == j).Select(p => p.Item1))
                        .Aggregate((IEnumerable<int>)C[j], (acc, e) => acc.Union(C[e]));

                    foreach (var xq in Xq)
                    {
                        if (waitList.Contains((xp, xq)) || waitList.Contains((xq, xp)))
                            continue;
                        if (!R(states[xp], states[xq]))
                            return false;
                        waitList.Add((xp, xq));
                        var evs = transS[states[xp]].Select(t => t.Trigger)
                            .Intersect(transS[states[xq]].Select(t => t.Trigger));
                        foreach (var ev in evs)
                        {
                            var xpd = stateIdx[transS[states[xp]].Single(t => t.Trigger == ev).Destination];
                            var xqd = stateIdx[transS[states[xq]].Single(t => t.Trigger == ev).Destination];

                            if (xpd == xqd || waitList.Contains((xqd, xpd)) || waitList.Contains((xpd, xqd)))
                                continue;
                            if (C[xpd].Min() < cnode || C[xqd].Min() < cnode)
                                return false;
                            stack.Push(new Tuple<int, int>(xpd, xqd));
                        }
                    }
                }
            }

            return true;
        }

        for (int i = 0; i < states.Length; i++)
        {
            if (i > C[i].Min())
                continue;
            for (int j = i + 1; j < states.Length; j++)
            {
                if (j > C[j].Min())
                    continue;
                waitList.Clear();
                cnode = i;
                const int stackSize = 10000000;
                var flag = false;

                flag = CheckMergibility(i, j);

                if (!flag)
                    continue;

                foreach (var (x, y) in waitList)
                {
                    var set = C[x];
                    set.UnionWith(C[y]);

                    foreach (var k in set)
                        C[k] = set;
                }
            }
        }

        var cover = C.Distinct().ToArray();
        var snum = 0;
        var cover2state = cover.OrderBy(x => x.Contains(stateIdx[S.InitialState]) ? 0 : 1)
            .ToDictionary(set => set, set => new State($"{snum++}", set.Any(q => states[q].IsMarked) ? Marking.Marked : Marking.Unmarked));

        var trans = transS.SelectMany(k => k.Value)
            .Select(t => new Transition(cover2state[C[stateIdx[t.Origin]]], t.Trigger, cover2state[C[stateIdx[t.Destination]]]))
            .Distinct().ToArray();

        var Ecom = S.Events.Except(Ek).Where(e => trans.Any(t => t.Origin != t.Destination && t.Trigger == e)).ToArray();
        var Eloc = Ek.Union(Ecom).ToSet();

        trans = trans.Where(t => Eloc.Contains(t.Trigger)).ToArray();

        var initial = cover2state[cover.Single(x => x.Contains(stateIdx[S.InitialState]))];

        return (trans, initial);
    }

    /// <summary>
    /// Computes matching pairs between plant and supervisor states based on common transitions.
    /// </summary>
    /// <param name="P">The plant DFA.</param>
    /// <param name="S">The supervisor DFA.</param>
    /// <param name="transP">A dictionary grouping plant transitions by origin state.</param>
    /// <param name="transS">A dictionary grouping supervisor transitions by origin state.</param>
    /// <returns>
    /// A dictionary mapping each supervisor state to an array of corresponding plant states.
    /// </returns>
    private static Dictionary<AbstractState, AbstractState[]> MatchingPairs(DFA P, DFA S, Dictionary<AbstractState, Transition[]> transP, Dictionary<AbstractState, Transition[]> transS)
    {
        var visited = new HashSet<(AbstractState qp, AbstractState qs)>();
        var front = new HashSet<(AbstractState, AbstractState)> { (P.InitialState, S.InitialState) };

        while (front.Any())
        {
            visited.UnionWith(front);

            var nextFront = new HashSet<(AbstractState, AbstractState)>();

            foreach (var (qp, qs) in front)
            {
                var events = transP[qp].Select(t => t.Trigger).Intersect(transS[qs].Select(t => t.Trigger)).ToArray();
                foreach (var ev in events)
                {
                    var qpd = transP[qp].Single(t => t.Trigger == ev).Destination;
                    var qsd = transS[qs].Single(t => t.Trigger == ev).Destination;
                    var dest = (qpd, qsd);

                    if (!visited.Contains(dest))
                        nextFront.Add(dest);
                }
            }

            front = nextFront;
        }

        return visited.GroupBy(p => p.qs).ToDictionary(g => g.Key, g => g.Select(p => p.qp).ToArray());
    }
}
