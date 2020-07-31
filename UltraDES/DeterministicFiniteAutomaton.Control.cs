// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-22-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 05-20-2020
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UltraDES
{
    using DFA = DeterministicFiniteAutomaton;

    /// <summary>
    /// Class DeterministicFiniteAutomaton. This class cannot be inherited.
    /// </summary>
    partial class DeterministicFiniteAutomaton
    {
        /// <summary>
        /// Checks the state.
        /// </summary>
        /// <param name="G">The g.</param>
        /// <param name="nG">The n g.</param>
        /// <param name="nS">The n s.</param>
        /// <param name="posG">The position g.</param>
        /// <param name="posS">The position s.</param>
        /// <param name="eG">The e g.</param>
        /// <param name="eS">The e s.</param>
        /// <returns>Tuple&lt;System.Boolean, System.Boolean, System.Int32[], System.Int32[]&gt;.</returns>
        private Tuple<bool, bool, int[], int[]> CheckState(DFA G, int nG, int nS, int[] posG, int[] posS, int eG, int eS)
        {
            var nextG = new int[nG];
            var nextS = new int[nS];
            bool hasNextG = true, hasNextS = true;
            if (eG == -1)
            {
                for (var i = 0; i < nG; ++i)
                    nextG[i] = posG[i];
            }
            else
            {
                for (var i = 0; i < nG; ++i)
                {
                    if (!G._eventsList[i][eG])
                        nextG[i] = posG[i];
                    else
                    {
                        if (!G._adjacencyList[i].HasEvent(posG[i], eG))
                        {
                            hasNextG = false;
                            break;
                        }

                        nextG[i] = G._adjacencyList[i][posG[i], eG];
                    }
                }
            }

            if (eS == -1)
            {
                for (var i = 0; i < nS; ++i)
                    nextS[i] = posS[i];
            }
            else
            {
                for (var i = 0; i < nS; ++i)
                {
                    if (!_eventsList[i][eS])
                        nextS[i] = posS[i];
                    else
                    {
                        if (!_adjacencyList[i].HasEvent(posS[i], eS))
                        {
                            hasNextS = false;
                            break;
                        }

                        nextS[i] = _adjacencyList[i][posS[i], eS];
                    }
                }
            }

            return new Tuple<bool, bool, int[], int[]>(hasNextG, hasNextS, nextG, nextS);
        }

        /// <summary>
        /// Controllabilities the specified plants.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <returns>Controllability.</returns>
        public Controllability Controllability(params DFA[] plants) => Controllability((IEnumerable<DFA>) plants);

        /// <summary>
        /// Controllabilities the specified plants.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <returns>Controllability.</returns>
        public Controllability Controllability(IEnumerable<DFA> plants) => ControllabilityAndDisabledEvents(plants, false).Item1;

        /// <summary>
        /// Controllabilities the and disabled events.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <returns>Tuple&lt;Controllability, Dictionary&lt;AbstractState, List&lt;AbstractEvent&gt;&gt;&gt;.</returns>
        public Tuple<Controllability, Dictionary<AbstractState, List<AbstractEvent>>> ControllabilityAndDisabledEvents(params DFA[] plants) =>
            ControllabilityAndDisabledEvents(plants, true);

        /// <summary>
        /// Controllabilities the and disabled events.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <param name="getDisabledEvents">if set to <c>true</c> [get disabled events].</param>
        /// <returns>Tuple&lt;Controllability, Dictionary&lt;AbstractState, List&lt;AbstractEvent&gt;&gt;&gt;.</returns>
        /// <exception cref="Exception">Plant is invalid.</exception>
        public Tuple<Controllability, Dictionary<AbstractState, List<AbstractEvent>>> ControllabilityAndDisabledEvents(
            IEnumerable<DFA> plants, bool getDisabledEvents = true)
        {
            var G = ParallelComposition(plants, false);
            var nG = G._statesList.Count;
            var nS = _statesList.Count;
            var pos2 = new int[nS];
            var evs = _eventsUnion.Union(G._eventsUnion).OrderBy(i => i.Controllability).ToArray();
            var numUncontEvs = 0;
            var stackG = new Stack<int[]>();
            var stackS = new Stack<int[]>();
            var filteredStates = _validStates != null;
            var GfilteredStates = G._validStates != null;
            var evsMapG = new int[evs.Length];
            var evsMapS = new int[evs.Length];
            var GTuple = new StatesTuple(G._tupleSize);
            var STuple = new StatesTuple(_tupleSize);
            var controllable = UltraDES.Controllability.Controllable;
            var disabled = new Dictionary<AbstractState, List<AbstractEvent>>((int) Size);
            AbstractState currentState = null;

            if (!filteredStates)
                _validStates = new Dictionary<StatesTuple, bool>((int) Size, StatesTupleComparator.GetInstance());

            for (var e = 0; e < evs.Length; ++e)
            {
                if (!evs[e].IsControllable) ++numUncontEvs;
                evsMapG[e] = Array.IndexOf(G._eventsUnion, evs[e]);
                evsMapS[e] = Array.IndexOf(_eventsUnion, evs[e]);
            }

            stackG.Push(new int[nG]);
            STuple.Set(pos2, _bits);
            if (!filteredStates || _validStates.ContainsKey(STuple))
            {
                stackS.Push(pos2);
                if (filteredStates) _validStates[STuple] = true;
                else _validStates.Add(new StatesTuple(pos2, _bits, _tupleSize), true);
            }

            var stopSearch = false;

            while (stackS.Count > 0)
            {
                var pos1 = stackG.Pop();
                pos2 = stackS.Pop();

                if (getDisabledEvents)
                {
                    currentState = ComposeState(pos2);
                    disabled.Add(currentState, new List<AbstractEvent>());
                }

                for (var e = 0; e < evs.Length; ++e)
                {
                    var t = CheckState(G, nG, nS, pos1, pos2, evsMapG[e], evsMapS[e]);
                    var GHasNext = t.Item1;
                    var SHasNext = t.Item2;

                    if (GHasNext && GfilteredStates)
                    {
                        GTuple.Set(t.Item3, G._bits);
                        GHasNext = G._validStates.ContainsKey(GTuple);
                    }

                    if (SHasNext && filteredStates)
                    {
                        STuple.Set(t.Item4, _bits);
                        SHasNext = _validStates.ContainsKey(STuple);
                    }

                    if (!GHasNext && SHasNext) throw new Exception("Plant is invalid.");

                    if (GHasNext && !SHasNext)
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

                        if (getDisabledEvents) disabled[currentState].Add(evs[e]);
                    }
                    else if (GHasNext && SHasNext)
                    {
                        if (filteredStates)
                        {
                            if (!_validStates[STuple])
                            {
                                _validStates[STuple] = true;
                                stackG.Push(t.Item3);
                                stackS.Push(t.Item4);
                            }
                        }
                        else
                        {
                            STuple.Set(t.Item4, _bits);
                            if (!_validStates.ContainsKey(STuple))
                            {
                                _validStates.Add(new StatesTuple(t.Item4, _bits, _tupleSize), true);
                                stackG.Push(t.Item3);
                                stackS.Push(t.Item4);
                            }
                        }
                    }
                }

                if (stopSearch) break;
            }

            if (filteredStates)
                foreach (var t in _validStates.Reverse())
                    _validStates[t.Key] = false;
            else _validStates = null;

            return new Tuple<Controllability, Dictionary<AbstractState, List<AbstractEvent>>>(controllable, disabled);
        }

        /// <summary>
        /// Disableds the events.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <returns>Dictionary&lt;AbstractState, List&lt;AbstractEvent&gt;&gt;.</returns>
        public Dictionary<AbstractState, List<AbstractEvent>> DisabledEvents(params DFA[] plants) => 
            DisabledEvents((IEnumerable<DFA>) plants);

        /// <summary>
        /// Disableds the events.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <returns>Dictionary&lt;AbstractState, List&lt;AbstractEvent&gt;&gt;.</returns>
        public Dictionary<AbstractState, List<AbstractEvent>> DisabledEvents(IEnumerable<DFA> plants) => 
            ControllabilityAndDisabledEvents(plants).Item2;

        /// <summary>
        /// Finds the supervisor.
        /// </summary>
        /// <param name="nPlant">The n plant.</param>
        /// <param name="nonBlocking">if set to <c>true</c> [non blocking].</param>
        private void FindSupervisor(int nPlant, bool nonBlocking)
        {
            _numberOfRunningThreads = 0;
            _statesStack = new Stack<StatesTuple>();
            _removeBadStates = new Stack<bool>();

            _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.GetInstance());

            MakeReverseTransitions();

            var initialIndex = new StatesTuple(new int[_statesList.Count], _bits, _tupleSize);
            _statesStack.Push(initialIndex);
            _removeBadStates.Push(false);

            var vThreads = new Task[NumberOfThreads - 1];

            for (var i = 0; i < NumberOfThreads - 1; ++i) vThreads[i] = Task.Factory.StartNew(() => FindStates(nPlant));

            FindStates(nPlant);

            for (var i = 0; i < NumberOfThreads - 1; ++i) vThreads[i].Wait();

            foreach (var s in _validStates.Reverse())
                if (s.Value)
                    _validStates.Remove(s.Key);

            bool vNewBadStates;
            do
            {
                vNewBadStates = DepthFirstSearch(false, true);
                GC.Collect();
                if (nonBlocking)
                    vNewBadStates |= RemoveBlockingStates(true);
            } while (vNewBadStates);
        }

        /// <summary>
        /// Determines whether the specified supervisors is conflicting.
        /// </summary>
        /// <param name="supervisors">The supervisors.</param>
        /// <returns><c>true</c> if the specified supervisors is conflicting; otherwise, <c>false</c>.</returns>
        public static bool IsConflicting(IEnumerable<DFA> supervisors)
        {
            Parallel.ForEach(supervisors, s => { s.Simplify(); });

            var composition = supervisors.Aggregate((a, b) => a.ParallelCompositionWith(b));
            var oldSize = composition.Size;
            composition.RemoveBlockingStates();
            return composition.Size != oldSize;
        }

        /// <summary>
        /// Determines whether the specified plants is controllable.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <returns><c>true</c> if the specified plants is controllable; otherwise, <c>false</c>.</returns>
        public bool IsControllable(params DFA[] plants) => 
            IsControllable((IEnumerable<DFA>) plants);

        /// <summary>
        /// Determines whether the specified plants is controllable.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <returns><c>true</c> if the specified plants is controllable; otherwise, <c>false</c>.</returns>
        public bool IsControllable(IEnumerable<DFA> plants) => 
            Controllability(plants) == UltraDES.Controllability.Controllable;

        /// <summary>
        /// Locals the modular supervisor.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <param name="specifications">The specifications.</param>
        /// <param name="conflictResolvingSupervisor">The conflict resolving supervisor.</param>
        /// <returns>IEnumerable&lt;DFA&gt;.</returns>
        /// <exception cref="Exception">conflicting supervisors</exception>
        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            conflictResolvingSupervisor ??= new DFA[0];
            
            var supervisors = specifications.AsParallel().Select(e => MonolithicSupervisor(plants.Where(p => p._eventsUnion.Intersect(e._eventsUnion).Any()), new[] {e})).ToList();

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete)) throw new Exception("conflicting supervisors");
            GC.Collect();
            return complete;
        }

        /// <summary>
        /// Locals the modular supervisor.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <param name="specifications">The specifications.</param>
        /// <param name="compoundPlants">The compound plants.</param>
        /// <param name="conflictResolvingSupervisor">The conflict resolving supervisor.</param>
        /// <returns>IEnumerable&lt;DFA&gt;.</returns>
        /// <exception cref="Exception">conflicting supervisors</exception>
        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications,
            out List<DFA> compoundPlants, IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            conflictResolvingSupervisor ??= new DFA[0];

            var dic = specifications.ToDictionary(e => plants.Where(p => p._eventsUnion.Intersect(e._eventsUnion).Any()).Aggregate((a, b) => a.ParallelCompositionWith(b)));

            var supervisors = dic.AsParallel()
                .Select(automata => MonolithicSupervisor(new[] {automata.Key}, new[] {automata.Value})).ToList();

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete)) throw new Exception("conflicting supervisors");

            compoundPlants = dic.Keys.ToList();
            GC.Collect();
            return complete;
        }

        /// <summary>
        /// Monolithics the supervisor.
        /// </summary>
        /// <param name="plants">The plants.</param>
        /// <param name="specifications">The specifications.</param>
        /// <param name="nonBlocking">if set to <c>true</c> [non blocking].</param>
        /// <returns>DFA.</returns>
        public static DFA MonolithicSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications,
            bool nonBlocking = true)
        {
            var plant = plants.Aggregate((a, b) => a.ParallelCompositionWith(b, false));
            var specification = specifications.Aggregate((a, b) => a.ParallelCompositionWith(b, false));
            var result = plant.ParallelCompositionWith(specification, false);
            result.FindSupervisor(plant._statesList.Count(), nonBlocking);

            result.Name = $"Sup({result.Name})";
            result._numberOfPlants = plant._statesList.Count();

            return result;
        }

        /// <summary>
        /// Monolithics the supervisor legacy.
        /// </summary>
        /// <param name="plant">The plant.</param>
        /// <param name="spec">The spec.</param>
        /// <returns>DFA.</returns>
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
        /// Removes the blocking states.
        /// </summary>
        /// <param name="checkForBadStates">if set to <c>true</c> [check for bad states].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool RemoveBlockingStates(bool checkForBadStates = false)
        {
            MakeReverseTransitions();
            int n = _statesList.Count(), i;
            _numberOfRunningThreads = 0;
            var threads = new Task[NumberOfThreads - 1];

            var markedStates = new List<int>[n];
            var pos = new int[n];
            var statePos = new int[n];
            pos[n - 1] = -1;
            _statesStack = new Stack<StatesTuple>();

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
                    if (pos[i] < markedStates[i].Count()) break;
                    pos[i] = 0;
                }

                if (i < 0) break;

                for (i = 0; i < n; ++i) statePos[i] = markedStates[i][pos[i]];
                var tuple = new StatesTuple(statePos, _bits, _tupleSize);
                if (!_validStates.TryGetValue(tuple, out var vValue) && !vNotCheckValidState || vValue) continue;
                _validStates[tuple] = true;
                _statesStack.Push(tuple);
            }

            markedStates = null;
            Size = 0;

            for (i = 0; i < NumberOfThreads - 1; ++i)
                threads[i] = Task.Factory.StartNew(() => InverseSearchThread(vNotCheckValidState));

            InverseSearchThread(vNotCheckValidState);

            for (i = 0; i < NumberOfThreads - 1; ++i) threads[i].Wait();

            _statesStack = null;
            var v_newBadStates = false;
            var uncontrollableEventsCount = UncontrollableEvents.Count();

            if (checkForBadStates)
            {
                var removedStates = new List<StatesTuple>();
                foreach (var p in _validStates)
                    if (!p.Value)
                        removedStates.Add(p.Key);
                foreach (var p in removedStates) v_newBadStates |= RemoveBadStates(p, uncontrollableEventsCount, true);
            }

            _reverseTransitionsList = null;

            foreach (var p in _validStates.Reverse())
                if (!p.Value)
                    _validStates.Remove(p.Key);
                else
                    _validStates[p.Key] = false;

            GC.Collect();
            return v_newBadStates;
        }

        private static Dictionary<AbstractState, AbstractState> SupervisorPlantState(DFA sup, DFA plant, HashSet<AbstractState> forbidden)
        {
            var transG = plant.Transitions.GroupBy(t => t.Origin)
                .ToDictionary(g => g.Key, g => g.Select(t => (t.Trigger, t.Destination)).ToArray());
            var transS = sup.Transitions.GroupBy(t => t.Origin)
                .ToDictionary(g => g.Key, g => g.Select(t => (t.Trigger, t.Destination)).ToArray());

            var map = new Dictionary<AbstractState, AbstractState>((int) sup.Size);

            var frontier = new HashSet<(AbstractState, AbstractState)> {(sup.InitialState, plant.InitialState)};

            while (frontier.Any())
            {
                var newFrontier = new HashSet<(AbstractState, AbstractState)>();
                foreach (var (qs, qp) in frontier) map.Add(qs, qp);

                foreach (var (qs, qp) in frontier)
                foreach (var (e, dp) in transG[qp])
                {
                    var ds = transS[qs].Where(t => t.Trigger == e).DefaultIfEmpty((null, null)).SingleOrDefault()
                        .Destination;

                    if (ds == null)
                    {
                        if (!e.IsControllable) forbidden.Add(qs);
                        continue;
                    }

                    if (!map.ContainsKey(ds)) newFrontier.Add((ds, dp));
                }

                frontier = newFrontier;
            }

            return map;
        }

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

        private static bool VerifyControllability(DFA sup, HashSet<AbstractState> forbidden)
        {
            var controllable = true;
            var uncontrollableTrans = sup.Transitions.Where(t => !t.Trigger.IsControllable).ToList();
            while (true)
            {
                var newForbidden = uncontrollableTrans.AsParallel()
                    .Where(t => forbidden.Contains(t.Destination) && !forbidden.Contains(t.Origin))
                    .Select(t => t.Origin).ToList();

                if (!newForbidden.Any()) return controllable;
                controllable = false;

                forbidden.UnionWith(newForbidden);
            }
        }

        private bool VerifyNonblocking()
        {
            var nStates = Size;
            RemoveBlockingStates();
            return Size != nStates;
        }

        /// <summary>
        /// [Experimental]
        /// </summary>
        /// <param name="plant"></param>
        /// <param name="supervisor"></param>
        /// <returns></returns>
        public static DFA ReduceSupervisor(DFA plant, DFA supervisor, long maxIt = long.MaxValue)
        {
            var (trans, initial) = Reduction(plant, supervisor, null, maxIt);
            return new DFA(trans, initial, $"RED({supervisor.Name})");
        }

        /// <summary>
        /// [Experimental]
        /// </summary>
        /// <param name="globalPlant"></param>
        /// <param name="supervisor"></param>
        /// <param name="agents"></param>
        /// <param name="maxIt"></param>
        /// <returns></returns>
        public static IEnumerable<DFA> LocalizeSupervisor(DFA globalPlant, DFA supervisor, IEnumerable<DFA> agents, long maxIt = long.MaxValue)
        {
            return agents.AsParallel().Select(Gk =>
            {
                var (trans, initial) = Reduction(globalPlant, supervisor, Gk.Events.ToSet(), maxIt);
                return new DFA(trans, initial, $"LOC({supervisor.Name}, ({Gk.Name}))");
            }).ToArray();
        }

        /// <summary>
        /// [Experimental]
        /// </summary>
        /// <param name="plants"></param>
        /// <param name="specifications"></param>
        /// <param name="maxIt"></param>
        /// <returns></returns>
        public static IEnumerable<DFA> MonolithicLocalizedSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, long maxIt = long.MaxValue)
        {
            var plant = ParallelComposition(plants);
            var sup = MonolithicSupervisor(new[] {plant}, specifications, true);
            return LocalizeSupervisor(plant, sup, plants, maxIt);
        }

        /// <summary>
        /// [Experimental]
        /// </summary>
        /// <param name="plants"></param>
        /// <param name="specifications"></param>
        /// <param name="maxIt"></param>
        /// <returns></returns>
        public static DFA MonolithicReducedSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, long maxIt = long.MaxValue)
        {
            var plant = ParallelComposition(plants);
            var sup = MonolithicSupervisor(new[] { plant }, specifications, true);
            return ReduceSupervisor(plant, sup, maxIt);
        }

        /// <summary>
        /// [Experimental]
        /// </summary>
        /// <param name="plants"></param>
        /// <param name="specifications"></param>
        /// <param name="maxIt"></param>
        /// <returns></returns>
        public static IEnumerable<DFA> LocalModularLocalizedSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, long maxIt = long.MaxValue)
        {
            var supervisors = specifications.AsParallel().Select(spec =>
            {
                var localPlants = plants.Where(p => p._eventsUnion.Intersect(spec._eventsUnion).Any()).ToArray();
                var localPlant = ParallelComposition(localPlants);
                var localSup = MonolithicSupervisor(new[] {localPlant}, new[] {spec}, true);
                return (localPlant, localSup, localPlants);
            }).ToList();

            if (IsConflicting(supervisors.Select(t => t.localSup))) throw new Exception("Conflicting Supervisors");
            GC.Collect();
            return supervisors.AsParallel().SelectMany(t => LocalizeSupervisor(t.localPlant, t.localSup, t.localPlants, maxIt)).ToArray();
        }

        /// <summary>
        /// [Experimental]
        /// </summary>
        /// <param name="plants"></param>
        /// <param name="specifications"></param>
        /// <param name="maxIt"></param>
        /// <returns></returns>
        public static IEnumerable<DFA> LocalModularReducedSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, long maxIt = long.MaxValue)
        {
            var supervisors = specifications.AsParallel().Select(spec =>
            {
                var localPlants = plants.Where(p => p._eventsUnion.Intersect(spec._eventsUnion).Any()).ToArray();
                var localPlant = ParallelComposition(localPlants);
                var localSup = MonolithicSupervisor(new[] { localPlant }, new[] { spec }, true);
                return (localPlant, localSup);
            }).ToList();

            if (IsConflicting(supervisors.Select(t => t.localSup))) throw new Exception("Conflicting Supervisors");
            GC.Collect();
            return supervisors.AsParallel().Select(t => ReduceSupervisor(t.localPlant, t.localSup, maxIt)).ToArray();
        }

        private static (Transition[] trans, AbstractState initial) Reduction(DFA P, DFA S, HashSet<AbstractEvent> Ek, long maxIt = long.MaxValue)
        {
            AbstractEvent[] events;
            if (Ek == null)
            {
                Ek = P.Events.ToSet();
                events = Ek.Where(e => e.IsControllable).ToArray();
            }
            else events = Ek.ToArray();

            var eventsP = P.Events.Except(S.Events).ToArray();
            var eventsS = S.Events.Except(P.Events).ToArray();

            var transP = P.Transitions.Union(P.States.SelectMany(q => eventsS.Select(e => new Transition(q, e, q)))).GroupBy(t => t.Origin).ToDictionary(g => g.Key, g => g.ToArray());
            var transS = S.Transitions.Union(S.States.SelectMany(q => eventsP.Select(e => new Transition(q, e, q)))).GroupBy(t => t.Origin).ToDictionary(g => g.Key, g => g.ToArray());

            var pairs = MatchingPairs(P, S, transP, transS);

            var E = transS.ToDictionary(k => k.Key, k => new HashSet<AbstractEvent>(k.Value.Select(t => t.Trigger)));
            var D = transS.ToDictionary(k => k.Key, k => events.Where(e => k.Value.All(t => t.Trigger != e) && pairs[k.Key].Any(qp => transP[qp].Any(t => t.Trigger == e))).ToSet());
            var M = transS.ToDictionary(k => k.Key, k => k.Key.IsMarked);
            var T = pairs.ToDictionary(k => k.Key, k => k.Value.Any(qp => qp.IsMarked));

            bool R(AbstractState q1, AbstractState q2) => !E[q1].Intersect(D[q2]).Any() && !E[q2].Intersect(D[q1]).Any() && (T[q1] != T[q2] || M[q1] == M[q2]);

            var states = S.States.OrderBy(q => D[q].Count).ThenByDescending(q => E[q].Count).ToArray();
            var stateIdx = Enumerable.Range(0, states.Length).ToDictionary(i => states[i], i => i);
            var C = Enumerable.Range(0, states.Length).Select(i => new HashSet<int> { i }).ToArray();
            var waitList = new HashSet<(int, int)>();
            int cnode, it = 0;

            bool CheckMergibility(int i, int j)
            {
                if (it++ > maxIt) return false;

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
                        if (waitList.Contains((xp, xq)) || waitList.Contains((xq, xp))) continue;
                        if (!R(states[xp], states[xq])) return false;
                        waitList.Add((xp, xq));
                        var evs = transS[states[xp]].Select(t => t.Trigger)
                            .Intersect(transS[states[xq]].Select(t => t.Trigger));
                        foreach (var ev in evs)
                        {
                            var xpd = stateIdx[transS[states[xp]].Single(t => t.Trigger == ev).Destination];
                            var xqd = stateIdx[transS[states[xq]].Single(t => t.Trigger == ev).Destination];

                            if (xpd == xqd || waitList.Contains((xqd, xpd)) || waitList.Contains((xpd, xqd))) continue;
                            if (C[xpd].Min() < cnode || C[xqd].Min() < cnode) return false;
                            if (!CheckMergibility(xpd, xqd)) return false;
                        }
                    }
                }

                return true;
            }

            for (int i = 0; i < states.Length; i++)
            {
                if (i > C[i].Min()) continue;
                for (int j = i + 1; j < states.Length; j++)
                {
                    if (j > C[j].Min()) continue;
                    waitList.Clear();
                    cnode = i;
                    const int stackSize = 10000000;
                    var flag = false;
                    var thread = new Thread(() => flag = CheckMergibility(i, j), stackSize);
                    thread.Start();
                    thread.Join();

                    if (!flag) continue;

                    foreach (var (x, y) in waitList)
                    {
                        var set = C[x];
                        set.UnionWith(C[y]);

                        foreach (var k in set) C[k] = set;
                    }
                }
            }

            var cover = C.Distinct().ToArray();
            var snum = 0;
            var cover2state = cover.OrderBy(x => x.Contains(stateIdx[S.InitialState]) ? 0 : 1).ToDictionary(set => set, set => new State($"{snum++}", set.Any(q => states[q].IsMarked) ? Marking.Marked : Marking.Unmarked));

            var trans = transS.SelectMany(k => k.Value).Select(t => new Transition(cover2state[C[stateIdx[t.Origin]]], t.Trigger, cover2state[C[stateIdx[t.Destination]]])).Distinct().ToArray();

            var Ecom = S.Events.Except(Ek).Where(e => trans.Any(t => t.Origin != t.Destination && t.Trigger == e)).ToArray();
            var Eloc = Ek.Union(Ecom).ToSet();

            trans = trans.Where(t => Eloc.Contains(t.Trigger)).ToArray();

            var initial = cover2state[cover.Single(x => x.Contains(stateIdx[S.InitialState]))];

            return (trans, initial);
        }
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

                        if (!visited.Contains(dest)) nextFront.Add(dest);
                    }
                }

                front = nextFront;
            }

            return visited.GroupBy(p => p.qs).ToDictionary(g => g.Key, g => g.Select(p => p.qp).ToArray());
        }
    }
}