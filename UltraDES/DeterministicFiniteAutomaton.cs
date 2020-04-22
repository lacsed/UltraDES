using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UltraDES
{
    using DFA = DeterministicFiniteAutomaton;

    [Serializable]
    public sealed partial class DeterministicFiniteAutomaton
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

        private Stack<bool> _removeBadStates;

        private List<List<int>[]>[] _reverseTransitionsList;

        private Stack<StatesTuple> _statesStack;
        private int _tupleSize;

        private Dictionary<StatesTuple, bool> _validStates;

        public DeterministicFiniteAutomaton(IEnumerable<(AbstractState, AbstractEvent, AbstractState)> transitions, AbstractState initial, string name) : 
            this(transitions.Select(t => (Transition)t), initial, name)
        { }
        public DeterministicFiniteAutomaton(IEnumerable<Transition> transitions, AbstractState initial, string name) : this(1)
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
            _maxSize[0] = (1 << (int) Math.Max(Math.Ceiling(Math.Log(Size, 2)), 1)) - 1;
            _tupleSize = 1;

            for (var i = 0; i < _statesList[0].Length; ++i)
            {
                _adjacencyList[0].Add(i,
                    transitionsLocal.AsParallel().Where(t => t.Origin == _statesList[0][i]).Select(t =>
                        Tuple.Create(Array.IndexOf(_eventsUnion, t.Trigger),
                            Array.IndexOf(_statesList[0], t.Destination))).ToArray());
            }
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

        public IEnumerable<AbstractEvent> Events => _eventsUnion;

        public AbstractState InitialState => IsEmpty() ? null : composeState(new int[_statesList.Count]);

        public IEnumerable<AbstractState> MarkedStates
        {
            get
            {
                var n = _statesList.Count;
                var pos = new int[n];

                if (_validStates != null)
                {
                    foreach (var s in _validStates)
                    {
                        s.Key.Get(pos, _bits, _maxSize);
                        if (IsMarketState(pos))
                            yield return composeState(pos);
                    }
                }
                else if (!IsEmpty())
                {
                    do
                    {
                        if (IsMarketState(pos))
                            yield return composeState(pos);
                    } while (IncrementPosition(pos));
                }
            }
        }

        public string Name { get; private set; }

        public ulong Size { get; private set; }

        public IEnumerable<AbstractState> States
        {
            get
            {
                var n = _statesList.Count;
                var pos = new int[n];

                if (_validStates != null)
                {
                    foreach (var s in _validStates)
                    {
                        s.Key.Get(pos, _bits, _maxSize);
                        yield return composeState(pos);
                    }
                }
                else if (!IsEmpty())
                {
                    do
                    {
                        yield return composeState(pos);
                    } while (IncrementPosition(pos));
                }
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
                    {
                        if (_adjacencyList[0].HasEvent(i, e))
                            a[i, _adjacencyList[0][i, e]] += _eventsUnion[e];
                    }
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
                    {
                        if (!st[i].TryGetValue(S[i], out pos[i]))
                            return None<AbstractState>.Create();
                    }

                    var k = Array.IndexOf(_eventsUnion, e);
                    if (k < 0) return None<AbstractState>.Create();

                    for (var i = 0; i < n; ++i)
                    {
                        if (_eventsList[i][k])
                        {
                            pos[i] = _adjacencyList[i][pos[i], k];
                            if (pos[i] < 0) return None<AbstractState>.Create();
                        }
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
                {
                    foreach (var s in _validStates)
                    {
                        s.Key.Get(pos, _bits, _maxSize);
                        foreach (var i in getTransitionsFromState(pos))
                            yield return i;
                    }
                }
                else if (!IsEmpty())
                {
                    do
                    {
                        foreach (var i in getTransitionsFromState(pos))
                            yield return i;
                    } while (IncrementPosition(pos));
                }
            }
        }

        public IEnumerable<AbstractEvent> UncontrollableEvents => _eventsUnion.Where(i => !i.IsControllable);

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
                G._validStates = new Dictionary<StatesTuple, bool>(_validStates, StatesTupleComparator.GetInstance());

            return G;
        }

        public DFA Clone()
        {
            return Clone(_statesList.Count());
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
                threads[i] = Task.Factory.StartNew(() => DepthFirstSearchThread(acceptAllStates));

            DepthFirstSearchThread(acceptAllStates);
            for (var i = 0; i < NumberOfThreads - 1; ++i) threads[i].Wait();

            _statesStack = null;

            var vNewBadStates = false;
            var vUncontrollableEventsCount = UncontrollableEvents.Count();
            if (checkForBadStates)
            {
                foreach (var p in _validStates)
                    if (!p.Value)
                        vNewBadStates |= RemoveBadStates(p.Key, vUncontrollableEventsCount, true);
            }

            foreach (var p in _validStates.Reverse())
                if (!p.Value) _validStates.Remove(p.Key);
                else _validStates[p.Key] = false;
            return vNewBadStates;
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
                        var nextEvent = false;
                        int i;
                        for (i = n - 1; i >= 0; --i)
                        {
                            if (!_eventsList[i][e])
                                nextPosition[i] = pos[i];
                            else if (_adjacencyList[i].HasEvent(pos[i], e))
                                nextPosition[i] = _adjacencyList[i][pos[i], e];
                            else
                            {
                                nextEvent = true;
                                break;
                            }
                        }

                        if (nextEvent) continue;
                        tuple = new StatesTuple(nextPosition, _bits, _tupleSize);
                        lock (_lockObject)
                        {
                            if (_validStates.TryGetValue(tuple, out var invalid))
                            {
                                if (invalid) continue;
                                _validStates[tuple] = true;
                                _statesStack.Push(tuple);
                            }
                            else if (acceptAllStates)
                            {
                                _validStates[tuple] = true;
                                _statesStack.Push(tuple);
                            }
                        }
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

        public void drawLatexFigure(string fileName = null, bool openAfterFinish = true)
        {
            if (fileName == null) fileName = Name;
            fileName = fileName.Replace('|', '_');
            Drawing.drawLatexFigure(this, fileName, openAfterFinish);
        }

        public void drawSVGFigure(string fileName = null, bool openAfterFinish = true)
        {
            if (fileName == null) fileName = Name;
            fileName = fileName.Replace('|', '_');
            Drawing.drawSVG(this, fileName, openAfterFinish);
        }


        private void findStates(int obj)
        {
            var n = _statesList.Count;
            var nPlant = obj;
            var pos = new int[n];
            var nextPosition = new int[n];
            var uncontrollableEventsCount = UncontrollableEvents.Count();
            var nextStates = new StatesTuple[uncontrollableEventsCount];

            while (true)
            {
                lock (_lockObject)
                {
                    ++_numberOfRunningThreads;
                }

                while (true)
                {
                    var nextState = false;
                    StatesTuple tuple;
                    bool vRemoveBadStates;
                    lock (_lockObject)
                    {
                        if (_statesStack.Count == 0) break;
                        tuple = _statesStack.Pop();
                        vRemoveBadStates = _removeBadStates.Pop();
                        if (_validStates.ContainsKey(tuple)) continue;
                        tuple.Get(pos, _bits, _maxSize);
                    }

                    var k = 0;

                    for (var e = 0; e < uncontrollableEventsCount; ++e)
                    {
                        var nextEvent = false;
                        var plantHasEvent = false;

                        for (var i = 0; i < nPlant; ++i)
                        {
                            if (!_eventsList[i][e])
                                nextPosition[i] = pos[i];
                            else if (_adjacencyList[i].HasEvent(pos[i], e))
                            {
                                nextPosition[i] = _adjacencyList[i][pos[i], e];
                                plantHasEvent = true;
                            }
                            else
                            {
                                nextEvent = true;
                                break;
                            }
                        }

                        if (nextEvent) continue;

                        for (var i = nPlant; i < n; ++i)
                        {
                            if (!_eventsList[i][e])
                                nextPosition[i] = pos[i];
                            else if (_adjacencyList[i].HasEvent(pos[i], e))
                                nextPosition[i] = _adjacencyList[i][pos[i], e];
                            else
                            {
                                if (!plantHasEvent)
                                {
                                    nextEvent = true;
                                    break;
                                }

                                if (vRemoveBadStates) RemoveBadStates(tuple, uncontrollableEventsCount);
                                nextState = true;
                                break;
                            }
                        }

                        if (nextState) break;

                        if (nextEvent) continue;

                        nextStates[k++] = new StatesTuple(nextPosition, _bits, _tupleSize);
                    }

                    if (nextState) continue;

                    lock (_lockObject)
                    {
                        if (_validStates.ContainsKey(tuple))
                            continue;

                        var j = 0;
                        for (var i = 0; i < k; ++i)
                        {
                            if (!_validStates.TryGetValue(nextStates[i], out var vValue))
                            {
                                _statesStack.Push(nextStates[i]);
                                _removeBadStates.Push(true);
                                ++j;
                            }
                            else if (vValue)
                            {
                                while (--j >= 0)
                                {
                                    _statesStack.Pop();
                                    _removeBadStates.Pop();
                                }

                                _validStates.Add(tuple, true);
                                RemoveBadStates(tuple, uncontrollableEventsCount);
                                nextState = true;
                                break;
                            }
                        }

                        if (nextState) continue;

                        _validStates.Add(tuple, false);
                    }

                    for (var e = uncontrollableEventsCount; e < _eventsUnion.Length; ++e)
                    {
                        var nextEvent = false;
                        for (var i = 0; i < n; ++i)
                        {
                            if (!_eventsList[i][e])
                                nextPosition[i] = pos[i];
                            else if (_adjacencyList[i].HasEvent(pos[i], e))
                                nextPosition[i] = _adjacencyList[i][pos[i], e];
                            else
                            {
                                nextEvent = true;
                                break;
                            }
                        }

                        if (nextEvent) continue;
                        var nextTuple = new StatesTuple(nextPosition, _bits, _tupleSize);

                        lock (_lockObject)
                        {
                            if (!_validStates.ContainsKey(nextTuple))
                            {
                                _statesStack.Push(nextTuple);
                                _removeBadStates.Push(false);
                            }
                        }
                    }
                }

                lock (_lockObject)
                {
                    --_numberOfRunningThreads;
                }

                if (_numberOfRunningThreads > 0) Thread.Sleep(1);
                else break;
            }
        }


        private IEnumerable<Transition> getTransitionsFromState(int[] pos)
        {
            var n = _statesList.Count;
            var nextPosition = new int[n];
            int e, i, k;
            bool plantHasEvent;
            var uncontrollableEventsCount = UncontrollableEvents.Count();
            var nextStates = new StatesTuple[uncontrollableEventsCount];
            var v_events = new int[uncontrollableEventsCount];
            k = 0;

            var currentState = composeState(pos);

            for (e = 0; e < uncontrollableEventsCount; ++e)
            {
                var nextEvent = false;
                plantHasEvent = false;

                for (i = 0; i < _numberOfPlants; ++i)
                {
                    if (!_eventsList[i][e])
                        nextPosition[i] = pos[i];
                    else if (_adjacencyList[i].HasEvent(pos[i], e))
                    {
                        nextPosition[i] = _adjacencyList[i][pos[i], e];
                        plantHasEvent = true;
                    }
                    else
                    {
                        nextEvent = true;
                        break;
                    }
                }

                if (nextEvent) continue;

                for (i = _numberOfPlants; i < n; ++i)
                {
                    if (!_eventsList[i][e])
                        nextPosition[i] = pos[i];
                    else if (_adjacencyList[i].HasEvent(pos[i], e))
                        nextPosition[i] = _adjacencyList[i][pos[i], e];
                    else
                    {
                        if (!plantHasEvent)
                        {
                            nextEvent = true;
                            break;
                        }

                        yield break;
                    }
                }

                if (nextEvent) continue;

                nextStates[k] = new StatesTuple(nextPosition, _bits, _tupleSize);
                if (_validStates == null || _validStates.ContainsKey(nextStates[k]))
                {
                    v_events[k] = e;
                    ++k;
                }
            }

            for (i = 0; i < k; ++i)
            {
                nextStates[i].Get(nextPosition, _bits, _maxSize);
                var nextState = composeState(nextPosition);
                yield return new Transition(currentState, _eventsUnion[v_events[i]], nextState);
            }

            for (e = uncontrollableEventsCount; e < _eventsUnion.Length; ++e)
            {
                var nextEvent = false;

                for (i = 0; i < n; ++i)
                {
                    if (!_eventsList[i][e])
                        nextPosition[i] = pos[i];
                    else if (_adjacencyList[i].HasEvent(pos[i], e))
                        nextPosition[i] = _adjacencyList[i][pos[i], e];
                    else
                    {
                        nextEvent = true;
                        break;
                    }
                }

                if (nextEvent) continue;
                var nextTuple = new StatesTuple(nextPosition, _bits, _tupleSize);

                if (_validStates == null || _validStates.ContainsKey(nextTuple))
                {
                    var nextState = composeState(nextPosition);
                    yield return new Transition(currentState, _eventsUnion[e], nextState);
                }
            }
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

        private void InverseSearchThread(object p_param)
        {
            var length = 0;
            var n = _statesList.Count;
            var pos = new int[n];
            var nextPos = new int[n];
            var movs = new int[n];

            var v_NotCheckValidState = (bool) p_param;

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
                        tuple.Get(pos, _bits, _maxSize);
                    }

                    ++length;

                    for (var e = 0; e < _eventsUnion.Length; ++e)
                    {
                        var nextEvent = false;
                        int i;
                        for (i = 0; i < n; ++i)
                        {
                            if (!_reverseTransitionsList[i][pos[i]][e].Any())
                            {
                                nextEvent = true;
                                break;
                            }
                        }

                        if (nextEvent) continue;

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


        private bool IsEmpty()
        {
            return Size == 0;
        }

        private bool IsMarketState(int[] p_pos)
        {
            var n = _statesList.Count;
            for (var i = 0; i < n; ++i)
            {
                if (!_statesList[i][p_pos[i]].IsMarked)
                    return false;
            }

            return true;
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
                {
                    foreach (var tr in _adjacencyList[i][state])
                        _reverseTransitionsList[i][tr.Value][tr.Key].Add(state);
                }

                for (var e = 0; e < _eventsUnion.Count(); ++e)
                {
                    if (!_eventsList[i][e])
                    {
                        for (var state = 0; state < _statesList[i].Length; ++state)
                            _reverseTransitionsList[i][state][e].Add(state);
                    }
                }

                for (var state = 0; state < _statesList[i].Length; ++state)
                {
                    foreach (var p in _reverseTransitionsList[i][state])
                        p.TrimExcess();
                }
            });
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

        private void minimize()
        {
            var numStatesOld = _statesList[0].Length;
            var statesMap = new int[numStatesOld];
            var positions = new int[numStatesOld];

            int k, l, numStates;
            int iState = 0, numEvents = _eventsUnion.Length;
            var hasEvents = new bool[numEvents];
            var transitions = new int[numEvents];

            for (var i = 0; i < numStatesOld; ++i)
            {
                statesMap[i] = i;
                positions[i] = i;
            }

            while (true)
            {
                var changed = false;
                RadixSort(statesMap, positions);
                numStates = 0;
                for (var i = 0; i < numStatesOld;)
                {
                    var nextState = false;
                    k = positions[i];
                    if (statesMap[k] != k)
                    {
                        ++i;
                        continue;
                    }

                    ++numStates;
                    for (var e = 0; e < numEvents; ++e)
                    {
                        hasEvents[e] = _adjacencyList[0].HasEvent(k, e);
                        if (hasEvents[e]) transitions[e] = _adjacencyList[0][k, e];
                    }

                    var j = i + 1;
                    while (true)
                    {
                        while (j < numStatesOld && statesMap[positions[j]] == k) ++j;
                        if (j >= numStatesOld) break;
                        l = positions[j];
                        for (var e = numEvents - 1; e >= 0; --e)
                        {
                            if (hasEvents[e] != _adjacencyList[0].HasEvent(l, e) || hasEvents[e] &&
                                statesMap[transitions[e]] != statesMap[_adjacencyList[0][l, e]])
                            {
                                nextState = true;
                                break;
                            }
                        }

                        if (nextState) break;

                        if (_statesList[0][k].IsMarked != _statesList[0][l].IsMarked) break;
                        statesMap[l] = k;
                        changed = true;
                        ++j;
                    }

                    i = j;
                }

                if (!changed) break;
                for (var i = 0; i < numStatesOld; ++i)
                {
                    k = i;
                    while (statesMap[k] != k) k = statesMap[k];
                    if (i != k) statesMap[i] = k;
                }
            }

            var newStates = new AbstractState[numStates];
            var newTransitions = new AdjacencyMatrix(numStates, numEvents, true);
            var initial = 0;

            for (var i = 0; i < numStatesOld; ++i)
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

            for (var i = 0; i < numStatesOld; ++i)
            {
                k = statesMap[i];
                k = k == 0 ? initial : k == initial ? 0 : k;
                for (var e = 0; e < numEvents; ++e)
                {
                    if (_adjacencyList[0].HasEvent(i, e))
                    {
                        l = statesMap[_adjacencyList[0][i, e]];
                        l = l == 0 ? initial : l == initial ? 0 : l;
                        newTransitions.Add(k, e, l);
                    }
                }
            }

            _statesList[0] = newStates;
            _adjacencyList[0] = newTransitions;
            _maxSize[0] = (1 << (int) Math.Max(Math.Ceiling(Math.Log(newStates.Length, 2)), 1)) - 1;
            Size = (ulong) newStates.Length;
            Name = "Min(" + Name + ")";
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
                    k = _adjacencyList[0].HasEvent(positions[i], e) ? map[_adjacencyList[0][positions[i], e]] : n;
                    ++bucket[k];
                }

                for (i = 1; i <= n; ++i) bucket[i] += bucket[i - 1];
                for (i = n - 1; i >= 0; --i)
                {
                    k = _adjacencyList[0].HasEvent(positions[i], e) ? map[_adjacencyList[0][positions[i], e]] : n;
                    b[--bucket[k]] = positions[i];
                }

                Array.Copy(b, positions, n);
            }
        }

        private bool RemoveBadStates(StatesTuple initialPos, int uncontrolEventsCount, bool defaultValue = false)
        {
            var n = _statesList.Count;
            var stack = new Stack<StatesTuple>();
            var pos = new int[n];
            var nextPos = new int[n];
            var movs = new int[n];
            var found = false;

            stack.Push(initialPos);

            while (stack.Count > 0)
            {
                var tuple = stack.Pop();
                tuple.Get(pos, _bits, _maxSize);


                for (var e = 0; e < uncontrolEventsCount; ++e)
                {
                    var nextEvent = false;
                    int i;
                    for (i = 0; i < n; ++i)
                    {
                        if (!_reverseTransitionsList[i][pos[i]][e].Any())
                        {
                            nextEvent = true;
                            break;
                        }
                    }

                    if (nextEvent) continue;

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
                }
            }

            return found;
        }

        private void RemoveNoAccessibleStates()
        {
            if (_validStates == null)
            {
                _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.GetInstance());
                DepthFirstSearch(true);
            }
            else
                DepthFirstSearch(false);
        }

        private static void removeUnusedTransitions(DFA[] composition)
        {
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
                        var s1 = stack1.Pop();
                        var s2 = stack2.Pop();

                        var p = s1 * (int) other.Size + s2;

                        if (validStates[p]) continue;

                        validStates[p] = true;


                        for (var e = 0; e < composition[i]._eventsUnion.Length; ++e)
                        {
                            if (!composition[i]._adjacencyList[0].HasEvent(s1, e)) continue;

                            if (otherEvents[e] < 0)
                            {
                                stack1.Push(composition[i]._adjacencyList[0][s1, e]);
                                stack2.Push(s2);
                                validEvents[s1][e] = true;
                            }
                            else if (other._adjacencyList[0].HasEvent(s2, otherEvents[e]))
                            {
                                stack1.Push(composition[i]._adjacencyList[0][s1, e]);
                                stack2.Push(other._adjacencyList[0][s2, otherEvents[e]]);
                                validEvents[s1][e] = true;
                            }
                        }

                        foreach (var e in extraEvents)
                            if (other._adjacencyList[0].HasEvent(s2, e))
                            {
                                stack1.Push(s1);
                                stack2.Push(other._adjacencyList[0][s2, e]);
                            }
                    }

                    for (var k = 0; k < validEvents.Count(); ++k)
                    for (var e = 0; e < validEvents[k].Count; ++e)
                    {
                        if (!validEvents[k][e] && composition[i]._adjacencyList[0].HasEvent(k, e))
                            composition[i]._adjacencyList[0].Remove(k, e);
                    }
                }
            }
        }

        public void showAutomaton(string name = "")
        {
            ShowAutomaton(name);
        }

        public void ShowAutomaton(string name = "")
        {
            if (name == "") name = Name;
            Draw.ShowDotCode(ToDotCode, name);
        }

        private void simplify()
        {
            var newStates = new AbstractState[Size];
            var newAdjacencyMatrix = new AdjacencyMatrix((int) Size, _eventsUnion.Length, true);
            int id = 0, n = _statesList.Count();
            var positionNewStates = new Dictionary<StatesTuple, int>(StatesTupleComparator.GetInstance());

            if (_validStates == null && n == 1)
                return;

            id = 0;
            var pos0 = new int[n];

            if (_validStates != null)
            {
                foreach (var state in _validStates)
                {
                    state.Key.Get(pos0, _bits, _maxSize);
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
                    positionNewStates.Add(new StatesTuple(pos0, _bits, _tupleSize), id);
                    ++id;
                } while (IncrementPosition(pos0));
            }

            Parallel.ForEach(positionNewStates, state =>
            {
                var pos = new int[n];
                var nextPos = new int[n];
                var nextTuple = new StatesTuple(_tupleSize);

                state.Key.Get(pos, _bits, _maxSize);

                for (var e = 0; e < _eventsUnion.Length; ++e)
                {
                    var nextEvent = false;
                    for (var j = n - 1; j >= 0; --j)
                    {
                        if (_eventsList[j][e])
                        {
                            if (_adjacencyList[j].HasEvent(pos[j], e))
                                nextPos[j] = _adjacencyList[j][pos[j], e];
                            else
                            {
                                nextEvent = true;
                                break;
                            }
                        }
                        else
                            nextPos[j] = pos[j];
                    }

                    if (nextEvent) continue;

                    nextTuple.Set(nextPos, _bits);
                    var k = 0;
                    if (positionNewStates.TryGetValue(nextTuple, out k)) newAdjacencyMatrix.Add(state.Value, e, k);
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
            _maxSize[0] = (1 << (int) Math.Max(Math.Ceiling(Math.Log(Size, 2)), 1)) - 1;

            GC.Collect();
        }

        public Dictionary<string, string> simplifyName(string newName = null, bool simplifyStatesName = true)
        {
            simplify();
            var namesMap = new Dictionary<string, string>(_statesList[0].Length);

            if (simplifyStatesName)
            {
                for (var s = 0; s < _statesList[0].Length; ++s)
                {
                    var newStateName = s.ToString();
                    namesMap.Add(newStateName, _statesList[0][s].ToString());
                    _statesList[0][s] = new State(newStateName, _statesList[0][s].Marking);
                }
            }

            if (newName != null) Name = newName;

            return namesMap;
        }


        public override string ToString()
        {
            return Name;
        }
    }
}