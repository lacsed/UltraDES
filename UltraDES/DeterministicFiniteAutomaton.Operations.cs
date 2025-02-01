using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UltraDES;

using DFA = DeterministicFiniteAutomaton;

/// <summary>
/// Partial implementation of the <see cref="DeterministicFiniteAutomaton"/> class that provides
/// additional operations such as computing accessible/coaccessible parts, minimization, product,
/// concatenation, projection, and isomorphism tests. This class cannot be inherited.
/// </summary>
partial class DeterministicFiniteAutomaton
{
    /// <summary>
    /// Gets the accessible (reachable) part of the automaton.
    /// This operation removes states that cannot be reached from the initial state.
    /// </summary>
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
    /// Gets the coaccessible (coreachable) part of the automaton.
    /// This operation removes states from which no marked (accepting) state can be reached.
    /// </summary>
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
    /// Gets the Kleene closure of the automaton.
    /// Note: This functionality is not yet implemented.
    /// </summary>
    /// <exception cref="NotImplementedException">
    /// Thrown to indicate that this method is still on the TO-DO list.
    /// </exception>
    public int KleeneClosure => throw new NotImplementedException("Sorry. This is still in TO-DO List");

    /// <summary>
    /// Gets the minimal (minimized) version of the automaton.
    /// The automaton is first reduced to its accessible part, then simplified and minimized.
    /// </summary>
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
    /// Gets the prefix closure of the automaton.
    /// This operation returns a new automaton where every state is made marked, so that every prefix of a word is accepted.
    /// </summary>
    public DFA PrefixClosure
    {
        get
        {
            // Create a mapping of each state to a marked version of that state.
            var marked = States.ToDictionary(s => s, s => s.ToMarked);

            return new DFA(
                Transitions.Select(t => new Transition(marked[t.Origin], t.Trigger, marked[t.Destination])),
                marked[InitialState],
                $"Prefix({Name})");
        }
    }

    /// <summary>
    /// Gets the trimmed version of the automaton.
    /// This operation returns an automaton that is both accessible and coaccessible.
    /// </summary>
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
    /// Builds the product (state-space) of the automaton.
    /// This method computes the full set of reachable composite states (the product) and stores them in _validStates.
    /// </summary>
    private void BuildProduct()
    {
        int numAutomata = _statesList.Count;
        var origin = new int[numAutomata];
        var destination = new int[numAutomata];
        var statesStack = new Stack<StatesTuple>();

        // Create the initial composite state tuple.
        var initialState = new StatesTuple(origin, _bits, _tupleSize);
        statesStack.Push(initialState);

        _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.GetInstance())
        {
            { initialState, false }
        };

        // Explore the product state space.
        while (statesStack.Count > 0)
        {
            statesStack.Pop().Get(origin, _bits, _maxSize);

            for (var e = 0; e < _eventsUnion.Length; ++e)
            {
                bool nextEvent = false;
                for (var i = 0; i < numAutomata; ++i)
                {
                    if (_adjacencyList[i].TryGet(origin[i], e, out int val))
                        destination[i] = val;
                    else
                    {
                        nextEvent = true;
                        break;
                    }
                }

                if (nextEvent) continue;
                var nextState = new StatesTuple(destination, _bits, _tupleSize);

                if (_validStates.ContainsKey(nextState)) continue;
                statesStack.Push(nextState);
                _validStates.Add(nextState, false);
            }
        }

