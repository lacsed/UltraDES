using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UltraDES
{
    using DFA = DeterministicFiniteAutomaton;

    partial class DeterministicFiniteAutomaton
    {
        public DFA AccessiblePart
        {
            get
            {
                var G = Clone();
                G.RemoveNoAccessibleStates();
                return G;
            }
        }

        public DFA CoaccessiblePart
        {
            get
            {
                var G = Clone();
                G.RemoveBlockingStates();
                return G;
            }
        }

        public int KleeneClosure => throw new NotImplementedException("Sorry. This is still in TO-DO List");

        public DFA Minimal
        {
            get
            {
                if (IsEmpty()) return Clone();

                var G = AccessiblePart;
                G.simplify();
                G.minimize();
                return G;
            }
        }

        public DFA PrefixClosure
        {
            get
            {
                var marked = States.ToDictionary(s => s, s => s.ToMarked);

                return new DFA(
                    Transitions.Select(t => new Transition(marked[t.Origin], t.Trigger, marked[t.Destination])),
                    marked[InitialState], $"Prefix({Name})");
            }
        }

        public DFA Trim
        {
            get
            {
                var G = AccessiblePart;
                G.RemoveBlockingStates();
                return G;
            }
        }

        private void BuildProduct()
        {
            var n = _statesList.Count;
            var pos = new int[n];
            var nextPos = new int[n];
            _statesStack = new Stack<StatesTuple>();

            var initialState = new StatesTuple(pos, _bits, _tupleSize);

            _statesStack.Push(initialState);

            _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.GetInstance());
            _validStates.Add(initialState, false);

            while (_statesStack.Count > 0)
            {
                _statesStack.Pop().Get(pos, _bits, _maxSize);

                for (var e = 0; e < _eventsUnion.Length; ++e)
                {
                    var nextEvent = false;
                    for (var i = 0; i < n; ++i)
                    {
                        if (_adjacencyList[i].HasEvent(pos[i], e))
                            nextPos[i] = _adjacencyList[i][pos[i], e];
                        else
                        {
                            nextEvent = true;
                            break;
                        }
                    }

                    if (nextEvent) continue;
                    var nextState = new StatesTuple(nextPos, _bits, _tupleSize);

                    if (!_validStates.ContainsKey(nextState))
                    {
                        _statesStack.Push(nextState);
                        _validStates.Add(nextState, false);
                    }
                }
            }

            Size = (ulong) _validStates.Count;
        }

        private static DFA ConcatDFA(DFA G1, DFA G2)
        {
            if (G1._validStates != null) G1.simplify();
            if (G2._validStates != null) G2.simplify();

            var n = G1._adjacencyList.Count + G2._adjacencyList.Count;
            var G1G2 = G1.Clone(n);

            G1G2.Name += "||" + G2.Name;
            G1G2._adjacencyList.Clear();
            G1G2._eventsUnion = G1G2._eventsUnion.Concat(G2._eventsUnion).Distinct().OrderBy(i => i.Controllability)
                .ToArray();
            G1G2._eventsList.Clear();
            G1G2._statesList.AddRange(G2._statesList);
            G1G2._validStates = null;
            G1G2._numberOfPlants = n;
            G1G2.Size *= G2.Size;

            G1G2._tupleSize = 1;
            var k = 0;
            for (var i = 0; i < n; ++i)
            {
                G1G2._bits[i] = k;
                var p = (int) Math.Max(Math.Ceiling(Math.Log(G1G2._statesList[i].Length, 2)), 1);
                G1G2._maxSize[i] = (1 << p) - 1;
                k += p;
                if (k > sizeof(int) * 8)
                {
                    G1G2._bits[i] = 0;
                    ++G1G2._tupleSize;
                    k = p;
                }
            }

            for (var i = 0; i < G1._adjacencyList.Count; ++i)
            {
                G1G2._eventsList.Add(new bool[G1G2._eventsUnion.Length]);
                for (var e = 0; e < G1._eventsUnion.Length; ++e)
                    G1G2._eventsList[i][Array.IndexOf(G1G2._eventsUnion, G1._eventsUnion[e])] = G1._eventsList[i][e];
                G1G2._adjacencyList.Add(new AdjacencyMatrix(G1._statesList[i].Length, G1G2._eventsUnion.Length));
                for (var j = 0; j < G1._statesList[i].Length; ++j)
                {
                    foreach (var p in G1._adjacencyList[i][j])
                        G1G2._adjacencyList[i].Add(j, Array.IndexOf(G1G2._eventsUnion, G1._eventsUnion[p.Key]),
                            p.Value);
                }
            }

            for (var i = G1._adjacencyList.Count; i < n; ++i)
            {
                G1G2._eventsList.Add(new bool[G1G2._eventsUnion.Length]);
                for (var e = 0; e < G2._eventsUnion.Length; ++e)
                {
                    G1G2._eventsList[i][Array.IndexOf(G1G2._eventsUnion, G2._eventsUnion[e])] =
                        G2._eventsList[i - G1._adjacencyList.Count][e];
                }

                G1G2._adjacencyList.Add(new AdjacencyMatrix(G1G2._statesList[i].Length, G1G2._eventsUnion.Length));
                for (var j = 0; j < G1G2._statesList[i].Length; ++j)
                {
                    foreach (var q in G2._adjacencyList[i - G1._adjacencyList.Count][j])
                        G1G2._adjacencyList[i].Add(j, Array.IndexOf(G1G2._eventsUnion, G2._eventsUnion[q.Key]),
                            q.Value);
                }
            }

            return G1G2;
        }

        public DFA InverseProjection(IEnumerable<AbstractEvent> events)
        {
            if (IsEmpty()) return Clone();

            var evs = events.Except(Events).ToList();

            var invProj = Clone();

            if (evs.Count > 0)
            {
                var envLength = _eventsUnion.Length + evs.Count;

                invProj._eventsUnion = invProj._eventsUnion.Union(evs).OrderBy(i => i.Controllability).ToArray();
                var evMap = _eventsUnion.Select(i => Array.IndexOf(invProj._eventsUnion, i)).ToArray();
                var evMapNew = evs.Select(i => Array.IndexOf(invProj._eventsUnion, i)).ToArray();

                for (var i = 0; i < _statesList.Count; ++i)
                {
                    invProj._adjacencyList[i] = new AdjacencyMatrix(_statesList[i].Length, envLength);
                    for (var j = 0; j < _statesList[i].Length; ++j)
                    {
                        for (var e = 0; e < _eventsUnion.Length; ++e)
                        {
                            if (_adjacencyList[i].HasEvent(j, e))
                                invProj._adjacencyList[i].Add(j, evMap[e], _adjacencyList[i][j, e]);
                        }

                        for (var e = 0; e < evs.Count; ++e)
                            invProj._adjacencyList[i].Add(j, evMapNew[e], j);
                    }

                    invProj._eventsList[i] = new bool[envLength];
                    for (var e = 0; e < _eventsUnion.Length; ++e)
                        invProj._eventsList[i][evMap[e]] = _eventsList[i][e];
                    for (var e = 0; e < evs.Count; ++e)
                        invProj._eventsList[i][evMapNew[e]] = true;
                }
            }

            invProj.Name = string.Format("InvProjection({0})", Name);
            return invProj;
        }

        public DFA InverseProjection(params AbstractEvent[] events)
        {
            return InverseProjection((IEnumerable<AbstractEvent>) events);
        }

        public static DFA ParallelComposition(IEnumerable<DFA> list, bool removeNoAccessibleStates = true)
        {
            return list.Aggregate((a, b) => a.ParallelCompositionWith(b, removeNoAccessibleStates));
        }

        public static DFA ParallelComposition(DFA A, params DFA[] others)
        {
            return A.ParallelCompositionWith(others, true);
        }

        public DFA ParallelCompositionWith(DFA G2, bool removeNoAccessibleStates = true)
        {
            var G1G2 = ConcatDFA(this, G2);

            if (removeNoAccessibleStates)
            {
                G1G2.RemoveNoAccessibleStates();
                GC.Collect();
                GC.Collect();
            }

            return G1G2;
        }

        public DFA ParallelCompositionWith(IEnumerable<DFA> list, bool removeNoAccessibleStates = true)
        {
            return ParallelCompositionWith(
                list.Aggregate((a, b) => a.ParallelCompositionWith(b, removeNoAccessibleStates)),
                removeNoAccessibleStates);
        }

        public DFA ParallelCompositionWith(params DFA[] others)
        {
            return ParallelCompositionWith(others, true);
        }

        public static DFA Product(IEnumerable<DFA> list)
        {
            if (!list.Any()) return null;
            var G1G2 = list.Aggregate((a, b) => ConcatDFA(a, b));
            G1G2.BuildProduct();
            return G1G2;
        }

        public static DFA Product(DFA G, params DFA[] others)
        {
            return G.ProductWith(others);
        }

        public DFA ProductWith(params DFA[] Gs)
        {
            return ProductWith((IEnumerable<DFA>) Gs);
        }

        public DFA ProductWith(IEnumerable<DFA> list)
        {
            var G1G2 = this;
            foreach (var G in list) G1G2 = ConcatDFA(G1G2, G);
            G1G2.BuildProduct();
            return G1G2;
        }

        public DFA Projection(IEnumerable<AbstractEvent> removeEvents)
        {
            if (IsEmpty()) return Clone();

            var evLength = _eventsUnion.Length;

            simplify();
            var evs = removeEvents.Select(e => Array.IndexOf(_eventsUnion, e)).Where(i => i >= 0).ToArray();
            var removeEvsArray = removeEvents.ToArray();
            var auxEvs = new List<int>();

            var y = 0;
            for (var e = 0; e < _eventsUnion.Length; ++e)
            {
                if (y < evs.Length && e == evs[y])
                {
                    ++y;
                    continue;
                }

                auxEvs.Add(e);
            }

            var rEvs = auxEvs.ToArray();

            var removeEventsHash = new bool[evLength];
            foreach (var e in evs) removeEventsHash[e] = true;

            if (evs.Count() == 0) return Clone();

            int[] statesMap;
            List<int>[] newStatesList;

            projectionStates(evs, out statesMap, out newStatesList);

            // Determinizing...

            var transitions = new Dictionary<int, int[]>();
            var frontier = new Stack<Tuple<int, HashSet<int>>>();
            var visited = new Dictionary<int[], int>(IntArrayComparator.GetInstance());
            visited.Add(new[] {0}, 0);

            var initial = new HashSet<int>();
            for (var i = 0; i < newStatesList[0].Count; ++i) initial.Add(newStatesList[0][i]);
            frontier.Push(new Tuple<int, HashSet<int>>(0, initial));

            var threadsRunning = 0;
            var projectionAction = new Action(() =>
            {
                while (true)
                {
                    lock (_lockObject2)
                    {
                        ++threadsRunning;
                    }

                    while (true)
                    {
                        int newStatePos;
                        HashSet<int> oldStates;
                        lock (_lockObject2)
                        {
                            if (frontier.Count == 0) break;
                            var states = frontier.Pop();
                            newStatePos = states.Item1;
                            oldStates = states.Item2;
                        }

                        var firstTransition = true;

                        for (var e = 0; e < rEvs.Length; ++e)
                        {
                            var nextNewStates = new HashSet<int>();
                            int k;
                            foreach (var state in oldStates)
                            {
                                if (!_adjacencyList[0].HasEvent(state, rEvs[e])) continue;
                                k = _adjacencyList[0][state, rEvs[e]];
                                if (!nextNewStates.Contains(statesMap[k])) nextNewStates.Add(statesMap[k]);
                            }

                            if (nextNewStates.Count > 0)
                            {
                                var sortedStates = nextNewStates.OrderBy(i => i).ToArray();
                                int nextPosition;
                                bool alreadyVisited;
                                lock (_lockObject3)
                                {
                                    alreadyVisited = visited.TryGetValue(sortedStates, out nextPosition);
                                    if (!alreadyVisited)
                                    {
                                        nextPosition = visited.Count;
                                        visited.Add(sortedStates, nextPosition);
                                    }
                                }

                                if (!alreadyVisited)
                                {
                                    var nextOldStates = new HashSet<int>();

                                    for (var i = 0; i < sortedStates.Length; ++i)
                                    {
                                        var t = newStatesList[sortedStates[i]];
                                        for (var j = 0; j < t.Count; ++j)
                                        {
                                            if (!nextOldStates.Contains(t[j]))
                                                nextOldStates.Add(t[j]);
                                        }
                                    }

                                    lock (_lockObject2)
                                    {
                                        frontier.Push(new Tuple<int, HashSet<int>>(nextPosition, nextOldStates));
                                    }
                                }

                                if (firstTransition)
                                {
                                    var transition = new int[rEvs.Length];
                                    for (var x = 0; x < transition.Length; ++x) transition[x] = -1;
                                    transition[e] = nextPosition;
                                    firstTransition = false;
                                    lock (_lockObject)
                                    {
                                        transitions.Add(newStatePos, transition);
                                    }
                                }
                                else
                                {
                                    lock (_lockObject)
                                    {
                                        transitions[newStatePos][e] = nextPosition;
                                    }
                                }
                            }
                        }
                    }

                    lock (_lockObject2)
                    {
                        --threadsRunning;
                        if (threadsRunning == 0) break;
                    }

                    Thread.Sleep(5);
                }
            });

            var threads = new Task[NumberOfThreads - 1];

            for (var i = 0; i < NumberOfThreads - 1; ++i) threads[i] = Task.Factory.StartNew(() => projectionAction());

            projectionAction();

            for (var i = 0; i < NumberOfThreads - 1; ++i) threads[i].Wait();

            var statesList = new AbstractState[visited.Count];
            foreach (var t in visited)
            {
                var oldPositions = new List<int>();
                var newPositions = t.Key;
                foreach (var t1 in newPositions)
                foreach (var t2 in newStatesList[t1])
                    if (!oldPositions.Contains(t2))
                        oldPositions.Add(t2);

                statesList[t.Value] = mergeStates(oldPositions, 0);
            }

            var proj = new DFA(1);
            proj._eventsUnion = new Event[rEvs.Length];
            proj._statesList.Add(statesList);
            proj._eventsList.Add(new bool[rEvs.Length]);
            proj._adjacencyList.Add(new AdjacencyMatrix(statesList.Length, rEvs.Length));
            proj.Size = (ulong) visited.Count;

            for (var e = 0; e < rEvs.Length; ++e)
            {
                proj._eventsUnion[e] = _eventsUnion[rEvs[e]];
                proj._eventsList[0][e] = _eventsList[0][rEvs[e]];
            }

            foreach (var j in transitions)
                for (var e = 0; e < j.Value.Length; ++e)
                {
                    if (j.Value[e] != -1)
                        proj._adjacencyList[0].Add(j.Key, e, j.Value[e]);
                }

            proj._adjacencyList[0].TrimExcess();

            proj.Name = $"Projection({Name})";

            proj._tupleSize = 1;
            proj._bits[0] = 0;
            proj._maxSize[0] = _maxSize[0];
            return proj;
        }

        public DFA Projection(params AbstractEvent[] removeEvents)
        {
            return Projection((IEnumerable<AbstractEvent>) removeEvents);
        }

        private void projectionStates(int[] evs, out int[] statesMap, out List<int>[] newStatesList)
        {
            var size = (int) Size;
            var newStates = new List<List<int>>();
            statesMap = new int[size];
            var numInvalid = 0;

            for (var i = 0; i < size; ++i) statesMap[i] = -1;

            for (var i = 0; i < size; ++i)
            {
                var currentPosition = statesMap[i];
                if (currentPosition == -1)
                {
                    var newGroup = new List<int>();
                    newGroup.Add(i);
                    newStates.Add(newGroup);
                    currentPosition = newStates.Count - 1;
                    statesMap[i] = currentPosition;
                }

                for (var e = 0; e < evs.Length; ++e)
                {
                    if (_adjacencyList[0].HasEvent(i, evs[e]))
                    {
                        var next = _adjacencyList[0][i, evs[e]];
                        var newNext = statesMap[next];
                        if (newNext == -1)
                        {
                            newStates[currentPosition].Add(next);
                            statesMap[next] = currentPosition;
                        }
                        else if (newNext != currentPosition)
                        {
                            if (newStates[currentPosition].Count < newStates[newNext].Count)
                            {
                                var aux = newNext;
                                newNext = currentPosition;
                                currentPosition = aux;
                            }

                            foreach (var st in newStates[newNext])
                            {
                                newStates[currentPosition].Add(st);
                                statesMap[st] = currentPosition;
                            }

                            newStates[newNext] = null;
                            ++numInvalid;
                        }
                    }
                }
            }

            newStatesList = new List<int>[newStates.Count - numInvalid];

            var k = -1;
            var initialPos = statesMap[0];
            if (initialPos != 0)
            {
                newStatesList[++k] = newStates[initialPos];
                foreach (var st in newStatesList[k]) statesMap[st] = k;
                newStates[initialPos] = null;
            }

            for (var i = 0; i < newStates.Count; ++i)
            {
                if (newStates[i] == null) continue;
                newStatesList[++k] = newStates[i];
                foreach (var st in newStatesList[k]) statesMap[st] = k;
            }
        }

        public static bool Isomorphism(DFA G1, DFA G2)
        {
            var tran1 = G1.Transitions.GroupBy(t => t.Origin)
                .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Trigger, t => t.Destination));
            var tran2 = G2.Transitions.GroupBy(t => t.Origin)
                .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Trigger, t => t.Destination));

            if (!new HashSet<AbstractEvent>(G1.Events).SetEquals(new HashSet<AbstractEvent>(G2.Events))) return false;

            var events = G1.Events;


            var ini = (G1.InitialState, G2.InitialState);
            var visited = new HashSet<(AbstractState q1,AbstractState q2)>();
            var frontier = new HashSet<(AbstractState q1, AbstractState q2)> {ini};

            while (frontier.Any())
            {
                visited.UnionWith(frontier);
                var newFrontier = new HashSet<(AbstractState, AbstractState)>();

                foreach (var (q1,q2) in frontier)
                {
                    if(!tran1.ContainsKey(q1) && !tran2.ContainsKey(q2)) continue;
                    if (!tran1.ContainsKey(q1) || !tran2.ContainsKey(q2)) return false;

                    foreach (var e in events)
                    {
                        if(!tran1[q1].ContainsKey(e) && !tran2[q2].ContainsKey(e)) continue;
                        if (!tran1[q1].ContainsKey(e) || !tran2[q2].ContainsKey(e)) return false;

                        if (!visited.Contains((tran1[q1][e], tran2[q2][e]))) newFrontier.Add((tran1[q1][e], tran2[q2][e]));
                    }

                }

                frontier = newFrontier;
            }

            return visited.All(q => q.q1.Marking == q.q2.Marking);
        }
    }
}