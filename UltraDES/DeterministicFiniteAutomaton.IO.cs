using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Linq;

namespace UltraDES
{
    using DFA = DeterministicFiniteAutomaton;

    partial class DeterministicFiniteAutomaton
    {
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
                    var k = _adjacencyList[0].HasEvent(i, j) ? _adjacencyList[0][i, j] : -1;
                    if (k == -1) continue;

                    var t = (XmlElement) transitions.AppendChild(doc.CreateElement("Transition"));

                    t.SetAttribute("Origin", i.ToString());
                    t.SetAttribute("Trigger", j.ToString());
                    t.SetAttribute("Destination", k.ToString());
                }

                return doc.OuterXml;
            }
        }

        public static DFA DeserializeAutomaton(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var obj = (DFA) formatter.Deserialize(stream);
            stream.Close();
            return obj;
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
                v_stateSet = Enumerable.Range(0, states).Select(i => new State(i.ToString(), Marking.Marked)).ToArray();
            else if (v_marked == string.Empty)
            {
                v_stateSet = Enumerable.Range(0, states).Select(i => new State(i.ToString())).ToArray();
            }
            else
            {
                var markedSet = v_marked.Split().Select(int.Parse).ToList();
                v_stateSet = Enumerable.Range(0, states).Select(i =>
                        markedSet.Contains(i) ? new State(i.ToString(), Marking.Marked) : new State(i.ToString()))
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

                var trans = v_line.Split().Where(txt => txt != string.Empty).Select(int.Parse).ToArray();

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
                    {
                        eventsList.Add(transition[0],
                            new Event(transition[0],
                                transition[2] == "c"
                                    ? UltraDES.Controllability.Controllable
                                    : UltraDES.Controllability.Uncontrollable));
                    }

                    if (!statesList.ContainsKey(transition[1]))
                        throw new Exception("Invalid transition. Destination state not found.");
                    transitionsList.Add(new Transition(statesList[stateInfo[0]], eventsList[transition[0]],
                        statesList[transition[1]]));
                }
            }

            return new DFA(transitionsList, initialState, automatonName);
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

        public static DFA FromXMLFile(string p_FilePath, bool p_StateName = true)
        {
            var v_xdoc = XDocument.Load(p_FilePath);

            var v_name = v_xdoc.Descendants("Automaton").Select(dfa => dfa.Attribute("Name").Value).Single();
            var v_states = v_xdoc.Descendants("State").ToDictionary(s => s.Attribute("Id").Value,
                s => new State(p_StateName ? s.Attribute("Name").Value : s.Attribute("Id").Value,
                    s.Attribute("Marking").Value == "Marked" ? Marking.Marked : Marking.Unmarked));

            var v_events = v_xdoc.Descendants("Event").ToDictionary(e => e.Attribute("Id").Value,
                e => new Event(e.Attribute("Name").Value,
                    e.Attribute("Controllability").Value == "Controllable"
                        ? UltraDES.Controllability.Controllable
                        : UltraDES.Controllability.Uncontrollable));

            var v_initial = v_xdoc.Descendants("InitialState").Select(i => v_states[i.Attribute("Id").Value]).Single();

            var v_transitions = v_xdoc.Descendants("Transition").Select(t =>
                new Transition(v_states[t.Attribute("Origin").Value], v_events[t.Attribute("Trigger").Value],
                    v_states[t.Attribute("Destination").Value]));

            return new DFA(v_transitions, v_initial, v_name);
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

        public void SerializeAutomaton(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
            stream.Close();
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
                {
                    if (g._statesList[0][s].IsMarked)
                    {
                        if (vFirst)
                        {
                            vFirst = false;
                            vMarkedStates = s.ToString();
                        }
                        else
                            vMarkedStates += "\r\n" + s;
                    }
                }

                file.WriteLine("{0}\r\n", vMarkedStates);

                file.WriteLine("Vocal states:\r\n");

                file.WriteLine("Transitions:");

                for (var s = 0; s < g._statesList[0].Length; ++s)
                {
                    foreach (var t in g._adjacencyList[0][s])
                        file.WriteLine("{0,-5} {1,-3} {2,-5}", s, vEventsMaps[t.Key], t.Value);
                }

                file.Close();
            }
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
                writer.Write("{0}\t{1}\t{2}\n", s, s.IsMarked ? 1 : 0, transtions.Length);
                foreach (var t in transtions)
                    writer.Write("{0}\t{1}\t{2}\to\n", t.Trigger, t.Destination, t.Trigger.IsControllable ? "c" : "uc");
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

                        for (var i = 0; i < n; ++i)
                        {
                            if (pos[i] != 0)
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
                    } while (IncrementPosition(pos));
                }
            }

            writer.Close();
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
            var v_states = new Dictionary<StatesTuple, ulong>((int) Size, StatesTupleComparator.GetInstance());
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
                        var nextEvent = false;
                        for (var e = 0; e < n; ++e)
                        {
                            if (_eventsList[e][j])
                            {
                                if (_adjacencyList[e].HasEvent(pos[e], j))
                                    nextPos[e] = _adjacencyList[e][pos[e], j];
                                else
                                {
                                    nextEvent = true;
                                    break;
                                }
                            }
                            else
                                nextPos[e] = pos[e];
                        }

                        if (nextEvent) continue;

                        nextTuple.Set(nextPos, _bits);

                        if (v_states.TryGetValue(nextTuple, out var value))
                        {
                            writer.WriteStartElement("Transition");

                            writer.WriteAttributeString("Origin", state.Value.ToString());
                            writer.WriteAttributeString("Trigger", j.ToString());
                            writer.WriteAttributeString("Destination", value.ToString());

                            writer.WriteEndElement();
                        }
                    }
                }

                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            v_states = null;

            GC.Collect();
            GC.Collect();
        }
    }
}