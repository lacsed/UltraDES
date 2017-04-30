////////////////////////////////////////////////////////////////////////////////////////////////////
// file:	DeterministicFiniteAutomaton.cs
//
// summary:	Implements the deterministic finite automaton class
////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace UltraDES
{
    using Some = Some<AbstractState>;
    using None = None<AbstractState>;
    using DesablingStructure = Dictionary<AbstractState, ISet<AbstractEvent>>;
    using DFA = DeterministicFiniteAutomaton;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    ///     (Serializable)a deterministic finite automaton. This class cannot be inherited.
    /// </summary>
    /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public sealed class DeterministicFiniteAutomaton
    {
        /// <summary>   The adjacency. </summary>
        private readonly AdjacencyMatrix _adjacency;

        /// <summary>   The events. </summary>
        private readonly AbstractEvent[] _events;

        /// <summary>   The initial. </summary>
        private readonly int _initial;

        /// <summary>   The states. </summary>
        private readonly AbstractState[] _states;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="transitions">  The transitions. </param>
        /// <param name="initial">      The initial. </param>
        /// <param name="name">         The name. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DeterministicFiniteAutomaton(IEnumerable<Transition> transitions, AbstractState initial, string name)
        {
            Name = name;

            var transitionsLocal = transitions as Transition[] ?? transitions.ToArray();
            _states = transitionsLocal.SelectMany(t => new[] {t.Origin, t.Destination}).Distinct().ToArray();
            _events = transitionsLocal.Select(t => t.Trigger).Distinct().ToArray();

            _adjacency = new AdjacencyMatrix(_states.Length);

            _initial = Array.IndexOf(_states, initial);

            for (var i = 0; i < _states.Length; i++)
            {
                var i1 = i;
                _adjacency.Add(i,
                    transitionsLocal.AsParallel()
                        .Where(t => t.Origin == _states[i1])
                        .Select(
                            t => Tuple.Create(Array.IndexOf(_events, t.Trigger), Array.IndexOf(_states, t.Destination)))
                        .ToArray());
            }
            Debug.WriteLine("{0} {1}", Name, MarkedStates.Count());
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="transitions">  The transitions. </param>
        /// <param name="events">       The events. </param>
        /// <param name="initial">      The initial. </param>
        /// <param name="name">         The name. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private DeterministicFiniteAutomaton(IEnumerable<Transition> transitions, AbstractEvent[] events,
            AbstractState initial, string name)
        {
            Name = name;

            var transitionsLocal = transitions as Transition[] ?? transitions.ToArray();
            _states = transitionsLocal.SelectMany(t => new[] {t.Origin, t.Destination}).Distinct().ToArray();
            _events = events;

            _adjacency = new AdjacencyMatrix(_states.Length);

            var si = _states.AsParallel()
                .AsOrdered()
                .Select((s, i) => new {ss = s, ii = i})
                .ToDictionary(o => o.ss, o => o.ii);
            var ei = _events.AsParallel()
                .AsOrdered()
                .Select((s, i) => new {ss = s, ii = i})
                .ToDictionary(o => o.ss, o => o.ii);

            _initial = si[initial];

            for (var i = 0; i < _states.Length; i++)
            {
                var i1 = i;
                _adjacency.Add(i,
                    transitionsLocal.AsParallel()
                        .Where(t => t.Origin == _states[i1])
                        .Select(t => Tuple.Create(ei[t.Trigger], si[t.Destination]))
                        .ToArray());
            }
            Debug.WriteLine("MSP {0}", MarkedStates.Count());
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Constructor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="states">       [out] The states. </param>
        /// <param name="events">       The events. </param>
        /// <param name="adjacency">    The adjacency. </param>
        /// <param name="initial">      The initial. </param>
        /// <param name="name">         The name. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private DeterministicFiniteAutomaton(AbstractState[] states, AbstractEvent[] events, AdjacencyMatrix adjacency,
            int initial, string name)
        {
            _states = states;
            _events = events;
            Name = name;
            _initial = initial;
            _adjacency = adjacency;

            Debug.WriteLine("MSP {0}", MarkedStates.Count());
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the accessible part. </summary>
        /// <value> The accessible part. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA AccessiblePart
        {
            get
            {
                var visited = new BitArray(_states.Length, false);
                DepthFirstSearch(_initial, visited);

                AbstractState[] states;

                var adjacency = RemoveStates(visited, this, out states);

                var initial = Array.IndexOf(states, _states[_initial]);

                return new DFA(states, _events, adjacency, initial,
                    string.Format("Ac({0})", Name));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the coaccessible part. </summary>
        /// <value> The coaccessible part. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA CoaccessiblePart
        {
            get
            {
                var visited = new BitArray(_states.Length, false);
                _states.Select((s, i) => s.IsMarked ? i : -1)
                    .Where(s => s != -1)
                    .ToList()
                    .ForEach(s => InverseDepthFirstSearch(s, visited));

                AbstractState[] states;

                var adjacency = RemoveStates(visited, this, out states);

                var initial = Array.IndexOf(states, _states[_initial]);

                return new DFA(states, _events, adjacency, initial,
                    string.Format("Coac({0})", Name));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the trim. </summary>
        /// <value> The trim. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA Trim
        {
            get { return AccessiblePart.CoaccessiblePart; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the prefix closure. </summary>
        /// <value> The prefix closure. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA PrefixClosure
        {
            get
            {
                var states = _states.AsParallel().AsOrdered().Select(s => s.ToMarked).ToArray();

                return new DFA(states, _events, _adjacency, _initial,
                    string.Format("PrefixClosure({0})", Name));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the kleene closure. </summary>
        /// <value> The kleene closure. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA KleeneClosure
        {
            get
            {
                var transitions = new HashSet<Transition>();

                for (var i = 0; i < _adjacency.Length; i++)
                {
                    var s1 = _states[i];
                    foreach (var kvp in _adjacency[i])
                    {
                        var e = _events[kvp.Key];
                        var s2 = _states[kvp.Value];

                        transitions.Add(new Transition(s1, e, s2));
                    }
                }

                transitions.UnionWith(MarkedStates.Select(sm => new Transition(sm, Epsilon.EpsilonEvent, InitialState)));

                return Determinize(transitions, InitialState, string.Format("KleeneClosure({0})", Name));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the minimal. </summary>
        /// <value> The minimal. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA Minimal
        {
            get
            {
                var g1 = new HashSet<AbstractState>(MarkedStates);
                var g2 = new HashSet<AbstractState>(States.Except(g1));

                var partitions = new List<HashSet<AbstractState>> {g1, g2};

                var size = 0;

                while (partitions.Count > size)
                {
                    size = partitions.Count;
                    var newPartitions = new List<HashSet<AbstractState>>();

                    foreach (var partition in partitions.ToArray())
                    {
                        if (partition.Count <= 1)
                        {
                            newPartitions.Add(partition);
                            continue;
                        }

                        var groups = partition.GroupBy(s =>
                            new HashSet<HashSet<AbstractState>>(
                                Transitions.Where(t => t.Origin == s)
                                    .Select(ns => partitions.First(p => p.Contains(ns.Destination)))),
                            HashSet<HashSet<AbstractState>>.CreateSetComparer());

                        newPartitions.AddRange(groups.Select(g => new HashSet<AbstractState>(g)));
                    }
                    partitions = newPartitions;
                }

                var mapping = partitions.Select(
                    p => Tuple.Create(p, p.Count == 1 ? p.Single() : p.Aggregate((a, b) => a.MergeWith(b, 1))))
                    .ToList();

                var transitions = Transitions.Select(t =>
                {
                    var s1 = mapping.Single(m => m.Item1.Contains(t.Origin)).Item2;
                    var s2 = mapping.Single(m => m.Item1.Contains(t.Destination)).Item2;

                    return new Transition(s1, t.Trigger, s2);
                }).Distinct();

                return new DFA(transitions,
                    mapping.Single(m => m.Item1.Contains(InitialState)).Item2, string.Format("Min({0})", Name));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to dot code. </summary>
        /// <value> to dot code. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string ToDotCode
        {
            get
            {
                var dot = new StringBuilder("digraph {\nrankdir=TB;");

                dot.Append("\nnode [shape = doublecircle];");

                foreach (var ms in MarkedStates)
                    dot.AppendFormat(" \"{0}\" ", ms);

                dot.Append("\nnode [shape = circle];");

                foreach (var s in States.Except(MarkedStates))
                    dot.AppendFormat(" \"{0}\" ", s);

                dot.AppendFormat("\nnode [shape = point ]; Initial\nInitial -> \"{0}\";\n", InitialState);

                foreach (
                    var group in Transitions.GroupBy(t => new {t.Origin, t.Destination}))
                {
                    dot.AppendFormat("\"{0}\" -> \"{1}\" [ label = \"{2}\" ];\n", group.Key.Origin,
                        group.Key.Destination,
                        group.Aggregate("", (acc, t) => string.Format("{0}{1},", acc, t.Trigger))
                            .Trim(' ', ','));
                }

                dot.Append("}");

                return dot.ToString();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to regular expression. </summary>
        /// <value> to regular expression. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public RegularExpression ToRegularExpression
        {
            get
            {
                var t = Enumerable.Range(0, _states.Length).ToArray();
                var aux = t[0];
                t[0] = _initial;
                t[_initial] = aux;


                var tf = _adjacency;
                var size = _states.Length;
                var b = new RegularExpression[size];
                var a = new RegularExpression[size, size];

                for (var i = 0; i < size; i++)
                    b[i] = _states[t[i]].IsMarked ? Symbol.Epsilon : Symbol.Empty;

                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < size; j++)
                    {
                        if (a[i, j] == null) a[i, j] = Symbol.Empty;
                        for (var k = 0; k < _events.Length; k++)
                        {
                            if (tf[t[i], k] == t[j]) a[i, j] += _events[k];
                        }
                    }
                }

                for (var n = size - 1; n >= 0; n--)
                {
                    b[n] = new KleeneStar(a[n, n])*b[n];
                    for (var j = 0; j <= n; j++)
                    {
                        a[n, j] = new KleeneStar(a[n, n])*a[n, j];
                    }
                    for (var i = 0; i <= n; i++)
                    {
                        b[i] += a[i, n]*b[n];
                        for (var j = 0; j <= n; j++)
                        {
                            a[i, j] += a[i, n]*a[n, j];
                        }
                    }
                }

                return b[0].Simplify;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the states. </summary>
        /// <value> The states. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public IEnumerable<AbstractState> States
        {
            get { return new List<AbstractState>(_states); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets a list of states of the marked. </summary>
        /// <value> The marked states. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public IEnumerable<AbstractState> MarkedStates
        {
            get { return _states.AsParallel().Where(s => s.IsMarked); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the events. </summary>
        /// <value> The events. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public IEnumerable<AbstractEvent> Events
        {
            get { return new List<AbstractEvent>(_events); }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the state of the initial. </summary>
        /// <value> The initial state. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public AbstractState InitialState
        {
            get { return _states[_initial]; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the name. </summary>
        /// <value> The name. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string Name { get; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the transition function. </summary>
        /// <value> The transition function. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public Func<AbstractState, AbstractEvent, Option<AbstractState>> TransitionFunction
        {
            get
            {
                var si = _states.Select((s, i) => new {ss = s, ii = i}).ToDictionary(o => o.ss, o => o.ii);

                return (s, e) =>
                {
                    if (e == Epsilon.EpsilonEvent) return Some.Create(s);
                    var i = si[s];
                    var k = Array.IndexOf(_events, e);

                    return _adjacency[i].ContainsKey(k) ? Some.Create(_states[_adjacency[i][k]]) : None.Create();
                };
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the inverse transition function. </summary>
        /// <value> The inverse transition function. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public Func<AbstractState, AbstractEvent, AbstractState[]> InverseTransitionFunction
        {
            get
            {
                return (s, e) =>
                {
                    if (e == Epsilon.EpsilonEvent) return new[] {s};
                    var i = Array.IndexOf(_states, s);
                    var k = Array.IndexOf(_events, e);

                    return
                        Enumerable.Range(0, _adjacency.Length).AsParallel()
                            .Where(key => _adjacency[key].ContainsKey(k) && _adjacency[key][k] == i)
                            .Select(key => _states[key])
                            .ToArray();
                };
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets the transitions. </summary>
        /// <value> The transitions. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public IEnumerable<Transition> Transitions
        {
            get
            {
                for (var i = 0; i < _adjacency.Length; i++)
                {
                    var s1 = _states[i];
                    foreach (var kvp in _adjacency[i])
                    {
                        var e = _events[kvp.Key];
                        var s2 = _states[kvp.Value];

                        yield return new Transition(s1, e, s2);
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets to XML. </summary>
        /// <value> to XML. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public string ToXML
        {
            get
            {
                var doc = new XmlDocument();
                var automaton = (XmlElement) doc.AppendChild(doc.CreateElement("Automaton"));
                automaton.SetAttribute("Name", Name);

                var states = (XmlElement) automaton.AppendChild(doc.CreateElement("States"));
                for (var i = 0; i < _states.Length; i++)
                {
                    var state = _states[i];

                    var s = (XmlElement) states.AppendChild(doc.CreateElement("State"));
                    s.SetAttribute("Name", state.ToString());
                    s.SetAttribute("Marking", state.Marking.ToString());
                    s.SetAttribute("Id", i.ToString());
                }

                var initial = (XmlElement) automaton.AppendChild(doc.CreateElement("InitialState"));
                initial.SetAttribute("Id", _initial.ToString());

                var events = (XmlElement) automaton.AppendChild(doc.CreateElement("Events"));
                for (var i = 0; i < _events.Length; i++)
                {
                    var @event = _events[i];

                    var e = (XmlElement) events.AppendChild(doc.CreateElement("Event"));
                    e.SetAttribute("Name", @event.ToString());
                    e.SetAttribute("Controllability", @event.Controllability.ToString());
                    e.SetAttribute("Id", i.ToString());
                }

                var transitions = (XmlElement) automaton.AppendChild(doc.CreateElement("Transitions"));
                for (var i = 0; i < _states.Length; i++)
                {
                    for (var j = 0; j < _events.Length; j++)
                    {
                        var k = _adjacency[i].ContainsKey(j) ? _adjacency[i][j] : -1;
                        if (k == -1) continue;

                        var t = (XmlElement) transitions.AppendChild(doc.CreateElement("Transition"));

                        t.SetAttribute("Origin", i.ToString());
                        t.SetAttribute("Trigger", j.ToString());
                        t.SetAttribute("Destination", k.ToString());
                    }
                }

                return doc.OuterXml;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Depth first search. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="initial">  The initial. </param>
        /// <param name="visited">  The visited. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void DepthFirstSearch(int initial, BitArray visited)
        {
            const int parallelThreshold = 100;

            if (visited.Length < parallelThreshold)
            {
                var s = new Stack<int>();
                s.Push(initial);

                while (s.Count != 0)
                {
                    var v = s.Pop();
                    if (visited[v]) continue;

                    visited[v] = true;

                    var neighbors = _adjacency[v].Values.Distinct().ToArray();

                    foreach (var destination in neighbors)
                        s.Push(destination);
                }
            }
            else
            {
                var frontier = new List<int> {initial};

                while (frontier.Count != 0)
                {
                    frontier.ForEach(st => visited[st] = true);

                    if (frontier.Count > parallelThreshold/2)
                        frontier = frontier.AsParallel()
                            .SelectMany(v => _adjacency[v].Values)
                            .Distinct()
                            .Where(v => !visited[v]).ToList();
                    else
                        frontier = frontier
                            .SelectMany(v => _adjacency[v].Values)
                            .Distinct()
                            .Where(v => !visited[v]).ToList();
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Inverse depth first search. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="initial">  The initial. </param>
        /// <param name="states">   [out] The states. </param>
        /// <param name="visited">  The visited. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void InverseDepthFirstSearch(int initial, BitArray states, BitArray visited)
        {
            const int parallelThreshold = 100;

            var inverseAdjacency = Enumerable.Range(0, visited.Length).AsParallel().Where(s1 => states[s1])
                .SelectMany(s1 => _adjacency[s1].Where(s2 => states[s2.Value]).Select(s2 => Tuple.Create(s1, s2.Value)))
                .GroupBy(t => t.Item2)
                .ToDictionary(g => g.Key, g => g.Select(t => t.Item1).ToArray());

            var frontier = new List<int> {initial};

            while (frontier.Count != 0)
            {
                frontier.ForEach(st => visited[st] = true);

                if (frontier.Count > parallelThreshold/2)
                    frontier = frontier.AsParallel()
                        .SelectMany(v => inverseAdjacency[v])
                        .Distinct()
                        .Where(v => !visited[v]).ToList();
                else
                    frontier = frontier
                        .SelectMany(v => inverseAdjacency[v])
                        .Distinct()
                        .Where(v => !visited[v]).ToList();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Inverse depth first search. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="initial">  The initial. </param>
        /// <param name="visited">  The visited. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void InverseDepthFirstSearch(int initial, BitArray visited)
        {
            var s = new Stack<int>();
            s.Push(initial);

            while (s.Count != 0)
            {
                var v = s.Pop();
                if (visited[v]) continue;

                visited[v] = true;

                var neighbors =
                    Enumerable.Range(0, _adjacency.Length)
                        .AsParallel()
                        .Where(s1 => _adjacency[s1].Values.Contains(v))
                        .ToArray();

                foreach (var dest in neighbors)
                    s.Push(dest);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Removes the states. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        /// <param name="visited">  The visited. </param>
        /// <param name="G">        The DFA to process. </param>
        /// <param name="states">   [out] The states. </param>
        /// <returns>   An AdjacencyMatrix. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static AdjacencyMatrix RemoveStates(BitArray visited, DFA G,
            out AbstractState[] states)
        {
            const int parallelThreshold = 1000;

            var map2New = new Dictionary<int, int>(G._states.Length/10);
            for (int i = 0, k = 0; i < G._states.Length; i++)
                if (visited[i]) map2New.Add(i, k++);

            var adjacency = new AdjacencyMatrix(Enumerable.Range(0, visited.Count).Count(i => visited[i]));

            if (G._states.Length > parallelThreshold)
            {
                Parallel.For(0, G._states.Length, s =>
                {
                    if (!map2New.ContainsKey(s)) return;
                    var i = map2New[s];
                    for (var e = 0; e < G._events.Length; e++)
                    {
                        var k = G._adjacency[s].ContainsKey(e) ? G._adjacency[s][e] : -1;

                        if (k == -1 || !map2New.ContainsKey(k)) continue;

                        if (!adjacency[i].ContainsKey(e)) adjacency[i].Add(e, map2New[k]);
                        else throw new Exception("Nondeterministic automaton");
                    }
                });
            }
            else
            {
                for (var s = 0; s < G._states.Length; s++)
                {
                    if (!map2New.ContainsKey(s)) continue;
                    var i = map2New[s];
                    for (var e = 0; e < G._events.Length; e++)
                    {
                        var k = G._adjacency[s].ContainsKey(e) ? G._adjacency[s][e] : -1;

                        if (k == -1 || !map2New.ContainsKey(k)) continue;

                        if (!adjacency[i].ContainsKey(e)) adjacency[i].Add(e, map2New[k]);
                        else throw new Exception("Nondeterministic automaton");
                    }
                }
            }

            states = G._states.Where((s, i) => visited[i]).ToArray();

            adjacency.TrimExcess();

            return adjacency;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Projections the given remove events. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="removeEvents"> The events to be removed. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA Projection(IEnumerable<Event> removeEvents)
        {
            var evs = new HashSet<Event>(removeEvents);

            var transitions =
                Transitions.Select(
                    t => !evs.Contains(t.Trigger) ? t : new Transition(t.Origin, Epsilon.EpsilonEvent, t.Destination));

            return Determinize(transitions, InitialState, string.Format("Projection({0})", Name));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Inverse projection. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="events">   The events. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA InverseProjection(IEnumerable<Event> events)
        {
            var evs = events.Except(Events).ToList();

            var transitions = Transitions as HashSet<Transition> ?? new HashSet<Transition>();

            transitions.UnionWith(States.SelectMany(s => evs.Select(e => new Transition(s, e, s))));

            return Determinize(transitions, InitialState, string.Format("InvProjection({0})", Name));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Determinizes. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="transitions">  The transitions. </param>
        /// <param name="initial">      The initial. </param>
        /// <param name="name">         The name. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static DFA Determinize(IEnumerable<Transition> transitions,
            AbstractState initial, string name)
        {
            var visited = new HashSet<AbstractState>();
            var newTransitions = new List<Transition>();
            var localTransitions = transitions as Transition[] ?? transitions.ToArray();
            var events = localTransitions.Select(t => t.Trigger).Distinct().ToArray();
            var initialSet = EpsilonJumps(localTransitions, initial);
            var frontier = new HashSet<HashSet<AbstractState>>(HashSet<AbstractState>.CreateSetComparer()) {initialSet};

            while (frontier.Count > 0)
            {
                var newFrontier = new HashSet<HashSet<AbstractState>>(HashSet<AbstractState>.CreateSetComparer());

                foreach (var states in frontier)
                {
                    var origin = states.Count == 1
                        ? states.Single()
                        : states.OrderBy(s => s.ToString())
                            .ThenBy(s => s.Marking)
                            .Aggregate((a, b) => a.MergeWith(b, 0, false));
                    visited.Add(origin);
                    foreach (var e in events)
                    {
                        if (e == Epsilon.EpsilonEvent || e == Empty.EmptyEvent) continue;

                        var destinationSet = new HashSet<AbstractState>();

                        foreach (var s in states)
                        {
                            destinationSet.UnionWith(localTransitions.Where(t => t.Origin == s && t.Trigger == e)
                                .Select(t => t.Destination)
                                .SelectMany(s2 => EpsilonJumps(localTransitions, s2)));
                        }

                        if (destinationSet.Count == 0) continue;

                        var destination = destinationSet.Count == 1
                            ? destinationSet.Single()
                            : destinationSet.OrderBy(s => s.ToString())
                                .ThenBy(s => s.Marking)
                                .Aggregate((a, b) => a.MergeWith(b, 0, false));

                        if (!visited.Contains(destination)) newFrontier.Add(destinationSet);

                        newTransitions.Add(new Transition(origin, e, destination));
                    }
                }

                frontier = newFrontier;
            }


            var newInitial = initialSet.Count == 1
                ? initialSet.Single()
                : initialSet.OrderBy(s => s.ToString())
                    .ThenBy(s => s.Marking)
                    .Aggregate((a, b) => a.MergeWith(b, 0, false));

            return new DFA(newTransitions, newInitial, string.Format("Det({0})", name));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Epsilon jumps. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="transitions">  The transitions. </param>
        /// <param name="initial">      The initial. </param>
        /// <returns>   A HashSet&lt;AbstractState&gt; </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static HashSet<AbstractState> EpsilonJumps(Transition[] transitions, AbstractState initial)
        {
            var accessible = new HashSet<AbstractState>();
            var frontier = new HashSet<AbstractState> {initial};

            while (frontier.Count > 0)
            {
                var newFrontier = new HashSet<AbstractState>();
                foreach (var s in frontier)
                {
                    accessible.Add(s);
                    newFrontier.UnionWith(
                        transitions.Where(
                            t =>
                                t.Origin == s && t.Trigger == Epsilon.EpsilonEvent &&
                                !accessible.Contains(t.Destination)).Select(t => t.Destination));
                }
                frontier = newFrontier;
            }

            return accessible;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Parallel composition with. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        /// <param name="G2">   The second DFA. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA ParallelCompositionWith(DFA G2)
        {
            const int parallelThreshold = 1000;
            var G1 = this;

            var events = G1._events.Union(G2._events).ToArray();
            var states =
                G1._states.AsParallel()
                    .AsOrdered()
                    .SelectMany(
                        s1 => G2._states
                            .AsParallel()
                            .AsOrdered()
                            .Select(s2 => s1.MergeWith(s2, G2._states.Length))).ToArray();

            var events2G1 = events.AsParallel().AsOrdered().Select(e => Array.IndexOf(G1._events, e)).ToArray();
            var events2G2 = events.AsParallel().AsOrdered().Select(e => Array.IndexOf(G2._events, e)).ToArray();

            var adjacency = new AdjacencyMatrix(states.Length);

            if (G1._states.Length > parallelThreshold)
            {
                Parallel.For(0, G1._states.Length, s1 =>
                {
                    Parallel.For(0, G2._states.Length, s2 =>
                    {
                        for (var e = 0; e < events.Length; e++)
                        {
                            var origin = s1*G2._states.Length + s2;

                            var dest1 = events2G1[e] == -1
                                ? s1
                                : G1._adjacency[s1].ContainsKey(events2G1[e])
                                    ? G1._adjacency[s1][events2G1[e]]
                                    : -1;

                            var dest2 = events2G2[e] == -1
                                ? s2
                                : G2._adjacency[s2].ContainsKey(events2G2[e])
                                    ? G2._adjacency[s2][events2G2[e]]
                                    : -1;

                            if (dest1 == -1 || dest2 == -1) continue;

                            var destination = dest1*G2._states.Length + dest2;

                            if (!adjacency[origin].ContainsKey(e)) adjacency[origin].Add(e, destination);
                            else throw new Exception("Nondeterministic automaton");
                        }
                    });
                });
            }
            else
            {
                for (var s1 = 0; s1 < G1._states.Length; s1++)
                {
                    for (var s2 = 0; s2 < G2._states.Length; s2++)
                    {
                        for (var e = 0; e < events.Length; e++)
                        {
                            var origin = s1*G2._states.Length + s2;

                            var dest1 = events2G1[e] == -1
                                ? s1
                                : G1._adjacency[s1].ContainsKey(events2G1[e])
                                    ? G1._adjacency[s1][events2G1[e]]
                                    : -1;

                            var dest2 = events2G2[e] == -1
                                ? s2
                                : G2._adjacency[s2].ContainsKey(events2G2[e])
                                    ? G2._adjacency[s2][events2G2[e]]
                                    : -1;

                            if (dest1 == -1 || dest2 == -1) continue;

                            var destination = dest1*G2._states.Length + dest2;

                            if (!adjacency[origin].ContainsKey(e)) adjacency[origin].Add(e, destination);
                            else throw new Exception("Nondeterministic automaton");
                        }
                    }
                }
            }

            var initial = G1._initial*G2._states.Length + G2._initial;

            adjacency.TrimExcess();

            return new DFA(states, events, adjacency, initial,
                string.Format("{0}||{1}", G1.Name, G2.Name)).AccessiblePart;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Product with. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        /// <param name="G2">   The second DFA. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public DFA ProductWith(DFA G2)
        {
            var G1 = this;

            var events = G1._events.Intersect(G2._events).ToArray();
            var states =
                G1._states.AsParallel()
                    .AsOrdered()
                    .SelectMany(
                        s1 => G2._states
                            .AsParallel()
                            .AsOrdered()
                            .Select(s2 => s1.MergeWith(s2, G2._states.Length)))
                    .ToArray();

            var events2G1 = events.AsParallel().AsOrdered().Select(e => Array.IndexOf(G1._events, e)).ToArray();
            var events2G2 = events.AsParallel().AsOrdered().Select(e => Array.IndexOf(G2._events, e)).ToArray();

            var adjacency = new AdjacencyMatrix(states.Length);

            for (var s1 = 0; s1 < G1._states.Length; s1++)
            {
                for (var s2 = 0; s2 < G2._states.Length; s2++)
                {
                    for (var e = 0; e < events.Length; e++)
                    {
                        var origin = s1*G2._states.Length + s2;

                        var dest1 = G1._adjacency[s1].ContainsKey(events2G1[e])
                            ? G1._adjacency[s1][events2G1[e]]
                            : -1;

                        var dest2 = G2._adjacency[s2].ContainsKey(events2G2[e])
                            ? G2._adjacency[s2][events2G2[e]]
                            : -1;

                        if (dest1 == -1 || dest2 == -1) continue;

                        var destination = dest1*G2._states.Length + dest2;

                        if (!adjacency[origin].ContainsKey(e)) adjacency[origin].Add(e, destination);
                        else throw new Exception("Nondeterministic automaton");
                    }
                }
            }

            var initial = G1._initial*G2._states.Length + G2._initial;

            adjacency.TrimExcess();

            return new DFA(states, events, adjacency, initial,
                string.Format("{0}||{1}", G1.Name, G2.Name)).AccessiblePart;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Monolitic supervisor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="plants">           The plants. </param>
        /// <param name="specifications">   The specifications. </param>
        /// <param name="nonBlocking">      true to non blocking. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        [Obsolete("MonoliticSupervisor is deprecated, please use MonolithicSupervisor instead. (Misspelling)")]
        public static DFA MonoliticSupervisor(IEnumerable<DFA> plants,
            IEnumerable<DFA> specifications, bool nonBlocking = false)
        {
            return MonolithicSupervisor(plants, specifications, nonBlocking);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Monolithic supervisor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="plants">           The plants. </param>
        /// <param name="specifications">   The specifications. </param>
        /// <param name="nonBlocking">      true to non blocking. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static DFA MonolithicSupervisor(IEnumerable<DFA> plants,
            IEnumerable<DFA> specifications, bool nonBlocking = false)
        {
            var plantTask =
                Task.Factory.StartNew(
                    () => plants.AsParallel().Aggregate((a, b) => a.ParallelCompositionWith(b)));
            var specificationTask =
                Task.Factory.StartNew(
                    () => specifications.AsParallel().Aggregate((a, b) => a.ParallelCompositionWith(b)));


            var plant = plantTask.Result;
            var specification = specificationTask.Result;


            GC.Collect();
            GC.Collect();
            GC.WaitForFullGCComplete();

            var result = plant.ParallelCompositionWith(specification);

            GC.Collect();
            GC.Collect();
            GC.WaitForFullGCComplete();

            var allowed = new BitArray(result._states.Length, true);

            var result2Plant =
                result._states.AsParallel()
                    .AsOrdered()
                    .OfType<AbstractCompoundState>()
                    .Select(sr => Array.IndexOf(plant._states, sr.S1)).ToArray();

            var change = true;

            var marked = result._states.Select((s, i) => s.IsMarked ? i : -1)
                .Where(s => s != -1)
                .ToList();

            while (change)
            {
                change = VerifyControlabillity(result, plant, result2Plant, allowed);
                if (nonBlocking)
                    change |= VerifyNonblocking(result, marked, allowed);
            }

            AbstractState[] states;

            var adjacency = RemoveStates(allowed, result, out states);

            var initial = Array.IndexOf(states, result._states[result._initial]);

            return
                new DFA(states, result._events, adjacency, initial,
                    string.Format("Sup({0})", result.Name));
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Verify nonblocking. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="result">   The result. </param>
        /// <param name="marked">   The marked. </param>
        /// <param name="allowed">  The allowed. </param>
        /// <returns>   true if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static bool VerifyNonblocking(DFA result, List<int> marked, BitArray allowed)
        {
            var visited = new BitArray(result._states.Length, false);
            marked.ForEach(s => result.InverseDepthFirstSearch(s, allowed, visited));
            allowed.And(visited);

            return Enumerable.Range(0, visited.Length).Any(i => allowed[i] && !visited[i]);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Verify controlabillity. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="result">       The result. </param>
        /// <param name="plant">        The plant. </param>
        /// <param name="result2Plant"> The result 2 plant. </param>
        /// <param name="allowed">      The allowed. </param>
        /// <returns>   true if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static bool VerifyControlabillity(DFA result, DFA plant, IReadOnlyList<int> result2Plant, BitArray allowed)
        {
            var change = false;

            for (var s1 = 0; s1 < result._states.Length; s1++)
            {
                if (!allowed[s1]) continue;

                for (var e = 0; e < result._events.Length; e++)
                {
                    if (result._events[e].IsControllable) continue;

                    if (!result._adjacency[s1].ContainsKey(e) && plant._adjacency[result2Plant[s1]].ContainsKey(e))
                    {
                        allowed[s1] = false;
                        change = true;
                        continue;
                    }

                    if (result._adjacency[s1].ContainsKey(e) && !allowed[result._adjacency[s1][e]])
                    {
                        allowed[s1] = false;
                        change = true;
                    }
                }
            }

            return change;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Enumerates local modular supervisor in this collection. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <exception cref="Exception">    Thrown when there is conflict. </exception>
        /// <param name="plants">                       The plants. </param>
        /// <param name="specifications">               The specifications. </param>
        /// <param name="conflictResolvingSupervisor">  The conflict resolving supervisor. </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process local modular supervisor in this
        ///     collection.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            if (conflictResolvingSupervisor == null) conflictResolvingSupervisor = new DFA[0];

            var dic = specifications.ToDictionary(
                e => { return plants.Where(p => p._events.Intersect(e._events).Any()).ToArray(); });

            var supervisors =
                dic.AsParallel()
                    //.WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(automata => MonolithicSupervisor(automata.Key, new[] {automata.Value}))
                    .ToList();

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete))
            {
                throw new Exception("conflicting supervisors");
            }

            return complete;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Enumerates local modular supervisor in this collection. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <exception cref="Exception">    Thrown when an exception when there is conflict. </exception>
        /// <param name="plants">                       The plants. </param>
        /// <param name="specifications">               The specifications. </param>
        /// <param name="compoundPlants">               [out] The compound plants. </param>
        /// <param name="conflictResolvingSupervisor">  The conflict resolving supervisor. </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process local modular supervisor in this
        ///     collection.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static IEnumerable<DFA> LocalModularSupervisor(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, out List<DFA> compoundPlants,
            IEnumerable<DFA> conflictResolvingSupervisor = null)
        {
            if (conflictResolvingSupervisor == null) conflictResolvingSupervisor = new DFA[0];

            var dic = specifications.ToDictionary(
                    e =>
                    {
                        return plants.Where(p => p._events.Intersect(e._events).Any()).Aggregate((a, b) => a.ParallelCompositionWith(b))
                                .CoaccessiblePart;
                    });

            var supervisors =
                dic.AsParallel()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(automata => MonolithicSupervisor(new[] {automata.Key}, new[] {automata.Value}, true))
                    .ToList();

            var complete = supervisors.Union(conflictResolvingSupervisor).ToList();

            if (IsConflicting(complete))
            {
                throw new Exception("conflicting supervisors");
            }

            compoundPlants = dic.Keys.ToList();

            return complete;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Enumerates local modular reduced supervisor in this collection. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        /// <param name="plants">                       The plants. </param>
        /// <param name="specifications">               The specifications. </param>
        /// <param name="conflictResolvingSupervisor">  The conflict resolving supervisor. </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process local modular reduced supervisor
        ///     in this collection.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static IEnumerable<Tuple<DFA, DesablingStructure>> LocalModularReducedSupervisor
            (
            IEnumerable<DFA> plants,
            IEnumerable<DFA> specifications,
            IEnumerable<Tuple<IEnumerable<DFA>,
                IEnumerable<DFA>>> conflictResolvingSupervisor = null)
        {
            if (conflictResolvingSupervisor == null)
                conflictResolvingSupervisor = new List<Tuple<IEnumerable<DFA>,
                    IEnumerable<DFA>>>();

            var dic = specifications.ToDictionary(e => plants.Where(p => p._events.Intersect(e._events).Any()).ToArray());

            var supervisors =
                dic.AsParallel()
                    .AsOrdered()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(automata => MonolithicSupervisor(automata.Key, new[] {automata.Value}, true))
                    .ToList();

            var ss = supervisors.ToList();
            ss.AddRange(conflictResolvingSupervisor.Select(crs => MonolithicSupervisor(crs.Item1, crs.Item2, true)));


            if (IsConflicting(ss))
            {
                throw new Exception("conflicting supervisors");
            }

            var pp = dic.Select(m => m.Key.Aggregate((a, b) => a.ParallelCompositionWith(b)).CoaccessiblePart).ToList();
            pp.AddRange(
                conflictResolvingSupervisor.Select(
                    crs => crs.Item1.Aggregate((a, b) => a.ParallelCompositionWith(b)).CoaccessiblePart));
            var ee = dic.Select(m => m.Value).ToList();
            ee.AddRange(
                conflictResolvingSupervisor.Select(
                    crs => crs.Item2.Aggregate((a, b) => a.ParallelCompositionWith(b)).CoaccessiblePart));

            return pp.AsParallel().AsOrdered().Select((t, i) => ReduceSupervisor(t, ss[i], ee[i]._events)).ToList();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Reduce supervisor. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="plant">        The plant. </param>
        /// <param name="supervisor">   The supervisor. </param>
        /// <param name="events">       The events. </param>
        /// <returns>   A Tuple&lt;DFA,DesablingStructure&gt; </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static Tuple<DFA, DesablingStructure> ReduceSupervisor(DFA plant, DFA supervisor,
            IEnumerable<AbstractEvent> events)
        {
            var states = supervisor._states;

            var E = new Dictionary<AbstractState, IEnumerable<AbstractEvent>>();
            var D = new Dictionary<AbstractState, IEnumerable<AbstractEvent>>();

            foreach (var s in supervisor.States.OfType<AbstractCompoundState>())
            {
                E.Add(s, supervisor.Transitions.Where(t => t.Origin == s).Select(t => t.Trigger).Distinct().ToArray());
                D.Add(s,
                    plant.Transitions.Where(t => t.Origin == s.S1)
                        .Select(t => t.Trigger)
                        .Distinct()
                        .Except(supervisor.Transitions.Where(t => t.Origin == s).Select(t => t.Trigger).Distinct())
                        .ToArray());
            }

            var R = new Dictionary<Tuple<AbstractState, AbstractState>, List<Tuple<AbstractState, AbstractState>>>();

            for (var i = 0; i < states.Length - 1; i++)
            {
                for (var j = i + 1; j < states.Length; j++)
                {
                    var x1 = states[i];
                    var x2 = states[j];
                    var c1 = !E[x1].Intersect(D[x2]).Any() && !E[x2].Intersect(D[x1]).Any();
                    var c2 = x1.Marking == x2.Marking;

                    if (!c1 || !c2)
                    {
                        R.Add(Tuple.Create(x1, x2), null);
                        continue;
                    }

                    var shared = E[x1].Intersect(E[x2]).ToArray();

                    R.Add(Tuple.Create(x1, x2), new List<Tuple<AbstractState, AbstractState>>());

                    if (!shared.Any()) continue;

                    foreach (var e in shared)
                    {
                        var d1 = supervisor.TransitionFunction(x1, e).Value;
                        var d2 = supervisor.TransitionFunction(x2, e).Value;

                        if (d1 == d2)
                        {
                            R[Tuple.Create(x1, x2)] = null;
                            continue;
                        }

                        R[Tuple.Create(x1, x2)].Add(Tuple.Create(d1, d2));
                    }
                }
            }

            var change = true;

            while (change)
            {
                change = false;

                foreach (
                    var kvp in
                        new Dictionary<Tuple<AbstractState, AbstractState>, List<Tuple<AbstractState, AbstractState>>>(R)
                    )
                {
                    if (kvp.Value == null || !kvp.Value.Any()) continue;

                    var count = 0;
                    foreach (var xy in kvp.Value)
                    {
                        if (R.ContainsKey(xy))
                        {
                            if (R[xy] == null)
                            {
                                R[kvp.Key] = null;
                                change = true;
                                break;
                            }
                            if (R[xy].Count == 0)
                                count++;
                        }
                        else
                        {
                            var yx = Tuple.Create(xy.Item2, xy.Item1);

                            if (R[yx] == null)
                            {
                                R[kvp.Key] = null;
                                change = true;
                                break;
                            }
                            if (R[yx].Count == 0)
                                count++;
                        }
                    }
                    if (count != kvp.Value.Count) continue;

                    change = true;
                    R[kvp.Key] = new List<Tuple<AbstractState, AbstractState>>();
                }
            }

            foreach (var kvp in R.ToList().Where(kvp => (kvp.Value != null && kvp.Value.Any()) || kvp.Value == null))
                R.Remove(kvp.Key);

            var C = new List<HashSet<HashSet<AbstractState>>>
            {
                new HashSet<HashSet<AbstractState>>(states.Select(x => new HashSet<AbstractState> {x}))
            };

            var flag = false;
            var n = 1;

            while (!flag)
            {
                flag = true;
                foreach (var Ci in C[n - 1])
                {
                    foreach (var x1 in states.Where(x1 => Ci.All(x2 =>
                        (R.ContainsKey(Tuple.Create(x1, x2)) && !R[Tuple.Create(x1, x2)].Any()) ||
                        (R.ContainsKey(Tuple.Create(x2, x1)) && !R[Tuple.Create(x2, x1)].Any())
                        )))
                    {
                        if (C.Count < n + 1) C.Add(new HashSet<HashSet<AbstractState>>());

                        var aux = new HashSet<AbstractState>(Ci) {x1};

                        if (C[n].Any(Cj => Cj.SetEquals(aux))) continue;
                        C[n].Add(aux);
                        flag = false;
                    }
                }

                n++;
            }


            var CC = C.SelectMany(c => c).ToList();
            var Cn = new HashSet<HashSet<AbstractState>>();
            var tot = new HashSet<AbstractState>();

            while (tot.Count != states.Length)
            {
                var chosen = CC.Aggregate((a, b) => a.Except(tot).Count() >= b.Except(tot).Count() ? a : b);
                Cn.Add(chosen);
                tot.UnionWith(chosen);
            }

            //foreach (var cc in PowerSet(CC.ToArray(), 1, states.Length))
            //{
            //    if (cc.SelectMany(el => el).Distinct().Count() != states.Length) continue;
            //    Cn = new HashSet<HashSet<AbstractState>>(cc);
            //    break;
            //}


            var st = Cn.ToDictionary(o => o, o => o.Aggregate((a, b) => a.MergeWith(b).ToMarked));

            var disabled =
                Cn.SelectMany(X => X.Aggregate(new AbstractEvent[0], (a, b) => a.Union(D[b]).ToArray()))
                    .Distinct()
                    .ToList();

            var transitions = (from x1 in Cn
                let ev = x1.Aggregate(new List<AbstractEvent>(), (a, b) => a.Union(E[b]).ToList())
                from e in ev.Intersect(events.Union(disabled))
                let dest =
                    x1.SelectMany(x => supervisor.Transitions.Where(t => t.Origin == x && t.Trigger == e)
                        .Select(t => t.Destination)).Distinct().ToList()
                let X2 = Cn.SingleOrDefault(X1 => dest.TrueForAll(X1.Contains))
                where X2 != null
                select new Transition(st[x1], e, st[X2])).ToList();

            var initial = st[Cn.First(X => X.Contains(supervisor.InitialState))];

            var supred = new DFA(transitions, initial,
                string.Format("SupRed({0})", supervisor.Name)).Trim;

            var disabling = Cn.ToDictionary(X => st[X],
                X =>
                    new HashSet<AbstractEvent>(X.Aggregate(new AbstractEvent[0], (a, b) => a.Union(D[b]).ToArray())) as
                        ISet<AbstractEvent>);

            return Tuple.Create(supred, disabling);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Enumerates power set in this collection. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="seq">  The sequence. </param>
        /// <param name="min">  The minimum. </param>
        /// <param name="max">  The maximum. </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process power set in this collection.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static IEnumerable<T[]> PowerSet<T>(T[] seq, int min, int max)
        {
            for (var i = min; i <= max; i++)
            {
                foreach (var c in Combinations(i, seq.Length))
                    yield return c.Select(e => seq[e]).ToArray();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Enumerates combinations in this collection. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="m">    The int to process. </param>
        /// <param name="n">    The int to process. </param>
        /// <returns>
        ///     An enumerator that allows foreach to be used to process combinations in this collection.
        /// </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static IEnumerable<int[]> Combinations(int m, int n)
        {
            var result = new int[m];
            var stack = new Stack<int>();
            stack.Push(0);

            while (stack.Count > 0)
            {
                var index = stack.Count - 1;
                var value = stack.Pop();

                while (value < n)
                {
                    result[index++] = value++;
                    stack.Push(value);
                    if (index == m)
                    {
                        yield return result;
                        break;
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Query if 'supervisors' is conflicting. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="supervisors">  The supervisors. </param>
        /// <returns>   true if conflicting, false if not. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static bool IsConflicting(IEnumerable<DFA> supervisors)
        {
            var composition = supervisors.AsParallel().Aggregate((a, b) => a.ParallelCompositionWith(b));

            var marked = composition._states.Select((s, i) => s.IsMarked ? i : -1)
                .Where(s => s != -1)
                .ToList();

            var visited = new BitArray(composition._states.Length, false);
            marked.ForEach(s => composition.InverseDepthFirstSearch(s, visited));

            return visited.OfType<bool>().Any(b => !b);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Returns a string that represents the current object. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <returns>   A string that represents the current object. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public override string ToString()
        {
            return Name;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Converts an automaton to an XML file. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="filepath"> The filepath. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void ToXMLFile(string filepath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                Async = true
            };

            using (var writer = XmlWriter.Create(filepath, settings))
            {
                writer.WriteStartElement("Automaton");
                writer.WriteAttributeString("Name", Name);

                writer.WriteStartElement("States");
                for (var i = 0; i < _states.Length; i++)
                {
                    var state = _states[i];

                    writer.WriteStartElement("State");
                    writer.WriteAttributeString("Name", state.ToString());
                    writer.WriteAttributeString("Marking", state.Marking.ToString());
                    writer.WriteAttributeString("Id", i.ToString());

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                writer.WriteStartElement("InitialState");
                writer.WriteAttributeString("Id", _initial.ToString());

                writer.WriteEndElement();

                writer.WriteStartElement("Events");
                for (var i = 0; i < _events.Length; i++)
                {
                    var @event = _events[i];

                    writer.WriteStartElement("Event");
                    writer.WriteAttributeString("Name", @event.ToString());
                    writer.WriteAttributeString("Controllability", @event.Controllability.ToString());
                    writer.WriteAttributeString("Id", i.ToString());

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                writer.WriteStartElement("Transitions");
                for (var i = 0; i < _states.Length; i++)
                {
                    for (var j = 0; j < _events.Length; j++)
                    {
                        var k = _adjacency[i].ContainsKey(j) ? _adjacency[i][j] : -1;
                        if (k == -1) continue;

                        writer.WriteStartElement("Transition");

                        writer.WriteAttributeString("Origin", i.ToString());
                        writer.WriteAttributeString("Trigger", j.ToString());
                        writer.WriteAttributeString("Destination", k.ToString());

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Generates an automaton from XML file. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="filepath">     The filepath. </param>
        /// <param name="stateName">    true to state name. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static DFA FromXMLFile(string filepath, bool stateName = true)
        {
            var xdoc = XDocument.Load(filepath);

            var name = xdoc.Descendants("Automaton").Select(dfa => dfa.Attribute("Name").Value).Single();
            var states = xdoc.Descendants("State")
                .ToDictionary(s => s.Attribute("Id").Value,
                    s =>
                        new State(stateName ? s.Attribute("Name").Value : s.Attribute("Id").Value,
                            s.Attribute("Marking").Value == "Marked" ? Marking.Marked : Marking.Unmarked));

            var events = xdoc.Descendants("Event")
                .ToDictionary(e => e.Attribute("Id").Value,
                    e =>
                        new Event(e.Attribute("Name").Value,
                            e.Attribute("Controllability").Value == "Controllable"
                                ? Controllability.Controllable
                                : Controllability.Uncontrollable));

            var initial = xdoc.Descendants("InitialState").Select(i => states[i.Attribute("Id").Value]).Single();

            var transitions =
                xdoc.Descendants("Transition")
                    .Select(
                        t =>
                            new Transition(states[t.Attribute("Origin").Value], events[t.Attribute("Trigger").Value],
                                states[t.Attribute("Destination").Value]));

            return new DFA(transitions, events.Values.OfType<AbstractEvent>().ToArray(),
                initial,
                name);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Converts this object to the ads file. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="filepath"> The filepath. </param>
        /// <param name="odd">      The odd. </param>
        /// <param name="even">     The even. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void ToAdsFile(string filepath, int odd = 1, int even = 2)
        {
            var events = new Dictionary<AbstractEvent, int>();

            foreach (var e in _events)
            {
                if (!e.IsControllable)
                {
                    events.Add(e, even);
                    even += 2;
                }
                else
                {
                    events.Add(e, odd);
                    odd += 2;
                }
            }

            var file = File.CreateText(filepath);

            file.WriteLine("# UltraDES ADS FILE - LACSED | UFMG\r\n");

            file.WriteLine("{0}\r\n", Name);

            file.WriteLine("State size (State set will be (0,1....,size-1)):");
            file.WriteLine("{0}\r\n", _states.Length);

            file.WriteLine("Marker states:");
            file.WriteLine("{0}\r\n",
                _states.Select((s, i) => new {ss = s, ii = i})
                    .Aggregate("", (a, b) => a + (b.ss.IsMarked ? b.ii.ToString() : "") + " ")
                    .Trim());

            file.WriteLine("Vocal states:\r\n");

            file.WriteLine("Transitions:");

            var map = _states.Select((s, i) => new {ss = s, ii = i}).ToDictionary(o => o.ss, o => o.ii);


            map[_states[0]] = _initial;
            map[_states[_initial]] = 0;

            foreach (var t in Transitions)
            {
                file.WriteLine("{0} {1} {2}", map[t.Origin], events[t.Trigger], map[t.Destination]);
            }

            file.Close();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Converts this object to the ads file. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="filepath"> The filepath. </param>
        /// <param name="eventSet"> Set the event belongs to. </param>
        /// <param name="odd">      The odd. </param>
        /// <param name="even">     The even. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void ToAdsFile(string filepath, AbstractEvent[] eventSet, int odd = 1, int even = 2)
        {
            var events = new Dictionary<AbstractEvent, int>();
            //int odd = 1, even = 2;

            foreach (var e in eventSet)
            {
                if (!e.IsControllable)
                {
                    events.Add(e, even);
                    even += 2;
                }
                else
                {
                    events.Add(e, odd);
                    odd += 2;
                }
            }

            var file = File.CreateText(filepath);

            file.WriteLine("# UltraDES ADS FILE - LACSED | UFMG\r\n");

            file.WriteLine("{0}\r\n", Name);

            file.WriteLine("State size (State set will be (0,1....,size-1)):");
            file.WriteLine("{0}\r\n", _states.Length);

            file.WriteLine("Marker states:");
            file.WriteLine("{0}\r\n",
                _states.Select((s, i) => new {ss = s, ii = i})
                    .Aggregate("", (a, b) => a + (b.ss.IsMarked ? b.ii.ToString() : "") + " ")
                    .Trim());

            file.WriteLine("Vocal states:\r\n");

            file.WriteLine("Transitions:");

            var map = _states.Select((s, i) => new {ss = s, ii = i}).ToDictionary(o => o.ss, o => o.ii);


            map[_states[0]] = _initial;
            map[_states[_initial]] = 0;

            foreach (var t in Transitions)
            {
                file.WriteLine("{0} {1} {2}", map[t.Origin], events[t.Trigger], map[t.Destination]);
            }

            file.Close();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Initializes this object from the given from ads file. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        /// <param name="filepath"> The filepath. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static DFA FromAdsFile(string filepath)
        {
            var file = File.OpenText(filepath);

            var name = NextValidLine(file);

            if (!NextValidLine(file).Contains("State size")) throw new Exception("File is not on ADS Format.");

            var states = int.Parse(NextValidLine(file));

            if (!NextValidLine(file).Contains("Marker states")) throw new Exception("File is not on ADS Format.");

            var marked = string.Empty;

            var line = NextValidLine(file);
            if (!line.Contains("Vocal states"))
            {
                marked = line;
                line = NextValidLine(file);
            }

            AbstractState[] stateSet;

            if (marked == "*")
            {
                stateSet = Enumerable.Range(0, states).Select(i => new State(i.ToString(), Marking.Marked)).ToArray();
            }
            else if (marked == string.Empty)
            {
                stateSet = Enumerable.Range(0, states).Select(i => new State(i.ToString(), Marking.Unmarked)).ToArray();
            }
            else
            {
                var markedSet = marked.Split().Select(int.Parse).ToList();
                stateSet =
                    Enumerable.Range(0, states)
                        .Select(
                            i =>
                                markedSet.Contains(i)
                                    ? new State(i.ToString(), Marking.Marked)
                                    : new State(i.ToString(), Marking.Unmarked))
                        .ToArray();
            }

            if (!NextValidLine(file).Contains("Vocal states")) throw new Exception("File is not on ADS Format.");

            line = NextValidLine(file);
            while (!line.Contains("Transitions")) line = NextValidLine(file);

            var evs = new Dictionary<int, AbstractEvent>();
            var transitions = new List<Transition>();

            while (file.EndOfStream)
            {
                line = NextValidLine(file);
                if (line == string.Empty) continue;

                var trans = line.Split().Select(int.Parse).ToArray();

                if (!evs.ContainsKey(trans[1]))
                {
                    var e = new Event(trans[1].ToString(),
                        trans[1]%2 == 0 ? Controllability.Uncontrollable : Controllability.Controllable);
                    evs.Add(trans[1], e);
                }

                transitions.Add(new Transition(stateSet[trans[0]], evs[trans[1]], stateSet[trans[2]]));
            }

            return new DFA(transitions, stateSet[0], name);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Serialize automaton. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="filepath"> The filepath. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public void SerializeAutomaton(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
            stream.Close();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Deserialize automaton. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="filepath"> The filepath. </param>
        /// <returns>   A DFA. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public static DFA DeserializeAutomaton(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var obj = (DFA) formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Next valid line. </summary>
        /// <remarks>   Lucas Alves, 05/01/2016. </remarks>
        /// <param name="file"> The file. </param>
        /// <returns>   A string. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        private static string NextValidLine(StreamReader file)
        {
            var line = string.Empty;
            while (line == string.Empty && !file.EndOfStream)
            {
                line = file.ReadLine();
                if (line[0] == '#')
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
    }
}