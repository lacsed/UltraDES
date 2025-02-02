using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UltraDES;

using DFA = DeterministicFiniteAutomaton;

/// <summary>
/// Represents a deterministic finite automaton (DFA) that is used within the UltraDES framework.
/// This class is sealed and cannot be inherited.
/// </summary>
[Serializable]
public sealed partial class DeterministicFiniteAutomaton
{
    /// <summary>
    /// Gets or sets a value indicating whether UltraDES uses parallel programming.
    /// </summary>
    public static bool Multicore { get; set; } = true;

    /// <summary>
    /// Gets the number of threads to be used based on the Multicore setting and processor count.
    /// </summary>
    private static int NumberOfThreads => Multicore ? Math.Max(2, 10 * Environment.ProcessorCount) : 1;

    /// <summary>
    /// List of adjacency matrices, one per automaton component.
    /// </summary>
    private readonly List<AdjacencyMatrix> _adjacencyList;

    /// <summary>
    /// List of event availability arrays, one per automaton component.
    /// </summary>
    private readonly List<bool[]> _eventsList;

    /// <summary>
    /// Lock object for synchronizing access to shared resources.
    /// </summary>
    private readonly object _lockObject = new();

    /// <summary>
    /// List of state arrays, one per automaton component.
    /// </summary>
    private readonly List<AbstractState[]> _statesList;

    /// <summary>
    /// Bit masks used for state representation.
    /// </summary>
    private int[] _bits;

    /// <summary>
    /// Union of all events appearing in the transitions.
    /// </summary>
    private AbstractEvent[] _eventsUnion;

    /// <summary>
    /// Maximum sizes for bit representation per component.
    /// </summary>
    private int[] _maxSize;

    /// <summary>
    /// Number of plants (components) in the automaton.
    /// </summary>
    private int _numberOfPlants;

    /// <summary>
    /// Reverse transitions for each component, organized by state and event.
    /// </summary>
    private List<int>[][][] _reverseTransitionsList;

    /// <summary>
    /// Tuple size used for representing composite states.
    /// </summary>
    private int _tupleSize;

    /// <summary>
    /// Dictionary of valid composite states. The key is a tuple of state indices and the value indicates validity.
    /// </summary>
    private Dictionary<StatesTuple, bool> _validStates;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicFiniteAutomaton"/> class using a set of transitions,
    /// an initial state, and a name.
    /// </summary>
    /// <param name="transitions">A collection of transitions expressed as tuples (Origin, Trigger, Destination).</param>
    /// <param name="initial">The initial state of the automaton.</param>
    /// <param name="name">The name of the automaton.</param>
    public DeterministicFiniteAutomaton(IEnumerable<(AbstractState, AbstractEvent, AbstractState)> transitions,
        AbstractState initial, string name) :
        this(transitions.Select(t => (Transition)t), initial, name)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeterministicFiniteAutomaton"/> class using a set of transitions,
    /// an initial state, and a name.
    /// </summary>
    /// <param name="transitions">A collection of <see cref="Transition"/> objects representing the transitions.</param>
    /// <param name="initial">The initial state of the automaton.</param>
    /// <param name="name">The name of the automaton.</param>
    public DeterministicFiniteAutomaton(IEnumerable<Transition> transitions, AbstractState initial, string name) :
        this(1)
    {
        Name = name;

        var transitionsLocal = transitions as Transition[] ?? transitions.ToArray();

        // Build the state list from all transitions and include the initial state.
        _statesList.Add(transitionsLocal.SelectMany(t => new[] { t.Origin, t.Destination }).Union(new[] { initial })
            .Distinct().ToArray());
        // Build the union of events, ordering them by controllability.
        _eventsUnion = transitionsLocal.Select(t => t.Trigger).Distinct().OrderBy(i => i.Controllability).ToArray();
        // Create an adjacency matrix for the single automaton component.
        _adjacencyList.Add(new AdjacencyMatrix(_statesList[0].Length, _eventsUnion.Length));

        // Ensure that the initial state is positioned at index 0.
        var initialIdx = Array.IndexOf(_statesList[0], initial);
        if (initialIdx != 0) (_statesList[0][0], _statesList[0][initialIdx]) = (_statesList[0][initialIdx], _statesList[0][0]);

        // Initialize the events availability array (all events available initially).
        var events = new bool[_eventsUnion.Length];
        for (var i = 0; i < events.Length; ++i)
            events[i] = true;
        _eventsList.Add(events);

        Size = _statesList[0].Length;

        _bits[0] = 0;
        _maxSize[0] = (1 << MinNumOfBits(_statesList[0].Length)) - 1;
        _tupleSize = 1;

        // Build the transitions for each state in the adjacency list.
        for (var i = 0; i < _statesList[0].Length; ++i)
        {
            _adjacencyList[0].Add(i,
                transitionsLocal.AsParallel().WithDegreeOfParallelism(NumberOfThreads)
                    .Where(t => t.Origin == _statesList[0][i]).Select(t =>
                        (Array.IndexOf(_eventsUnion, t.Trigger),
                            Array.IndexOf(_statesList[0], t.Destination))).ToArray());
        }
    }

    /// <summary>
    /// Private constructor that initializes internal collections with the specified capacity.
    /// </summary>
    /// <param name="n">The capacity (number of automaton components).</param>
    private DeterministicFiniteAutomaton(int n)
    {
        _statesList = new List<AbstractState[]>(n);
        _eventsList = new List<bool[]>(n);
        _adjacencyList = new List<AdjacencyMatrix>(n);
        _bits = new int[n];
        _maxSize = new int[n];
        _numberOfPlants = n;
    }

    /// <summary>
    /// Gets the set of all events in the automaton.
    /// </summary>
    public IEnumerable<AbstractEvent> Events => _eventsUnion;

