using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UltraDES
{
    using DFA = DeterministicFiniteAutomaton;

    /// <summary>
    /// Class DeterministicFiniteAutomaton. This class cannot be inherited.
    /// </summary>
    partial class DeterministicFiniteAutomaton
    {
        /// <summary>
        /// Gets the accessible part.
        /// </summary>
        /// <value>The accessible part.</value>
        public DFA AccessiblePart
        {
            get
            {
                var G = Clone();
                G.RemoveNoAccessibleStates();
                return G;
            }
        }

        /// <summary>
        /// Gets the coaccessible part.
        /// </summary>
        /// <value>The coaccessible part.</value>
        public DFA CoaccessiblePart
        {
            get
            {
                var G = Clone();
                G.RemoveBlockingStates();
                return G;
            }
        }

        /// <summary>
        /// Gets the kleene closure.
        /// </summary>
        /// <value>The kleene closure.</value>
        /// <exception cref="NotImplementedException">Sorry. This is still in TO-DO List</exception>
        public int KleeneClosure => throw new NotImplementedException("Sorry. This is still in TO-DO List");

        /// <summary>
        /// Gets the minimal.
        /// </summary>
        /// <value>The minimal.</value>
        public DFA Minimal
        {
            get
            {
                if (IsEmpty()) return Clone();

                var G = AccessiblePart;
                G.Simplify();
                G.Minimize();
                return G;
            }
        }

        /// <summary>
        /// Gets the prefix closure.
        /// </summary>
        /// <value>The prefix closure.</value>
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

        /// <summary>
        /// Gets the trim.
        /// </summary>
        /// <value>The trim.</value>
        public DFA Trim
        {
            get
            {
                var G = AccessiblePart;
                G.RemoveBlockingStates();
                return G;
            }
        }

        /// <summary>
        /// Builds the product.
        /// </summary>
        private void BuildProduct()
        {
            var numAutomata = _statesList.Count;
            var origin = new int[numAutomata];
            var destination = new int[numAutomata];
            _statesStack = new Stack<StatesTuple>();

            var initialState = new StatesTuple(origin, _bits, _tupleSize);

            _statesStack.Push(initialState);

            _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.GetInstance()) {{initialState, false}};

            while (_statesStack.Count > 0)
            {
                _statesStack.Pop().Get(origin, _bits, _maxSize);

                for (var e = 0; e < _eventsUnion.Length; ++e)
                {
                    var nextEvent = false;
                    for (var i = 0; i < numAutomata; ++i)
                    {
                        if (_adjacencyList[i].HasEvent(origin[i], e))
                            destination[i] = _adjacencyList[i][origin[i], e];
                        else
                        {
                            nextEvent = true;
                            break;
                        }
                    }

                    if (nextEvent) continue;
                    var nextState = new StatesTuple(destination, _bits, _tupleSize);

                    if (_validStates.ContainsKey(nextState)) continue;
                    _statesStack.Push(nextState);
                    _validStates.Add(nextState, false);
                }
            }

            Size = _validStates.Count;
        }

        /// <summary>
        /// Concatenations the specified g1.
        /// </summary>
        /// <param name="G1">The g1.</param>
        /// <param name="G2">The g2.</param>
        /// <returns>DFA.</returns>
        private static DFA Concatenation(DFA G1, DFA G2)
        {
            if (G1._validStates != null) G1.Simplify();
            if (G2._validStates != null) G2.Simplify();

            var n = G1._adjacencyList.Count + G2._adjacencyList.Count;
            var G12 = G1.Clone(n);

            G12.Name += "||" + G2.Name;
            G12._adjacencyList.Clear();
            G12._eventsUnion = G12._eventsUnion.Concat(G2._eventsUnion).Distinct().OrderBy(i => i.Controllability)
                .ToArray();
            G12._eventsList.Clear();
            G12._statesList.AddRange(G2._statesList);
            G12._validStates = null;
            G12._numberOfPlants = n;
            G12.Size *= G2.Size;

            G12._tupleSize = 1;
            var k = 0;
            for (var i = 0; i < n; ++i)
            {
                G12._bits[i] = k;
                var p = MinNumOfBits(G12._statesList[i].Length);//var p = (int) Math.Max(Math.Ceiling(Math.Log(G12._statesList[i].Length, 2)), 1);
                G12._maxSize[i] = (1 << p) - 1;
                k += p;
                if (k <= sizeof(int) * 8) continue;

                G12._bits[i] = 0;
                ++G12._tupleSize;
                k = p;
            }

            for (var i = 0; i < G1._adjacencyList.Count; ++i)
            {
                G12._eventsList.Add(new bool[G12._eventsUnion.Length]);
                for (var e = 0; e < G1._eventsUnion.Length; ++e)
                    G12._eventsList[i][Array.IndexOf(G12._eventsUnion, G1._eventsUnion[e])] = G1._eventsList[i][e];
                G12._adjacencyList.Add(new AdjacencyMatrix(G1._statesList[i].Length, G12._eventsUnion.Length));
                for (var j = 0; j < G1._statesList[i].Length; ++j)
                {
                    foreach (var p in G1._adjacencyList[i][j])
                        G12._adjacencyList[i].Add(j, Array.IndexOf(G12._eventsUnion, G1._eventsUnion[p.e]), p.s);
                }
            }

            for (var i = G1._adjacencyList.Count; i < n; ++i)
            {
                G12._eventsList.Add(new bool[G12._eventsUnion.Length]);
                for (var e = 0; e < G2._eventsUnion.Length; ++e)
                {
                    G12._eventsList[i][Array.IndexOf(G12._eventsUnion, G2._eventsUnion[e])] =
                        G2._eventsList[i - G1._adjacencyList.Count][e];
                }

                G12._adjacencyList.Add(new AdjacencyMatrix(G12._statesList[i].Length, G12._eventsUnion.Length));
                for (var j = 0; j < G12._statesList[i].Length; ++j)
                {
                    foreach (var q in G2._adjacencyList[i - G1._adjacencyList.Count][j])
                        G12._adjacencyList[i].Add(j, Array.IndexOf(G12._eventsUnion, G2._eventsUnion[q.e]), q.s);
                }
            }

            return G12;
        }

        static readonly int[] multiplyDeBruijnBitPosition2 =
        [
            1,
            1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8, 31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18,
            6, 11, 5, 10, 9
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MinNumOfBits(int n)
        {
            

            var v = (uint) n;
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;

            return multiplyDeBruijnBitPosition2[(v * 0x077CB531U) >> 27];
        }

        /// <summary>
        /// Inverses the projection.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <returns>DFA.</returns>
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

            invProj.Name = $"InvProjection({Name})";
            return invProj;
        }

        /// <summary>
        /// Inverses the projection.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <returns>DFA.</returns>
        public DFA InverseProjection(params AbstractEvent[] events) => InverseProjection((IEnumerable<AbstractEvent>) events);

        /// <summary>
        /// Parallels the composition.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="removeNoAccessibleStates">if set to <c>true</c> [remove no accessible states].</param>
        /// <returns>DFA.</returns>
        public static DFA ParallelComposition(IEnumerable<DFA> list, bool removeNoAccessibleStates = true) => list.Aggregate((a, b) => a.ParallelCompositionWith(b, removeNoAccessibleStates));

        /// <summary>
        /// Parallels the composition.
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="others">The others.</param>
        /// <returns>DFA.</returns>
        public static DFA ParallelComposition(DFA A, params DFA[] others) => A.ParallelCompositionWith(others, true);

        /// <summary>
        /// Parallels the composition with.
        /// </summary>
        /// <param name="G2">The g2.</param>
        /// <param name="removeNoAccessibleStates">if set to <c>true</c> [remove no accessible states].</param>
        /// <returns>DFA.</returns>
        public DFA ParallelCompositionWith(DFA G2, bool removeNoAccessibleStates = true)
        {
            var G1G2 = Concatenation(this, G2);

            if (!removeNoAccessibleStates) return G1G2;
            
            G1G2.RemoveNoAccessibleStates();
            GC.Collect();
            GC.Collect();

            return G1G2;
        }

        /// <summary>
        /// Parallels the composition with.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="removeNoAccessibleStates">if set to <c>true</c> [remove no accessible states].</param>
        /// <returns>DFA.</returns>
        public DFA ParallelCompositionWith(IEnumerable<DFA> list, bool removeNoAccessibleStates = true) =>
            ParallelCompositionWith(list.Aggregate((a, b) => a.ParallelCompositionWith(b, removeNoAccessibleStates)),
                removeNoAccessibleStates);

        /// <summary>
        /// Parallels the composition with.
        /// </summary>
        /// <param name="others">The others.</param>
        /// <returns>DFA.</returns>
        public DFA ParallelCompositionWith(params DFA[] others) => ParallelCompositionWith(others, true);

        /// <summary>
        /// Products the specified list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>DFA.</returns>
        public static DFA Product(IEnumerable<DFA> list)
        {
            if (!list.Any()) return null;
            var G1G2 = list.Aggregate(Concatenation);
            G1G2.BuildProduct();
            return G1G2;
        }

        /// <summary>
        /// Products the specified g.
        /// </summary>
        /// <param name="G">The g.</param>
        /// <param name="others">The others.</param>
        /// <returns>DFA.</returns>
        public static DFA Product(DFA G, params DFA[] others)
        {
            return G.ProductWith(others);
        }

        /// <summary>
        /// Products the with.
        /// </summary>
        /// <param name="Gs">The gs.</param>
        /// <returns>DFA.</returns>
        public DFA ProductWith(params DFA[] Gs)
        {
            return ProductWith((IEnumerable<DFA>) Gs);
        }

        /// <summary>
        /// Products the with.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>DFA.</returns>
        public DFA ProductWith(IEnumerable<DFA> list)
        {
            var G1G2 = this;
            G1G2 = list.Aggregate(G1G2, Concatenation);
            G1G2.BuildProduct();
            return G1G2;
        }

        /// <summary>
        /// Projections the specified remove events.
        /// </summary>
        /// <param name="removeEvents">The remove events.</param>
        /// <returns>DFA.</returns>
        public DFA Projection(IEnumerable<AbstractEvent> removeEvents)
        {
            var transitions = Transitions.Select(t => removeEvents.Contains(t.Trigger) ? new Transition(t.Origin, Epsilon.EpsilonEvent, t.Destination) : t);

            return new NondeterministicFiniteAutomaton(transitions, InitialState, Name).Determinize;
        }

        /// <summary>
        /// Projections the specified remove events.
        /// </summary>
        /// <param name="removeEvents">The remove events.</param>
        /// <returns>DFA.</returns>
        public DFA Projection(params AbstractEvent[] removeEvents) => Projection((IEnumerable<AbstractEvent>) removeEvents);

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