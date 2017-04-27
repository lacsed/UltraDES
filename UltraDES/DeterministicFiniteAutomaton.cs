////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	DeterministicFiniteAutomaton.cs
//
// summary:	Implements the deterministic finite automaton class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace UltraDES
{
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using DFA = DeterministicFiniteAutomaton;
    [Serializable]
    public sealed class DeterministicFiniteAutomaton
    {
        private static int m_numberOfThreads = Math.Max(2, Environment.ProcessorCount);

        private Dictionary<StatesTuple, bool> m_validStates;

        private List<AdjacencyMatrix> m_adjacencyList;

        private AbstractEvent[] m_eventsUnion;

        private List<bool[]> m_eventsList;

        private List<AbstractState[]> m_statesList;

        private Stack<StatesTuple> m_statesStack = null;

        private List<List<int>[]>[] m_reverseTransitionsList;

        private int m_numberOfRunningThreads;

        private Object m_lockObject = new Object();

        private Object m_lockObject2 = new Object();

        private Object m_lockObject3 = new Object();

        private int[] m_bits;
        private int[] m_maxSize;
        private int m_tupleSize;
        private int m_numberOfPlants;

        public ulong Size { get; private set; }

        public string Name { get; private set; }

        public DFA clone(int capacity)
        {
            DFA G = new DFA(capacity);
            G.m_eventsUnion = (AbstractEvent[])m_eventsUnion.Clone();
            G.m_statesList.AddRange(m_statesList);

            for (int i = 0; i < m_adjacencyList.Count; ++i)
            {
                G.m_adjacencyList.Add(m_adjacencyList[i].Clone());
                G.m_eventsList.Add((bool[])m_eventsList[i].Clone());
                G.m_bits[i] = m_bits[i];
                G.m_maxSize[i] = m_maxSize[i];
            }

            G.Size = Size;
            G.Name = Name;
            G.m_numberOfPlants = m_numberOfPlants;
            G.m_tupleSize = m_tupleSize;

            if (m_validStates != null)
                G.m_validStates = new Dictionary<StatesTuple, bool>(m_validStates, StatesTupleComparator.getInstance());

            return G;
        }

        public DFA Clone()
        {
            return clone(m_statesList.Count());
        }

        static DeterministicFiniteAutomaton()
        {
            string currentPath = Directory.GetCurrentDirectory();
            string newPath = currentPath + "\\..\\..\\..\\USER";
            if(!Directory.Exists(newPath))
            {
                newPath = currentPath + "\\USER";
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
            }
            Directory.SetCurrentDirectory(newPath);
        }

        public DeterministicFiniteAutomaton(IEnumerable<Transition> transitions, AbstractState initial, string name)
            : this(1)
        {
            Name = name;

            var transitionsLocal = transitions as Transition[] ?? transitions.ToArray();

            m_statesList.Add(transitionsLocal.SelectMany(t => new[] { t.Origin, t.Destination }).Distinct().ToArray());
            m_eventsUnion = transitionsLocal.Select(t => t.Trigger).Distinct().OrderBy(i => i.Controllability).ToArray();
            m_adjacencyList.Add(new AdjacencyMatrix(m_statesList[0].Length, m_eventsUnion.Length));
            int initialIdx = Array.IndexOf(m_statesList[0], initial);
            if(initialIdx != 0)
            {
                AbstractState aux = m_statesList[0][0];
                m_statesList[0][0] = m_statesList[0][initialIdx];
                m_statesList[0][initialIdx] = aux;
            }

            bool[] events = new bool[m_eventsUnion.Length];

            for (int i = 0; i < events.Length; ++i)
                events[i] = true;
            m_eventsList.Add(events);

            Size = (ulong)m_statesList[0].Length;

            m_bits[0] = 0;
            m_maxSize[0] = (1 << (int)Math.Ceiling(Math.Log(Size, 2))) - 1;
            m_tupleSize = 1;

            for (var i = 0; i < m_statesList[0].Length; ++i)
            {
                m_adjacencyList[0].Add(i,
                    transitionsLocal.AsParallel()
                        .Where(t => t.Origin == m_statesList[0][i])
                        .Select(
                            t => Tuple.Create(Array.IndexOf(m_eventsUnion, t.Trigger),
                            Array.IndexOf(m_statesList[0], t.Destination)))
                        .ToArray());
            }
        }

        private DeterministicFiniteAutomaton(int n)
        {
            m_statesList = new List<AbstractState[]>(n);
            m_eventsList = new List<bool[]>(n);
            m_adjacencyList = new List<AdjacencyMatrix>(n);
            m_bits = new int[n];
            m_maxSize = new int[n];
            m_numberOfPlants = n;
        }

        public IEnumerable<AbstractState> States
        {
            get
            {
                int n = m_statesList.Count;
                int[] pos = new int[n];

                if (m_validStates != null)
                {
                    foreach (var s in m_validStates)
                    {
                        s.Key.Get(pos, m_bits, m_maxSize);
                        yield return composeState(pos);
                    }
                }
                else if(!IsEmpty())
                {
                    do
                    {
                        yield return composeState(pos);
                    } while (incrementPosition(pos));
                }
            }
        }

        public IEnumerable<AbstractState> MarkedStates
        {
            get {
                int n = m_statesList.Count;
                int[] pos = new int[n];

                if (m_validStates != null)
                {
                    foreach (var s in m_validStates)
                    {
                        s.Key.Get(pos, m_bits, m_maxSize);
                        if(IsMarketState(pos))
                            yield return composeState(pos);
                    }
                }
                else if (!IsEmpty())
                {
                    do
                    {
                        if(IsMarketState(pos))
                            yield return composeState(pos);
                    } while (incrementPosition(pos));
                }
            }
        }

        public AbstractState InitialState
        {
            get {
                if (IsEmpty()) return null;
                return composeState(new int[m_statesList.Count]);
            }
        }

        public IEnumerable<AbstractEvent> Events
        {
            get
            {
                foreach(var e in m_eventsUnion)
                {
                    yield return e;
                }
            }
        }

        public IEnumerable<AbstractEvent> UncontrollableEvents
        {
            get
            {
                return m_eventsUnion.Where(i => !i.IsControllable);
            }
        }

        public DFA AccessiblePart
        {
            get
            {
                DFA G = this.Clone();
                G.removeNoAccessibleStates();
                return G;
            }
        }

        private void removeNoAccessibleStates()
        {
            if (m_validStates == null)
            {
                m_validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.getInstance());
                DepthFirstSearch(true);
            }
            else
                DepthFirstSearch(false);
        }

        Stack<bool> m_removeBadStates;

        private void findSupervisor(int nPlant, bool nonBlocking)
        {
            m_numberOfRunningThreads = 0;
            m_statesStack = new Stack<StatesTuple>();
            m_removeBadStates = new Stack<bool>();

            m_validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.getInstance());

            makeReverseTransitions();

            StatesTuple initialIndex = new StatesTuple(new int[m_statesList.Count], m_bits, m_tupleSize);
            m_statesStack.Push(initialIndex);
            m_removeBadStates.Push(false);

            Thread[] v_threads = new Thread[m_numberOfThreads - 1];

            for (int i = 0; i < m_numberOfThreads - 1; ++i)
            {
                v_threads[i] = new Thread(findStates);
                v_threads[i].Start(nPlant);
            }

            findStates(nPlant);

            for (int i = 0; i < m_numberOfThreads - 1; ++i)
            {
                v_threads[i].Join();
            }

            foreach (var s in m_validStates.Reverse())
            {
                if (s.Value) m_validStates.Remove(s.Key);
            }

            bool v_newBadStates;
            do {
                v_newBadStates = DepthFirstSearch(false, true);
                GC.Collect();
                if (nonBlocking)
                    v_newBadStates |= removeBlockingStates(true);
            } while (v_newBadStates);
        }

        private void findStates(Object obj)
        {
            int n = m_statesList.Count;
            int e, i, k, nPlant = (int)obj;
            int[] pos = new int[n];
            int[] nextPosition = new int[n];
            int UncontrollableEventsCount = UncontrollableEvents.Count();
            StatesTuple[] nextStates = new StatesTuple[UncontrollableEventsCount];
            bool plantHasEvent;
            StatesTuple tuple, nextTuple;
            bool v_removeBadStates, v_value;

            while (true)
            {
                lock (m_lockObject)
                {
                    ++m_numberOfRunningThreads;
                }
                while (true)
                {
                    lock (m_lockObject)
                    {
                        if (m_statesStack.Count == 0)
                        {
                            break;
                        }
                        tuple = m_statesStack.Pop();
                        v_removeBadStates = m_removeBadStates.Pop();
                        if (m_validStates.ContainsKey(tuple)) continue;
                        tuple.Get(pos, m_bits, m_maxSize);
                    }

                    k = 0;

                    for (e = 0; e < UncontrollableEventsCount; ++e)
                    {
                        plantHasEvent = false;

                        for (i = 0; i < nPlant; ++i)
                        {
                            if (!m_eventsList[i][e])
                            {
                                nextPosition[i] = pos[i];
                            }
                            else if (m_adjacencyList[i].hasEvent(pos[i], e))
                            {
                                nextPosition[i] = m_adjacencyList[i][(int)pos[i], e];
                                plantHasEvent = true;
                            }
                            else
                            {
                                goto nextEvent;
                            }
                        }

                        for (i = nPlant; i < n; ++i)
                        {
                            if (!m_eventsList[i][e])
                            {
                                nextPosition[i] = pos[i];
                            }
                            else if (m_adjacencyList[i].hasEvent(pos[i], e))
                            {
                                nextPosition[i] = m_adjacencyList[i][(int)pos[i], e];
                            }
                            else
                            {
                                if (!plantHasEvent) goto nextEvent;

                                if (v_removeBadStates)
                                    removeBadStates(tuple, UncontrollableEventsCount);
                                goto nextState;
                            }
                        }
                        nextStates[k++] = new StatesTuple(nextPosition, m_bits, m_tupleSize);

                        nextEvent:;
                    }
                    lock (m_lockObject)
                    {
                        if (m_validStates.ContainsKey(tuple))
                            continue;

                        int j = 0;
                        for (i = 0; i < k; ++i)
                        {
                            if (!m_validStates.TryGetValue(nextStates[i], out v_value))
                            {
                                m_statesStack.Push(nextStates[i]);
                                m_removeBadStates.Push(true);
                                ++j;
                            }
                            else if (v_value)
                            {
                                while (--j >= 0)
                                {
                                    m_statesStack.Pop();
                                    m_removeBadStates.Pop();
                                }
                                m_validStates.Add(tuple, true);
                                removeBadStates(tuple, UncontrollableEventsCount);
                                goto nextState;
                            }
                        }
                        m_validStates.Add(tuple, false);
                    }

                    for (e = UncontrollableEventsCount; e < m_eventsUnion.Length; ++e)
                    {
                        for (i = 0; i < n; ++i)
                        {
                            if (!m_eventsList[i][e])
                            {
                                nextPosition[i] = pos[i];
                            }
                            else if (m_adjacencyList[i].hasEvent(pos[i], e))
                            {
                                nextPosition[i] = m_adjacencyList[i][pos[i], e];
                            }
                            else
                            {
                                goto nextEvent;
                            }
                        }
                        nextTuple = new StatesTuple(nextPosition, m_bits, m_tupleSize);

                        lock (m_lockObject)
                        {
                            if (!m_validStates.ContainsKey(nextTuple))
                            {
                                m_statesStack.Push(nextTuple);
                                m_removeBadStates.Push(false);
                            }
                        }
                        nextEvent:;
                    }
                    nextState:;
                }
                lock (m_lockObject)
                {
                    --m_numberOfRunningThreads;
                }
                if (m_numberOfRunningThreads > 0) Thread.Sleep(1);
                else break;
            }
        }

        private void makeReverseTransitions()
        {
            if (m_reverseTransitionsList != null)
                return;

            m_reverseTransitionsList = new List<List<int>[]>[m_statesList.Count()];
            Parallel.For(0, m_statesList.Count(), i => {
                m_reverseTransitionsList[i] = new List<List<int>[]>(m_statesList[i].Length);
                for (int state = 0; state < m_statesList[i].Length; ++state)
                {
                    m_reverseTransitionsList[i].Add(new List<int>[m_eventsUnion.Count()]);
                    for (int e = 0; e < m_eventsUnion.Count(); ++e)
                        m_reverseTransitionsList[i][state][e] = new List<int>();
                }
                for (int state = 0; state < m_statesList[i].Length; ++state)
                {
                    foreach (var tr in m_adjacencyList[i][state])
                    {
                        m_reverseTransitionsList[i][tr.Value][tr.Key].Add((int)state);
                    }
                }
                for (int e = 0; e < m_eventsUnion.Count(); ++e)
                {
                    if (!m_eventsList[i][e])
                    {
                        for (int state = 0; state < m_statesList[i].Length; ++state)
                        {
                            m_reverseTransitionsList[i][state][e].Add(state);
                        }
                    }
                }

                for (int state = 0; state < m_statesList[i].Length; ++state)
                {
                    foreach (var p in m_reverseTransitionsList[i][state]) p.TrimExcess();
                }
            });
        }

        private bool removeBlockingStates(bool checkForBadStates = false)
        {
            makeReverseTransitions();
            int n = m_statesList.Count(), i;
            m_numberOfRunningThreads = 0;
            Thread[] threads = new Thread[m_numberOfThreads - 1];

            List<int>[] markedStates = new List<int>[n];
            int[] pos = new int[n];
            int[] statePos = new int[n];
            pos[n - 1] = -1;
            m_statesStack = new Stack<StatesTuple>();

            bool v_NotCheckValidState = false;
            if(m_validStates == null)
            {
                v_NotCheckValidState = true;
                m_validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.getInstance());
            }

            for (i = 0; i < n; ++i)
            {
                markedStates[i] = m_statesList[i].Where(st => st.IsMarked).
                    Select(st => Array.IndexOf(m_statesList[i], st)).ToList();
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

                for (i = 0; i < n; ++i)
                {
                    statePos[i] = markedStates[i][pos[i]];
                }
                StatesTuple tuple = new StatesTuple(statePos, m_bits, m_tupleSize);
                bool v_value;
                if ((m_validStates.TryGetValue(tuple, out v_value) || v_NotCheckValidState) && !v_value)
                {
                    m_validStates[tuple] = true;
                    m_statesStack.Push(tuple);
                }
            }

            markedStates = null;
            Size = 0;

            for (i = 0; i < m_numberOfThreads - 1; ++i)
            {
                threads[i] = new Thread(InverseSearchThread);
                threads[i].Start(v_NotCheckValidState);
            }
            InverseSearchThread(v_NotCheckValidState);

            for (i = 0; i < m_numberOfThreads - 1; ++i)
            {
                threads[i].Join();
            }

            m_statesStack = null;
            bool v_newBadStates = false;
            int uncontrolEventsCount = UncontrollableEvents.Count();

            if (checkForBadStates)
            {
                var removedStates = new List<StatesTuple>();
                foreach (var p in m_validStates)
                {
                    if (!p.Value) removedStates.Add(p.Key);
                }
                foreach(var p in removedStates)
                {
                    v_newBadStates |= removeBadStates(p, uncontrolEventsCount, true);
                }
            }

            m_reverseTransitionsList = null;

            foreach (var p in m_validStates.Reverse())
            {
                if (!p.Value)
                    m_validStates.Remove(p.Key);
                else
                    m_validStates[p.Key] = false;
            }

            GC.Collect();
            return v_newBadStates;
        }

        public DFA CoaccessiblePart
        {
            get
            {
                DFA G = this.Clone();
                G.removeBlockingStates();
                return G;
            }
        }

        public DFA Trim
        {
            get {
                DFA G = this.AccessiblePart;
                G.removeBlockingStates();
                return G;
            }
        }

        public int KleeneClosure {
            get {
                Console.WriteLine("Sorry. This is still in TO-DO List");
                return 0;
            }
        }

        private void radixSort(int[] map, int[] positions)
        {
            int n = positions.Length;
            int[] b = new int[n];
            int[] bucket = new int[n + 1];
            int i, k;

            for (i = 0; i < n; ++i) ++bucket[m_statesList[0][positions[i]].IsMarked ? 1 : 0];
            bucket[1] += bucket[0];
            for (i = n - 1; i >= 0; --i)
            {
                k = positions[i];
                b[--bucket[m_statesList[0][k].IsMarked ? 1 : 0]] = k;
            }
            Array.Copy(b, positions, n);

            for (var e = 0; e < m_eventsUnion.Length; ++e)
            {
                Array.Clear(bucket, 0, bucket.Length);
                for (i = 0; i < n; ++i)
                {
                    k = m_adjacencyList[0].hasEvent(positions[i], e) ? 
                            map[m_adjacencyList[0][positions[i], e]] : n;
                    ++bucket[k];
                }
                for (i = 1; i <= n; ++i) bucket[i] += bucket[i - 1];
                for (i = n - 1; i >= 0; --i)
                {
                    k = m_adjacencyList[0].hasEvent(positions[i], e) ? 
                            map[m_adjacencyList[0][positions[i], e]] : n;
                    b[--bucket[k]] = positions[i];
                }
                Array.Copy(b, positions, n);
            }
        }

        private void minimize()
        {
            var numStatesOld = m_statesList[0].Length;
            var statesMap = new int[numStatesOld];
            var positions = new int[numStatesOld];
            bool changed;
            int i, j, k, l, numStates;
            int iState = 0, numEvents = m_eventsUnion.Length;
            bool[] hasEvents = new bool[numEvents];
            int[] transitions = new int[numEvents];

            for (i = 0; i < numStatesOld; ++i)
            {
                statesMap[i] = i;
                positions[i] = i;
            }

            while (true)
            {
                changed = false;
                radixSort(statesMap, positions);
                numStates = 0;
                for (i = 0; i < numStatesOld;)
                {
                    k = positions[i];
                    if (statesMap[k] != k)
                    {
                        ++i;
                        continue;
                    }
                    ++numStates;
                    for (var e = 0; e < numEvents; ++e)
                    {
                        hasEvents[e] = m_adjacencyList[0].hasEvent(k, e);
                        if (hasEvents[e]) transitions[e] = m_adjacencyList[0][k, e];
                    }

                    j = i + 1;
                    while (true)
                    {
                        while (j < numStatesOld && statesMap[positions[j]] == k) ++j;
                        if (j >= numStatesOld) break;
                        l = positions[j];
                        for (var e = numEvents - 1; e >= 0; --e)
                        {
                            if (hasEvents[e] != m_adjacencyList[0].hasEvent(l, e) ||
                                (hasEvents[e] && statesMap[transitions[e]] !=
                                statesMap[m_adjacencyList[0][l, e]]))
                            {
                                goto nextState;
                            }
                        }
                        if (m_statesList[0][k].IsMarked != m_statesList[0][l].IsMarked)
                        {
                            break;
                        }
                        statesMap[l] = k;
                        changed = true;
                        ++j;
                    }
                    nextState:;
                    i = j;
                }
                if (!changed) break;
                for (i = 0; i < numStatesOld; ++i)
                {
                    k = i;
                    while (statesMap[k] != k) k = statesMap[k];
                    if (i != k) statesMap[i] = k;
                }
            }

            var newStates = new AbstractState[numStates];
            var newTransitions = new AdjacencyMatrix(numStates, numEvents, true);
            int initial = 0;

            for (i = 0; i < numStatesOld; ++i)
            {
                k = positions[i];
                if (statesMap[k] == k)
                {
                    if (k == 0) initial = iState;
                    newStates[iState] = m_statesList[0][k];
                    statesMap[k] = iState++;
                }
                else
                {
                    var pos = statesMap[statesMap[k]];
                    if (k == 0) initial = pos;
                    newStates[pos] = newStates[pos].MergeWith(m_statesList[0][k]);
                    statesMap[k] = pos;
                }
            }

            if(initial != 0)
            {
                var aux = newStates[0];
                newStates[0] = newStates[initial];
                newStates[initial] = aux;
            }

            for (i = 0; i < numStatesOld; ++i)
            {
                k = statesMap[i];
                k = k == 0 ? initial : (k == initial ? 0 : k);
                for (var e = 0; e < numEvents; ++e)
                {
                    if (m_adjacencyList[0].hasEvent(i, e))
                    {
                        l = statesMap[m_adjacencyList[0][i, e]];
                        l = l == 0 ? initial : (l == initial ? 0 : l);
                        newTransitions.Add(k, e, l);
                    }
                }
            }

            m_statesList[0] = newStates;
            m_adjacencyList[0] = newTransitions;
            m_maxSize[0] = (1 << (int)Math.Ceiling(Math.Log(newStates.Length, 2))) - 1;
            Size = (ulong)newStates.Length;
            Name = "Min(" + Name + ")";
        }

        public DFA Minimal
        {
            get
            {
                if (IsEmpty()) return this.Clone();

                DFA G = AccessiblePart;
                G.simplify();
                G.minimize();
                return G;
            }
        }

        public DFA PrefixClosure {
            get
            {
                var G = this.Clone();

                for(var i = 0; i < G.m_statesList.Count; ++i)
                {
                    for(var s = 0; s < G.m_statesList[i].Length; ++s)
                    {
                        G.m_statesList[i][s] = G.m_statesList[i][s].ToMarked;
                    }
                }

                return G;
            }
        }

        public string ToDotCode {
            get {
                var dot = new StringBuilder("digraph {\nrankdir=TB;", (int)Size * InitialState.ToString().Length);

                dot.Append("\nnode [shape = doublecircle];");

                foreach (var ms in MarkedStates)
                    dot.AppendFormat(" \"{0}\" ", ms);

                dot.Append("\nnode [shape = circle];");

                int n = m_statesList.Count;
                int[] pos = new int[n];

                var addTransitions = new Action(() => {
                    foreach (var group in getTransitionsFromState(pos).GroupBy(t => t.Destination))
                    {
                        dot.AppendFormat("\"{0}\" -> \"{1}\" [ label = \"", group.First().Origin, group.Key);
                        bool first = true;
                        foreach (var t in group)
                        {
                            if (!first) dot.Append(",");
                            else first = false;
                            dot.Append(t.Trigger);
                        }
                        dot.Append("\" ];\n");
                    }
                });

                if (m_validStates != null)
                {
                    foreach (var s in m_validStates)
                    {
                        s.Key.Get(pos, m_bits, m_maxSize);
                        if (!IsMarketState(pos))
                            dot.AppendFormat(" \"{0}\" ", composeState(pos));
                    }

                    dot.AppendFormat("\nnode [shape = point ]; Initial\nInitial -> \"{0}\";\n", InitialState);
                    foreach (var s in m_validStates)
                    {
                        s.Key.Get(pos, m_bits, m_maxSize);
                        addTransitions();
                    }
                }
                else if (!IsEmpty())
                {
                    do
                    {
                        if (!IsMarketState(pos))
                            dot.AppendFormat(" \"{0}\" ", composeState(pos));
                    } while (incrementPosition(pos));

                    dot.AppendFormat("\nnode [shape = point ]; Initial\nInitial -> \"{0}\";\n", InitialState);

                    do
                    {
                        addTransitions();
                    } while (incrementPosition(pos));
                }            
                dot.Append("}");

                return dot.ToString();
            }
        }

        public RegularExpression ToRegularExpression {
            get {
                if (IsEmpty()) return Symbol.Empty;
                simplify();

                var t = Enumerable.Range(0, (int)Size).ToArray();

                int len = m_statesList.Count;
                var size = (int)Size;
                var b = new RegularExpression[size];
                var a = new RegularExpression[size, size];

                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < size; j++)
                    {
                        a[i, j] = Symbol.Empty;
                    }

                    for (var e = 0; e < m_eventsUnion.Length; e++)
                    {
                        if(m_adjacencyList[0].hasEvent(i, e))
                        {
                            a[i, m_adjacencyList[0][i, e]] += m_eventsUnion[e];
                        }
                    }
                }

                for (var i = 0; i < size; ++i)
                    b[i] = m_statesList[0][i].IsMarked ? Event.Epsilon : Event.Empty;

                for (var n = size - 1; n >= 0; n--)
                {
                    b[n] = new KleeneStar(a[n, n]) * b[n];
                    for (var j = 0; j <= n; j++)
                    {
                        a[n, j] = new KleeneStar(a[n, n]) * a[n, j];
                    }
                    for (var i = 0; i <= n; i++)
                    {
                        b[i] += a[i, n] * b[n];
                        for (var j = 0; j <= n; j++)
                        {
                            a[i, j] += a[i, n] * a[n, j];
                        }
                    }
                }

                return b[0].Simplify;
            }
        }

        public string ToXML {
            get
            {
                simplify();
                var doc = new XmlDocument();
                var automaton = (XmlElement)doc.AppendChild(doc.CreateElement("Automaton"));
                automaton.SetAttribute("Name", Name);

                var states = (XmlElement)automaton.AppendChild(doc.CreateElement("States"));
                for (var i = 0; i < m_statesList[0].Length; i++)
                {
                    var state = m_statesList[0][i];

                    var s = (XmlElement)states.AppendChild(doc.CreateElement("State"));
                    s.SetAttribute("Name", state.ToString());
                    s.SetAttribute("Marking", state.Marking.ToString());
                    s.SetAttribute("Id", i.ToString());
                }

                var initial = (XmlElement)automaton.AppendChild(doc.CreateElement("InitialState"));
                initial.SetAttribute("Id", "0");

                var events = (XmlElement)automaton.AppendChild(doc.CreateElement("Events"));
                for (var i = 0; i < m_eventsUnion.Length; i++)
                {
                    var @event = m_eventsUnion[i];

                    var e = (XmlElement)events.AppendChild(doc.CreateElement("Event"));
                    e.SetAttribute("Name", @event.ToString());
                    e.SetAttribute("Controllability", @event.Controllability.ToString());
                    e.SetAttribute("Id", i.ToString());
                }

                var transitions = (XmlElement)automaton.AppendChild(doc.CreateElement("Transitions"));
                for (var i = 0; i < m_statesList[0].Length; i++)
                {
                    for (var j = 0; j < m_eventsUnion.Length; j++)
                    {
                        var k = m_adjacencyList[0].hasEvent(i, j) ? m_adjacencyList[0][i,j] : -1;
                        if (k == -1) continue;

                        var t = (XmlElement)transitions.AppendChild(doc.CreateElement("Transition"));

                        t.SetAttribute("Origin", i.ToString());
                        t.SetAttribute("Trigger", j.ToString());
                        t.SetAttribute("Destination", k.ToString());
                    }
                }

                return doc.OuterXml;
            }
        }

        public Func<AbstractState, AbstractEvent, Option<AbstractState>> TransitionFunction
        {
            get
            {
                var n = m_statesList.Count;
                var st = new Dictionary<AbstractState, int>[n];
                for(var i = 0; i < n; ++i)
                {
                    st[i] = new Dictionary<AbstractState, int>(m_statesList[i].Length);
                    for (var j = 0; j < m_statesList[i].Length; ++j) st[i].Add(m_statesList[i][j], j);
                }

                return (s, e) =>
                {
                    if (e == Epsilon.EpsilonEvent) return Some<AbstractState>.Create(s);
                    var p = s as CompoundState;
                    var S = p != null ? p.S : new[] { s };
                    if (n == 1 && S.Length > 1) S = new[] { p.Join() };

                    if (S.Length != n) return None<AbstractState>.Create();

                    var pos = new int[n];
                    for (var i = 0; i < n; ++i)
                    {
                        if(!st[i].TryGetValue(S[i], out pos[i]))
                        {
                            return None<AbstractState>.Create();
                        }
                    }
                    var k = Array.IndexOf(m_eventsUnion, e);
                    if(k < 0) return None<AbstractState>.Create();

                    for(var i = 0; i < n; ++i)
                    {
                        if (m_eventsList[i][k])
                        {
                            pos[i] = m_adjacencyList[i][pos[i], k];
                            if(pos[i] < 0) return None<AbstractState>.Create();
                        }
                    }

                    if(m_validStates != null && !m_validStates.ContainsKey(new StatesTuple(pos, m_bits, m_tupleSize)))
                    {
                        return None<AbstractState>.Create();
                    }

                    return Some<AbstractState>.Create(n == 1 ? m_statesList[0][pos[0]] : composeState(pos));
                };
            }
        }
        
        private void InverseSearchThread(object p_param)
        {
            int i, length = 0;
            int n = m_statesList.Count;
            int[] pos = new int[n];
            int[] nextPos = new int[n];
            int[] movs = new int[n];
            StatesTuple tuple;

            bool v_NotCheckValidState = (bool)p_param;

            while (true)
            {
                lock (m_lockObject)
                {
                    ++m_numberOfRunningThreads;
                }
                while (true)
                {
                    lock (m_lockObject)
                    {
                        if (m_statesStack.Count == 0) break;
                        tuple = m_statesStack.Pop();
                        tuple.Get(pos, m_bits, m_maxSize);
                    }

                    ++length;

                    for (int e = 0; e < m_eventsUnion.Length; ++e)
                    {
                        for (i = 0; i < n; ++i)
                        {
                            if (m_reverseTransitionsList[i][pos[i]][e].Count() == 0)
                            {
                                goto nextEvent;
                            }
                        }

                        for (i = 0; i < n - 1; ++i)
                        {
                            nextPos[i] = m_reverseTransitionsList[i][pos[i]][e][movs[i]];
                        }
                        movs[n - 1] = -1;
                        while (true)
                        {
                            for (i = n - 1; i >= 0; --i)
                            {
                                ++movs[i];
                                if (movs[i] < m_reverseTransitionsList[i][pos[i]][e].Count()) break;
                                movs[i] = 0;
                                nextPos[i] = m_reverseTransitionsList[i][pos[i]][e][0];
                            }
                            if (i < 0) break;

                            nextPos[i] = m_reverseTransitionsList[i][pos[i]][e][movs[i]];

                            tuple = new StatesTuple(nextPos, m_bits, m_tupleSize);
                            lock (m_lockObject)
                            {
                                bool value;
                                if ((m_validStates.TryGetValue(tuple, out value) || v_NotCheckValidState) && !value)
                                {
                                    m_validStates[tuple] = true;
                                    m_statesStack.Push(tuple);
                                }
                            }
                        }
                        nextEvent:;
                    }
                }
                lock (m_lockObject)
                {
                    --m_numberOfRunningThreads;
                }
                if (m_numberOfRunningThreads > 0) Thread.Sleep(1);
                else break;
            }

            lock (m_lockObject)
            {
                Size += (ulong)length;
            }
        }

        private bool removeBadStates(StatesTuple initialPos, int uncontrolEventsCount, bool defaultValue = false)
        {
            int e, i, n = m_statesList.Count;
            StatesTuple tuple;
            Stack<StatesTuple> stack = new Stack<StatesTuple>();
            int[] pos = new int[n];
            int[] nextPos = new int[n];
            int[] movs = new int[n];
            bool v_value, found = false;

            stack.Push(initialPos);

            while (stack.Count > 0)
            {
                tuple = stack.Pop();
                tuple.Get(pos, m_bits, m_maxSize);

                for (e = 0; e < uncontrolEventsCount; ++e)
                {
                    for (i = 0; i < n; ++i)
                    {
                        if (m_reverseTransitionsList[i][pos[i]][e].Count() == 0)
                        {
                            goto nextEvent;
                        }
                    }
                    for (i = 0; i < n; ++i)
                    {
                        nextPos[i] = m_reverseTransitionsList[i][pos[i]][e][movs[i]];
                    }
                    movs[n - 1] = -1;
                    while (true)
                    {
                        for (i = n - 1; i >= 0; --i)
                        {
                            ++movs[i];
                            if (movs[i] < m_reverseTransitionsList[i][pos[i]][e].Count()) break;
                            movs[i] = 0;
                            nextPos[i] = m_reverseTransitionsList[i][pos[i]][e][movs[i]];
                        }
                        if (i < 0) break;
                        nextPos[i] = m_reverseTransitionsList[i][pos[i]][e][movs[i]];

                        tuple = new StatesTuple(nextPos, m_bits, m_tupleSize);

                        lock (m_lockObject)
                        {
                            if (m_validStates.TryGetValue(tuple, out v_value) && v_value == defaultValue)
                            {
                                m_validStates[tuple] = !defaultValue;
                                stack.Push(tuple);
                                found = true;
                            }
                        }
                    }
                    nextEvent:;
                }
            }
            return found;
        }

        private void DepthFirstSearchThread(Object param)
        {
            int length = 0;
            int n = m_statesList.Count;
            int[] pos = new int[n];
            int[] nextPosition = new int[n];
            bool acceptAllStates = (bool)param;
            int e, i;
            StatesTuple tuple;

            while (true)
            {
                lock (m_lockObject)
                {
                    ++m_numberOfRunningThreads;
                }
                while (true)
                {
                    lock (m_lockObject)
                    {
                        if (m_statesStack.Count == 0)
                        {
                            break;
                        }
                        tuple = m_statesStack.Pop();
                    }

                    ++length;
                    tuple.Get(pos, m_bits, m_maxSize);

                    for (e = 0; e < m_eventsUnion.Length; ++e)
                    {
                        for (i = n - 1; i >= 0; --i)
                        {
                            if (!m_eventsList[i][e])
                            {
                                nextPosition[i] = pos[i];
                            }
                            else if (m_adjacencyList[i].hasEvent(pos[i], e))
                            {
                                nextPosition[i] = m_adjacencyList[i][pos[i], e];
                            }
                            else
                            {
                                goto nextEvent;
                            }
                        }
                        tuple = new StatesTuple(nextPosition, m_bits, m_tupleSize);
                        bool invalid;
                        lock (m_lockObject)
                        {
                            if (m_validStates.TryGetValue(tuple, out invalid))
                            {
                                if (!invalid)
                                {
                                    m_validStates[tuple] = true;
                                    m_statesStack.Push(tuple);
                                }
                            }
                            else if (acceptAllStates)
                            {
                                m_validStates[tuple] = true;
                                m_statesStack.Push(tuple);
                            }
                        }
                        nextEvent:;
                    }
                }
                lock (m_lockObject)
                {
                    --m_numberOfRunningThreads;
                }
                if (m_numberOfRunningThreads > 0) Thread.Sleep(1);
                else break;
            }

            lock (m_lockObject)
            {
                Size += (ulong)length;
            }
        }

        private bool DepthFirstSearch(bool acceptAllStates, bool checkForBadStates = false)
        {
            m_statesStack = new Stack<StatesTuple>();
            Size = 0;
            m_numberOfRunningThreads = 0;

            StatesTuple initialState = new StatesTuple(new int[m_statesList.Count], m_bits, m_tupleSize);

            if (!acceptAllStates && !m_validStates.ContainsKey(initialState))
                return false;

            m_validStates[initialState] = true;
            m_statesStack.Push(initialState);

            Thread[] threads = new Thread[m_numberOfThreads - 1];

            for (int i = 0; i < m_numberOfThreads - 1; ++i)
            {
                threads[i] = new Thread(DepthFirstSearchThread);
                threads[i].Start(acceptAllStates);
            }
            DepthFirstSearchThread(acceptAllStates);
            for (int i = 0; i < m_numberOfThreads - 1; ++i)
            {
                threads[i].Join();
            }

            m_statesStack = null;

            bool v_newBadStates = false;
            int v_uncontrolEventsCount = UncontrollableEvents.Count();
            if (checkForBadStates)
            {
                foreach (var p in m_validStates)
                {
                    if (!p.Value)
                            v_newBadStates |= removeBadStates(p.Key, v_uncontrolEventsCount, true);
                }
            }
            foreach (var p in m_validStates.Reverse())
            {
                if (!p.Value)
                    m_validStates.Remove(p.Key);
                else
                    m_validStates[p.Key] = false;
            }
            return v_newBadStates;
        }

        private static DFA ConcatDFA(DFA G1, DFA G2)
        {
            if(G1.m_validStates != null)
            {
                G1.simplify();
            }
            if(G2.m_validStates != null)
            {
                G2.simplify();
            }

            int n = G1.m_adjacencyList.Count + G2.m_adjacencyList.Count;
            DFA G1G2 = G1.clone(n);

            G1G2.Name += "||" + G2.Name;
            G1G2.m_adjacencyList.Clear();
            G1G2.m_eventsUnion = G1G2.m_eventsUnion.Concat(G2.m_eventsUnion).Distinct().OrderBy(i => i.Controllability).ToArray();
            G1G2.m_eventsList.Clear();
            G1G2.m_statesList.AddRange(G2.m_statesList);
            G1G2.m_validStates = null;
            G1G2.m_numberOfPlants = n;
            G1G2.Size *= G2.Size;

            G1G2.m_tupleSize = 1;
            int k = 0;
            for (int i = 0; i < n; ++i)
            {
                G1G2.m_bits[i] = k;
                int p = (int)Math.Ceiling(Math.Log(G1G2.m_statesList[i].Length, 2));
                G1G2.m_maxSize[i] = (1 << p) - 1;
                k += p;
                if (k > sizeof(int) * 8)
                {
                    G1G2.m_bits[i] = 0;
                    ++G1G2.m_tupleSize;
                    k = p;
                }
            }

            for (int i = 0; i < G1.m_adjacencyList.Count; ++i)
            {
                G1G2.m_eventsList.Add(new bool[G1G2.m_eventsUnion.Length]);
                for (int e = 0; e < G1.m_eventsUnion.Length; ++e)
                {
                    G1G2.m_eventsList[i][Array.IndexOf(G1G2.m_eventsUnion, G1.m_eventsUnion[e])] = G1.m_eventsList[i][e];
                }
                G1G2.m_adjacencyList.Add(new AdjacencyMatrix(G1.m_statesList[i].Length, G1G2.m_eventsUnion.Length));
                for (int j = 0; j < G1.m_statesList[i].Length; ++j)
                {
                    foreach (var p in G1.m_adjacencyList[i][j])
                    {
                        G1G2.m_adjacencyList[i].Add((int)j, Array.IndexOf(G1G2.m_eventsUnion, G1.m_eventsUnion[p.Key]), p.Value);
                    }
                }
            }
            for (int i = G1.m_adjacencyList.Count; i < n; ++i)
            {
                G1G2.m_eventsList.Add(new bool[G1G2.m_eventsUnion.Length]);
                for (int e = 0; e < G2.m_eventsUnion.Length; ++e)
                {
                    G1G2.m_eventsList[i][Array.IndexOf(G1G2.m_eventsUnion, G2.m_eventsUnion[e])] = G2.m_eventsList[i - G1.m_adjacencyList.Count][e];
                }
                G1G2.m_adjacencyList.Add(new AdjacencyMatrix(G1G2.m_statesList[i].Length, G1G2.m_eventsUnion.Length));
                for (int j = 0; j < G1G2.m_statesList[i].Length; ++j)
                {
                    foreach (var q in G2.m_adjacencyList[i - G1.m_adjacencyList.Count][j])
                    {
                        G1G2.m_adjacencyList[i].Add((int)j, Array.IndexOf(G1G2.m_eventsUnion, G2.m_eventsUnion[q.Key]), q.Value);
                    }
                }
            }

            return G1G2;

        }

        public DFA ParallelCompositionWith(DFA G2, bool removeNoAccessibleStates = true)
        {
            DFA G1G2 = ConcatDFA(this, G2);

            if (removeNoAccessibleStates)
            {
                G1G2.removeNoAccessibleStates();
                GC.Collect();
                GC.Collect();
            }
            return G1G2;
        }

        public DFA ParallelCompositionWith(IEnumerable<DFA> list, bool removeNoAccessibleStates = true)
        {
            return this.ParallelCompositionWith(
                    list.Aggregate((a, b) => a.ParallelCompositionWith(b, removeNoAccessibleStates)),
                    removeNoAccessibleStates
                );
        }

        public DFA ParallelCompositionWith(params DFA[] others)
        {
            return this.ParallelCompositionWith(others, true);
        }

        public static DFA ParallelComposition(IEnumerable<DFA> list, bool removeNoAccessibleStates = true)
        {
            return list.Aggregate((a, b) => a.ParallelCompositionWith(b, removeNoAccessibleStates));
        }

        public static DFA ParallelComposition(DFA A, params DFA[] others)
        {
            return A.ParallelCompositionWith(others, true);
        }

        private void BuildProduct()
        {
            int n = m_statesList.Count;
            int[] pos = new int[n];
            int[] nextPos = new int[n];
            m_statesStack = new Stack<StatesTuple>();

            var initialState = new StatesTuple(pos, m_bits, m_tupleSize);

            m_statesStack.Push(initialState);

            m_validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.getInstance());
            m_validStates.Add(initialState, false);

            while(m_statesStack.Count > 0)
            {
                m_statesStack.Pop().Get(pos, m_bits, m_maxSize);

                for(var e = 0; e < m_eventsUnion.Length; ++e)
                {
                    for(var i = 0; i < n; ++i)
                    {
                        if(m_adjacencyList[i].hasEvent(pos[i], e))
                        {
                            nextPos[i] = m_adjacencyList[i][pos[i], e];
                        }
                        else
                        {
                            goto nextEvent;
                        }
                    }
                    var nextState = new StatesTuple(nextPos, m_bits, m_tupleSize);

                    if (!m_validStates.ContainsKey(nextState))
                    {
                        m_statesStack.Push(nextState);
                        m_validStates.Add(nextState, false);
                    }
                    nextEvent:;
                }
            }
            Size = (ulong)m_validStates.Count;
        }

        public DFA ProductWith(params DFA[] Gs)
        {
            return this.ProductWith((IEnumerable<DFA>)Gs);
        }

        public DFA ProductWith(IEnumerable<DFA> list)
        {
            DFA G1G2 = this;
            foreach (var G in list)
            {
                G1G2 = ConcatDFA(G1G2, G);
            }
            G1G2.BuildProduct();
            return G1G2;
        }

        public static DFA Product(IEnumerable<DFA> list)
        {
            if (list.Count() == 0) return null;
            DFA G1G2 = list.Aggregate((a, b) => ConcatDFA(a, b));
            G1G2.BuildProduct();
            return G1G2;
        }

        public static DFA Product(DFA G, params DFA[] others)
        {
            return G.ProductWith(others);
        }

        public static DFA MonolithicSupervisor(IEnumerable<DFA> plants,
            IEnumerable<DFA> specifications, bool nonBlocking = false)
        {
            var plant = plants.Aggregate((a, b) => a.ParallelCompositionWith(b, false));
            var specification = specifications.Aggregate((a, b) => a.ParallelCompositionWith(b, false));
            var result = plant.ParallelCompositionWith(specification, false);
            result.findSupervisor(plant.m_statesList.Count(), nonBlocking);

            result.Name = string.Format("Sup({0})", result.Name);
            result.m_numberOfPlants = plant.m_statesList.Count();

            return result;
        }

        private bool VerifyNonblocking()
        {
            ulong nStates = Size;
            removeBlockingStates();
            return Size != nStates;
        }

        public override string ToString()
        {
            return Name;
        }

        public void ToXMLFile(string filepath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n"
            };

            int n = m_statesList.Count();
            int[] pos = new int[n];
            int[] nextPos = new int[n];
            Dictionary<StatesTuple, ulong> v_states = new Dictionary<StatesTuple, ulong>((int)Size,
                StatesTupleComparator.getInstance());
            StatesTuple nextTuple = new StatesTuple(m_tupleSize);

            using (var writer = XmlWriter.Create(filepath, settings))
            {
                writer.WriteStartElement("Automaton");
                writer.WriteAttributeString("Name", Name);

                writer.WriteStartElement("States");

                var addState = new Action<StatesTuple, ulong>((state, id) => {
                    state.Get(pos, m_bits, m_maxSize);
                    v_states.Add(state, id);

                    var s = composeState(pos);

                    writer.WriteStartElement("State");
                    writer.WriteAttributeString("Name", s.ToString());
                    writer.WriteAttributeString("Marking", s.Marking.ToString());
                    writer.WriteAttributeString("Id", id.ToString());

                    writer.WriteEndElement();
                });

                if (m_validStates != null)
                {
                    ulong id = 0;
                    foreach (var state in m_validStates)
                    {
                        addState(state.Key, id++);
                    }
                }
                else
                {
                    for (ulong id = 0; id < Size; ++id)
                    {
                        addState(new StatesTuple(pos, m_bits, m_tupleSize), id);
                        incrementPosition(pos);
                    }
                }

                writer.WriteEndElement();

                writer.WriteStartElement("InitialState");
                writer.WriteAttributeString("Id", "0");

                writer.WriteEndElement();

                writer.WriteStartElement("Events");
                for (int i = 0; i < m_eventsUnion.Length; ++i)
                {
                    writer.WriteStartElement("Event");
                    writer.WriteAttributeString("Name", m_eventsUnion[i].ToString());
                    writer.WriteAttributeString("Controllability", m_eventsUnion[i].Controllability.ToString());
                    writer.WriteAttributeString("Id", i.ToString());

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                writer.WriteStartElement("Transitions");

                foreach (var state in v_states)
                {
                    state.Key.Get(pos, m_bits, m_maxSize);

                    for (int j = 0; j < m_eventsUnion.Length; ++j)
                    {
                        for (int e = 0; e < n; ++e)
                        {
                            if (m_eventsList[e][j])
                            {
                                if (m_adjacencyList[e].hasEvent(pos[e], j))
                                    nextPos[e] = m_adjacencyList[e][pos[e], j];
                                else
                                    goto nextEvent;
                            }
                            else
                                nextPos[e] = pos[e];
                        }

                        ulong value;
                        nextTuple.Set(nextPos, m_bits);

                        if (v_states.TryGetValue(nextTuple, out value))
                        {
                            writer.WriteStartElement("Transition");

                            writer.WriteAttributeString("Origin", state.Value.ToString());
                            writer.WriteAttributeString("Trigger", j.ToString());
                            writer.WriteAttributeString("Destination", value.ToString());

                            writer.WriteEndElement();
                        }

                        nextEvent:;
                    }
                }

                writer.WriteEndElement();

                writer.WriteEndElement();
            }
            v_states = null;

            GC.Collect();
            GC.Collect();
        }

        public static DFA FromXMLFile(string p_FilePath, bool p_StateName = true)
        {
            var v_xdoc = XDocument.Load(p_FilePath);

            var v_name = v_xdoc.Descendants("Automaton").Select(dfa => dfa.Attribute("Name").Value).Single();
            var v_states = v_xdoc.Descendants("State")
                .ToDictionary(s => s.Attribute("Id").Value,
                    s =>
                        new State(p_StateName ? s.Attribute("Name").Value : s.Attribute("Id").Value,
                            s.Attribute("Marking").Value == "Marked" ? Marking.Marked : Marking.Unmarked));

            var v_events = v_xdoc.Descendants("Event")
                .ToDictionary(e => e.Attribute("Id").Value,
                    e =>
                        new Event(e.Attribute("Name").Value,
                            e.Attribute("Controllability").Value == "Controllable"
                                ? Controllability.Controllable
                                : Controllability.Uncontrollable));

            var v_initial = v_xdoc.Descendants("InitialState").Select(i => v_states[i.Attribute("Id").Value]).Single();

            var v_transitions =
                v_xdoc.Descendants("Transition")
                    .Select(
                        t =>
                            new Transition(v_states[t.Attribute("Origin").Value], v_events[t.Attribute("Trigger").Value],
                                v_states[t.Attribute("Destination").Value]));

            return new DFA(v_transitions, v_initial, v_name);
        }

        public void SerializeAutomaton(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
            stream.Close();
        }

        public static DFA DeserializeAutomaton(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var obj = (DFA)formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        private IEnumerable<Transition> getTransitionsFromState(int[] pos)
        {
            int n = m_statesList.Count;
            int[] nextPosition = new int[n];
            int e, i, k;
            bool plantHasEvent;
            int UncontrollableEventsCount = UncontrollableEvents.Count();
            StatesTuple[] nextStates = new StatesTuple[UncontrollableEventsCount];
            int[] v_events = new int[UncontrollableEventsCount];
            k = 0;

            var currentState = composeState(pos);

            for (e = 0; e < UncontrollableEventsCount; ++e)
            {
                plantHasEvent = false;

                for (i = 0; i < m_numberOfPlants; ++i)
                {
                    if (!m_eventsList[i][e])
                    {
                        nextPosition[i] = pos[i];
                    }
                    else if (m_adjacencyList[i].hasEvent(pos[i], e))
                    {
                        nextPosition[i] = m_adjacencyList[i][pos[i], e];
                        plantHasEvent = true;
                    }
                    else
                    {
                        goto nextEvent;
                    }
                }

                for (i = m_numberOfPlants; i < n; ++i)
                {
                    if (!m_eventsList[i][e])
                    {
                        nextPosition[i] = pos[i];
                    }
                    else if (m_adjacencyList[i].hasEvent(pos[i], e))
                    {
                        nextPosition[i] = m_adjacencyList[i][pos[i], e];
                    }
                    else
                    {
                        if (!plantHasEvent) goto nextEvent;

                        goto nextState;
                    }
                }
                nextStates[k] = new StatesTuple(nextPosition, m_bits, m_tupleSize);
                if (m_validStates == null || m_validStates.ContainsKey(nextStates[k]))
                {
                    v_events[k] = e;
                    ++k;
                }

                nextEvent:;
            }

            for (i = 0; i < k; ++i)
            {
                nextStates[i].Get(nextPosition, m_bits, m_maxSize);
                var nextState = composeState(nextPosition);
                yield return new Transition(currentState, m_eventsUnion[v_events[i]], nextState);
            }

            for (e = UncontrollableEventsCount; e < m_eventsUnion.Length; ++e)
            {
                for (i = 0; i < n; ++i)
                {
                    if (!m_eventsList[i][e])
                    {
                        nextPosition[i] = pos[i];
                    }
                    else if (m_adjacencyList[i].hasEvent(pos[i], e))
                    {
                        nextPosition[i] = m_adjacencyList[i][pos[i], e];
                    }
                    else
                    {
                        goto nextEvent;
                    }
                }
                StatesTuple nextTuple = new StatesTuple(nextPosition, m_bits, m_tupleSize);

                if (m_validStates == null || m_validStates.ContainsKey(nextTuple))
                {
                    var nextState = composeState(nextPosition);
                    yield return new Transition(currentState, m_eventsUnion[e], nextState);
                }
                nextEvent:;
            }
            nextState:;
        }

        public IEnumerable<Transition> Transitions
        {
            get
            {
                int n = m_statesList.Count;
                int[] pos = new int[n];
                if (m_validStates != null)
                {
                    foreach (var s in m_validStates)
                    {
                        s.Key.Get(pos, m_bits, m_maxSize);
                        foreach (var i in getTransitionsFromState(pos))
                            yield return i;
                    }
                }
                else if(!IsEmpty())
                {
                    do
                    {
                        foreach (var i in getTransitionsFromState(pos))
                            yield return i;
                    }while(incrementPosition(pos));
                }
            }
        }

        private AbstractState composeState(int[] p_pos)
        {
            int n = p_pos.Length;

            if (n == 1) return m_statesList[0][p_pos[0]];

            bool marked = m_statesList[0][p_pos[0]].IsMarked;
            var states = new AbstractState[n];
            states[0] = m_statesList[0][p_pos[0]];
            for (int j = 1; j < n; ++j)
            {
                states[j] = m_statesList[j][p_pos[j]];
                marked &= m_statesList[j][p_pos[j]].IsMarked;
            }
            return new CompoundState(states, marked ? Marking.Marked : Marking.Unmarked);
        }

        private State mergeStates(List<int> p_states, int p_index)
        {
            string first = m_statesList[p_index][p_states[0]].ToString();
            StringBuilder name = new StringBuilder(first, (first.Length + 2) * p_states.Count);
            bool marked = m_statesList[p_index][p_states[0]].IsMarked;

            for (var i = 1; i < p_states.Count; ++i)
            {
                name.Append("|");
                name.Append(m_statesList[p_index][p_states[i]].ToString());
                marked |= m_statesList[p_index][p_states[i]].IsMarked;
            }
            return new State(name.ToString(), marked ? Marking.Marked : Marking.Unmarked);
        }

        private void simplify()
        {
            AbstractState[] newStates = new AbstractState[Size];
            AdjacencyMatrix newAdjacencyMatrix = new AdjacencyMatrix((int)Size, m_eventsUnion.Length, true);
            int id = 0, n = m_statesList.Count();
            var positionNewStates = new Dictionary<StatesTuple, int>(StatesTupleComparator.getInstance());

            if (m_validStates == null && n == 1)
                return;

            id = 0;
            int[] pos0 = new int[n];

            if (m_validStates != null)
            {
                foreach (var state in m_validStates)
                {
                    state.Key.Get(pos0, m_bits, m_maxSize);
                    newStates[id] = composeState(pos0);
                    positionNewStates.Add(state.Key, id);
                    ++id;
                }
            }
            else
            {
                do
                {
                    newStates[id] = composeState(pos0);
                    positionNewStates.Add(new StatesTuple(pos0, m_bits, m_tupleSize), id);
                    ++id;
                } while (incrementPosition(pos0));
            }

            Parallel.ForEach(positionNewStates, (state) =>
            {
                int[] pos = new int[n];
                int[] nextPos = new int[n];
                var nextTuple = new StatesTuple(m_tupleSize);

                state.Key.Get(pos, m_bits, m_maxSize);

                for (var e = 0; e < m_eventsUnion.Length; ++e)
                {
                    for (var j = n - 1; j >= 0; --j)
                    {
                        if (m_eventsList[j][e])
                        {
                            if (m_adjacencyList[j].hasEvent(pos[j], e))
                                nextPos[j] = m_adjacencyList[j][pos[j], e];
                            else
                                goto nextEvent;
                        }
                        else
                            nextPos[j] = pos[j];
                    }
                    nextTuple.Set(nextPos, m_bits);
                    int k = 0;
                    if (positionNewStates.TryGetValue(nextTuple, out k))
                    {
                        newAdjacencyMatrix.Add(state.Value, e, k);
                    }
                    nextEvent:;
                }
            });

            m_statesList.Clear();
            m_adjacencyList.Clear();
            m_eventsList.Clear();
            m_validStates = null;

            m_statesList.Add(newStates);
            newAdjacencyMatrix.TrimExcess();
            m_adjacencyList.Add(newAdjacencyMatrix);

            m_eventsList.Add(new bool[m_eventsUnion.Length]);
            for (var j = 0; j < m_eventsUnion.Length; ++j)
                m_eventsList[0][j] = true;

            m_statesList.TrimExcess();
            m_eventsList.TrimExcess();
            positionNewStates = null;

            m_bits = new int[1];
            m_tupleSize = 1;
            m_numberOfPlants = 1;
            m_maxSize = new int[1];
            m_maxSize[0] = (1 << (int)Math.Ceiling(Math.Log(Size, 2))) - 1;

            GC.Collect();
        }

        private static void removeUnusedTransitions(DFA[] composition)
        {
            int s1, s2;
            Stack<int> stack1 = new Stack<int>();
            Stack<int> stack2 = new Stack<int>();
            int n = composition.Count();

            for (int i = 0; i < n; ++i)
            {
                BitArray[] validEvents = new BitArray[composition[i].Size];
                for (int j = 0; j < (int)composition[i].Size; ++j)
                {
                    validEvents[j] = new BitArray(composition[i].m_eventsUnion.Length, false);
                }

                for (int j = 0; j < n; ++j)
                {
                    if (j == i) continue;

                    var other = composition.ElementAt(j);
                    stack1.Push(0);
                    stack2.Push(0);
                    var validStates = new BitArray((int)(composition[i].Size * other.Size), false);

                    var otherEvents = composition[i].m_eventsUnion.Select(e => Array.IndexOf(other.m_eventsUnion, e)).ToArray();

                    var extraEvents = other.m_eventsUnion.Where(e => Array.IndexOf(composition[i].m_eventsUnion, e) == -1)
                                        .Select(e => Array.IndexOf(other.m_eventsUnion, e)).ToArray();

                    while (stack1.Count > 0)
                    {
                        s1 = stack1.Pop();
                        s2 = stack2.Pop();

                        int p = s1 * (int)other.Size + s2;

                        if (validStates[p]) continue;

                        validStates[p] = true;


                        for (int e = 0; e < composition[i].m_eventsUnion.Length; ++e)
                        {
                            if (!composition[i].m_adjacencyList[0].hasEvent(s1, e)) continue;

                            if (otherEvents[e] < 0)
                            {
                                stack1.Push(composition[i].m_adjacencyList[0][s1, e]);
                                stack2.Push(s2);
                                validEvents[s1][e] = true;
                            }
                            else if (other.m_adjacencyList[0].hasEvent(s2, otherEvents[e]))
                            {
                                stack1.Push(composition[i].m_adjacencyList[0][s1, e]);
                                stack2.Push(other.m_adjacencyList[0][s2, otherEvents[e]]);
                                validEvents[s1][e] = true;
                            }
                        }
                        for (int e = 0; e < extraEvents.Length; ++e)
                        {
                            if (other.m_adjacencyList[0].hasEvent(s2, extraEvents[e]))
                            {
                                stack1.Push(s1);
                                stack2.Push(other.m_adjacencyList[0][s2, extraEvents[e]]);
                            }
                        }
                    }
                    for (int k = 0; k < validEvents.Count(); ++k)
                    {
                        for (int e = 0; e < validEvents[k].Count; ++e)
                        {
                            if (!validEvents[k][e] && composition[i].m_adjacencyList[0].hasEvent(k, e))
                            {
                                composition[i].m_adjacencyList[0].Remove(k, e);
                            }
                        }
                    }
                }
            }
        }

        public static bool IsConflicting(IEnumerable<DFA> supervisors)
        {
            Parallel.ForEach(supervisors, s =>
            {
               s.simplify();
            });

            DFA composition = supervisors.Aggregate((a, b) => a.ParallelCompositionWith(b));
            ulong oldSize = composition.Size;
            composition.removeBlockingStates();
            return composition.Size != oldSize;
        }

        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            if (conflictResolvingSupervisor == null) conflictResolvingSupervisor = new DFA[0];
            ;
            IEnumerable<DFA> supervisors = specifications.AsParallel().Select(
                    e =>
                    {
                        return MonolithicSupervisor(plants.Where(p => p.m_eventsUnion.Intersect(e.m_eventsUnion).Any()), new[] { e });
                    });

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete))
            {
                throw new Exception("conflicting supervisors");
            }
            GC.Collect();
            GC.Collect();
            return complete;
        }

        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, out List<DFA> compoundPlants,
            IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            if (conflictResolvingSupervisor == null) conflictResolvingSupervisor = new DFA[0];

            var dic = specifications.ToDictionary(
                    e =>
                    {
                        return plants.Where(p => p.m_eventsUnion.Intersect(e.m_eventsUnion).Any()).Aggregate((a, b) =>
                        {
                            return a.ParallelCompositionWith(b);
                        });
                    });

            var supervisors =
                dic.AsParallel()
                    .Select(automata => MonolithicSupervisor(new[] { automata.Key }, new[] { automata.Value }, true))
                    .ToList();

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete))
            {
                throw new Exception("conflicting supervisors");
            }

            compoundPlants = dic.Keys.ToList();
            GC.Collect();
            GC.Collect();
            return complete;
        }

        public static void ToWmodFile(string p_filename, IEnumerable<DFA> p_plants, IEnumerable<DFA> p_specifications, string p_ModuleName = "UltraDES")
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n"
            };

            using (var writer = XmlWriter.Create(p_filename, settings))
            {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("Module", "http://waters.sourceforge.net/xsd/module");
                writer.WriteAttributeString("Name", p_ModuleName);
                writer.WriteAttributeString("xmlns", "ns2", null, "http://waters.sourceforge.net/xsd/base");

                writer.WriteElementString("ns2", "Comment", null, "By UltraDES");

                writer.WriteStartElement("EventDeclList");

                writer.WriteStartElement("EventDecl");
                writer.WriteAttributeString("Kind", "PROPOSITION");
                writer.WriteAttributeString("Name", ":accepting");
                writer.WriteEndElement();
                writer.WriteStartElement("EventDecl");
                writer.WriteAttributeString("Kind", "PROPOSITION");
                writer.WriteAttributeString("Name", ":forbidden");
                writer.WriteEndElement();

                var v_events = new List<AbstractEvent>();
                foreach(var v_plant in p_plants)
                {
                    v_events = v_events.Union(v_plant.Events).ToList();
                }
                foreach (var v_spec in p_specifications)
                {
                    v_events = v_events.Union(v_spec.Events).ToList();
                }

                foreach (var v_event in v_events)
                {
                    writer.WriteStartElement("EventDecl");
                    writer.WriteAttributeString("Kind", v_event.IsControllable ? "CONTROLLABLE" : "UNCONTROLLABLE");
                    writer.WriteAttributeString("Name", v_event.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteStartElement("ComponentList");

                var addAutomaton = new Action<DFA, string>((G, kind) => {
                    writer.WriteStartElement("SimpleComponent");
                    writer.WriteAttributeString("Kind", kind);
                    writer.WriteAttributeString("Name", G.Name);
                    writer.WriteStartElement("Graph");
                    writer.WriteStartElement("NodeList");
                    for(var i = 0; i < G.m_statesList[0].Count(); ++i)
                    {
                        var v_state = G.m_statesList[0][i];
                        writer.WriteStartElement("SimpleNode");
                        writer.WriteAttributeString("Name", v_state.ToString());
                        if (i == 0)
                            writer.WriteAttributeString("Initial", "true");

                        if (v_state.IsMarked)
                        {
                            writer.WriteStartElement("EventList");
                            writer.WriteStartElement("SimpleIdentifier");
                            writer.WriteAttributeString("Name", ":accepting");
                            writer.WriteEndElement();
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();

                    writer.WriteStartElement("EdgeList");
                    for (var i = 0; i < G.m_statesList[0].Count(); ++i)
                    {
                        string v_source = G.m_statesList[0][i].ToString();

                        for (var e = 0; e < G.m_eventsList[0].Count(); ++e)
                        {
                            var v_target = G.m_adjacencyList[0][i, e];
                            if (v_target >= 0)
                            {
                                writer.WriteStartElement("Edge");
                                writer.WriteAttributeString("Source", v_source);
                                writer.WriteAttributeString("Target", G.m_statesList[0][v_target].ToString());
                                writer.WriteStartElement("LabelBlock");
                                writer.WriteStartElement("SimpleIdentifier");
                                writer.WriteAttributeString("Name", G.m_eventsUnion[e].ToString());
                                writer.WriteEndElement();
                                writer.WriteEndElement();
                                writer.WriteEndElement();
                            }
                        }
                    }
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                });

                foreach (var v_plant in p_plants)
                {
                    addAutomaton(v_plant, "PLANT");
                }
                foreach (var v_spec in p_specifications)
                {
                    addAutomaton(v_spec, "SPEC");
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            GC.Collect();
        }

        public static void FromWmodFile(string p_filename, out List<DFA> p_plants, out List<DFA> p_specs)
        {
            p_plants = new List<DFA>();
            p_specs = new List<DFA>();
            var v_events = new List<AbstractEvent>();
            var v_eventsMap = new Dictionary<string, AbstractEvent>();

            using (var reader = XmlReader.Create(p_filename, new XmlReaderSettings()))
            {
                if (!reader.ReadToFollowing("EventDeclList"))
                {
                    throw new Exception("Invalid Format.");
                }
                var eventsReader = reader.ReadSubtree();
                while (eventsReader.ReadToFollowing("EventDecl"))
                {
                    eventsReader.MoveToAttribute("Kind");
                    var v_kind = eventsReader.Value;
                    if (v_kind == "PROPOSITION") continue;
                    eventsReader.MoveToAttribute("Name");
                    var v_name = eventsReader.Value;
                    var ev = new Event(v_name, v_kind == "CONTROLLABLE" ?
                        Controllability.Controllable : Controllability.Uncontrollable);

                    v_events.Add(ev);
                    v_eventsMap.Add(v_name, ev);
                }

                if (!reader.ReadToFollowing("ComponentList"))
                {
                    throw new Exception("Invalid Format.");
                }

                var componentsReader = reader.ReadSubtree();

                while (componentsReader.ReadToFollowing("SimpleComponent"))
                {
                    componentsReader.MoveToAttribute("Kind");
                    var v_kind = componentsReader.Value;
                    componentsReader.MoveToAttribute("Name");
                    var v_DFAName = componentsReader.Value;

                    if (!componentsReader.ReadToFollowing("NodeList"))
                    {
                        throw new Exception("Invalid Format.");
                    }
                    var statesReader = componentsReader.ReadSubtree();

                    var states = new Dictionary<string, AbstractState>();

                    string initial = "";
                    while (statesReader.ReadToFollowing("SimpleNode"))
                    {
                        var evList = statesReader.ReadSubtree();

                        statesReader.MoveToAttribute("Name");
                        var v_name = statesReader.Value;
                        bool v_marked = false;
                        if(statesReader.MoveToAttribute("Initial") && statesReader.ReadContentAsBoolean())
                        {
                            initial = v_name;
                        }
                        
                        if (evList.ReadToFollowing("EventList"))
                        {
                            evList.ReadToFollowing("SimpleIdentifier");
                            evList.MoveToAttribute("Name");
                            v_marked = evList.Value == ":accepting";
                        }
                        states.Add(v_name, new State(v_name, v_marked ? Marking.Marked : Marking.Unmarked));
                    }

                    if (!componentsReader.ReadToFollowing("EdgeList"))
                    {
                        throw new Exception("Invalid Format.");
                    }

                    var edgesReader = componentsReader.ReadSubtree();
                    var transitions = new List<Transition>();
                    while (edgesReader.ReadToFollowing("Edge"))
                    {
                        eventsReader = componentsReader.ReadSubtree();

                        edgesReader.MoveToAttribute("Source");
                        string v_source = edgesReader.Value;
                        edgesReader.MoveToAttribute("Target");
                        string v_target = edgesReader.Value;

                        while (eventsReader.ReadToFollowing("SimpleIdentifier"))
                        {
                            eventsReader.MoveToAttribute("Name");
                            var v_event = eventsReader.Value;
                            transitions.Add(new Transition(states[v_source], v_eventsMap[v_event], states[v_target]));
                        }
                    }

                    var G = new DFA(transitions, states[initial], v_DFAName);
                    if(v_kind == "PLANT") p_plants.Add(G);
                    else p_specs.Add(G);
                }
            }
            GC.Collect();
        }

	    public static DFA FromAdsFile(string p_FilePath)
        {
            var v_file = File.OpenText(p_FilePath);

            var v_name = NextValidLine(v_file);

            if (!NextValidLine(v_file).Contains("State size"))
                throw new Exception("File is not on ADS Format.");

            var states = int.Parse(NextValidLine(v_file));

            if (!NextValidLine(v_file).Contains("Marker states"))
                throw new Exception("File is not on ADS Format.");

            var v_marked = string.Empty;

            var v_line = NextValidLine(v_file);
            if (!v_line.Contains("Vocal states"))
            {
                v_marked = v_line;
                v_line = NextValidLine(v_file);
            }

            AbstractState[] v_stateSet;

            if (v_marked == "*")
            {
                v_stateSet = Enumerable.Range(0, states).Select(i => new State(i.ToString(), Marking.Marked)).ToArray();
            }
            else if (v_marked == string.Empty)
            {
                v_stateSet = Enumerable.Range(0, states).Select(i => new State(i.ToString(), Marking.Unmarked)).ToArray();
            }
            else
            {
                var markedSet = v_marked.Split().Select(int.Parse).ToList();
                v_stateSet =
                    Enumerable.Range(0, states)
                        .Select(
                            i =>
                                markedSet.Contains(i)
                                    ? new State(i.ToString(), Marking.Marked)
                                    : new State(i.ToString(), Marking.Unmarked))
                        .ToArray();
            }

            if (!v_line.Contains("Vocal states"))
                throw new Exception("File is not on ADS Format.");

            v_line = NextValidLine(v_file);
            while (!v_line.Contains("Transitions"))
                v_line = NextValidLine(v_file);

            var evs = new Dictionary<int, AbstractEvent>();
            var transitions = new List<Transition>();

            while (!v_file.EndOfStream)
            {
                v_line = NextValidLine(v_file);
                if (v_line == string.Empty) continue;

                var trans = v_line.Split().Select(int.Parse).ToArray();

                if (!evs.ContainsKey(trans[1]))
                {
                    var e = new Event(trans[1].ToString(),
                        trans[1] % 2 == 0 ? Controllability.Uncontrollable : Controllability.Controllable);
                    evs.Add(trans[1], e);
                }

                transitions.Add(new Transition(v_stateSet[trans[0]], evs[trans[1]], v_stateSet[trans[2]]));
            }

            return new DFA(transitions, v_stateSet[0], v_name);
        }

        public DFA InverseProjection(IEnumerable<AbstractEvent> events)
        {
            if (IsEmpty())  return this.Clone();

            var evs = events.Except(Events).ToList();

            var invProj = this.Clone();

            if (evs.Count > 0)
            {
                int envLength = m_eventsUnion.Length + evs.Count;

                invProj.m_eventsUnion = invProj.m_eventsUnion.Union(evs).OrderBy(i => i.Controllability).ToArray();
                int[] evMap = m_eventsUnion.Select(i => Array.IndexOf(invProj.m_eventsUnion, i)).ToArray();
                int[] evMapNew = evs.Select(i => Array.IndexOf(invProj.m_eventsUnion, i)).ToArray();

                for (var i = 0; i < m_statesList.Count; ++i)
                {
                    invProj.m_adjacencyList[i] = new AdjacencyMatrix(m_statesList[i].Length, envLength);
                    for(var j = 0; j < m_statesList[i].Length; ++j)
                    {
                        for(var e = 0; e < m_eventsUnion.Length; ++e)
                        {
                            if(m_adjacencyList[i].hasEvent(j, e))
                                invProj.m_adjacencyList[i].Add(j, evMap[e], m_adjacencyList[i][j, e]);
                        }
                        for(var e = 0; e < evs.Count; ++e)
                            invProj.m_adjacencyList[i].Add(j, evMapNew[e], j);
                    }
                    invProj.m_eventsList[i] = new bool[envLength];
                    for (var e = 0; e < m_eventsUnion.Length; ++e)
                        invProj.m_eventsList[i][evMap[e]] = m_eventsList[i][e];
                    for (var e = 0; e < evs.Count; ++e)
                        invProj.m_eventsList[i][evMapNew[e]] = true;
                }
            }
            invProj.Name = string.Format("InvProjection({0})", this.Name);
            return invProj;
        }

        public DFA InverseProjection(params AbstractEvent[] events)
        {
            return InverseProjection((IEnumerable<AbstractEvent>)events);
        }

        private HashSet<int> getExtendedState(int state, IEnumerable<int> toRemove)
        {
            var exp = new HashSet<int>();
            exp.Add(state);
            foreach (var e in toRemove)
            {
                if (m_adjacencyList[0].hasEvent(state, e))
                {
                    int next = m_adjacencyList[0][state, e];
                    if (!exp.Contains(next)) exp.Add(next);
                }
            }
            return exp;
        }

        private int getNewPosition(List<int> states, Dictionary<List<int>, int> statesHash, List<AbstractState> statesList)
        {
            int newPos;

            lock (m_lockObject3)
            {
                if (!statesHash.TryGetValue(states, out newPos))
                {
                    newPos = statesHash.Count;
                    statesHash.Add(states, newPos);
                    statesList.Add(mergeStates(states, 0));
                }
            }
            return newPos;
        }

        public DFA Projection(IEnumerable<AbstractEvent> removeEvents)
        {
            if (IsEmpty()) return this.Clone();

            var evLength = m_eventsUnion.Length;
            simplify();

            var proj = new DFA(1);

            var transitions = new HashSet<int[]>(IntArrayComparator.getInstance());
            var statesList = new List<AbstractState>();
            var statesHash = new Dictionary<List<int>, int>(IntListComparator.getInstance());

            var evs = removeEvents.Select(e => Array.IndexOf(m_eventsUnion, e)).Where(i => i >= 0);
            var removeEventsHash = new bool[evLength];
            foreach (var e in evs) removeEventsHash[e] = true;

            if (evs.Count() == 0)
            {
                return this.Clone();
            }

            var visitedStates = new Dictionary<int, bool>((int)this.Size);

            var initial = getExtendedState(0, evs);
            var frontier = new Stack<Tuple<HashSet<int>, int>>();

            proj.m_tupleSize = 1;
            proj.m_bits[0] = 0;
            proj.m_maxSize[0] = m_maxSize[0];

            var newPosition = getNewPosition(initial.OrderBy(i => i).ToList(), statesHash, statesList);
            visitedStates.Add(newPosition, true);

            frontier.Push(new Tuple<HashSet<int>, int>(initial, newPosition));

            var threadsRunning = 0;

            proj.Size = 0;

            var projectionAction = new Action(() => {
                ulong statesCount = 0;
                int p = 0, position, nextPos;

                while (true)
                {
                    lock (m_lockObject2)
                    {
                        ++threadsRunning;
                    }
                    while (true)
                    {
                        HashSet<int> states;
                        lock (m_lockObject2)
                        {
                            if (frontier.Count == 0) break;

                            var tuple = frontier.Pop();
                            states = tuple.Item1;
                            position = tuple.Item2;
                        }
                        ++statesCount;

                        p = 0;
                        for (var e = 0; e < evLength; ++e)
                        {
                            if (removeEventsHash[e]) continue;

                            var nextStateHash = new HashSet<int>();

                            foreach (var st in states)
                            {
                                if (!m_eventsList[0][e])
                                    nextPos = st;
                                else if (m_adjacencyList[0].hasEvent(st, e))
                                    nextPos = m_adjacencyList[0][st, e];
                                else
                                    continue;

                                foreach (var ePos in getExtendedState(nextPos, evs))
                                {
                                    if (!nextStateHash.Contains(ePos)) nextStateHash.Add(ePos);
                                }
                            }
                            if (nextStateHash.Count > 0)
                            {
                                var nextPosition = getNewPosition(nextStateHash.OrderBy(i => i).ToList(), statesHash, statesList);
                                if (m_eventsList[0][e])
                                {
                                    var transitionTuple = new int[] { position, p, nextPosition };
                                    lock (m_lockObject)
                                    {
                                        if (!transitions.Contains(transitionTuple))
                                            transitions.Add(transitionTuple);
                                    }
                                }
                                lock (m_lockObject2)
                                {
                                    if (!visitedStates.ContainsKey(nextPosition))
                                    {
                                        visitedStates.Add(nextPosition, true);
                                        frontier.Push(new Tuple<HashSet<int>, int>(nextStateHash, nextPosition));
                                    }
                                }
                            }
                            ++p;
                        }
                    }
                    lock (m_lockObject2)
                    {
                        --threadsRunning;
                        if (threadsRunning == 0) break;
                    }
                    Thread.Sleep(5);
                }
                lock (m_lockObject2)
                {
                    proj.Size += statesCount;
                }
            });

            Thread[] threads = new Thread[m_numberOfThreads - 1];

            for (var i = 0; i < m_numberOfThreads - 1; ++i)
            {
                threads[i] = new Thread(new ParameterizedThreadStart(j => projectionAction()));
                threads[i].Start();
            }

            projectionAction();

            for (var i = 0; i < m_numberOfThreads - 1; ++i)
            {
                threads[i].Join();
            }

            var newEvCount = evLength - evs.Count();
            proj.m_eventsUnion = new Event[newEvCount];

            proj.m_statesList.Add(statesList.ToArray());
            proj.m_eventsList.Add(new bool[newEvCount]);
            proj.m_adjacencyList.Add(new AdjacencyMatrix(proj.m_statesList[0].Length, newEvCount));
            var k = 0;
            for (var e = 0; e < evLength; ++e)
            {
                if (removeEventsHash[e]) continue;
                proj.m_eventsUnion[k] = m_eventsUnion[e];
                proj.m_eventsList[0][k] = m_eventsList[0][e];
                ++k;
            }
            foreach (var j in transitions)
            {
                proj.m_adjacencyList[0].Add(j[0], j[1], j[2]);
            }
            proj.m_adjacencyList[0].TrimExcess();

            proj.Name = string.Format("Projection({0})", Name);

            return proj;
        }

        public DFA Projection(params AbstractEvent[] removeEvents)
        {
            return this.Projection((IEnumerable<AbstractEvent>)removeEvents);
        }

        public void ToAdsFile(string filepath, int odd = 1, int even = 2)
        {
            ToAdsFile(new[] { this }, new[] { filepath }, odd, even);
        }

        public static void ToAdsFile(IEnumerable<DFA> automota, IEnumerable<string> filepaths, int odd = 1, int even = 2)
        {
            foreach (var g in automota)
            {
                g.simplify();
            }

            var v_eventsUnion = automota.SelectMany(g => g.Events).Distinct();
            var v_events = new Dictionary<AbstractEvent, int>();

            foreach(var e in v_eventsUnion)
            {
                if (!e.IsControllable)
                {
                    v_events.Add(e, even);
                    even += 2;
                }
                else
                {
                    v_events.Add(e, odd);
                    odd += 2;
                }
            }

            var v_filepaths = filepaths.ToArray();
            int i = -1;

            foreach (var g in automota)
            {
                ++i;
                var v_eventsMaps = new Dictionary<int, int>();

                for (var e = 0; e < g.m_eventsUnion.Length; ++e)
                {
                    v_eventsMaps.Add(e, v_events[g.m_eventsUnion[e]]);
                }

                var file = File.CreateText(v_filepaths[i]);

                file.WriteLine("# UltraDES ADS FILE - LACSED | UFMG\r\n");

                file.WriteLine("{0}\r\n", g.Name);

                file.WriteLine("State size (State set will be (0,1....,size-1)):");
                file.WriteLine("{0}\r\n", g.Size);

                file.WriteLine("Marker states:");
                string v_markedStates = "";
                bool v_first = true;
                for (var s = 0; s < g.m_statesList[0].Length; ++s)
                {
                    if (g.m_statesList[0][s].IsMarked)
                    {
                        if (v_first)
                        {
                            v_first = false;
                            v_markedStates = s.ToString();
                        }
                        else
                        {
                            v_markedStates += " " + s.ToString();
                        }
                    }
                }
                file.WriteLine("{0}\r\n", v_markedStates);

                file.WriteLine("Vocal states:\r\n");

                file.WriteLine("Transitions:");

                for (int s = 0; s < g.m_statesList[0].Length; ++s)
                {
                    foreach (var t in g.m_adjacencyList[0][s])
                    {
                        file.WriteLine("{0} {1} {2}", s, v_eventsMaps[t.Key], t.Value);
                    }
                }

                file.Close();
            }
        }

        private static string NextValidLine(StreamReader file)
        {
            var line = string.Empty;
            while (line == string.Empty && !file.EndOfStream)
            {
                line = file.ReadLine();
                if (line == "" || line[0] == '#')
                {
                    line = string.Empty;
                    continue;
                }

                var ind = line.IndexOf('#');
                if (ind != -1) line = line.Remove(ind);
                line = line.Trim();
            }

            return line;
        }

        private bool IsEmpty()
        {
            return Size == 0;
        }

        private bool IsMarketState(int[] p_pos)
        {
            int n = m_statesList.Count;
            for (int i = 0; i < n; ++i)
            {
                if (!m_statesList[i][p_pos[i]].IsMarked)
                {
                    return false;
                }
            }
            return true;
        }

        private bool incrementPosition(int[] p_pos)
        {
            int k = m_statesList.Count - 1;
            while (k >= 0)
            {
                if (++p_pos[k] < m_statesList[k].Length)
                    return true;
                p_pos[k] = 0;
                --k;
            }
            return false;
        }

        public bool IsControllable()
        {
            return false;
        }

        public void drawSVGFigure(string fileName = null, bool openAfterFinish = true)
        {
            if (fileName == null) fileName = Name;
            fileName = fileName.Replace('|', '_');
            Drawing.drawSVG(this, fileName, openAfterFinish);
        }

        public void drawLatexFigure(string fileName = null, bool openAfterFinish = true)
        {
            if (fileName == null) fileName = Name;
            fileName = fileName.Replace('|', '_');
            Drawing.drawLatexFigure(this, fileName, openAfterFinish);
        }

        public void showAutomaton(string name = "Automaton")
        {
            GraphVizDraw.showAutomaton(this, name);
        }

        public Dictionary<string, string> simplifyName(string newName = null, bool simplifyStatesName = true)
        {
            simplify();
            var namesMap = new Dictionary<string, string>(m_statesList[0].Length);

            if (simplifyStatesName)
            {
                for (var s = 0; s < m_statesList[0].Length; ++s)
                {
                    string newStateName = s.ToString();
                    namesMap.Add(newStateName, m_statesList[0][s].ToString());
                    m_statesList[0][s] = new State(newStateName, m_statesList[0][s].Marking);
                }
            }

            if(newName != null)
            {
                Name = newName;
            }

            return namesMap;
        }

        public void ToFsmFile(string fileName = null)
        {
            int n = m_statesList.Count;
            var pos = new int[n];
            if (fileName == null) fileName = Name;
            fileName = fileName.Replace('|', '_');

            if (!fileName.EndsWith(".fsm")) fileName += ".fsm";

            var writer = new StreamWriter(fileName);

            writer.Write("{0}\n\n", Size);

            var writeState = new Action<AbstractState, Transition[]>((s, transtions) => {
                writer.Write("{0}\t{1}\t{2}\n", s.ToString(), s.IsMarked ? 1 : 0, transtions.Length);
                foreach (var t in transtions)
                {
                    writer.Write("{0}\t{1}\t{2}\to\n", t.Trigger.ToString(), t.Destination.ToString(), 
                                                       t.Trigger.IsControllable ? "c" : "uc");
                }
                writer.Write("\n");
            });

            if (!IsEmpty())
            {
                if (m_validStates != null)
                {
                    writeState(composeState(pos), getTransitionsFromState(pos).ToArray());
                    foreach (var sT in m_validStates)
                    {
                        sT.Key.Get(pos, m_bits, m_maxSize);
                        // imprimimos somente se não for o estado inicial, já que o estado inicial já
                        // foi impresso
                        for(var i = 0; i < n; ++i)
                        {
                            if(pos[i] != 0)
                            {
                                writeState(composeState(pos), getTransitionsFromState(pos).ToArray());
                                break;
                            }
                        }
                    }
                }
                else
                {
                    do
                    {
                        writeState(composeState(pos), getTransitionsFromState(pos).ToArray());
                    } while (incrementPosition(pos));
                }
            }
            writer.Close();
        }

        public static DFA FromFsmFile(string fileName)
        {
            if(fileName == null)
            {
                throw new Exception("Filename can not be null.");
            }

            if (!File.Exists(fileName))
            {
                throw new Exception("File not found.");
            }

            var automatonName = fileName.Split('/').Last().Split('.').First();
            var statesList = new Dictionary<string, AbstractState>();
            var eventsList = new Dictionary<string, AbstractEvent>();
            var transitionsList = new List<Transition>();
            AbstractState initialState = null;

            var reader = new StreamReader(fileName);

            string numberOfStates = reader.ReadLine();
            if(numberOfStates == null || numberOfStates == "")
            {
                throw new Exception("Invalid Format.");
            }
            int numStates = int.Parse(numberOfStates);

            // reads all states first
            for (var i = 0; i < numStates; ++i)
            {
                string stateLine;
                do { stateLine = reader.ReadLine(); } while (stateLine == "");
                if(stateLine == null)
                {
                    throw new Exception("Invalid Format.");
                }
                var stateInfo = stateLine.Split('\t');
                if(stateInfo.Length != 3)
                {
                    throw new Exception("Invalid Format.");
                }
                if (statesList.ContainsKey(stateInfo[0]))
                {
                    throw new Exception("Invalid Format: Duplicated state.");
                }
                var state = new State(stateInfo[0], stateInfo[1] == "1" ? Marking.Marked : Marking.Unmarked);
                statesList.Add(stateInfo[0], state);

                if (initialState == null) initialState = state;

                var numTransitions = int.Parse(stateInfo[2]);
                for (var t = 0; t < numTransitions; ++t) reader.ReadLine();
            }

            reader.DiscardBufferedData();
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            reader.ReadLine();

            for (var i = 0; i < numStates; ++i)
            {
                string stateLine;
                do { stateLine = reader.ReadLine(); } while (stateLine == "");
                var stateInfo = stateLine.Split('\t');
                var numTransitions = int.Parse(stateInfo[2]);
                for (var t = 0; t < numTransitions; ++t)
                {
                    var transition = reader.ReadLine().Split('\t');
                    if (transition.Length != 4)
                    {
                        throw new Exception("Invalid Format.");
                    }
                    if (!eventsList.ContainsKey(transition[0]))
                    {
                        eventsList.Add(transition[0], new Event(transition[0], transition[2] == "c" ? 
                                        Controllability.Controllable : Controllability.Uncontrollable));
                    }
                    if (!statesList.ContainsKey(transition[1]))
                    {
                        throw new Exception("Invalid transition. Destination state not found.");
                    }
                    transitionsList.Add(new Transition(statesList[stateInfo[0]], 
                                        eventsList[transition[0]], statesList[transition[1]]));
                }
            }

            return new DFA(transitionsList, initialState, automatonName);
        }
    }
    
}