    /// <summary>
    /// Gets the initial composite state of the automaton.
    /// Returns null if the automaton is empty.
    /// </summary>
    public AbstractState InitialState => IsEmpty() ? null : ComposeState(new int[_statesList.Count]);

    /// <summary>
    /// Gets the marked (accepting) states of the automaton.
    /// </summary>
    public IEnumerable<AbstractState> MarkedStates
    {
        get
        {
            var numAut = _statesList.Count;
            var stateArr = new int[numAut];

            if (_validStates != null)
            {
                foreach (var s in _validStates)
                {
                    s.Key.Get(stateArr, _bits, _maxSize);
                    if (IsMarketState(stateArr))
                        yield return ComposeState(stateArr);
                }
            }
            else if (!IsEmpty())
            {
                do
                {
                    if (IsMarketState(stateArr))
                        yield return ComposeState(stateArr);
                } while (IncrementPosition(stateArr));
            }
        }
    }

    /// <summary>
    /// Gets the name of the automaton.
    /// </summary>
    public string Name { get; private set; }

    private long _size = 0;

    /// <summary>
    /// Gets or sets the total number of composite states in the automaton.
    /// </summary>
    public long Size
    {
        get => _size;
        set => _size = value;
    }

    /// <summary>
    /// Gets all composite states of the automaton.
    /// </summary>
    public IEnumerable<AbstractState> States
    {
        get
        {
            var numAut = _statesList.Count;
            var stateArr = new int[numAut];

            if (_validStates != null)
            {
                foreach (var s in _validStates)
                {
                    s.Key.Get(stateArr, _bits, _maxSize);
                    yield return ComposeState(stateArr);
                }
            }
            else if (!IsEmpty())
            {
                do
                {
                    yield return ComposeState(stateArr);
                } while (IncrementPosition(stateArr));
            }
        }
    }

    /// <summary>
    /// Generates a DOT language representation of the automaton with formatting options for states and transitions.
    /// </summary>
    /// <param name="stateColor">
    /// A collection of tuples mapping states to their corresponding SVG color.
    /// </param>
    /// <param name="transitionStyle">
    /// (Optional) A collection of tuples mapping transitions to a tuple of SVG color and GraphViz style.
    /// </param>
    /// <returns>A string containing the DOT representation of the automaton.</returns>
    public string ToFormattedDotCode(IEnumerable<(AbstractState q, SVGColors c)> stateColor, IEnumerable<(Transition t, SVGColors c, GraphVizStyle s)> transitionStyle = null)
    {
        stateColor ??= Array.Empty<(AbstractState q, SVGColors c)>();
        transitionStyle ??= Array.Empty<(Transition t, SVGColors c, GraphVizStyle s)>();

        var styleState = stateColor.ToDictionary(s => s.q, s => s.c);
        var styleTrans = transitionStyle.ToDictionary(s => s.t, s => (s.c, s.s));

        var transitions = Transitions.ToArray();
        var states = States.ToArray();

        var dot = new StringBuilder("digraph {\n\trankdir=TB;", (int)Size * InitialState.ToString().Length);

        foreach (var q in states)
        {
            var style = styleState.TryGetValue(q, out var value) ? $" style = filled fillcolor = {value}" : "";
            dot.Append($"\n\t\"{q}\" [shape = {(q.IsMarked ? "doublecircle" : "circle")}{style}];");
        }

        dot.Append($"\n\tInitial [shape = point]; \n\tInitial -> \"{InitialState}\";\n");

        foreach (var group in transitions.GroupBy(t => (t.Origin, t.Destination)))
        {
            var events1 = group.Where(t => !styleTrans.ContainsKey(t))
                .Aggregate("", (acc, t) => acc + "," + t.Trigger)
                .Trim(',');
            if (events1 != "")
                dot.Append($"\n\t\"{group.Key.Origin}\" -> \"{group.Key.Destination}\" [label = \"{events1}\"]");

            foreach (var groupStyle in group.Where(t => styleTrans.ContainsKey(t)).GroupBy(t => styleTrans[t]))
            {
                var events2 = groupStyle.Aggregate("", (acc, t) => acc + "," + t.Trigger).Trim(',');
                var style = $"style = {groupStyle.Key.s} color = {groupStyle.Key.c} fontcolor = {groupStyle.Key.c}";
                dot.Append($"\n\t\"{group.Key.Origin}\" -> \"{group.Key.Destination}\" [label = \"{events2}\" {style}]");
            }
        }

        dot.Append("\n}");
        return dot.ToString();
    }

