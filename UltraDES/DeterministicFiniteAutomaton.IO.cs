// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-22-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 05-20-2020
// ***********************************************************************
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

    /// <summary>
    /// Class DeterministicFiniteAutomaton. This class cannot be inherited.
    /// </summary>
    partial class DeterministicFiniteAutomaton
    {
        /// <summary>
        /// Converts to xml.
        /// </summary>
        /// <value>To XML.</value>
        public string ToXML
        {
            get
            {
                Simplify();
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

        /// <summary>
        /// Deserializes the automaton.
        /// </summary>
        /// <param name="filepath">The filepath.</param>
        /// <returns>DFA.</returns>
        public static DFA DeserializeAutomaton(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var obj = (DFA) formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }

        /// <summary>
        /// Froms the ads file.
        /// </summary>
        /// <param name="pFilePath">The p file path.</param>
        /// <returns>DFA.</returns>
        /// <exception cref="Exception">File is not on ADS Format.</exception>
        /// <exception cref="Exception">File is not on ADS Format.</exception>
        /// <exception cref="Exception">File is not on ADS Format.</exception>
        public static DFA FromAdsFile(string pFilePath)
        {
            var vFile = File.OpenText(pFilePath);

            var vName = NextValidLine(vFile);

            if (!NextValidLine(vFile).Contains("State size"))
                throw new Exception("File is not on ADS Format.");

            var states = int.Parse(NextValidLine(vFile));

            if (!NextValidLine(vFile).Contains("Marker states"))
                throw new Exception("File is not on ADS Format.");

            var vMarked = string.Empty;

            var vLine = NextValidLine(vFile);
            if (!vLine.Contains("Vocal states"))
            {
                vMarked = vLine;
                vLine = NextValidLine(vFile);
            }

            AbstractState[] vStateSet;

            if (vMarked == "*")
                vStateSet = Enumerable.Range(0, states).Select(i => new State(i.ToString(), Marking.Marked)).ToArray();
            else if (vMarked == string.Empty)
            {
                vStateSet = Enumerable.Range(0, states).Select(i => new State(i.ToString())).ToArray();
            }
            else
            {
                var markedSet = vMarked.Split().Select(int.Parse).ToList();
                vStateSet = Enumerable.Range(0, states).Select(i =>
                        markedSet.Contains(i) ? new State(i.ToString(), Marking.Marked) : new State(i.ToString()))
                    .ToArray();
            }

            if (!vLine.Contains("Vocal states"))
                throw new Exception("File is not on ADS Format.");

            vLine = NextValidLine(vFile);
            while (!vLine.Contains("Transitions"))
                vLine = NextValidLine(vFile);

            var evs = new Dictionary<int, AbstractEvent>();
            var transitions = new List<Transition>();

            while (!vFile.EndOfStream)
            {
                vLine = NextValidLine(vFile);
                if (vLine == string.Empty) continue;

                var trans = vLine.Split().Where(txt => txt != string.Empty).Select(int.Parse).ToArray();

                if (!evs.ContainsKey(trans[1]))
                {
                    var e = new Event(trans[1].ToString(),
                        trans[1] % 2 == 0
                            ? UltraDES.Controllability.Uncontrollable
                            : UltraDES.Controllability.Controllable);
                    evs.Add(trans[1], e);
                }

                transitions.Add(new Transition(vStateSet[trans[0]], evs[trans[1]], vStateSet[trans[2]]));
            }

            return new DFA(transitions, vStateSet[0], vName);
        }



        /// <summary>
        /// Froms the FSM file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>DFA.</returns>
        /// <exception cref="Exception">Filename can not be null.</exception>
        /// <exception cref="Exception">File not found.</exception>
        /// <exception cref="Exception">Invalid Format.</exception>
        /// <exception cref="Exception">Invalid Format.</exception>
        /// <exception cref="Exception">Invalid Format.</exception>
        /// <exception cref="Exception">Invalid Format: Duplicated state.</exception>
        /// <exception cref="Exception">Invalid Format.</exception>
        /// <exception cref="Exception">Invalid transition. Destination state not found.</exception>
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

        /// <summary>
        /// Froms the wmod file.
        /// </summary>
        /// <param name="pFilename">The p filename.</param>
        /// <param name="pPlants">The p plants.</param>
        /// <param name="pSpecs">The p specs.</param>
        /// <exception cref="Exception">Invalid Format.</exception>
        /// <exception cref="Exception">Invalid Format.</exception>
        /// <exception cref="Exception">Invalid Format.</exception>
        /// <exception cref="Exception">Invalid Format.</exception>
        public static void FromWmodFile(string pFilename, out List<DFA> pPlants, out List<DFA> pSpecs)
        {
            pPlants = new List<DFA>();
            pSpecs = new List<DFA>();
            var vEvents = new List<AbstractEvent>();
            var vEventsMap = new Dictionary<string, AbstractEvent>();

            using (var reader = XmlReader.Create(pFilename, new XmlReaderSettings()))
            {
                if (!reader.ReadToFollowing("EventDeclList")) throw new Exception("Invalid Format.");
                var eventsReader = reader.ReadSubtree();
                while (eventsReader.ReadToFollowing("EventDecl"))
                {
                    eventsReader.MoveToAttribute("Kind");
                    var vKind = eventsReader.Value;
                    if (vKind == "PROPOSITION") continue;
                    eventsReader.MoveToAttribute("Name");
                    var vName = eventsReader.Value;
                    var ev = new Event(vName,
                        vKind == "CONTROLLABLE"
                            ? UltraDES.Controllability.Controllable
                            : UltraDES.Controllability.Uncontrollable);

                    vEvents.Add(ev);
                    vEventsMap.Add(vName, ev);
                }

                if (!reader.ReadToFollowing("ComponentList")) throw new Exception("Invalid Format.");

                var componentsReader = reader.ReadSubtree();

                while (componentsReader.ReadToFollowing("SimpleComponent"))
                {
                    componentsReader.MoveToAttribute("Kind");
                    var vKind = componentsReader.Value;
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
                        var vName = statesReader.Value;
                        var vMarked = false;
                        if (statesReader.MoveToAttribute("Initial") && statesReader.ReadContentAsBoolean())
                            initial = vName;

                        if (evList.ReadToFollowing("EventList"))
                        {
                            evList.ReadToFollowing("SimpleIdentifier");
                            evList.MoveToAttribute("Name");
                            vMarked = evList.Value == ":accepting";
                        }

                        states.Add(vName, new State(vName, vMarked ? Marking.Marked : Marking.Unmarked));
                    }

                    if (!componentsReader.ReadToFollowing("EdgeList")) throw new Exception("Invalid Format.");

                    var edgesReader = componentsReader.ReadSubtree();
                    var transitions = new List<Transition>();
                    while (edgesReader.ReadToFollowing("Edge"))
                    {
                        eventsReader = componentsReader.ReadSubtree();

                        edgesReader.MoveToAttribute("Source");
                        var vSource = edgesReader.Value;
                        edgesReader.MoveToAttribute("Target");
                        var vTarget = edgesReader.Value;

                        while (eventsReader.ReadToFollowing("SimpleIdentifier"))
                        {
                            eventsReader.MoveToAttribute("Name");
                            var vEvent = eventsReader.Value;
                            transitions.Add(new Transition(states[vSource], vEventsMap[vEvent], states[vTarget]));
                        }
                    }

                    var G = new DFA(transitions, states[initial], v_DFAName);
                    if (vKind == "PLANT") pPlants.Add(G);
                    else pSpecs.Add(G);
                }
            }

            GC.Collect();
        }

        /// <summary>
        /// Froms the XML file.
        /// </summary>
        /// <param name="pFilePath">The p file path.</param>
        /// <param name="pStateName">if set to <c>true</c> [p state name].</param>
        /// <returns>DFA.</returns>
        public static DFA FromXMLFile(string pFilePath, bool pStateName = true)
        {
            var vXdoc = XDocument.Load(pFilePath);

            var vName = vXdoc.Descendants("Automaton").Select(dfa => dfa.Attribute("Name").Value).Single();
            var vStates = vXdoc.Descendants("State").ToDictionary(s => s.Attribute("Id").Value,
                s => new State(pStateName ? s.Attribute("Name").Value : s.Attribute("Id").Value,
                    s.Attribute("Marking").Value == "Marked" ? Marking.Marked : Marking.Unmarked));

            var vEvents = vXdoc.Descendants("Event").ToDictionary(e => e.Attribute("Id").Value,
                e => new Event(e.Attribute("Name").Value,
                    e.Attribute("Controllability").Value == "Controllable"
                        ? UltraDES.Controllability.Controllable
                        : UltraDES.Controllability.Uncontrollable));

            var vInitial = vXdoc.Descendants("InitialState").Select(i => vStates[i.Attribute("Id").Value]).Single();

            var vTransitions = vXdoc.Descendants("Transition").Select(t =>
                new Transition(vStates[t.Attribute("Origin").Value], vEvents[t.Attribute("Trigger").Value],
                    vStates[t.Attribute("Destination").Value]));

            return new DFA(vTransitions, vInitial, vName);
        }

        /// <summary>
        /// Nexts the valid line.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>System.String.</returns>
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

        /// <summary>
        /// Serializes the automaton.
        /// </summary>
        /// <param name="filepath">The filepath.</param>
        public void SerializeAutomaton(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
            stream.Close();
        }

        /// <summary>
        /// Converts to adsfile.
        /// </summary>
        /// <param name="filepath">The filepath.</param>
        /// <param name="odd">The odd.</param>
        /// <param name="even">The even.</param>
        public void ToAdsFile(string filepath, int odd = 1, int even = 2) => ToAdsFile(new[] {this}, new[] {filepath}, odd, even);

        /// <summary>
        /// Converts to fm.
        /// </summary>
        /// <param name="filepath">The filepath.</param>
        public void ToFM(string filepath)
        {
            var G = this;
            filepath = filepath.EndsWith(".fm") ? filepath : filepath + ".fm";

            using (var writer = new StreamWriter(filepath, false))
            {
                writer.WriteLine($"(START) |- {G.InitialState}");
                foreach (var t in G.Transitions) writer.WriteLine($"{t.Origin} {t.Trigger} {t.Destination}");
                foreach (var m in G.MarkedStates) writer.WriteLine($"{m} -| (FINAL)");
            }
        }

        /// <summary>
        /// Converts to adsfile.
        /// </summary>
        /// <param name="automata">The automata.</param>
        /// <param name="filepaths">The filepaths.</param>
        /// <param name="odd">The odd.</param>
        /// <param name="even">The even.</param>
        public static void ToAdsFile(IEnumerable<DFA> automata, IEnumerable<string> filepaths, int odd = 1,
            int even = 2)
        {
            foreach (var g in automata) g.Simplify();

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

                file.WriteLine($"{g.Name}\r\n");

                file.WriteLine("State size (State set will be (0,1....,size-1)):");
                file.WriteLine($"{g.Size}\r\n");

                file.WriteLine("Marker states:");
                var vMarkedStates = "";
                var vFirst = true;
                for (var s = 0; s < g._statesList[0].Length; ++s)
                {
                    if (!g._statesList[0][s].IsMarked) continue;
                    if (vFirst)
                    {
                        vFirst = false;
                        vMarkedStates = s.ToString();
                    }
                    else vMarkedStates += "\r\n" + s;
                }

                file.WriteLine($"{vMarkedStates}\r\n");

                file.WriteLine("Vocal states:\r\n");

                file.WriteLine("Transitions:");

                for (var s = 0; s < g._statesList[0].Length; ++s)
                    foreach (var t in g._adjacencyList[0][s])
                        file.WriteLine($"{s,-5} {vEventsMaps[t.Key],-3} {t.Value,-5}");

                file.Close();
            }
        }

        /// <summary>
        /// Converts to fsmfile.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void ToFsmFile(string fileName = null)
        {
            var n = _statesList.Count;
            var pos = new int[n];
            if (fileName == null) fileName = Name;
            fileName = fileName.Replace('|', '_');

            if (!fileName.EndsWith(".fsm")) fileName += ".fsm";

            var writer = new StreamWriter(fileName);

            writer.Write($"{Size}\n\n");

            var writeState = new Action<AbstractState, Transition[]>((s, transitions) =>
            {
                writer.Write($"{s}\t{(s.IsMarked ? 1 : 0)}\t{transitions.Length}\n");
                foreach (var t in transitions)
                    writer.Write($"{t.Trigger}\t{t.Destination}\t{(t.Trigger.IsControllable ? "c" : "uc")}\to\n");
                writer.Write("\n");
            });

            if (!IsEmpty())
            {
                if (_validStates != null)
                {
                    writeState(ComposeState(pos), GetTransitionsFromState(pos).ToArray());
                    foreach (var sT in _validStates)
                    {
                        sT.Key.Get(pos, _bits, _maxSize);

                        for (var i = 0; i < n; ++i)
                        {
                            if (pos[i] == 0) continue;
                            writeState(ComposeState(pos), GetTransitionsFromState(pos).ToArray());
                            break;
                        }
                    }
                }
                else
                {
                    do writeState(ComposeState(pos), GetTransitionsFromState(pos).ToArray());
                    while (IncrementPosition(pos));
                }
            }

            writer.Close();
        }

        /// <summary>
        /// Converts to wmodfile.
        /// </summary>
        /// <param name="pFilename">The p filename.</param>
        /// <param name="pPlants">The p plants.</param>
        /// <param name="pSpecifications">The p specifications.</param>
        /// <param name="pModuleName">Name of the p module.</param>
        public static void ToWmodFile(string pFilename, IEnumerable<DFA> pPlants, IEnumerable<DFA> pSpecifications,
            string pModuleName = "UltraDES")
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n"
            };

            using (var writer = XmlWriter.Create(pFilename, settings))
            {
                writer.WriteStartDocument(true);

                writer.WriteStartElement("Module", "http://waters.sourceforge.net/xsd/module");
                writer.WriteAttributeString("Name", pModuleName);
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

                var vEvents = new List<AbstractEvent>();
                vEvents = pPlants.Aggregate(vEvents, (current, v_plant) => current.Union(v_plant.Events).ToList());
                vEvents = pSpecifications.Aggregate(vEvents, (current, v_spec) => current.Union(v_spec.Events).ToList());

                foreach (var vEvent in vEvents)
                {
                    writer.WriteStartElement("EventDecl");
                    writer.WriteAttributeString("Kind", vEvent.IsControllable ? "CONTROLLABLE" : "UNCONTROLLABLE");
                    writer.WriteAttributeString("Name", vEvent.ToString());
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
                            if (v_target < 0) continue;
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

                    writer.WriteEndElement();

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                });

                foreach (var vPlant in pPlants) addAutomaton(vPlant, "PLANT");
                foreach (var vSpec in pSpecifications) addAutomaton(vSpec, "SPEC");

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            GC.Collect();
        }

        /// <summary>
        /// Converts to xmlfile.
        /// </summary>
        /// <param name="filepath">The filepath.</param>
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
            var vStates = new Dictionary<StatesTuple, ulong>((int) Size, StatesTupleComparator.GetInstance());
            var nextTuple = new StatesTuple(_tupleSize);

            using (var writer = XmlWriter.Create(filepath, settings))
            {
                writer.WriteStartElement("Automaton");
                writer.WriteAttributeString("Name", Name);

                writer.WriteStartElement("States");

                var addState = new Action<StatesTuple, ulong>((state, id) =>
                {
                    state.Get(pos, _bits, _maxSize);
                    vStates.Add(state, id);

                    var s = ComposeState(pos);

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

                foreach (var state in vStates)
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

                        if (!vStates.TryGetValue(nextTuple, out var value)) continue;

                        writer.WriteStartElement("Transition");

                        writer.WriteAttributeString("Origin", state.Value.ToString());
                        writer.WriteAttributeString("Trigger", j.ToString());
                        writer.WriteAttributeString("Destination", value.ToString());

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement();

                writer.WriteEndElement();
            }

            GC.Collect();
            GC.Collect();
        }
    }
}