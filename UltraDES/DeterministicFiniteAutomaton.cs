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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace UltraDES
{
    using DFA = DeterministicFiniteAutomaton;

    [Serializable]
    public sealed class DeterministicFiniteAutomaton
    {
        private static readonly int NumberOfThreads = Math.Max(2, Environment.ProcessorCount);

        private readonly List<AdjacencyMatrix> _adjacencyList;

        private readonly List<bool[]> _eventsList;

        private readonly object _lockObject = new object();

        private readonly object _lockObject2 = new object();

        private readonly object _lockObject3 = new object();

        private readonly List<AbstractState[]> _statesList;

        private int[] _bits;

        private AbstractEvent[] _eventsUnion;
        private int[] _maxSize;
        private int _numberOfPlants;

        private int _numberOfRunningThreads;

        private List<List<int>[]>[] _reverseTransitionsList;

        private Stack<StatesTuple> _statesStack;
        private int _tupleSize;

        private Dictionary<StatesTuple, bool> _validStates;

        private Stack<bool> m_removeBadStates;

        static DeterministicFiniteAutomaton()
        {
            var currentPath = Directory.GetCurrentDirectory();
            var origem = currentPath;
            var newPath = currentPath + "\\..\\..\\..\\USER";
            if (!Directory.Exists(newPath))
            {
                newPath = currentPath + "\\USER";
                if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
            }

            Directory.SetCurrentDirectory(newPath);
        }

        public DeterministicFiniteAutomaton(IEnumerable<Transition> transitions, AbstractState initial, string name)
            : this(1)
        {
            Name = name;

            var transitionsLocal = transitions as Transition[] ?? transitions.ToArray();

            _statesList.Add(transitionsLocal.SelectMany(t => new[] {t.Origin, t.Destination}).Union(new[] {initial})
                .Distinct().ToArray());
            _eventsUnion = transitionsLocal.Select(t => t.Trigger).Distinct().OrderBy(i => i.Controllability).ToArray();
            _adjacencyList.Add(new AdjacencyMatrix(_statesList[0].Length, _eventsUnion.Length));
            var initialIdx = Array.IndexOf(_statesList[0], initial);
            if (initialIdx != 0)
            {
                var aux = _statesList[0][0];
                _statesList[0][0] = _statesList[0][initialIdx];
                _statesList[0][initialIdx] = aux;
            }

            var events = new bool[_eventsUnion.Length];

            for (var i = 0; i < events.Length; ++i)
                events[i] = true;
            _eventsList.Add(events);

            Size = (ulong) _statesList[0].Length;

            _bits[0] = 0;
            _maxSize[0] = (1 << (int) Math.Ceiling(Math.Log(Size, 2))) - 1;
            _tupleSize = 1;

            for (var i = 0; i < _statesList[0].Length; ++i)
                _adjacencyList[0].Add(i,
                    transitionsLocal.AsParallel()
                        .Where(t => t.Origin == _statesList[0][i])
                        .Select(
                            t => Tuple.Create(Array.IndexOf(_eventsUnion, t.Trigger),
                                Array.IndexOf(_statesList[0], t.Destination)))
                        .ToArray());
        }

        private DeterministicFiniteAutomaton(int n)
        {
            _statesList = new List<AbstractState[]>(n);
            _eventsList = new List<bool[]>(n);
            _adjacencyList = new List<AdjacencyMatrix>(n);
            _bits = new int[n];
            _maxSize = new int[n];
            _numberOfPlants = n;
        }

        public ulong Size { get; private set; }

        public string Name { get; private set; }

        public IEnumerable<AbstractState> States
        {
            get
            {
                var n = _statesList.Count;
                var pos = new int[n];

                if (_validStates != null)
                    foreach (var s in _validStates)
                    {
                        s.Key.Get(pos, _bits, _maxSize);
                        yield return composeState(pos);
                    }
                else if (!IsEmpty())
                    do
                    {
                        yield return composeState(pos);
                    } while (IncrementPosition(pos));
            }
        }

        public IEnumerable<AbstractState> MarkedStates
        {
            get
            {
                var n = _statesList.Count;
                var pos = new int[n];

                if (_validStates != null)
                    foreach (var s in _validStates)
                    {
                        s.Key.Get(pos, _bits, _maxSize);
                        if (IsMarketState(pos))
                            yield return composeState(pos);
                    }
                else if (!IsEmpty())
                    do
                    {
                        if (IsMarketState(pos))
                            yield return composeState(pos);
                    } while (IncrementPosition(pos));
            }
        }

        public AbstractState InitialState
        {
            get
            {
                if (IsEmpty()) return null;
                return composeState(new int[_statesList.Count]);
            }
        }

        public IEnumerable<AbstractEvent> Events
        {
            get
            {
                foreach (var e in _eventsUnion) yield return e;
            }
        }

        public IEnumerable<AbstractEvent> UncontrollableEvents
        {
            get { return _eventsUnion.Where(i => !i.IsControllable); }
        }

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

        public DFA Trim
        {
            get
            {
                var G = AccessiblePart;
                G.RemoveBlockingStates();
                return G;
            }
        }

        public int KleeneClosure
        {
            get
            {
                Console.WriteLine("Sorry. This is still in TO-DO List");
                return 0;
            }
        }

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
                var G = Clone();

                for (var i = 0; i < G._statesList.Count; ++i)
                for (var s = 0; s < G._statesList[i].Length; ++s)
                    G._statesList[i][s] = G._statesList[i][s].ToMarked;

                return G;
            }
        }

        public string ToDotCode
        {
            get
            {
                var dot = new StringBuilder("digraph {\nrankdir=TB;", (int) Size * InitialState.ToString().Length);

                dot.Append("\nnode [shape = doublecircle];");

                foreach (var ms in MarkedStates)
                    dot.AppendFormat(" \"{0}\" ", ms);

                dot.Append("\nnode [shape = circle];");

                var n = _statesList.Count;
                var pos = new int[n];

                var addTransitions = new Action(() =>
                {
                    foreach (var group in getTransitionsFromState(pos).GroupBy(t => t.Destination))
                    {
                        dot.AppendFormat("\"{0}\" -> \"{1}\" [ label = \"", group.First().Origin, group.Key);
                        var first = true;
                        foreach (var t in group)
                        {
                            if (!first) dot.Append(",");
                            else first = false;
                            dot.Append(t.Trigger);
                        }

                        dot.Append("\" ];\n");
                    }
                });

                if (_validStates != null)
                {
                    foreach (var s in _validStates)
                    {
                        s.Key.Get(pos, _bits, _maxSize);
                        if (!IsMarketState(pos))
                            dot.AppendFormat(" \"{0}\" ", composeState(pos));
                    }

                    dot.AppendFormat("\nnode [shape = point ]; Initial\nInitial -> \"{0}\";\n", InitialState);
                    foreach (var s in _validStates)
                    {
                        s.Key.Get(pos, _bits, _maxSize);
                        addTransitions();
                    }
                }
                else if (!IsEmpty())
                {
                    do
                    {
                        if (!IsMarketState(pos))
                            dot.AppendFormat(" \"{0}\" ", composeState(pos));
                    } while (IncrementPosition(pos));

                    dot.AppendFormat("\nnode [shape = point ]; Initial\nInitial -> \"{0}\";\n", InitialState);

                    do
                    {
                        addTransitions();
                    } while (IncrementPosition(pos));
                }

                dot.Append("}");

                return dot.ToString();
            }
        }

        public RegularExpression ToRegularExpression
        {
            get
            {
                if (IsEmpty()) return Symbol.Empty;
                simplify();

                var t = Enumerable.Range(0, (int) Size).ToArray();

                var len = _statesList.Count;
                var size = (int) Size;
                var b = new RegularExpression[size];
                var a = new RegularExpression[size, size];

                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < size; j++) a[i, j] = Symbol.Empty;

                    for (var e = 0; e < _eventsUnion.Length; e++)
                        if (_adjacencyList[0].hasEvent(i, e))
                            a[i, _adjacencyList[0][i, e]] += _eventsUnion[e];
                }

                for (var i = 0; i < size; ++i)
                    b[i] = _statesList[0][i].IsMarked ? Symbol.Epsilon : Symbol.Empty;

                for (var n = size - 1; n >= 0; n--)
                {
                    b[n] = new KleeneStar(a[n, n]) * b[n];
                    for (var j = 0; j <= n; j++) a[n, j] = new KleeneStar(a[n, n]) * a[n, j];
                    for (var i = 0; i <= n; i++)
                    {
                        b[i] += a[i, n] * b[n];
                        for (var j = 0; j <= n; j++) a[i, j] += a[i, n] * a[n, j];
                    }
                }

                return b[0].Simplify;
            }
        }

        public string ToXML
        {
            get
            {
                simplify();
                var doc = new XmlDocument();
                var automaton = (XmlElement) doc.AppendChild(doc.CreateElement("Automaton"));
                automaton.SetAttribute("Name", Name);

                var states = (XmlElement) automaton.AppendChild(doc.CreateElement("States"));
                for (var i = 0; i < _statesList[0].Length; i++)
                {
                    var state = _statesList[0][i];

                    var s = (XmlElement) states.AppendChild(doc.CreateElement("State"));
                    s.SetAttribute("Name", state.ToString());
                    s.SetAttribute("Marking", state.Marking.ToString());
                    s.SetAttribute("Id", i.ToString());
                }

                var initial = (XmlElement) automaton.AppendChild(doc.CreateElement("InitialState"));
                initial.SetAttribute("Id", "0");

                var events = (XmlElement) automaton.AppendChild(doc.CreateElement("Events"));
                for (var i = 0; i < _eventsUnion.Length; i++)
                {
                    var @event = _eventsUnion[i];

                    var e = (XmlElement) events.AppendChild(doc.CreateElement("Event"));
                    e.SetAttribute("Name", @event.ToString());
                    e.SetAttribute("Controllability", @event.Controllability.ToString());
                    e.SetAttribute("Id", i.ToString());
                }

                var transitions = (XmlElement) automaton.AppendChild(doc.CreateElement("Transitions"));
                for (var i = 0; i < _statesList[0].Length; i++)
                for (var j = 0; j < _eventsUnion.Length; j++)
                {
                    var k = _adjacencyList[0].hasEvent(i, j) ? _adjacencyList[0][i, j] : -1;
                    if (k == -1) continue;

                    var t = (XmlElement) transitions.AppendChild(doc.CreateElement("Transition"));

                    t.SetAttribute("Origin", i.ToString());
                    t.SetAttribute("Trigger", j.ToString());
                    t.SetAttribute("Destination", k.ToString());
                }

                return doc.OuterXml;
            }
        }

        public Func<AbstractState, AbstractEvent, Option<AbstractState>> TransitionFunction
        {
            get
            {
                var n = _statesList.Count;
                var st = new Dictionary<AbstractState, int>[n];
                for (var i = 0; i < n; ++i)
                {
                    st[i] = new Dictionary<AbstractState, int>(_statesList[i].Length);
                    for (var j = 0; j < _statesList[i].Length; ++j) st[i].Add(_statesList[i][j], j);
                }

                return (s, e) =>
                {
                    if (e == Epsilon.EpsilonEvent) return Some<AbstractState>.Create(s);
                    var p = s as CompoundState;
                    var S = p != null ? p.S : new[] {s};
                    if (n == 1 && S.Length > 1) S = new[] {p.Join()};

                    if (S.Length != n) return None<AbstractState>.Create();

                    var pos = new int[n];
                    for (var i = 0; i < n; ++i)
                        if (!st[i].TryGetValue(S[i], out pos[i]))
                            return None<AbstractState>.Create();
                    var k = Array.IndexOf(_eventsUnion, e);
                    if (k < 0) return None<AbstractState>.Create();

                    for (var i = 0; i < n; ++i)
                        if (_eventsList[i][k])
                        {
                            pos[i] = _adjacencyList[i][pos[i], k];
                            if (pos[i] < 0) return None<AbstractState>.Create();
                        }

                    if (_validStates != null && !_validStates.ContainsKey(new StatesTuple(pos, _bits, _tupleSize)))
                        return None<AbstractState>.Create();

                    return Some<AbstractState>.Create(n == 1 ? _statesList[0][pos[0]] : composeState(pos));
                };
            }
        }

        public IEnumerable<Transition> Transitions
        {
            get
            {
                var n = _statesList.Count;
                var pos = new int[n];
                if (_validStates != null)
                    foreach (var s in _validStates)
                    {
                        s.Key.Get(pos, _bits, _maxSize);
                        foreach (var i in getTransitionsFromState(pos))
                            yield return i;
                    }
                else if (!IsEmpty())
                    do
                    {
                        foreach (var i in getTransitionsFromState(pos))
                            yield return i;
                    } while (IncrementPosition(pos));
            }
        }

        public DFA Clone(int capacity)
        {
            var G = new DFA(capacity);
            G._eventsUnion = (AbstractEvent[]) _eventsUnion.Clone();
            G._statesList.AddRange(_statesList);

            for (var i = 0; i < _adjacencyList.Count; ++i)
            {
                G._adjacencyList.Add(_adjacencyList[i].Clone());
                G._eventsList.Add((bool[]) _eventsList[i].Clone());
                G._bits[i] = _bits[i];
                G._maxSize[i] = _maxSize[i];
            }

            G.Size = Size;
            G.Name = Name;
            G._numberOfPlants = _numberOfPlants;
            G._tupleSize = _tupleSize;

            if (_validStates != null)
                G._validStates = new Dictionary<StatesTuple, bool>(_validStates, StatesTupleComparator.getInstance());

            return G;
        }

        public DFA Clone()
        {
            return Clone(_statesList.Count());
        }

        private void RemoveNoAccessibleStates()
        {
            if (_validStates == null)
            {
                _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.getInstance());
                DepthFirstSearch(true);
            }
            else DepthFirstSearch(false);
        }

        private void FindSupervisor(int nPlant, bool nonBlocking)
        {
            _numberOfRunningThreads = 0;
            _statesStack = new Stack<StatesTuple>();
            m_removeBadStates = new Stack<bool>();

            _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.getInstance());

            MakeReverseTransitions();

            var initialIndex = new StatesTuple(new int[_statesList.Count], _bits, _tupleSize);
            _statesStack.Push(initialIndex);
            m_removeBadStates.Push(false);

            var vThreads = new Task[NumberOfThreads - 1];

            for (var i = 0; i < NumberOfThreads - 1; ++i)
            {
                vThreads[i] = Task.Factory.StartNew(() => findStates(nPlant));
            }

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

        private void findStates(int obj)
        {
            var n = _statesList.Count;
            int nPlant = (int) obj;
            var pos = new int[n];
            var nextPosition = new int[n];
            var uncontrollableEventsCount = UncontrollableEvents.Count();
            var nextStates = new StatesTuple[uncontrollableEventsCount];

            while (true)
            {
                lock (_lockObject) ++_numberOfRunningThreads;

                while (true)
                {
                    StatesTuple tuple;
                    bool vRemoveBadStates;
                    lock (_lockObject)
                    {
                        if (_statesStack.Count == 0) break;
                        tuple = _statesStack.Pop();
                        vRemoveBadStates = m_removeBadStates.Pop();
                        if (_validStates.ContainsKey(tuple)) continue;
                        tuple.Get(pos, _bits, _maxSize);
                    }

                    var k = 0;

                    for (int e = 0; e < uncontrollableEventsCount; ++e)
                    {
                        var plantHasEvent = false;

                        for (int i = 0; i < nPlant; ++i)
                            if (!_eventsList[i][e])
                            {
                                nextPosition[i] = pos[i];
                            }
                            else if (_adjacencyList[i].hasEvent(pos[i], e))
                            {
                                nextPosition[i] = _adjacencyList[i][pos[i], e];
                                plantHasEvent = true;
                            }
                            else
                            {
                                goto nextEvent;
                            }

                        for (int i = nPlant; i < n; ++i)
                            if (!_eventsList[i][e])
                            {
                                nextPosition[i] = pos[i];
                            }
                            else if (_adjacencyList[i].hasEvent(pos[i], e))
                            {
                                nextPosition[i] = _adjacencyList[i][pos[i], e];
                            }
                            else
                            {
                                if (!plantHasEvent) goto nextEvent;

                                if (vRemoveBadStates)
                                    RemoveBadStates(tuple, uncontrollableEventsCount);
                                goto nextState;
                            }

                        nextStates[k++] = new StatesTuple(nextPosition, _bits, _tupleSize);

                        nextEvent: ;
                    }

                    lock (_lockObject)
                    {
                        if (_validStates.ContainsKey(tuple))
                            continue;

                        var j = 0;
                        for (int i = 0; i < k; ++i)
                        {
                            if (!_validStates.TryGetValue(nextStates[i], out var vValue))
                            {
                                _statesStack.Push(nextStates[i]);
                                m_removeBadStates.Push(true);
                                ++j;
                            }
                            else if (vValue)
                            {
                                while (--j >= 0)
                                {
                                    _statesStack.Pop();
                                    m_removeBadStates.Pop();
                                }

                                _validStates.Add(tuple, true);
                                RemoveBadStates(tuple, uncontrollableEventsCount);
                                goto nextState;
                            }
                        }

                        _validStates.Add(tuple, false);
                    }

                    for (int e = uncontrollableEventsCount; e < _eventsUnion.Length; ++e)
                    {
                        for (int i = 0; i < n; ++i)
                            if (!_eventsList[i][e])
                                nextPosition[i] = pos[i];
                            else if (_adjacencyList[i].hasEvent(pos[i], e))
                                nextPosition[i] = _adjacencyList[i][pos[i], e];
                            else
                                goto nextEvent;
                        var nextTuple = new StatesTuple(nextPosition, _bits, _tupleSize);

                        lock (_lockObject)
                        {
                            if (!_validStates.ContainsKey(nextTuple))
                            {
                                _statesStack.Push(nextTuple);
                                m_removeBadStates.Push(false);
                            }
                        }

                        nextEvent: ;
                    }

                    nextState: ;
                }

                lock (_lockObject) --_numberOfRunningThreads;

                if (_numberOfRunningThreads > 0) Thread.Sleep(1);
                else break;
            }
        }

        private void MakeReverseTransitions()
        {
            if (_reverseTransitionsList != null)
                return;

            _reverseTransitionsList = new List<List<int>[]>[_statesList.Count()];
            Parallel.For(0, _statesList.Count(), i =>
            {
                _reverseTransitionsList[i] = new List<List<int>[]>(_statesList[i].Length);
                for (var state = 0; state < _statesList[i].Length; ++state)
                {
                    _reverseTransitionsList[i].Add(new List<int>[_eventsUnion.Count()]);
                    for (var e = 0; e < _eventsUnion.Count(); ++e)
                        _reverseTransitionsList[i][state][e] = new List<int>();
                }

                for (var state = 0; state < _statesList[i].Length; ++state)
                    foreach (var tr in _adjacencyList[i][state])
                        _reverseTransitionsList[i][tr.Value][tr.Key].Add(state);
                for (var e = 0; e < _eventsUnion.Count(); ++e)
                    if (!_eventsList[i][e])
                        for (var state = 0; state < _statesList[i].Length; ++state)
                            _reverseTransitionsList[i][state][e].Add(state);

                for (var state = 0; state < _statesList[i].Length; ++state)
                    foreach (var p in _reverseTransitionsList[i][state])
                        p.TrimExcess();
            });
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
                _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.getInstance());
            }

            for (i = 0; i < n; ++i)
                markedStates[i] = _statesList[i].Where(st => st.IsMarked)
                    .Select(st => Array.IndexOf(_statesList[i], st)).ToList();

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
                if ((!_validStates.TryGetValue(tuple, out var vValue) && !vNotCheckValidState) || vValue) continue;
                _validStates[tuple] = true;
                _statesStack.Push(tuple);
            }

            markedStates = null;
            Size = 0;

            for (i = 0; i < NumberOfThreads - 1; ++i)
            {
                threads[i] = Task.Factory.StartNew(() => InverseSearchThread(vNotCheckValidState));
            }

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

        private void RadixSort(int[] map, int[] positions)
        {
            var n = positions.Length;
            var b = new int[n];
            var bucket = new int[n + 1];
            int i, k;

            for (i = 0; i < n; ++i) ++bucket[_statesList[0][positions[i]].IsMarked ? 1 : 0];
            bucket[1] += bucket[0];
            for (i = n - 1; i >= 0; --i)
            {
                k = positions[i];
                b[--bucket[_statesList[0][k].IsMarked ? 1 : 0]] = k;
            }

            Array.Copy(b, positions, n);

            for (var e = 0; e < _eventsUnion.Length; ++e)
            {
                Array.Clear(bucket, 0, bucket.Length);
                for (i = 0; i < n; ++i)
                {
                    k = _adjacencyList[0].hasEvent(positions[i], e) ? map[_adjacencyList[0][positions[i], e]] : n;
                    ++bucket[k];
                }

                for (i = 1; i <= n; ++i) bucket[i] += bucket[i - 1];
                for (i = n - 1; i >= 0; --i)
                {
                    k = _adjacencyList[0].hasEvent(positions[i], e) ? map[_adjacencyList[0][positions[i], e]] : n;
                    b[--bucket[k]] = positions[i];
                }

                Array.Copy(b, positions, n);
            }
        }

        private void minimize()
        {
            var numStatesOld = _statesList[0].Length;
            var statesMap = new int[numStatesOld];
            var positions = new int[numStatesOld];
            
            int k, l, numStates;
            int iState = 0, numEvents = _eventsUnion.Length;
            var hasEvents = new bool[numEvents];
            var transitions = new int[numEvents];

            for (int i = 0; i < numStatesOld; ++i)
            {
                statesMap[i] = i;
                positions[i] = i;
            }

            while (true)
            {
                var changed = false;
                RadixSort(statesMap, positions);
                numStates = 0;
                for (int i = 0; i < numStatesOld;)
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
                        hasEvents[e] = _adjacencyList[0].hasEvent(k, e);
                        if (hasEvents[e]) transitions[e] = _adjacencyList[0][k, e];
                    }

                    var j = i + 1;
                    while (true)
                    {
                        while (j < numStatesOld && statesMap[positions[j]] == k) ++j;
                        if (j >= numStatesOld) break;
                        l = positions[j];
                        for (var e = numEvents - 1; e >= 0; --e)
                            if (hasEvents[e] != _adjacencyList[0].hasEvent(l, e) ||
                                hasEvents[e] && statesMap[transitions[e]] !=
                                statesMap[_adjacencyList[0][l, e]])
                                goto nextState;
                        if (_statesList[0][k].IsMarked != _statesList[0][l].IsMarked) break;
                        statesMap[l] = k;
                        changed = true;
                        ++j;
                    }

                    nextState: ;
                    i = j;
                }

                if (!changed) break;
                for (int i = 0; i < numStatesOld; ++i)
                {
                    k = i;
                    while (statesMap[k] != k) k = statesMap[k];
                    if (i != k) statesMap[i] = k;
                }
            }

            var newStates = new AbstractState[numStates];
            var newTransitions = new AdjacencyMatrix(numStates, numEvents, true);
            var initial = 0;

            for (int i = 0; i < numStatesOld; ++i)
            {
                k = positions[i];
                if (statesMap[k] == k)
                {
                    if (k == 0) initial = iState;
                    newStates[iState] = _statesList[0][k];
                    statesMap[k] = iState++;
                }
                else
                {
                    var pos = statesMap[statesMap[k]];
                    if (k == 0) initial = pos;
                    newStates[pos] = newStates[pos].MergeWith(_statesList[0][k]);
                    statesMap[k] = pos;
                }
            }

            if (initial != 0)
            {
                var aux = newStates[0];
                newStates[0] = newStates[initial];
                newStates[initial] = aux;
            }

            for (int i = 0; i < numStatesOld; ++i)
            {
                k = statesMap[i];
                k = k == 0 ? initial : k == initial ? 0 : k;
                for (var e = 0; e < numEvents; ++e)
                    if (_adjacencyList[0].hasEvent(i, e))
                    {
                        l = statesMap[_adjacencyList[0][i, e]];
                        l = l == 0 ? initial : l == initial ? 0 : l;
                        newTransitions.Add(k, e, l);
                    }
            }

            _statesList[0] = newStates;
            _adjacencyList[0] = newTransitions;
            _maxSize[0] = (1 << (int) Math.Ceiling(Math.Log(newStates.Length, 2))) - 1;
            Size = (ulong) newStates.Length;
            Name = "Min(" + Name + ")";
        }

        private void InverseSearchThread(object p_param)
        {
            int length = 0;
            var n = _statesList.Count;
            var pos = new int[n];
            var nextPos = new int[n];
            var movs = new int[n];

            var v_NotCheckValidState = (bool) p_param;

            while (true)
            {
                lock (_lockObject) ++_numberOfRunningThreads;

                while (true)
                {
                    StatesTuple tuple;
                    lock (_lockObject)
                    {
                        if (_statesStack.Count == 0) break;
                        tuple = _statesStack.Pop();
                        tuple.Get(pos, _bits, _maxSize);
                    }

                    ++length;

                    for (var e = 0; e < _eventsUnion.Length; ++e)
                    {
                        int i;
                        for (i = 0; i < n; ++i)
                            if (!_reverseTransitionsList[i][pos[i]][e].Any())
                                goto nextEvent;

                        for (i = 0; i < n - 1; ++i) nextPos[i] = _reverseTransitionsList[i][pos[i]][e][movs[i]];
                        movs[n - 1] = -1;
                        while (true)
                        {
                            for (i = n - 1; i >= 0; --i)
                            {
                                ++movs[i];
                                if (movs[i] < _reverseTransitionsList[i][pos[i]][e].Count()) break;
                                movs[i] = 0;
                                nextPos[i] = _reverseTransitionsList[i][pos[i]][e][0];
                            }

                            if (i < 0) break;

                            nextPos[i] = _reverseTransitionsList[i][pos[i]][e][movs[i]];

                            tuple = new StatesTuple(nextPos, _bits, _tupleSize);
                            lock (_lockObject)
                            {
                                bool value;
                                if ((_validStates.TryGetValue(tuple, out value) || v_NotCheckValidState) && !value)
                                {
                                    _validStates[tuple] = true;
                                    _statesStack.Push(tuple);
                                }
                            }
                        }

                        nextEvent: ;
                    }
                }

                lock (_lockObject) --_numberOfRunningThreads;

                if (_numberOfRunningThreads > 0) Thread.Sleep(1);
                else break;
            }

            lock (_lockObject) Size += (ulong) length;
        }

        private bool RemoveBadStates(StatesTuple initialPos, int uncontrolEventsCount, bool defaultValue = false)
        {
            int n = _statesList.Count;
            var stack = new Stack<StatesTuple>();
            var pos = new int[n];
            var nextPos = new int[n];
            var movs = new int[n];
            bool found = false;

            stack.Push(initialPos);

            while (stack.Count > 0)
            {
                var tuple = stack.Pop();
                tuple.Get(pos, _bits, _maxSize);


                for (int e = 0; e < uncontrolEventsCount; ++e)
                {
                    int i;
                    for (i = 0; i < n; ++i)
                        if (!_reverseTransitionsList[i][pos[i]][e].Any())
                            goto nextEvent;
                    for (i = 0; i < n; ++i) nextPos[i] = _reverseTransitionsList[i][pos[i]][e][movs[i]];
                    movs[n - 1] = -1;
                    while (true)
                    {
                        for (i = n - 1; i >= 0; --i)
                        {
                            ++movs[i];
                            if (movs[i] < _reverseTransitionsList[i][pos[i]][e].Count()) break;
                            movs[i] = 0;
                            nextPos[i] = _reverseTransitionsList[i][pos[i]][e][movs[i]];
                        }

                        if (i < 0) break;
                        nextPos[i] = _reverseTransitionsList[i][pos[i]][e][movs[i]];

                        tuple = new StatesTuple(nextPos, _bits, _tupleSize);

                        lock (_lockObject)
                        {
                            if (!_validStates.TryGetValue(tuple, out var vValue) || vValue != defaultValue) continue;
                            _validStates[tuple] = !defaultValue;
                            stack.Push(tuple);
                            found = true;
                        }
                    }

                    nextEvent: ;
                }
            }

            return found;
        }

        private void DepthFirstSearchThread(object param)
        {
            var length = 0;
            var n = _statesList.Count;
            var pos = new int[n];
            var nextPosition = new int[n];
            var acceptAllStates = (bool) param;

            while (true)
            {
                lock (_lockObject)
                {
                    ++_numberOfRunningThreads;
                }

                while (true)
                {
                    StatesTuple tuple;
                    lock (_lockObject)
                    {
                        if (_statesStack.Count == 0) break;
                        tuple = _statesStack.Pop();
                    }

                    ++length;
                    tuple.Get(pos, _bits, _maxSize);

                    int e;
                    for (e = 0; e < _eventsUnion.Length; ++e)
                    {
                        int i;
                        for (i = n - 1; i >= 0; --i)
                            if (!_eventsList[i][e])
                                nextPosition[i] = pos[i];
                            else if (_adjacencyList[i].hasEvent(pos[i], e))
                                nextPosition[i] = _adjacencyList[i][pos[i], e];
                            else
                                goto nextEvent;
                        tuple = new StatesTuple(nextPosition, _bits, _tupleSize);
                        bool invalid;
                        lock (_lockObject)
                        {
                            if (_validStates.TryGetValue(tuple, out invalid))
                            {
                                if (!invalid)
                                {
                                    _validStates[tuple] = true;
                                    _statesStack.Push(tuple);
                                }
                            }
                            else if (acceptAllStates)
                            {
                                _validStates[tuple] = true;
                                _statesStack.Push(tuple);
                            }
                        }

                        nextEvent: ;
                    }
                }

                lock (_lockObject)
                {
                    --_numberOfRunningThreads;
                }

                if (_numberOfRunningThreads > 0) Thread.Sleep(1);
                else break;
            }

            lock (_lockObject)
            {
                Size += (ulong) length;
            }
        }

        private bool DepthFirstSearch(bool acceptAllStates, bool checkForBadStates = false)
        {
            _statesStack = new Stack<StatesTuple>();
            Size = 0;
            _numberOfRunningThreads = 0;

            var initialState = new StatesTuple(new int[_statesList.Count], _bits, _tupleSize);

            if (!acceptAllStates && !_validStates.ContainsKey(initialState))
                return false;

            _validStates[initialState] = true;
            _statesStack.Push(initialState);

            var threads = new Task[NumberOfThreads - 1];

            for (var i = 0; i < NumberOfThreads - 1; ++i)
            {
                threads[i] = Task.Factory.StartNew(() => DepthFirstSearchThread(acceptAllStates));
            }

            DepthFirstSearchThread(acceptAllStates);
            for (var i = 0; i < NumberOfThreads - 1; ++i) threads[i].Wait();

            _statesStack = null;

            var vNewBadStates = false;
            var vUncontrollableEventsCount = UncontrollableEvents.Count();
            if (checkForBadStates)
                foreach (var p in _validStates)
                    if (!p.Value)
                        vNewBadStates |= RemoveBadStates(p.Key, vUncontrollableEventsCount, true);
            foreach (var p in _validStates.Reverse())
                if (!p.Value) _validStates.Remove(p.Key);
                else _validStates[p.Key] = false;
            return vNewBadStates;
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
                var p = (int) Math.Ceiling(Math.Log(G1G2._statesList[i].Length, 2));
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
                    foreach (var p in G1._adjacencyList[i][j])
                        G1G2._adjacencyList[i].Add(j, Array.IndexOf(G1G2._eventsUnion, G1._eventsUnion[p.Key]),
                            p.Value);
            }

            for (var i = G1._adjacencyList.Count; i < n; ++i)
            {
                G1G2._eventsList.Add(new bool[G1G2._eventsUnion.Length]);
                for (var e = 0; e < G2._eventsUnion.Length; ++e)
                    G1G2._eventsList[i][Array.IndexOf(G1G2._eventsUnion, G2._eventsUnion[e])] =
                        G2._eventsList[i - G1._adjacencyList.Count][e];
                G1G2._adjacencyList.Add(new AdjacencyMatrix(G1G2._statesList[i].Length, G1G2._eventsUnion.Length));
                for (var j = 0; j < G1G2._statesList[i].Length; ++j)
                    foreach (var q in G2._adjacencyList[i - G1._adjacencyList.Count][j])
                        G1G2._adjacencyList[i].Add(j, Array.IndexOf(G1G2._eventsUnion, G2._eventsUnion[q.Key]),
                            q.Value);
            }

            return G1G2;
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
                removeNoAccessibleStates
            );
        }

        public DFA ParallelCompositionWith(params DFA[] others)
        {
            return ParallelCompositionWith(others, true);
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
            var n = _statesList.Count;
            var pos = new int[n];
            var nextPos = new int[n];
            _statesStack = new Stack<StatesTuple>();

            var initialState = new StatesTuple(pos, _bits, _tupleSize);

            _statesStack.Push(initialState);

            _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.getInstance());
            _validStates.Add(initialState, false);

            while (_statesStack.Count > 0)
            {
                _statesStack.Pop().Get(pos, _bits, _maxSize);

                for (var e = 0; e < _eventsUnion.Length; ++e)
                {
                    for (var i = 0; i < n; ++i)
                        if (_adjacencyList[i].hasEvent(pos[i], e))
                            nextPos[i] = _adjacencyList[i][pos[i], e];
                        else
                            goto nextEvent;
                    var nextState = new StatesTuple(nextPos, _bits, _tupleSize);

                    if (!_validStates.ContainsKey(nextState))
                    {
                        _statesStack.Push(nextState);
                        _validStates.Add(nextState, false);
                    }

                    nextEvent: ;
                }
            }

            Size = (ulong) _validStates.Count;
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

        public static DFA Product(IEnumerable<DFA> list)
        {
            if (list.Count() == 0) return null;
            var G1G2 = list.Aggregate((a, b) => ConcatDFA(a, b));
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
            result.FindSupervisor(plant._statesList.Count(), nonBlocking);

            result.Name = $"Sup({result.Name})";
            result._numberOfPlants = plant._statesList.Count();

            return result;
        }

        private bool VerifyNonblocking()
        {
            var nStates = Size;
            RemoveBlockingStates();
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

            var n = _statesList.Count();
            var pos = new int[n];
            var nextPos = new int[n];
            var v_states = new Dictionary<StatesTuple, ulong>((int) Size,
                StatesTupleComparator.getInstance());
            var nextTuple = new StatesTuple(_tupleSize);

            using (var writer = XmlWriter.Create(filepath, settings))
            {
                writer.WriteStartElement("Automaton");
                writer.WriteAttributeString("Name", Name);

                writer.WriteStartElement("States");

                var addState = new Action<StatesTuple, ulong>((state, id) =>
                {
                    state.Get(pos, _bits, _maxSize);
                    v_states.Add(state, id);

                    var s = composeState(pos);

                    writer.WriteStartElement("State");
                    writer.WriteAttributeString("Name", s.ToString());
                    writer.WriteAttributeString("Marking", s.Marking.ToString());
                    writer.WriteAttributeString("Id", id.ToString());

                    writer.WriteEndElement();
                });

                if (_validStates != null)
                {
                    ulong id = 0;
                    foreach (var state in _validStates) addState(state.Key, id++);
                }
                else
                {
                    for (ulong id = 0; id < Size; ++id)
                    {
                        addState(new StatesTuple(pos, _bits, _tupleSize), id);
                        IncrementPosition(pos);
                    }
                }

                writer.WriteEndElement();

                writer.WriteStartElement("InitialState");
                writer.WriteAttributeString("Id", "0");

                writer.WriteEndElement();

                writer.WriteStartElement("Events");
                for (var i = 0; i < _eventsUnion.Length; ++i)
                {
                    writer.WriteStartElement("Event");
                    writer.WriteAttributeString("Name", _eventsUnion[i].ToString());
                    writer.WriteAttributeString("Controllability", _eventsUnion[i].Controllability.ToString());
                    writer.WriteAttributeString("Id", i.ToString());

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                writer.WriteStartElement("Transitions");

                foreach (var state in v_states)
                {
                    state.Key.Get(pos, _bits, _maxSize);

                    for (var j = 0; j < _eventsUnion.Length; ++j)
                    {
                        for (var e = 0; e < n; ++e)
                            if (_eventsList[e][j])
                            {
                                if (_adjacencyList[e].hasEvent(pos[e], j))
                                    nextPos[e] = _adjacencyList[e][pos[e], j];
                                else
                                    goto nextEvent;
                            }
                            else
                            {
                                nextPos[e] = pos[e];
                            }

                        ulong value;
                        nextTuple.Set(nextPos, _bits);

                        if (v_states.TryGetValue(nextTuple, out value))
                        {
                            writer.WriteStartElement("Transition");

                            writer.WriteAttributeString("Origin", state.Value.ToString());
                            writer.WriteAttributeString("Trigger", j.ToString());
                            writer.WriteAttributeString("Destination", value.ToString());

                            writer.WriteEndElement();
                        }

                        nextEvent: ;
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
                                ? UltraDES.Controllability.Controllable
                                : UltraDES.Controllability.Uncontrollable));

            var v_initial = v_xdoc.Descendants("InitialState").Select(i => v_states[i.Attribute("Id").Value]).Single();

            var v_transitions =
                v_xdoc.Descendants("Transition")
                    .Select(
                        t =>
                            new Transition(v_states[t.Attribute("Origin").Value],
                                v_events[t.Attribute("Trigger").Value],
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
            var obj = (DFA) formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        private IEnumerable<Transition> getTransitionsFromState(int[] pos)
        {
            var n = _statesList.Count;
            var nextPosition = new int[n];
            int e, i, k;
            bool plantHasEvent;
            var UncontrollableEventsCount = UncontrollableEvents.Count();
            var nextStates = new StatesTuple[UncontrollableEventsCount];
            var v_events = new int[UncontrollableEventsCount];
            k = 0;

            var currentState = composeState(pos);

            for (e = 0; e < UncontrollableEventsCount; ++e)
            {
                plantHasEvent = false;

                for (i = 0; i < _numberOfPlants; ++i)
                    if (!_eventsList[i][e])
                    {
                        nextPosition[i] = pos[i];
                    }
                    else if (_adjacencyList[i].hasEvent(pos[i], e))
                    {
                        nextPosition[i] = _adjacencyList[i][pos[i], e];
                        plantHasEvent = true;
                    }
                    else
                    {
                        goto nextEvent;
                    }

                for (i = _numberOfPlants; i < n; ++i)
                    if (!_eventsList[i][e])
                    {
                        nextPosition[i] = pos[i];
                    }
                    else if (_adjacencyList[i].hasEvent(pos[i], e))
                    {
                        nextPosition[i] = _adjacencyList[i][pos[i], e];
                    }
                    else
                    {
                        if (!plantHasEvent) goto nextEvent;

                        goto nextState;
                    }

                nextStates[k] = new StatesTuple(nextPosition, _bits, _tupleSize);
                if (_validStates == null || _validStates.ContainsKey(nextStates[k]))
                {
                    v_events[k] = e;
                    ++k;
                }

                nextEvent: ;
            }

            for (i = 0; i < k; ++i)
            {
                nextStates[i].Get(nextPosition, _bits, _maxSize);
                var nextState = composeState(nextPosition);
                yield return new Transition(currentState, _eventsUnion[v_events[i]], nextState);
            }

            for (e = UncontrollableEventsCount; e < _eventsUnion.Length; ++e)
            {
                for (i = 0; i < n; ++i)
                    if (!_eventsList[i][e])
                        nextPosition[i] = pos[i];
                    else if (_adjacencyList[i].hasEvent(pos[i], e))
                        nextPosition[i] = _adjacencyList[i][pos[i], e];
                    else
                        goto nextEvent;
                var nextTuple = new StatesTuple(nextPosition, _bits, _tupleSize);

                if (_validStates == null || _validStates.ContainsKey(nextTuple))
                {
                    var nextState = composeState(nextPosition);
                    yield return new Transition(currentState, _eventsUnion[e], nextState);
                }

                nextEvent: ;
            }

            nextState: ;
        }

        private AbstractState composeState(int[] p_pos)
        {
            var n = p_pos.Length;

            if (n == 1) return _statesList[0][p_pos[0]];

            var marked = _statesList[0][p_pos[0]].IsMarked;
            var states = new AbstractState[n];
            states[0] = _statesList[0][p_pos[0]];
            for (var j = 1; j < n; ++j)
            {
                states[j] = _statesList[j][p_pos[j]];
                marked &= _statesList[j][p_pos[j]].IsMarked;
            }

            return new CompoundState(states, marked ? Marking.Marked : Marking.Unmarked);
        }

        private State mergeStates(List<int> p_states, int p_index)
        {
            var first = _statesList[p_index][p_states[0]].ToString();
            var name = new StringBuilder(first, (first.Length + 2) * p_states.Count);
            var marked = _statesList[p_index][p_states[0]].IsMarked;

            for (var i = 1; i < p_states.Count; ++i)
            {
                name.Append("|");
                name.Append(_statesList[p_index][p_states[i]]);
                marked |= _statesList[p_index][p_states[i]].IsMarked;
            }

            return new State(name.ToString(), marked ? Marking.Marked : Marking.Unmarked);
        }

        private void simplify()
        {
            var newStates = new AbstractState[Size];
            var newAdjacencyMatrix = new AdjacencyMatrix((int) Size, _eventsUnion.Length, true);
            int id = 0, n = _statesList.Count();
            var positionNewStates = new Dictionary<StatesTuple, int>(StatesTupleComparator.getInstance());

            if (_validStates == null && n == 1)
                return;

            id = 0;
            var pos0 = new int[n];

            if (_validStates != null)
                foreach (var state in _validStates)
                {
                    state.Key.Get(pos0, _bits, _maxSize);
                    newStates[id] = composeState(pos0);
                    positionNewStates.Add(state.Key, id);
                    ++id;
                }
            else
                do
                {
                    newStates[id] = composeState(pos0);
                    positionNewStates.Add(new StatesTuple(pos0, _bits, _tupleSize), id);
                    ++id;
                } while (IncrementPosition(pos0));

            Parallel.ForEach(positionNewStates, state =>
            {
                var pos = new int[n];
                var nextPos = new int[n];
                var nextTuple = new StatesTuple(_tupleSize);

                state.Key.Get(pos, _bits, _maxSize);

                for (var e = 0; e < _eventsUnion.Length; ++e)
                {
                    for (var j = n - 1; j >= 0; --j)
                        if (_eventsList[j][e])
                        {
                            if (_adjacencyList[j].hasEvent(pos[j], e))
                                nextPos[j] = _adjacencyList[j][pos[j], e];
                            else
                                goto nextEvent;
                        }
                        else
                        {
                            nextPos[j] = pos[j];
                        }

                    nextTuple.Set(nextPos, _bits);
                    var k = 0;
                    if (positionNewStates.TryGetValue(nextTuple, out k)) newAdjacencyMatrix.Add(state.Value, e, k);
                    nextEvent: ;
                }
            });

            _statesList.Clear();
            _adjacencyList.Clear();
            _eventsList.Clear();
            _validStates = null;

            _statesList.Add(newStates);
            newAdjacencyMatrix.TrimExcess();
            _adjacencyList.Add(newAdjacencyMatrix);

            _eventsList.Add(new bool[_eventsUnion.Length]);
            for (var j = 0; j < _eventsUnion.Length; ++j)
                _eventsList[0][j] = true;

            _statesList.TrimExcess();
            _eventsList.TrimExcess();
            positionNewStates = null;

            _bits = new int[1];
            _tupleSize = 1;
            _numberOfPlants = 1;
            _maxSize = new int[1];
            _maxSize[0] = (1 << (int) Math.Ceiling(Math.Log(Size, 2))) - 1;

            GC.Collect();
        }

        private static void removeUnusedTransitions(DFA[] composition)
        {
            int s1, s2;
            var stack1 = new Stack<int>();
            var stack2 = new Stack<int>();
            var n = composition.Count();

            for (var i = 0; i < n; ++i)
            {
                var validEvents = new BitArray[composition[i].Size];
                for (var j = 0; j < (int) composition[i].Size; ++j)
                    validEvents[j] = new BitArray(composition[i]._eventsUnion.Length, false);

                for (var j = 0; j < n; ++j)
                {
                    if (j == i) continue;

                    var other = composition.ElementAt(j);
                    stack1.Push(0);
                    stack2.Push(0);
                    var validStates = new BitArray((int) (composition[i].Size * other.Size), false);

                    var otherEvents = composition[i]._eventsUnion.Select(e => Array.IndexOf(other._eventsUnion, e))
                        .ToArray();

                    var extraEvents = other._eventsUnion.Where(e => Array.IndexOf(composition[i]._eventsUnion, e) == -1)
                        .Select(e => Array.IndexOf(other._eventsUnion, e)).ToArray();

                    while (stack1.Count > 0)
                    {
                        s1 = stack1.Pop();
                        s2 = stack2.Pop();

                        var p = s1 * (int) other.Size + s2;

                        if (validStates[p]) continue;

                        validStates[p] = true;


                        for (var e = 0; e < composition[i]._eventsUnion.Length; ++e)
                        {
                            if (!composition[i]._adjacencyList[0].hasEvent(s1, e)) continue;

                            if (otherEvents[e] < 0)
                            {
                                stack1.Push(composition[i]._adjacencyList[0][s1, e]);
                                stack2.Push(s2);
                                validEvents[s1][e] = true;
                            }
                            else if (other._adjacencyList[0].hasEvent(s2, otherEvents[e]))
                            {
                                stack1.Push(composition[i]._adjacencyList[0][s1, e]);
                                stack2.Push(other._adjacencyList[0][s2, otherEvents[e]]);
                                validEvents[s1][e] = true;
                            }
                        }

                        for (var e = 0; e < extraEvents.Length; ++e)
                            if (other._adjacencyList[0].hasEvent(s2, extraEvents[e]))
                            {
                                stack1.Push(s1);
                                stack2.Push(other._adjacencyList[0][s2, extraEvents[e]]);
                            }
                    }

                    for (var k = 0; k < validEvents.Count(); ++k)
                    for (var e = 0; e < validEvents[k].Count; ++e)
                        if (!validEvents[k][e] && composition[i]._adjacencyList[0].hasEvent(k, e))
                            composition[i]._adjacencyList[0].Remove(k, e);
                }
            }
        }

        public static bool IsConflicting(IEnumerable<DFA> supervisors)
        {
            Parallel.ForEach(supervisors, s => { s.simplify(); });

            var composition = supervisors.Aggregate((a, b) => a.ParallelCompositionWith(b));
            var oldSize = composition.Size;
            composition.RemoveBlockingStates();
            return composition.Size != oldSize;
        }

        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications,
            IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            if (conflictResolvingSupervisor == null) conflictResolvingSupervisor = new DFA[0];
            ;
            var supervisors = specifications.Select(
                e =>
                {
                    return MonolithicSupervisor(plants.Where(p => p._eventsUnion.Intersect(e._eventsUnion).Any()),
                        new[] {e}, true);
                });

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete)) throw new Exception("conflicting supervisors");
            GC.Collect();
            return complete;
        }

        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications,
            out List<DFA> compoundPlants,
            IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            if (conflictResolvingSupervisor == null) conflictResolvingSupervisor = new DFA[0];

            var dic = specifications.ToDictionary(
                e =>
                {
                    return plants.Where(p => p._eventsUnion.Intersect(e._eventsUnion).Any()).Aggregate((a, b) =>
                    {
                        return a.ParallelCompositionWith(b);
                    });
                });

            var supervisors =
                dic.AsParallel()
                    .Select(automata => MonolithicSupervisor(new[] {automata.Key}, new[] {automata.Value}, true))
                    .ToList();

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete)) throw new Exception("conflicting supervisors");

            compoundPlants = dic.Keys.ToList();
            GC.Collect();
            return complete;
        }

        public static void ToWmodFile(string p_filename, IEnumerable<DFA> p_plants, IEnumerable<DFA> p_specifications,
            string p_ModuleName = "UltraDES")
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
                foreach (var v_plant in p_plants) v_events = v_events.Union(v_plant.Events).ToList();
                foreach (var v_spec in p_specifications) v_events = v_events.Union(v_spec.Events).ToList();

                foreach (var v_event in v_events)
                {
                    writer.WriteStartElement("EventDecl");
                    writer.WriteAttributeString("Kind", v_event.IsControllable ? "CONTROLLABLE" : "UNCONTROLLABLE");
                    writer.WriteAttributeString("Name", v_event.ToString());
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteStartElement("ComponentList");

                var addAutomaton = new Action<DFA, string>((G, kind) =>
                {
                    writer.WriteStartElement("SimpleComponent");
                    writer.WriteAttributeString("Kind", kind);
                    writer.WriteAttributeString("Name", G.Name);
                    writer.WriteStartElement("Graph");
                    writer.WriteStartElement("NodeList");
                    for (var i = 0; i < G._statesList[0].Count(); ++i)
                    {
                        var v_state = G._statesList[0][i];
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
                    for (var i = 0; i < G._statesList[0].Count(); ++i)
                    {
                        var v_source = G._statesList[0][i].ToString();

                        for (var e = 0; e < G._eventsList[0].Count(); ++e)
                        {
                            var v_target = G._adjacencyList[0][i, e];
                            if (v_target >= 0)
                            {
                                writer.WriteStartElement("Edge");
                                writer.WriteAttributeString("Source", v_source);
                                writer.WriteAttributeString("Target", G._statesList[0][v_target].ToString());
                                writer.WriteStartElement("LabelBlock");
                                writer.WriteStartElement("SimpleIdentifier");
                                writer.WriteAttributeString("Name", G._eventsUnion[e].ToString());
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

                foreach (var v_plant in p_plants) addAutomaton(v_plant, "PLANT");
                foreach (var v_spec in p_specifications) addAutomaton(v_spec, "SPEC");

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
                if (!reader.ReadToFollowing("EventDeclList")) throw new Exception("Invalid Format.");
                var eventsReader = reader.ReadSubtree();
                while (eventsReader.ReadToFollowing("EventDecl"))
                {
                    eventsReader.MoveToAttribute("Kind");
                    var v_kind = eventsReader.Value;
                    if (v_kind == "PROPOSITION") continue;
                    eventsReader.MoveToAttribute("Name");
                    var v_name = eventsReader.Value;
                    var ev = new Event(v_name,
                        v_kind == "CONTROLLABLE"
                            ? UltraDES.Controllability.Controllable
                            : UltraDES.Controllability.Uncontrollable);

                    v_events.Add(ev);
                    v_eventsMap.Add(v_name, ev);
                }

                if (!reader.ReadToFollowing("ComponentList")) throw new Exception("Invalid Format.");

                var componentsReader = reader.ReadSubtree();

                while (componentsReader.ReadToFollowing("SimpleComponent"))
                {
                    componentsReader.MoveToAttribute("Kind");
                    var v_kind = componentsReader.Value;
                    componentsReader.MoveToAttribute("Name");
                    var v_DFAName = componentsReader.Value;

                    if (!componentsReader.ReadToFollowing("NodeList")) throw new Exception("Invalid Format.");
                    var statesReader = componentsReader.ReadSubtree();

                    var states = new Dictionary<string, AbstractState>();

                    var initial = "";
                    while (statesReader.ReadToFollowing("SimpleNode"))
                    {
                        var evList = statesReader.ReadSubtree();

                        statesReader.MoveToAttribute("Name");
                        var v_name = statesReader.Value;
                        var v_marked = false;
                        if (statesReader.MoveToAttribute("Initial") && statesReader.ReadContentAsBoolean())
                            initial = v_name;

                        if (evList.ReadToFollowing("EventList"))
                        {
                            evList.ReadToFollowing("SimpleIdentifier");
                            evList.MoveToAttribute("Name");
                            v_marked = evList.Value == ":accepting";
                        }

                        states.Add(v_name, new State(v_name, v_marked ? Marking.Marked : Marking.Unmarked));
                    }

                    if (!componentsReader.ReadToFollowing("EdgeList")) throw new Exception("Invalid Format.");

                    var edgesReader = componentsReader.ReadSubtree();
                    var transitions = new List<Transition>();
                    while (edgesReader.ReadToFollowing("Edge"))
                    {
                        eventsReader = componentsReader.ReadSubtree();

                        edgesReader.MoveToAttribute("Source");
                        var v_source = edgesReader.Value;
                        edgesReader.MoveToAttribute("Target");
                        var v_target = edgesReader.Value;

                        while (eventsReader.ReadToFollowing("SimpleIdentifier"))
                        {
                            eventsReader.MoveToAttribute("Name");
                            var v_event = eventsReader.Value;
                            transitions.Add(new Transition(states[v_source], v_eventsMap[v_event], states[v_target]));
                        }
                    }

                    var G = new DFA(transitions, states[initial], v_DFAName);
                    if (v_kind == "PLANT") p_plants.Add(G);
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
                v_stateSet = Enumerable.Range(0, states).Select(i => new State(i.ToString(), Marking.Unmarked))
                    .ToArray();
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
                        trans[1] % 2 == 0
                            ? UltraDES.Controllability.Uncontrollable
                            : UltraDES.Controllability.Controllable);
                    evs.Add(trans[1], e);
                }

                transitions.Add(new Transition(v_stateSet[trans[0]], evs[trans[1]], v_stateSet[trans[2]]));
            }

            return new DFA(transitions, v_stateSet[0], v_name);
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
                            if (_adjacencyList[i].hasEvent(j, e))
                                invProj._adjacencyList[i].Add(j, evMap[e], _adjacencyList[i][j, e]);
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
                    if (_adjacencyList[0].hasEvent(i, evs[e]))
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
            var visited = new Dictionary<int[], int>(IntArrayComparator.getInstance());
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
                                if (!_adjacencyList[0].hasEvent(state, rEvs[e])) continue;
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
                                            if (!nextOldStates.Contains(t[j]))
                                                nextOldStates.Add(t[j]);
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

            for (var i = 0; i < NumberOfThreads - 1; ++i)
            {
                threads[i] = Task.Factory.StartNew(() => projectionAction());
            }

            projectionAction();

            for (var i = 0; i < NumberOfThreads - 1; ++i) threads[i].Wait();

            var statesList = new AbstractState[visited.Count];
            foreach (var t in visited)
            {
                var oldPositions = new List<int>();
                var newPositions = t.Key;
                foreach (var t1 in newPositions)
                {
                    foreach (var t2 in newStatesList[t1])
                        if (!oldPositions.Contains(t2))
                            oldPositions.Add(t2);
                }

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
                    if (j.Value[e] != -1)
                        proj._adjacencyList[0].Add(j.Key, e, j.Value[e]);
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

        public void ToAdsFile(string filepath, int odd = 1, int even = 2)
        {
            ToAdsFile(new[] {this}, new[] {filepath}, odd, even);
        }

        public static void ToAdsFile(IEnumerable<DFA> automata, IEnumerable<string> filepaths, int odd = 1,
            int even = 2)
        {
            foreach (var g in automata) g.simplify();

            var vEventsUnion = automata.SelectMany(g => g.Events).Distinct();
            var vEvents = new Dictionary<AbstractEvent, int>();

            foreach (var e in vEventsUnion)
                if (!e.IsControllable)
                {
                    vEvents.Add(e, even);
                    even += 2;
                }
                else
                {
                    vEvents.Add(e, odd);
                    odd += 2;
                }

            var vFilepaths = filepaths.ToArray();
            var i = -1;

            foreach (var g in automata)
            {
                ++i;
                var vEventsMaps = new Dictionary<int, int>();

                for (var e = 0; e < g._eventsUnion.Length; ++e) vEventsMaps.Add(e, vEvents[g._eventsUnion[e]]);

                var file = File.CreateText(vFilepaths[i]);

                file.WriteLine("# UltraDES ADS FILE - LACSED | UFMG\r\n");

                file.WriteLine("{0}\r\n", g.Name);

                file.WriteLine("State size (State set will be (0,1....,size-1)):");
                file.WriteLine("{0}\r\n", g.Size);

                file.WriteLine("Marker states:");
                var vMarkedStates = "";
                var vFirst = true;
                for (var s = 0; s < g._statesList[0].Length; ++s)
                    if (g._statesList[0][s].IsMarked)
                    {
                        if (vFirst)
                        {
                            vFirst = false;
                            vMarkedStates = s.ToString();
                        }
                        else
                        {
                            vMarkedStates += " " + s;
                        }
                    }

                file.WriteLine("{0}\r\n", vMarkedStates);

                file.WriteLine("Vocal states:\r\n");

                file.WriteLine("Transitions:");

                for (var s = 0; s < g._statesList[0].Length; ++s)
                    foreach (var t in g._adjacencyList[0][s])
                        file.WriteLine("{0} {1} {2}", s, vEventsMaps[t.Key], t.Value);

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
            var n = _statesList.Count;
            for (var i = 0; i < n; ++i)
                if (!_statesList[i][p_pos[i]].IsMarked)
                    return false;
            return true;
        }

        private bool IncrementPosition(int[] p_pos)
        {
            var k = _statesList.Count - 1;
            while (k >= 0)
            {
                if (++p_pos[k] < _statesList[k].Length)
                    return true;
                p_pos[k] = 0;
                --k;
            }

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
            var namesMap = new Dictionary<string, string>(_statesList[0].Length);

            if (simplifyStatesName)
                for (var s = 0; s < _statesList[0].Length; ++s)
                {
                    var newStateName = s.ToString();
                    namesMap.Add(newStateName, _statesList[0][s].ToString());
                    _statesList[0][s] = new State(newStateName, _statesList[0][s].Marking);
                }

            if (newName != null) Name = newName;

            return namesMap;
        }

        public void ToFsmFile(string fileName = null)
        {
            var n = _statesList.Count;
            var pos = new int[n];
            if (fileName == null) fileName = Name;
            fileName = fileName.Replace('|', '_');

            if (!fileName.EndsWith(".fsm")) fileName += ".fsm";

            var writer = new StreamWriter(fileName);

            writer.Write("{0}\n\n", Size);

            var writeState = new Action<AbstractState, Transition[]>((s, transtions) =>
            {
                writer.Write("{0}\t{1}\t{2}\n", s.ToString(), s.IsMarked ? 1 : 0, transtions.Length);
                foreach (var t in transtions)
                    writer.Write("{0}\t{1}\t{2}\to\n", t.Trigger.ToString(), t.Destination.ToString(),
                        t.Trigger.IsControllable ? "c" : "uc");
                writer.Write("\n");
            });

            if (!IsEmpty())
            {
                if (_validStates != null)
                {
                    writeState(composeState(pos), getTransitionsFromState(pos).ToArray());
                    foreach (var sT in _validStates)
                    {
                        sT.Key.Get(pos, _bits, _maxSize);
                        // imprimimos somente se não for o estado inicial, já que o estado inicial já
                        // foi impresso
                        for (var i = 0; i < n; ++i)
                            if (pos[i] != 0)
                            {
                                writeState(composeState(pos), getTransitionsFromState(pos).ToArray());
                                break;
                            }
                    }
                }
                else
                {
                    do
                    {
                        writeState(composeState(pos), getTransitionsFromState(pos).ToArray());
                    } while (IncrementPosition(pos));
                }
            }

            writer.Close();
        }

        public static DFA FromFsmFile(string fileName)
        {
            if (fileName == null) throw new Exception("Filename can not be null.");

            if (!File.Exists(fileName)) throw new Exception("File not found.");

            var automatonName = fileName.Split('/').Last().Split('.').First();
            var statesList = new Dictionary<string, AbstractState>();
            var eventsList = new Dictionary<string, AbstractEvent>();
            var transitionsList = new List<Transition>();
            AbstractState initialState = null;

            var reader = new StreamReader(fileName);

            var numberOfStates = reader.ReadLine();
            if (string.IsNullOrEmpty(numberOfStates)) throw new Exception("Invalid Format.");
            var numStates = int.Parse(numberOfStates);

            // reads all states first
            for (var i = 0; i < numStates; ++i)
            {
                string stateLine;
                do
                {
                    stateLine = reader.ReadLine();
                } while (stateLine == "");

                if (stateLine == null) throw new Exception("Invalid Format.");
                var stateInfo = stateLine.Split('\t');
                if (stateInfo.Length != 3) throw new Exception("Invalid Format.");
                if (statesList.ContainsKey(stateInfo[0])) throw new Exception("Invalid Format: Duplicated state.");
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
                do
                {
                    stateLine = reader.ReadLine();
                } while (stateLine == "");

                var stateInfo = stateLine.Split('\t');
                var numTransitions = int.Parse(stateInfo[2]);
                for (var t = 0; t < numTransitions; ++t)
                {
                    var transition = reader.ReadLine().Split('\t');
                    if (transition.Length != 4) throw new Exception("Invalid Format.");
                    if (!eventsList.ContainsKey(transition[0]))
                        eventsList.Add(transition[0],
                            new Event(transition[0],
                                transition[2] == "c"
                                    ? UltraDES.Controllability.Controllable
                                    : UltraDES.Controllability.Uncontrollable));
                    if (!statesList.ContainsKey(transition[1]))
                        throw new Exception("Invalid transition. Destination state not found.");
                    transitionsList.Add(new Transition(statesList[stateInfo[0]],
                        eventsList[transition[0]], statesList[transition[1]]));
                }
            }

            return new DFA(transitionsList, initialState, automatonName);
        }

        public bool IsControllable(params DFA[] plants)
        {
            return IsControllable((IEnumerable<DFA>) plants);
        }

        public bool IsControllable(IEnumerable<DFA> plants)
        {
            return Controllability(plants) == UltraDES.Controllability.Controllable;
        }

        public Controllability Controllability(params DFA[] plants)
        {
            return Controllability((IEnumerable<DFA>) plants);
        }

        public Controllability Controllability(IEnumerable<DFA> plants)
        {
            return ControllabilityAndDisabledEvents(plants, false).Item1;
        }

        public Tuple<Controllability, Dictionary<AbstractState, List<AbstractEvent>>>
            ControllabilityAndDisabledEvents(params DFA[] plants)
        {
            return ControllabilityAndDisabledEvents(plants, true);
        }

        public Tuple<Controllability, Dictionary<AbstractState, List<AbstractEvent>>>
            ControllabilityAndDisabledEvents(IEnumerable<DFA> plants, bool getDisabledEvents = true)
        {
            var G = ParallelComposition(plants, false);
            var nG = G._statesList.Count;
            var nS = _statesList.Count;
            int[] pos1, pos2 = new int[nS];
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
            bool GHasNext, SHasNext;
            var controllabity = UltraDES.Controllability.Controllable;
            var disabled = new Dictionary<AbstractState, List<AbstractEvent>>((int) Size);
            AbstractState currentState = null;

            if (!filteredStates)
                _validStates = new Dictionary<StatesTuple, bool>((int) Size,
                    StatesTupleComparator.getInstance());

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

            while (stackS.Count > 0)
            {
                pos1 = stackG.Pop();
                pos2 = stackS.Pop();

                if (getDisabledEvents)
                {
                    currentState = composeState(pos2);
                    disabled.Add(currentState, new List<AbstractEvent>());
                }

                for (var e = 0; e < evs.Length; ++e)
                {
                    var t = CheckState(G, nG, nS, pos1, pos2, evsMapG[e], evsMapS[e]);
                    GHasNext = t.Item1;
                    SHasNext = t.Item2;

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
                            controllabity = UltraDES.Controllability.Uncontrollable;
                            if (!getDisabledEvents) goto stopSearch;
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
            }

            stopSearch:
            if (filteredStates)
                foreach (var t in _validStates.Reverse())
                    _validStates[t.Key] = false;
            else _validStates = null;

            return new Tuple<Controllability, Dictionary<AbstractState, List<AbstractEvent>>>(controllabity, disabled);
        }

        private Tuple<bool, bool, int[], int[]> CheckState(DFA G, int nG, int nS, int[] posG, int[] posS, int eG,
            int eS)
        {
            var nextG = new int[nG];
            var nextS = new int[nS];
            bool hasNextG = true, hasNextS = true;
            if (eG == -1)
                for (var i = 0; i < nG; ++i)
                    nextG[i] = posG[i];
            else
                for (var i = 0; i < nG; ++i)
                    if (!G._eventsList[i][eG])
                    {
                        nextG[i] = posG[i];
                    }
                    else
                    {
                        if (!G._adjacencyList[i].hasEvent(posG[i], eG))
                        {
                            hasNextG = false;
                            break;
                        }

                        nextG[i] = G._adjacencyList[i][posG[i], eG];
                    }

            if (eS == -1)
                for (var i = 0; i < nS; ++i)
                    nextS[i] = posS[i];
            else
                for (var i = 0; i < nS; ++i)
                    if (!_eventsList[i][eS])
                    {
                        nextS[i] = posS[i];
                    }
                    else
                    {
                        if (!_adjacencyList[i].hasEvent(posS[i], eS))
                        {
                            hasNextS = false;
                            break;
                        }

                        nextS[i] = _adjacencyList[i][posS[i], eS];
                    }

            return new Tuple<bool, bool, int[], int[]>(hasNextG, hasNextS, nextG, nextS);
        }

        public Dictionary<AbstractState, List<AbstractEvent>> DisabledEvents(params DFA[] plants)
        {
            return DisabledEvents((IEnumerable<DFA>) plants);
        }

        public Dictionary<AbstractState, List<AbstractEvent>> DisabledEvents(IEnumerable<DFA> plants)
        {
            return ControllabilityAndDisabledEvents(plants, true).Item2;
        }
    }
}