    /// <summary>
    /// Generates a DOT language representation of the automaton with formatting options for states and events.
    /// </summary>
    /// <param name="stateColor">
    /// A collection of tuples mapping states to their corresponding SVG color.
    /// </param>
    /// <param name="eventStyle">
    /// (Optional) A collection of tuples mapping events to their corresponding SVG color.
    /// </param>
    /// <returns>A string containing the DOT representation of the automaton.</returns>
    public string ToFormattedDotCode(IEnumerable<(AbstractState q, SVGColors c)> stateColor, IEnumerable<(AbstractEvent e, SVGColors c)> eventStyle = null)
    {
        stateColor ??= Array.Empty<(AbstractState q, SVGColors c)>();
        eventStyle ??= Array.Empty<(AbstractEvent e, SVGColors c)>();

        var styleState = stateColor.ToDictionary(s => s.q, s => s.c);
        var styleTrans = eventStyle.ToDictionary(s => s.e, s => s.c);

        var transitions = Transitions.ToArray();
        var states = States.ToArray();

        var dot = new StringBuilder("digraph {\n\trankdir=TB;", (int)Size * InitialState.ToString().Length);

        foreach (var q in states)
        {
            var style = styleState.TryGetValue(q, out var value) ? $" style = filled fillcolor = {value}" : "";
            dot.Append($"\n\t\"{q}\" [shape = {(q.IsMarked ? "doublecircle" : "circle")}{style}];");
        }

        dot.Append($"\n\tInitial [shape = point]; \n\tInitial -> \"{InitialState}\";\n");

        foreach (var group in transitions.GroupBy(t => (t.Origin, t.Destination)))
        {
            var events1 = group.Where(t => !styleTrans.ContainsKey(t.Trigger))
                .Aggregate("", (acc, t) => acc + "," + t.Trigger)
                .Trim(',');
            if (events1 != "")
                dot.Append($"\n\t\"{group.Key.Origin}\" -> \"{group.Key.Destination}\" [label = \"{events1}\"]");

            foreach (var groupStyle in group.Where(t => styleTrans.ContainsKey(t.Trigger)).GroupBy(t => styleTrans[t.Trigger]))
            {
                var events2 = groupStyle.Aggregate("", (acc, t) => acc + "," + t.Trigger).Trim(',');
                var style = $"color = {groupStyle.Key} fontcolor = {groupStyle.Key}";
                dot.Append($"\n\t\"{group.Key.Origin}\" -> \"{group.Key.Destination}\" [label = \"{events2}\" {style}]");
            }
        }

        dot.Append("\n}");
        return dot.ToString();
    }

    /// <summary>
    /// Gets a DOT language representation of the automaton without additional formatting.
    /// This representation includes the initial state, marked states, and transitions.
    /// </summary>
    public string ToDotCode
    {
        get
        {
            var dot = new StringBuilder("digraph {\nrankdir=TB;", (int)Size * InitialState.ToString().Length);

            // Marked states are represented with a double circle.
            dot.Append("\nnode [shape = doublecircle];");
            foreach (var ms in MarkedStates)
                dot.AppendFormat(" \"{0}\" ", ms);

            // All other states are represented with a single circle.
            dot.Append("\nnode [shape = circle];");

            var n = _statesList.Count;
            int[] pos = new int[n];

            // Local function to add transitions from the current state.
            var addTransitions = new Action(() =>
            {
                foreach (var group in GetTransitionsFromState(pos).GroupBy(t => t.Destination))
                {
                    dot.Append($"\"{@group.First().Origin}\" -> \"{@group.Key}\" [ label = \"");
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
                        dot.Append($" \"{ComposeState(pos)}\" ");
                }

                dot.Append($"\nnode [shape = point ]; Initial\nInitial -> \"{InitialState}\";\n");
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
                        dot.Append($" \"{ComposeState(pos)}\" ");
                } while (IncrementPosition(pos));

                dot.Append($"\nnode [shape = point ]; Initial\nInitial -> \"{InitialState}\";\n");

                do addTransitions();
                while (IncrementPosition(pos));
            }

            dot.Append("}");
            return dot.ToString();
        }
    }

    /// <summary>
    /// Converts the automaton into an equivalent regular expression.
    /// The automaton is first simplified and then transformed using state elimination.
    /// </summary>
    public RegularExpression ToRegularExpression
    {
        get
        {
            if (IsEmpty()) return Symbol.Empty;
            Simplify();

            var size = (int)Size;
            var b = new RegularExpression[size];
            var a = new RegularExpression[size, size];

            // Initialize the regular expression for each transition.
            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j < size; j++)
                    a[i, j] = Symbol.Empty;

                for (var e = 0; e < _eventsUnion.Length; e++)
                {
                    if (_adjacencyList[0].TryGet(i, e, out int val))
                        a[i, val] += _eventsUnion[e];
                }
            }

            // Initialize the state acceptance expressions.
            for (var i = 0; i < size; ++i)
                b[i] = _statesList[0][i].IsMarked ? Symbol.Epsilon : Symbol.Empty;

            // Perform state elimination to compute the regular expression.
            for (var n = size - 1; n >= 0; n--)
            {
                b[n] = new KleeneStar(a[n, n]) * b[n];
                for (var j = 0; j <= n; j++)
                    a[n, j] = new KleeneStar(a[n, n]) * a[n, j];
                for (var i = 0; i <= n; i++)
                {
                    b[i] += a[i, n] * b[n];
                    for (var j = 0; j <= n; j++)
                        a[i, j] += a[i, n] * a[n, j];
                }
            }

