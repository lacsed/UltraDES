using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UltraDES
{
    using DFA = DeterministicFiniteAutomaton;

    partial class DeterministicFiniteAutomaton
    {
        private Tuple<bool, bool, int[], int[]> CheckState(DFA G, int nG, int nS, int[] posG, int[] posS, int eG,
            int eS)
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

        public Controllability Controllability(params DFA[] plants)
        {
            return Controllability((IEnumerable<DFA>) plants);
        }

        public Controllability Controllability(IEnumerable<DFA> plants)
        {
            return ControllabilityAndDisabledEvents(plants, false).Item1;
        }

        public Tuple<Controllability, Dictionary<AbstractState, List<AbstractEvent>>> ControllabilityAndDisabledEvents(
            params DFA[] plants)
        {
            return ControllabilityAndDisabledEvents(plants, true);
        }

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
            {
                _validStates = new Dictionary<StatesTuple, bool>((int) Size, StatesTupleComparator.GetInstance());
            }

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
                    currentState = composeState(pos2);
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

        public Dictionary<AbstractState, List<AbstractEvent>> DisabledEvents(params DFA[] plants)
        {
            return DisabledEvents((IEnumerable<DFA>) plants);
        }

        public Dictionary<AbstractState, List<AbstractEvent>> DisabledEvents(IEnumerable<DFA> plants)
        {
            return ControllabilityAndDisabledEvents(plants).Item2;
        }

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

            for (var i = 0; i < NumberOfThreads - 1; ++i) vThreads[i] = Task.Factory.StartNew(() => findStates(nPlant));

            findStates(nPlant);

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

        public static bool IsConflicting(IEnumerable<DFA> supervisors)
        {
            Parallel.ForEach(supervisors, s => { s.simplify(); });

            var composition = supervisors.Aggregate((a, b) => a.ParallelCompositionWith(b));
            var oldSize = composition.Size;
            composition.RemoveBlockingStates();
            return composition.Size != oldSize;
        }

        public bool IsControllable(params DFA[] plants)
        {
            return IsControllable((IEnumerable<DFA>) plants);
        }

        public bool IsControllable(IEnumerable<DFA> plants)
        {
            return Controllability(plants) == UltraDES.Controllability.Controllable;
        }

        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications,
            IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            if (conflictResolvingSupervisor == null) conflictResolvingSupervisor = new DFA[0];
            ;
            var supervisors = specifications.Select(e =>
            {
                return MonolithicSupervisor(plants.Where(p => p._eventsUnion.Intersect(e._eventsUnion).Any()),
                    new[] {e});
            });

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete)) throw new Exception("conflicting supervisors");
            GC.Collect();
            return complete;
        }

        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications,
            out List<DFA> compoundPlants, IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            if (conflictResolvingSupervisor == null) conflictResolvingSupervisor = new DFA[0];

            var dic = specifications.ToDictionary(e =>
            {
                return plants.Where(p => p._eventsUnion.Intersect(e._eventsUnion).Any()).Aggregate((a, b) =>
                {
                    return a.ParallelCompositionWith(b);
                });
            });

            var supervisors = dic.AsParallel()
                .Select(automata => MonolithicSupervisor(new[] {automata.Key}, new[] {automata.Value})).ToList();

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete)) throw new Exception("conflicting supervisors");

            compoundPlants = dic.Keys.ToList();
            GC.Collect();
            return complete;
        }

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

        private static Dictionary<AbstractState, AbstractState> SupervisorPlantState(DeterministicFiniteAutomaton sup,
            DeterministicFiniteAutomaton plant, HashSet<AbstractState> forbidden)
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
    }
}