        Size = _validStates.Count;
    }

    /// <summary>
    /// Concatenates two automata into one.
    /// This method builds a new automaton that represents the sequential execution of G1 followed by G2.
    /// </summary>
    /// <param name="G1">The first automaton.</param>
    /// <param name="G2">The second automaton.</param>
    /// <returns>A new DFA representing the concatenation of G1 and G2.</returns>
    private static DFA Concatenation(DFA G1, DFA G2)
    {
        if (G1._validStates != null) G1.Simplify();
        if (G2._validStates != null) G2.Simplify();

        int n = G1._adjacencyList.Count + G2._adjacencyList.Count;
        var G12 = G1.Clone(n);

        G12.Name += "||" + G2.Name;
        G12._adjacencyList.Clear();
        G12._eventsUnion = G12._eventsUnion.Concat(G2._eventsUnion)
                                           .Distinct()
                                           .OrderBy(i => i.Controllability)
                                           .ToArray();
        G12._eventsList.Clear();
        G12._statesList.AddRange(G2._statesList);
        G12._validStates = null;
        G12._numberOfPlants = n;
        G12.Size *= G2.Size;

        // Compute new tuple sizes and bit masks.
        G12._tupleSize = 1;
        int k = 0;
        for (var i = 0; i < n; ++i)
        {
            G12._bits[i] = k;
            int p = MinNumOfBits(G12._statesList[i].Length);
            G12._maxSize[i] = (1 << p) - 1;
            k += p;
            if (k <= sizeof(int) * 8) continue;

            G12._bits[i] = 0;
            ++G12._tupleSize;
            k = p;
        }

        // Process transitions for automata from G1.
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

        // Process transitions for automata from G2.
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

    /// <summary>
    /// Returns the minimum number of bits required to represent a number.
    /// This method uses a de Bruijn sequence to efficiently determine the number of bits.
    /// </summary>
    /// <param name="n">The number to represent.</param>
    /// <returns>The minimum number of bits required to represent n.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MinNumOfBits(int n)
    {
        // De Bruijn sequence lookup table for 32-bit integers.
        // Note: multiplyDeBruijnBitPosition2[ ] has been defined as a static readonly field.
        var v = (uint)n;
        v--;
        v |= v >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;
        v++;

        return multiplyDeBruijnBitPosition2[(v * 0x077CB531U) >> 27];
    }

    // Lookup table used in MinNumOfBits.
    static readonly int[] multiplyDeBruijnBitPosition2 =
    {
        1,
        1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8, 31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18,
        6, 11, 5, 10, 9
    };

    /// <summary>
    /// Computes the inverse projection of the automaton with respect to the given set of events.
    /// The inverse projection adds the specified events as self-loops on every state.
    /// </summary>
    /// <param name="events">The events to be added to the projection.</param>
    /// <returns>A new DFA resulting from the inverse projection.</returns>
    public DFA InverseProjection(IEnumerable<AbstractEvent> events)
    {
        if (IsEmpty()) return Clone();

        // Calculate the events that are not originally in the automaton.
        var evs = events.Except(Events).ToList();
        var invProj = Clone();

        if (evs.Count > 0)
        {
            int envLength = _eventsUnion.Length + evs.Count;

            // Update the union of events and create mapping arrays.
            invProj._eventsUnion = invProj._eventsUnion.Union(evs)
                                                       .OrderBy(i => i.Controllability)
                                                       .ToArray();
            var evMap = _eventsUnion.Select(i => Array.IndexOf(invProj._eventsUnion, i)).ToArray();
            var evMapNew = evs.Select(i => Array.IndexOf(invProj._eventsUnion, i)).ToArray();

            // Update the transitions and events availability for each component.
            for (var i = 0; i < _statesList.Count; ++i)
            {
                invProj._adjacencyList[i] = new AdjacencyMatrix(_statesList[i].Length, envLength);
                for (var j = 0; j < _statesList[i].Length; ++j)
                {
                    for (var e = 0; e < _eventsUnion.Length; ++e)
                    {
                        if (_adjacencyList[i].TryGet(j, e, out int val))
                            invProj._adjacencyList[i].Add(j, evMap[e], val);
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
    /// Overload of <see cref="InverseProjection(IEnumerable{AbstractEvent})"/> that accepts a parameter array.
    /// </summary>
    /// <param name="events">The events to be added.</param>
    /// <returns>A new DFA resulting from the inverse projection.</returns>
    public DFA InverseProjection(params AbstractEvent[] events) => InverseProjection((IEnumerable<AbstractEvent>)events);

    /// <summary>
    /// Computes the parallel composition of a collection of automata.
    /// The result is obtained by aggregating the automata using the parallel composition operator.
    /// </summary>
    /// <param name="list">The collection of automata to compose in parallel.</param>
    /// <param name="removeNoAccessibleStates">
    /// If true, states that are not accessible will be removed from the resulting automaton.
    /// </param>
    /// <returns>A new DFA representing the parallel composition.</returns>
    public static DFA ParallelComposition(IEnumerable<DFA> list, bool removeNoAccessibleStates = true) =>
        list.Aggregate((a, b) => a.ParallelCompositionWith(b, removeNoAccessibleStates));

    /// <summary>
    /// Computes the parallel composition of the current automaton with additional automata.
    /// </summary>
    /// <param name="A">The first automaton.</param>
    /// <param name="others">Other automata to compose in parallel with the first.</param>
    /// <returns>A new DFA representing the parallel composition.</returns>
    public static DFA ParallelComposition(DFA A, params DFA[] others) => A.ParallelCompositionWith(others, true);

    /// <summary>
    /// Computes the parallel composition of the current automaton with another automaton.
    /// Optionally removes inaccessible states from the resulting automaton.
    /// </summary>
    /// <param name="G2">The second automaton.</param>
    /// <param name="removeNoAccessibleStates">
    /// If true, inaccessible states will be removed after the composition.
    /// </param>
    /// <returns>A new DFA representing the parallel composition.</returns>
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
    /// Computes the parallel composition of the current automaton with a collection of automata.
    /// </summary>
    /// <param name="list">The collection of automata to compose with.</param>
    /// <param name="removeNoAccessibleStates">
    /// If true, inaccessible states will be removed from the result.
    /// </param>
    /// <returns>A new DFA representing the parallel composition.</returns>
    public DFA ParallelCompositionWith(IEnumerable<DFA> list, bool removeNoAccessibleStates = true) =>
        ParallelCompositionWith(list.Aggregate((a, b) => a.ParallelCompositionWith(b, removeNoAccessibleStates)),
                                  removeNoAccessibleStates);

    /// <summary>
    /// Overload of <see cref="ParallelCompositionWith(IEnumerable{DFA}, bool)"/> that accepts a parameter array.
    /// </summary>
    /// <param name="others">Other automata to compose in parallel with the current automaton.</param>
    /// <returns>A new DFA representing the parallel composition.</returns>
    public DFA ParallelCompositionWith(params DFA[] others) => ParallelCompositionWith(others, true);

    /// <summary>
    /// Computes the product of a collection of automata.
    /// The product is obtained by first concatenating the automata and then building the product state space.
    /// </summary>
    /// <param name="list">The collection of automata.</param>
    /// <returns>A new DFA representing the product; null if the list is empty.</returns>
    public static DFA Product(IEnumerable<DFA> list)
    {
        if (!list.Any()) return null;
        var G1G2 = list.Aggregate(Concatenation);
        G1G2.BuildProduct();
        return G1G2;
    }

    /// <summary>
    /// Computes the product of the current automaton with additional automata.
    /// </summary>
    /// <param name="G">The first automaton.</param>
    /// <param name="others">Other automata to include in the product.</param>
    /// <returns>A new DFA representing the product.</returns>
    public static DFA Product(DFA G, params DFA[] others) => G.ProductWith(others);

    /// <summary>
    /// Computes the product of the current automaton with additional automata.
    /// Overload that accepts a parameter array.
    /// </summary>
    /// <param name="Gs">Other automata to include in the product.</param>
    /// <returns>A new DFA representing the product.</returns>
    public DFA ProductWith(params DFA[] Gs) => ProductWith((IEnumerable<DFA>)Gs);

    /// <summary>
    /// Computes the product of the current automaton with a collection of automata.
    /// The method concatenates the automata, then builds the product state space.
    /// </summary>
    /// <param name="list">The collection of automata.</param>
    /// <returns>A new DFA representing the product.</returns>
    public DFA ProductWith(IEnumerable<DFA> list)
    {
        var G1G2 = this;
        G1G2 = list.Aggregate(G1G2, Concatenation);
        G1G2.BuildProduct();
        return G1G2;
    }

    /// <summary>
    /// Computes the projection of the automaton by removing specified events.
    /// Transitions labeled with a removed event are replaced by transitions labeled with the epsilon event.
    /// The resulting nondeterministic automaton is then determinized.
    /// </summary>
    /// <param name="removeEvents">The events to remove from the automaton.</param>
    /// <returns>A new DFA representing the projected automaton.</returns>
    public DFA Projection(IEnumerable<AbstractEvent> removeEvents)
    {
        var transitions = Transitions.Select(t =>
            removeEvents.Contains(t.Trigger)
                ? new Transition(t.Origin, Epsilon.EpsilonEvent, t.Destination)
                : t);
        return new NondeterministicFiniteAutomaton(transitions, InitialState, Name).Determinize;
    }

    /// <summary>
    /// Overload of <see cref="Projection(IEnumerable{AbstractEvent})"/> that accepts a parameter array.
    /// </summary>
    /// <param name="removeEvents">The events to remove.</param>
    /// <returns>A new DFA representing the projected automaton.</returns>
    public DFA Projection(params AbstractEvent[] removeEvents) => Projection((IEnumerable<AbstractEvent>)removeEvents);

    /// <summary>
    /// Determines whether two automata are isomorphic.
    /// Two automata are isomorphic if there exists a one-to-one correspondence between their states
    /// that preserves transitions and marking.
    /// </summary>
    /// <param name="G1">The first automaton.</param>
    /// <param name="G2">The second automaton.</param>
    /// <returns>
    /// True if G1 and G2 are isomorphic; otherwise, false.
    /// </returns>
    public static bool Isomorphism(DFA G1, DFA G2)
    {
        var tran1 = G1.Transitions.GroupBy(t => t.Origin)
            .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Trigger, t => t.Destination));
        var tran2 = G2.Transitions.GroupBy(t => t.Origin)
            .ToDictionary(g => g.Key, g => g.ToDictionary(t => t.Trigger, t => t.Destination));

        if (!new HashSet<AbstractEvent>(G1.Events).SetEquals(new HashSet<AbstractEvent>(G2.Events)))
            return false;

        var events = G1.Events;

        // Initialize the state pair (initial states of G1 and G2).
        var ini = (G1.InitialState, G2.InitialState);
        var visited = new HashSet<(AbstractState q1, AbstractState q2)>();
        var frontier = new HashSet<(AbstractState q1, AbstractState q2)> { ini };

        // Perform a breadth-first search over state pairs.
        while (frontier.Any())
        {
            visited.UnionWith(frontier);
            var newFrontier = new HashSet<(AbstractState, AbstractState)>();

            foreach (var (q1, q2) in frontier)
            {
                // If both states have no outgoing transitions, continue.
                if (!tran1.ContainsKey(q1) && !tran2.ContainsKey(q2)) continue;
                if (!tran1.ContainsKey(q1) || !tran2.ContainsKey(q2)) return false;

                // Compare transitions for each event.
                foreach (var e in events)
                {
                    if (!tran1[q1].ContainsKey(e) && !tran2[q2].ContainsKey(e)) continue;
                    if (!tran1[q1].ContainsKey(e) || !tran2[q2].ContainsKey(e)) return false;

                    var pair = (tran1[q1][e], tran2[q2][e]);
                    if (!visited.Contains(pair))
                        newFrontier.Add(pair);
                }
            }

            frontier = newFrontier;
        }

        // All corresponding state pairs must have matching marking.
        return visited.All(q => q.q1.Marking == q.q2.Marking);
    }
}