            return b[0].Simplify;
        }
    }

    /// <summary>
    /// Gets the transition function of the automaton.
    /// Given a state and an event, the function returns the resulting state (if a valid transition exists).
    /// </summary>
    public Func<AbstractState, AbstractEvent, Option<AbstractState>> TransitionFunction
    {
        get
        {
            var n = _statesList.Count;
            var st = new Dictionary<AbstractState, int>[n];
            for (var i = 0; i < n; ++i)
            {
                st[i] = new Dictionary<AbstractState, int>(_statesList[i].Length);
                for (var j = 0; j < _statesList[i].Length; ++j)
                    st[i].Add(_statesList[i][j], j);
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
                    if (!st[i].TryGetValue(S[i], out pos[i]))
                        return None<AbstractState>.Create();
                }

                var k = Array.IndexOf(_eventsUnion, e);
                if (k < 0) return None<AbstractState>.Create();

                for (var i = 0; i < n; ++i)
                {
                    if (!_eventsList[i][k]) continue;
                    pos[i] = _adjacencyList[i][pos[i], k];
                    if (pos[i] < 0) return None<AbstractState>.Create();
                }

                if (_validStates != null && !_validStates.ContainsKey(new StatesTuple(pos, _bits, _tupleSize)))
                    return None<AbstractState>.Create();

                return Some<AbstractState>.Create(n == 1 ? _statesList[0][pos[0]] : ComposeState(pos));
            };
        }
    }

    /// <summary>
    /// Gets all transitions of the automaton.
    /// Iterates through all valid composite states and yields the transitions from each.
    /// </summary>
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
                    foreach (var i in GetTransitionsFromState(pos))
                        yield return i;
                }
            }
            else if (!IsEmpty())
            {
                do
                {
                    foreach (var i in GetTransitionsFromState(pos))
                        yield return i;
                } while (IncrementPosition(pos));
            }
        }
    }

    /// <summary>
    /// Gets the events that are uncontrollable.
    /// </summary>
    public IEnumerable<AbstractEvent> UncontrollableEvents => _eventsUnion.Where(i => !i.IsControllable);

    /// <summary>
    /// Creates a deep copy of the automaton with a specified capacity.
    /// </summary>
    /// <param name="capacity">The number of automaton components to copy.</param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/> that is a clone of the current automaton.</returns>
    public DFA Clone(int capacity)
    {
        var G = new DFA(capacity) { _eventsUnion = (AbstractEvent[])_eventsUnion.Clone() };
        G._statesList.AddRange(_statesList);

        for (var i = 0; i < _adjacencyList.Count; ++i)
        {
            G._adjacencyList.Add(_adjacencyList[i].Clone());
            G._eventsList.Add((bool[])_eventsList[i].Clone());
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

    /// <summary>
    /// Creates a deep copy of the automaton.
    /// </summary>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/> that is a clone of the current automaton.</returns>
    public DFA Clone() => Clone(_statesList.Count());

    /// <summary>
    /// Composes a composite state from an array of positions (one per automaton component).
    /// </summary>
    /// <param name="pPos">An array of indices representing the state in each component.</param>
    /// <returns>An <see cref="AbstractState"/> representing the composite state.</returns>
    private AbstractState ComposeState(int[] pPos)
    {
        var n = pPos.Length;

        if (n == 1) return _statesList[0][pPos[0]];

        var marked = _statesList[0][pPos[0]].IsMarked;
        var states = new AbstractState[n];
        states[0] = _statesList[0][pPos[0]];
        for (var j = 1; j < n; ++j)
        {
            states[j] = _statesList[j][pPos[j]];
            marked &= _statesList[j][pPos[j]].IsMarked;
        }

        return new CompoundState(states, marked ? Marking.Marked : Marking.Unmarked);
    }

    /// <summary>
    /// Performs a depth-first search (DFS) to explore all composite states.
    /// Optionally checks for "bad" states during the search.
    /// </summary>
    /// <param name="acceptAllStates">If true, all reachable states are accepted; otherwise, only valid states are accepted.</param>
    /// <param name="checkForBadStates">If true, checks and removes bad states during the search.</param>
    /// <returns>True if any bad states were removed; otherwise, false.</returns>
    private bool DepthFirstSearch(bool acceptAllStates, bool checkForBadStates = false)
    {
        // Initialize the DFS stack and reset the total state counter.
        var statesStack = new Stack<StatesTuple>();
        Size = 0;

        // Create the initial composite state tuple.
        var initialState = new StatesTuple(new int[_statesList.Count], _bits, _tupleSize);

        // If not accepting all states and the initial state is invalid, return false immediately.
        if (!acceptAllStates && !_validStates.ContainsKey(initialState))
            return false;

        _validStates[initialState] = true;
        statesStack.Push(initialState);

        // Launch parallel tasks for DFS using (NumberOfThreads - 1) threads.
        var tasks = new Task[NumberOfThreads - 1];
        for (int i = 0; i < NumberOfThreads - 1; i++)
        {
            tasks[i] = Task.Run(() => DepthFirstSearchThread(acceptAllStates, statesStack));
        }
        // Execute DFS on the current thread.
        DepthFirstSearchThread(acceptAllStates, statesStack);
        Task.WaitAll(tasks.ToArray());

        // Allow garbage collection of the DFS stack.
        statesStack = null;

        bool newBadStates = false;
        int uncontrollableEventsCount = UncontrollableEvents.Count();

        // If bad state checking is enabled, remove the invalid states.
        if (checkForBadStates)
        {
            newBadStates = _validStates
                .Where(kvp => !kvp.Value)
                .ToList()
                .Aggregate(newBadStates, (current, kvp) => current | RemoveBadStates(kvp.Key, uncontrollableEventsCount, true));
        }

        // Clean the _validStates dictionary: remove false entries and reset true entries.
        foreach (var key in _validStates.Keys.ToList())
        {
            if (!_validStates[key]) _validStates.Remove(key);
            else _validStates[key] = false;
        }

        return newBadStates;
    }

    /// <summary>
    /// Performs DFS in a separate thread. This method is used by <see cref="DepthFirstSearch"/> for parallel state exploration.
    /// </summary>
    /// <param name="param">Parameter indicating whether all states should be accepted.</param>
    /// <param name="statesStack">The shared stack containing state tuples to be processed.</param>
    private void DepthFirstSearchThread(object param, Stack<StatesTuple> statesStack)
    {
        var length = 0;
        var n = _statesList.Count;
        var pos = new int[n];
        var nextPosition = new int[n];
        var acceptAllStates = (bool)param;

        while (true)
        {
            StatesTuple tuple;
            lock (_lockObject)
            {
                if (statesStack.Count == 0) break;
                tuple = statesStack.Pop();
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
                    else if (_adjacencyList[i].TryGet(pos[i], e, out int val))
                        nextPosition[i] = val;
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
                        statesStack.Push(tuple);
                    }
                    else if (acceptAllStates)
                    {
                        _validStates[tuple] = true;
                        statesStack.Push(tuple);
                    }
                }
            }
        }

        Interlocked.Add(ref _size, length);
    }

    /// <summary>
    /// Draws the automaton as a LaTeX figure and optionally opens the resulting file.
    /// </summary>
    /// <param name="fileName">
    /// The file name to use. If null, the automaton name is used.
    /// </param>
    /// <param name="openAfterFinish">
    /// If true, the resulting file is opened after generation.
    /// </param>
    public void drawLatexFigure(string fileName = null, bool openAfterFinish = true)
    {
        fileName ??= Name;
        fileName = fileName.Replace('|', '_');
        Drawing.drawLatexFigure(this, fileName, openAfterFinish);
    }

    /// <summary>
    /// Draws the automaton as an SVG figure and optionally opens the resulting file.
    /// </summary>
    /// <param name="fileName">
    /// The file name to use. If null, the automaton name is used.
    /// </param>
    /// <param name="openAfterFinish">
    /// If true, the resulting file is opened after generation.
    /// </param>
    public void drawSVGFigure(string fileName = null, bool openAfterFinish = true)
    {
        fileName ??= Name;
        fileName = fileName.Replace('|', '_');
        Drawing.drawSVG(this, fileName, openAfterFinish);
    }

    /// <summary>
    /// Explores the automaton states starting from a given state using a modified DFS that also removes bad states.
    /// </summary>
    /// <param name="obj">An identifier for the plant component.</param>
    /// <param name="statesStack">A stack of state tuples to process.</param>
    /// <param name="removeBadStates">A stack indicating whether bad state removal should occur.</param>
    private void FindStates(int obj, Stack<StatesTuple> statesStack, Stack<bool> removeBadStates)
    {
        var n = _statesList.Count;
        var nPlant = obj;
        var pos = new int[n];
        var nextPosition = new int[n];
        var uncontrollableEventsCount = UncontrollableEvents.Count();
        var nextStates = new StatesTuple[uncontrollableEventsCount];

        while (true)
        {
            var nextState = false;
            StatesTuple tuple;
            bool vRemoveBadStates;
            lock (_lockObject)
            {
                if (statesStack.Count == 0) break;
                tuple = statesStack.Pop();
                vRemoveBadStates = removeBadStates.Pop();
                if (_validStates.ContainsKey(tuple)) continue;
            }

            tuple.Get(pos, _bits, _maxSize);
            var k = 0;

            for (var e = 0; e < uncontrollableEventsCount; ++e)
            {
                var nextEvent = false;
                var plantHasEvent = false;

                for (var i = 0; i < nPlant; ++i)
                {
                    if (!_eventsList[i][e]) nextPosition[i] = pos[i];
                    else if (_adjacencyList[i].TryGet(pos[i], e, out var val))
                    {
                        nextPosition[i] = val;
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
                    else if (_adjacencyList[i].TryGet(pos[i], e, out int val))
                        nextPosition[i] = val;
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
                if (_validStates.ContainsKey(tuple)) continue;

                var j = 0;
                for (var i = 0; i < k; ++i)
                {
                    if (!_validStates.TryGetValue(nextStates[i], out var vValue))
                    {
                        statesStack.Push(nextStates[i]);
                        removeBadStates.Push(true);
                        ++j;
                    }
                    else if (vValue)
                    {
                        while (--j >= 0)
                        {
                            statesStack.Pop();
                            removeBadStates.Pop();
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
                    else if (_adjacencyList[i].TryGet(pos[i], e, out int val))
                        nextPosition[i] = val;
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
                    if (_validStates.ContainsKey(nextTuple)) continue;
                    statesStack.Push(nextTuple);
                    removeBadStates.Push(false);
                }
            }
        }
    }

    /// <summary>
    /// Retrieves all outgoing transitions from the composite state represented by the specified positions.
    /// </summary>
    /// <param name="pos">An array representing the current position in each automaton component.</param>
    /// <returns>An enumerable collection of <see cref="Transition"/> objects from the current state.</returns>
    private IEnumerable<Transition> GetTransitionsFromState(int[] pos)
    {
        var n = _statesList.Count;
        var nextPosition = new int[n];
        int e, i;
        var uncontrollableEventsCount = UncontrollableEvents.Count();
        var nextStates = new StatesTuple[uncontrollableEventsCount];
        var vEvents = new int[uncontrollableEventsCount];
        var k = 0;

        var currentState = ComposeState(pos);

        // Process uncontrollable events first.
        for (e = 0; e < uncontrollableEventsCount; ++e)
        {
            var nextEvent = false;
            var plantHasEvent = false;

            for (i = 0; i < _numberOfPlants; ++i)
            {
                if (!_eventsList[i][e])
                    nextPosition[i] = pos[i];
                else if (_adjacencyList[i].TryGet(pos[i], e, out int val))
                {
                    nextPosition[i] = val;
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
                else if (_adjacencyList[i].TryGet(pos[i], e, out int val))
                    nextPosition[i] = val;
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
            if (_validStates != null && !_validStates.ContainsKey(nextStates[k])) continue;
            vEvents[k] = e;
            ++k;
        }

        for (i = 0; i < k; ++i)
        {
            nextStates[i].Get(nextPosition, _bits, _maxSize);
            var nextState = ComposeState(nextPosition);
            yield return new Transition(currentState, _eventsUnion[vEvents[i]], nextState);
        }

        // Process controllable events.
        for (e = uncontrollableEventsCount; e < _eventsUnion.Length; ++e)
        {
            var nextEvent = false;
            for (i = 0; i < n; ++i)
            {
                if (!_eventsList[i][e])
                    nextPosition[i] = pos[i];
                else if (_adjacencyList[i].TryGet(pos[i], e, out int val))
                    nextPosition[i] = val;
                else
                {
                    nextEvent = true;
                    break;
                }
            }

            if (nextEvent) continue;
            var nextTuple = new StatesTuple(nextPosition, _bits, _tupleSize);

            if (_validStates != null && !_validStates.ContainsKey(nextTuple)) continue;
            var nextState = ComposeState(nextPosition);
            yield return new Transition(currentState, _eventsUnion[e], nextState);
        }
    }

    /// <summary>
    /// Increments the given composite state position to the next possible state.
    /// </summary>
    /// <param name="pPos">An array of indices representing the current state in each component.</param>
    /// <returns>
    /// True if the position was successfully incremented to a new valid state; false if all states have been exhausted.
    /// </returns>
    private bool IncrementPosition(int[] pPos)
    {
        var k = _statesList.Count - 1;
        while (k >= 0)
        {
            if (++pPos[k] < _statesList[k].Length)
                return true;
            pPos[k] = 0;
            --k;
        }

        return false;
    }

    /// <summary>
    /// Performs an inverse search on the automaton to explore states from which the current state can be reached.
    /// </summary>
    /// <param name="pParam">
    /// A parameter indicating whether valid state checking should be skipped.
    /// </param>
    /// <param name="statesStack">
    /// A shared stack used to store state tuples for inverse search.
    /// </param>
    private void InverseSearchThread(object pParam, Stack<StatesTuple> statesStack)
    {
        var length = 0;
        var n = _statesList.Count;
        var pos = new int[n];
        var nextPos = new int[n];
        var movs = new int[n];

        var vNotCheckValidState = (bool)pParam;

        while (true)
        {
            StatesTuple tuple;
            lock (statesStack)
            {
                if (statesStack.Count == 0) break;
                tuple = statesStack.Pop();
            }
            tuple.Get(pos, _bits, _maxSize);

            ++length;

            for (var e = 0; e < _eventsUnion.Length; ++e)
            {
                var nextEvent = false;
                for (int i = 0; i < n; ++i)
                {
                    if (_reverseTransitionsList[i][pos[i]][e].Any()) continue;
                    nextEvent = true;
                    break;
                }

                if (nextEvent) continue;

                for (int i = 0; i < n - 1; ++i)
                    nextPos[i] = _reverseTransitionsList[i][pos[i]][e][movs[i]];
                movs[n - 1] = -1;
                while (true)
                {
                    int i;
                    for (i = n - 1; i >= 0; --i)
                    {
                        ++movs[i];
                        if (movs[i] < _reverseTransitionsList[i][pos[i]][e].Count) break;
                        movs[i] = 0;
                        nextPos[i] = _reverseTransitionsList[i][pos[i]][e][0];
                    }

                    if (i < 0) break;

                    nextPos[i] = _reverseTransitionsList[i][pos[i]][e][movs[i]];

                    tuple = new StatesTuple(nextPos, _bits, _tupleSize);
                    lock (statesStack)
                    {
                        if ((!_validStates.TryGetValue(tuple, out var value) && !vNotCheckValidState) || value) continue;
                        _validStates[tuple] = true;
                        statesStack.Push(tuple);
                    }
                }
            }
        }

        Interlocked.Add(ref _size, length);
    }

    /// <summary>
    /// Determines whether the automaton is empty.
    /// </summary>
    /// <returns>True if the automaton contains no states; otherwise, false.</returns>
    private bool IsEmpty() => Size == 0;

    /// <summary>
    /// Checks if the composite state represented by the given positions is marked.
    /// </summary>
    /// <param name="pPos">An array of indices representing the composite state.</param>
    /// <returns>True if the composite state is marked; otherwise, false.</returns>
    private bool IsMarketState(int[] pPos)
    {
        var n = _statesList.Count;
        for (var i = 0; i < n; ++i)
            if (!_statesList[i][pPos[i]].IsMarked)
                return false;

        return true;
    }

    /// <summary>
    /// Constructs the reverse transitions list for the automaton.
    /// This method is executed only once and caches the result.
    /// </summary>
    private void MakeReverseTransitions()
    {
        if (_reverseTransitionsList != null) return;

        _reverseTransitionsList = new List<int>[_statesList.Count][][];

        Parallel.For(0, _statesList.Count, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, Loop);
        return;

        void Loop(int aut)
        {
            var states = _statesList[aut];
            var adj = _adjacencyList[aut];
            var evs = _eventsList[aut];
            var trans = new List<int>[states.Length][];

            for (var state = 0; state < states.Length; ++state)
            {
                trans[state] = new List<int>[_eventsUnion.Length];
                for (var e = 0; e < _eventsUnion.Length; ++e)
                    trans[state][e] = new List<int>();
            }

            for (var state = 0; state < states.Length; ++state)
            {
                foreach (var tr in adj[state])
                    trans[tr.s][tr.e].Add(state);
            }

            for (var e = 0; e < _eventsUnion.Length; ++e)
            {
                if (evs[e]) continue;
                for (var state = 0; state < states.Length; ++state)
                    trans[state][e].Add(state);
            }

            for (var state = 0; state < states.Length; ++state)
            {
                foreach (var p in trans[state])
                    p.TrimExcess();
            }

            _reverseTransitionsList[aut] = trans;
        }
    }

    /// <summary>
    /// Minimizes the automaton by merging equivalent states.
    /// This method updates the states, transitions, and associated internal data structures.
    /// </summary>
    private void Minimize()
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
                    hasEvents[e] = _adjacencyList[0].TryGet(k, e, out int val);
                    if (hasEvents[e]) transitions[e] = val;
                }

                var j = i + 1;
                while (true)
                {
                    while (j < numStatesOld && statesMap[positions[j]] == k) ++j;
                    if (j >= numStatesOld) break;
                    l = positions[j];
                    for (var e = numEvents - 1; e >= 0; --e)
                    {
                        if (hasEvents[e] == _adjacencyList[0].TryGet(l, e, out int val) &&
                            (!hasEvents[e] || statesMap[transitions[e]] == statesMap[val])) continue;
                        nextState = true;
                        break;
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

        if (initial != 0) (newStates[0], newStates[initial]) = (newStates[initial], newStates[0]);

        for (var i = 0; i < numStatesOld; ++i)
        {
            k = statesMap[i];
            k = k == 0 ? initial : k == initial ? 0 : k;
            for (var e = 0; e < numEvents; ++e)
            {
                if (!_adjacencyList[0].TryGet(i, e, out int val)) continue;
                l = statesMap[val];
                l = l == 0 ? initial : l == initial ? 0 : l;
                newTransitions.Add(k, e, l);
            }
        }

        _statesList[0] = newStates;
        _adjacencyList[0] = newTransitions;
        _maxSize[0] = (1 << MinNumOfBits(newStates.Length)) - 1;
        Size = newStates.Length;
        Name = "Min(" + Name + ")";
    }

    /// <summary>
    /// Performs a radix sort on the positions array based on the states and transitions.
    /// This method is used by the minimization process.
    /// </summary>
    /// <param name="map">An array mapping old state indices to new ones.</param>
    /// <param name="positions">The array of state positions to be sorted.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RadixSort(int[] map, int[] positions)
    {
        int n = positions.Length;

        // Cache references for efficiency.
        var states = _statesList[0];
        var adjacency = _adjacencyList[0];
        var events = _eventsUnion;

        // Auxiliary array for sorted positions.
        var b = new int[n];
        // Bucket array with one extra element.
        var bucket = new int[n + 1];

        // -------- 1) Sort based on IsMarked property --------
        for (int i = 0; i < n; i++)
            bucket[states[positions[i]].IsMarked ? 1 : 0]++;

        bucket[1] += bucket[0];

        for (int i = n - 1; i >= 0; i--)
        {
            int pos = positions[i];
            bool marked = states[pos].IsMarked;
            b[--bucket[marked ? 1 : 0]] = pos;
        }

        // Swap references instead of copying arrays.
        var temp = positions;
        positions = b;
        b = temp;

        bucket[0] = bucket[1] = 0;

        // -------- 2) Sort by transitions for each event --------
        int eventCount = events.Length;
        for (int e = 0; e < eventCount; e++)
        {
            Array.Clear(bucket, 0, n + 1);

            for (int i = 0; i < n; i++)
            {
                int pos = positions[i];
                int dest = adjacency.TryGet(pos, e, out int val) ? map[val] : n;
                bucket[dest]++;
            }

            for (int i = 1; i <= n; i++)
            {
                bucket[i] += bucket[i - 1];
            }

            for (int i = n - 1; i >= 0; i--)
            {
                int pos = positions[i];
                int dest = adjacency.TryGet(pos, e, out int val) ? map[val] : n;
                b[--bucket[dest]] = pos;
            }

            temp = positions;
            positions = b;
            b = temp;
        }
    }

    /// <summary>
    /// Removes states that are considered "bad" (i.e. states that cannot be reached or lead to unsafe conditions).
    /// </summary>
    /// <param name="initialPos">The initial composite state tuple from which removal is started.</param>
    /// <param name="uncontrolEventsCount">The count of uncontrollable events.</param>
    /// <param name="defaultValue">
    /// The default validity value used when marking states. True indicates removal.
    /// </param>
    /// <returns>True if any bad state was removed; otherwise, false.</returns>
    private bool RemoveBadStates(StatesTuple initialPos, int uncontrolEventsCount, bool defaultValue = false)
    {
        var numAut = _statesList.Count;
        var stack = new Stack<StatesTuple>();
        var pos = new int[numAut];
        var nextPos = new int[numAut];
        var movs = new int[numAut];
        var found = false;

        stack.Push(initialPos);

        while (stack.Count > 0)
        {
            var tuple = stack.Pop();
            tuple.Get(pos, _bits, _maxSize);

            for (var e = 0; e < uncontrolEventsCount; ++e)
            {
                var nextEvent = false;

                for (int i = 0; i < numAut; ++i)
                {
                    if (_reverseTransitionsList[i][pos[i]][e].Count > 0) continue;
                    nextEvent = true;
                    break;
                }

                if (nextEvent) continue;

                for (int i = 0; i < numAut; ++i)
                    nextPos[i] = _reverseTransitionsList[i][pos[i]][e][movs[i]];
                movs[numAut - 1] = -1;
                while (true)
                {
                    int i;
                    for (i = numAut - 1; i >= 0; --i)
                    {
                        ++movs[i];
                        var origins = _reverseTransitionsList[i][pos[i]][e];
                        if (movs[i] < origins.Count) break;
                        movs[i] = 0;
                        nextPos[i] = origins[movs[i]];
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

    /// <summary>
    /// Removes states that are not accessible from the initial state.
    /// If the _validStates dictionary is null, it is initialized via a DFS.
    /// </summary>
    private void RemoveNoAccessibleStates()
    {
        if (_validStates == null)
        {
            _validStates = new Dictionary<StatesTuple, bool>(StatesTupleComparator.GetInstance());
            DepthFirstSearch(true);
        }
        else
        {
            DepthFirstSearch(false);
        }
    }

    /// <summary>
    /// (Obsolete) Displays the automaton using DOT code.
    /// Use <see cref="ShowAutomaton"/> instead.
    /// </summary>
    /// <param name="name">The name to use when displaying the automaton.</param>
    [Obsolete("This method will soon be deprecated. Use ShowAutomaton instead.")]
    public void showAutomaton(string name = "") => ShowAutomaton(name);

    /// <summary>
    /// Displays the automaton by generating its DOT representation and rendering it.
    /// </summary>
    /// <param name="name">
    /// The name to be used for the display. If empty, the automaton's name is used.
    /// </param>
    public void ShowAutomaton(string name = "")
    {
        if (name == "") name = Name;
        Draw.ShowDotCode(ToDotCode, name);
    }

    /// <summary>
    /// Displays a formatted version of the automaton using specified state and transition styles.
    /// </summary>
    /// <param name="stateStyle">
    /// A collection of tuples mapping states to SVG colors.
    /// </param>
    /// <param name="TransitionStyle">
    /// (Optional) A collection of tuples mapping transitions to SVG color and GraphViz style.
    /// </param>
    /// <param name="name">
    /// The name for the display. If empty, the automaton's name is used.
    /// </param>
    public void ShowFormattedAutomaton(IEnumerable<(AbstractState q, SVGColors c)> stateStyle, IEnumerable<(Transition t, SVGColors c, GraphVizStyle s)> TransitionStyle = null, string name = "")
    {
        if (name == "") name = Name;
        Draw.ShowDotCode(ToFormattedDotCode(stateStyle, TransitionStyle), name);
    }

    /// <summary>
    /// Displays a formatted version of the automaton using specified state and event styles.
    /// </summary>
    /// <param name="stateStyle">
    /// A collection of tuples mapping states to SVG colors.
    /// </param>
    /// <param name="eventStyle">
    /// A collection of tuples mapping events to SVG colors.
    /// </param>
    /// <param name="name">
    /// The name for the display. If empty, the automaton's name is used.
    /// </param>
    public void ShowFormattedAutomaton(IEnumerable<(AbstractState q, SVGColors c)> stateStyle, IEnumerable<(AbstractEvent e, SVGColors c)> eventStyle, string name = "")
    {
        if (name == "") name = Name;
        Draw.ShowDotCode(ToFormattedDotCode(stateStyle, eventStyle), name);
    }

    /// <summary>
    /// Simplifies the automaton by removing unreachable states and minimizing transitions.
    /// The method updates the internal states and transition structures.
    /// </summary>
    private void Simplify()
    {
        var newStates = new AbstractState[Size];
        var newAdjacencyMatrix = new AdjacencyMatrix((int)Size, _eventsUnion.Length, true);
        var n = _statesList.Count;
        var positionNewStates = new Dictionary<StatesTuple, int>(StatesTupleComparator.GetInstance());

        if (_validStates == null && n == 1) return;

        var id = 0;
        var pos0 = new int[n];

        if (_validStates != null)
        {
            foreach (var state in _validStates)
            {
                state.Key.Get(pos0, _bits, _maxSize);
                newStates[id] = ComposeState(pos0);
                positionNewStates.Add(state.Key, id);
                ++id;
            }
        }
        else
        {
            do
            {
                newStates[id] = ComposeState(pos0);
                positionNewStates.Add(new StatesTuple(pos0, _bits, _tupleSize), id);
                ++id;
            } while (IncrementPosition(pos0));
        }

        void Loop(KeyValuePair<StatesTuple, int> state)
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
                        if (_adjacencyList[j].TryGet(pos[j], e, out int val))
                            nextPos[j] = val;
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
                if (positionNewStates.TryGetValue(nextTuple, out var k))
                    newAdjacencyMatrix.Add(state.Value, e, k);
            }
        }
        Parallel.ForEach(positionNewStates, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, Loop);

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
        _maxSize[0] = (1 << MinNumOfBits((int)Size)) - 1;

        GC.Collect();
    }

    /// <summary>
    /// (Obsolete) Simplifies the names of the states and returns a mapping of new names to original names.
    /// Use <see cref="SimplifyStatesName"/> instead.
    /// </summary>
    /// <param name="newName">Optional new name for the automaton.</param>
    /// <param name="simplifyStatesName">If true, state names will be simplified.</param>
    /// <returns>A dictionary mapping new state names to their original names.</returns>
    [Obsolete("This method will soon be deprecated, use \"G = G.SimplifyStatesName();\" instead")]
    public Dictionary<string, string> simplifyName(string newName = null, bool simplifyStatesName = true)
    {
        Simplify();
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

    /// <summary>
    /// Simplifies the state names of the automaton and returns a new automaton instance with the updated names.
    /// </summary>
    /// <returns>A new <see cref="DeterministicFiniteAutomaton"/> with simplified state names.</returns>
    public DFA SimplifyStatesName() => SimplifyStatesName(out _);

    /// <summary>
    /// Simplifies the state names of the automaton and returns a new automaton instance with the updated names,
    /// along with a mapping between the original and new states.
    /// </summary>
    /// <param name="map">
    /// When the method returns, contains a dictionary mapping the original states to the simplified states.
    /// </param>
    /// <returns>A new <see cref="DeterministicFiniteAutomaton"/> with simplified state names.</returns>
    public DFA SimplifyStatesName(out Dictionary<AbstractState, AbstractState> map)
    {
        var states = States.ToArray();
        var num = 0;
        var dic = states.ToDictionary(q => q, q => (AbstractState)new State(Convert.ToString(num++, states.Length > 1000 ? 16 : 10), q.Marking));

        var trans = Transitions.Select(t => new Transition(dic[t.Origin], t.Trigger, dic[t.Destination])).ToArray();
        map = dic;

        return new DFA(trans, dic[InitialState], Name);
    }

    /// <summary>
    /// Returns the string representation of the automaton (its name).
    /// </summary>
    /// <returns>The name of the automaton.</returns>
    public override string ToString() => Name;
}
