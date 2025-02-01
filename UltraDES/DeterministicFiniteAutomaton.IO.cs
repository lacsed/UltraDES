using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace UltraDES;

using DFA = DeterministicFiniteAutomaton;

/// <summary>
/// Partial implementation of the <see cref="DeterministicFiniteAutomaton"/> class that handles
/// serialization, deserialization, and conversion of automata to/from various formats (XML, ADS, FSM, Wmod, and JSON).
/// This class cannot be inherited.
/// </summary>
partial class DeterministicFiniteAutomaton
{
    /// <summary>
    /// Gets an XML string representation of the automaton.
    /// </summary>
    public string ToXML
    {
        get
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n"
            };

            var xmlStr = new StringBuilder();
            using var writer = XmlWriter.Create(xmlStr, settings);
            AutomatonToXML(writer);

            return xmlStr.ToString();
        }
    }

    /// <summary>
    /// Deserializes an automaton from a binary file.
    /// </summary>
    /// <param name="filepath">The path of the file to deserialize.</param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/> loaded from the file.</returns>
    public static DFA DeserializeAutomaton(string filepath)
    {
        IFormatter formatter = new BinaryFormatter();
        Stream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var obj = (DFA)formatter.Deserialize(stream);
        stream.Close();
        return obj;
    }

    /// <summary>
    /// Reads an automaton from an ADS file.
    /// </summary>
    /// <param name="adsStr">The file path of the ADS file.</param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/> loaded from the ADS file.</returns>
    /// <exception cref="Exception">Thrown if the file is not in ADS format.</exception>
    public static DFA FromAdsFile(string adsStr) => AdsToAutomaton(File.OpenText(adsStr));

    /// <summary>
    /// Reads an automaton from an ADS string.
    /// </summary>
    /// <param name="adsStr">A string containing ADS formatted automaton data.</param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/> loaded from the string.</returns>
    /// <exception cref="Exception">Thrown if the string is not in ADS format.</exception>
    public static DFA FromAdsString(string adsStr)
    {
        var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(adsStr);
            writer.Flush();
            stream.Position = 0;
        }

        using var reader = new StreamReader(stream);
        return AdsToAutomaton(reader);
    }

    /// <summary>
    /// Converts an ADS-formatted input from a stream reader into a DFA.
    /// </summary>
    /// <param name="stream">The stream reader for ADS data.</param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/>.</returns>
    /// <exception cref="Exception">Thrown if the file is not in ADS format.</exception>
    private static DFA AdsToAutomaton(StreamReader stream)
    {
        // Helper function that reads the next non-empty, non-comment line.
        static string NextValidLine(StreamReader file)
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

        // Read automaton name.
        var vName = NextValidLine(stream);

        if (!NextValidLine(stream).Contains("State size"))
            throw new Exception("File is not on ADS Format.");

        // Read number of states.
        var states = int.Parse(NextValidLine(stream));

        if (!NextValidLine(stream).Contains("Marker states"))
            throw new Exception("File is not on ADS Format.");

        string vMarked = string.Empty;
        var vLine = NextValidLine(stream);
        if (!vLine.Contains("Vocal states"))
        {
            vMarked = vLine;
            vLine = NextValidLine(stream);
        }

        AbstractState[] vStateSet;
        switch (vMarked)
        {
            case "*":
                vStateSet = Enumerable.Range(0, states)
                    .Select(i => new State(i.ToString(), Marking.Marked))
                    .ToArray();
                break;
            case "":
                vStateSet = Enumerable.Range(0, states)
                    .Select(i => new State(i.ToString()))
                    .ToArray();
                break;
            default:
                var markedSet = vMarked.Split().Select(int.Parse).ToList();
                vStateSet = Enumerable.Range(0, states)
                    .Select(i => markedSet.Contains(i)
                        ? new State(i.ToString(), Marking.Marked)
                        : new State(i.ToString()))
                    .ToArray();
                break;
        }

        if (!vLine.Contains("Vocal states"))
            throw new Exception("File is not on ADS Format.");

        // Skip lines until reaching the "Transitions" section.
        vLine = NextValidLine(stream);
        while (!vLine.Contains("Transitions"))
            vLine = NextValidLine(stream);

        var evs = new Dictionary<int, AbstractEvent>();
        var transitions = new List<Transition>();

        // Read transitions.
        while (!stream.EndOfStream)
        {
            vLine = NextValidLine(stream);
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
    /// Reads an automaton from a FSM file.
    /// </summary>
    /// <param name="fileName">The FSM file path.</param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/> loaded from the file.</returns>
    /// <exception cref="Exception">Thrown if the file does not exist or has an invalid format.</exception>
    public static DFA FromFsmFile(string fileName)
    {
        if (fileName == null)
            throw new Exception("Filename cannot be null.");

        if (!File.Exists(fileName))
            throw new Exception("File not found.");

        using var reader = new StreamReader(fileName);
        var automatonName = fileName.Split('/').Last().Split('.').First();
        return FsmToAutomaton(reader, automatonName);
    }

    /// <summary>
    /// Reads an automaton from a FSM string.
    /// </summary>
    /// <param name="fsmStr">A string containing FSM formatted automaton data.</param>
    /// <param name="name">The name to assign to the automaton.</param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/> loaded from the string.</returns>
    public static DFA FromFsmString(string fsmStr, string name)
    {
        var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream))
        {
            writer.Write(fsmStr);
            writer.Flush();
            stream.Position = 0;
        }

        using var reader = new StreamReader(stream);
        return FsmToAutomaton(reader, name);
    }

    /// <summary>
    /// Converts FSM formatted data from a stream reader into a DFA.
    /// </summary>
    /// <param name="reader">The stream reader for FSM data.</param>
    /// <param name="automatonName">The name to assign to the automaton.</param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/>.</returns>
    /// <exception cref="Exception">Thrown if the input format is invalid.</exception>
    private static DeterministicFiniteAutomaton FsmToAutomaton(StreamReader reader, string automatonName)
    {
        var statesList = new Dictionary<string, AbstractState>();
        var eventsList = new Dictionary<string, AbstractEvent>();
        var transitionsList = new List<Transition>();
        AbstractState initialState = null;

        var numberOfStates = reader.ReadLine();
        if (string.IsNullOrEmpty(numberOfStates))
            throw new Exception("Invalid Format.");
        var numStates = int.Parse(numberOfStates);

        // Read state definitions.
        for (var i = 0; i < numStates; ++i)
        {
            string stateLine;
            do
            {
                stateLine = reader.ReadLine();
            } while (stateLine == "");

            if (stateLine == null)
                throw new Exception("Invalid Format.");
            var stateInfo = stateLine.Split('\t');
            if (stateInfo.Length != 3)
                throw new Exception("Invalid Format.");
            if (statesList.ContainsKey(stateInfo[0]))
                throw new Exception("Invalid Format: Duplicated state.");
            var state = new State(stateInfo[0], stateInfo[1] == "1" ? Marking.Marked : Marking.Unmarked);
            statesList.Add(stateInfo[0], state);

            if (initialState == null)
                initialState = state;

            var numTransitions = int.Parse(stateInfo[2]);
            for (var t = 0; t < numTransitions; ++t)
                reader.ReadLine();
        }

        // Reset the reader to process transitions.
        reader.DiscardBufferedData();
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        reader.ReadLine();

        // Read transitions for each state.
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
                if (transition.Length != 4)
                    throw new Exception("Invalid Format.");
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
    /// Reads automata from a Wmod file.
    /// </summary>
    /// <param name="filename">The path of the Wmod file.</param>
    /// <param name="plants">Output list of plant automata.</param>
    /// <param name="specs">Output list of specification automata.</param>
    public static void FromWmodFile(string filename, out List<DFA> plants, out List<DFA> specs)
    {
        using var reader = XmlReader.Create(filename, new XmlReaderSettings());
        WmodToAutomata(out plants, out specs, reader);
    }

    /// <summary>
    /// Reads automata from a Wmod string.
    /// </summary>
    /// <param name="wmodStr">A string containing Wmod formatted data.</param>
    /// <param name="plants">Output list of plant automata.</param>
    /// <param name="specs">Output list of specification automata.</param>
    public static void FromWmodString(string wmodStr, out List<DFA> plants, out List<DFA> specs)
    {
        using var reader = XmlReader.Create(new StringReader(wmodStr), new XmlReaderSettings());
        WmodToAutomata(out plants, out specs, reader);
    }

    /// <summary>
    /// Converts Wmod XML data into plant and specification automata.
    /// </summary>
    /// <param name="plants">Output list of plant automata.</param>
    /// <param name="specs">Output list of specification automata.</param>
    /// <param name="reader">The XML reader containing the Wmod data.</param>
    private static void WmodToAutomata(out List<DeterministicFiniteAutomaton> plants, out List<DeterministicFiniteAutomaton> specs, XmlReader reader)
    {
        plants = new List<DFA>();
        specs = new List<DFA>();
        var vEvents = new List<AbstractEvent>();
        var vEventsMap = new Dictionary<string, AbstractEvent>();

        if (!reader.ReadToFollowing("EventDeclList"))
            throw new Exception("Invalid Format.");
        var eventsReader = reader.ReadSubtree();
        while (eventsReader.ReadToFollowing("EventDecl"))
        {
            eventsReader.MoveToAttribute("Kind");
            var vKind = eventsReader.Value;
            if (vKind == "PROPOSITION")
                continue;
            eventsReader.MoveToAttribute("Name");
            var vName = eventsReader.Value;
            var ev = new Event(vName,
                vKind == "CONTROLLABLE"
                    ? UltraDES.Controllability.Controllable
                    : UltraDES.Controllability.Uncontrollable);

            vEvents.Add(ev);
            vEventsMap.Add(vName, ev);
        }

        if (!reader.ReadToFollowing("ComponentList"))
            throw new Exception("Invalid Format.");

        var componentsReader = reader.ReadSubtree();

        while (componentsReader.ReadToFollowing("SimpleComponent"))
        {
            componentsReader.MoveToAttribute("Kind");
            var vKind = componentsReader.Value;
            componentsReader.MoveToAttribute("Name");
            var DName = componentsReader.Value;

            if (!componentsReader.ReadToFollowing("NodeList"))
                throw new Exception("Invalid Format.");
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

            if (!componentsReader.ReadToFollowing("EdgeList"))
                throw new Exception("Invalid Format.");

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

            var G = new DFA(transitions, states[initial], DName);
            if (vKind == "PLANT")
                plants.Add(G);
            else
                specs.Add(G);
        }
    }

    /// <summary>
    /// Reads an automaton from an XML file.
    /// </summary>
    /// <param name="filePath">The XML file path.</param>
    /// <param name="stateName">
    /// If true, the state names will be loaded from the file; otherwise, state IDs are used.
    /// </param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/> loaded from the XML file.</returns>
    public static DFA FromXMLFile(string filePath, bool stateName = true) =>
        XMLtoAutomaton(stateName, XDocument.Load(filePath));

    /// <summary>
    /// Reads an automaton from an XML string.
    /// </summary>
    /// <param name="xmlStr">A string containing XML formatted automaton data.</param>
    /// <param name="stateName">
    /// If true, the state names will be loaded from the XML; otherwise, state IDs are used.
    /// </param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/> loaded from the XML string.</returns>
    public static DFA FromXMLString(string xmlStr, bool stateName = true) =>
        XMLtoAutomaton(stateName, XDocument.Parse(xmlStr));

    /// <summary>
    /// Converts an XML document into a DFA.
    /// </summary>
    /// <param name="statesName">
    /// If true, state names are used; otherwise, state IDs are used as state names.
    /// </param>
    /// <param name="xmlDoc">The XML document containing automaton data.</param>
    /// <returns>A new instance of <see cref="DeterministicFiniteAutomaton"/>.</returns>
    private static DFA XMLtoAutomaton(bool statesName, XDocument xmlDoc)
    {
        var vName = xmlDoc.Descendants("Automaton")
                          .Select(dfa => dfa.Attribute("Name").Value)
                          .Single();
        var vStates = xmlDoc.Descendants("State")
                            .ToDictionary(s => s.Attribute("Id").Value,
                                s => new State(
                                    statesName ? s.Attribute("Name").Value : s.Attribute("Id").Value,
                                    s.Attribute("Marking").Value == "Marked" ? Marking.Marked : Marking.Unmarked));
        var vEvents = xmlDoc.Descendants("Event")
                            .ToDictionary(e => e.Attribute("Id").Value,
                                e => new Event(
                                    e.Attribute("Name").Value,
                                    e.Attribute("Controllability").Value == "Controllable"
                                        ? UltraDES.Controllability.Controllable
                                        : UltraDES.Controllability.Uncontrollable));
        var vInitial = xmlDoc.Descendants("InitialState")
                             .Select(i => vStates[i.Attribute("Id").Value])
                             .Single();
        var vTransitions = xmlDoc.Descendants("Transition")
                                 .Select(t =>
                                     new Transition(
                                         vStates[t.Attribute("Origin").Value],
                                         vEvents[t.Attribute("Trigger").Value],
                                         vStates[t.Attribute("Destination").Value]));
        return new DFA(vTransitions, vInitial, vName);
    }

    /// <summary>
    /// Serializes the automaton into a binary file.
    /// </summary>
    /// <param name="filepath">The file path where the automaton will be saved.</param>
    public void SerializeAutomaton(string filepath)
    {
        IFormatter formatter = new BinaryFormatter();
        Stream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None);
        formatter.Serialize(stream, this);
        stream.Close();
    }

    /// <summary>
    /// Converts the automaton to an ADS file and writes it to the specified file path.
    /// </summary>
    /// <param name="filepath">The file path to write the ADS file.</param>
    /// <param name="odd">The starting odd number for controllable events.</param>
    /// <param name="even">The starting even number for uncontrollable events.</param>
    public void ToAdsFile(string filepath, int odd = 1, int even = 2) =>
        ToAdsFile(new[] { this }, new[] { filepath }, odd, even);

    /// <summary>
    /// (Obsolete) Converts the automaton to an FM file.
    /// Use <see cref="ToFmFile(string)"/> instead.
    /// </summary>
    /// <param name="filepath">The file path for the FM file.</param>
    [Obsolete("Method ToFM is obsolete and will be removed, use ToFmFile instead.")]
    public void ToFM(string filepath) => ToFmFile(filepath);

    /// <summary>
    /// Converts the automaton to an FM file and writes it to disk.
    /// </summary>
    /// <param name="filepath">The file path to write the FM file.</param>
    public void ToFmFile(string filepath)
    {
        var G = this;
        filepath = filepath.EndsWith(".fm") ? filepath : filepath + ".fm";

        using var writer = new StreamWriter(filepath, false);
        writer.WriteLine($"(START) |- {G.InitialState}");
        foreach (var (o, ev, d) in G.Transitions)
            writer.WriteLine($"{o} {ev} {d}");
        foreach (var m in G.MarkedStates)
            writer.WriteLine($"{m} -| (FINAL)");
    }

    /// <summary>
    /// Converts the automaton to an ADS file and writes it to disk.
    /// </summary>
    /// <param name="automata">The collection of automata to convert.</param>
    /// <param name="filepaths">The file paths where each ADS file will be written.</param>
    /// <param name="odd">The starting odd number for controllable events.</param>
    /// <param name="even">The starting even number for uncontrollable events.</param>
    public static void ToAdsFile(IEnumerable<DFA> automata, IEnumerable<string> filepaths, int odd = 1, int even = 2)
    {
        foreach (var g in automata)
            g.Simplify();

        var vEventsUnion = automata.SelectMany(g => g.Events)
                                    .Distinct();
        var vEvents = new Dictionary<AbstractEvent, int>();

        foreach (var e in vEventsUnion)
        {
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
        }

        var vFilepaths = filepaths.ToArray();
        int i = -1;
        foreach (var g in automata)
        {
            ++i;
            var vEventsMaps = new Dictionary<int, int>();
            for (var e = 0; e < g._eventsUnion.Length; ++e)
                vEventsMaps.Add(e, vEvents[g._eventsUnion[e]]);

            var file = File.CreateText(vFilepaths[i]);
            file.WriteLine("# UltraDES ADS FILE - LACSED | UFMG\r\n");
            file.WriteLine($"{g.Name}\r\n");
            file.WriteLine("State size (State set will be (0,1....,size-1)):");
            file.WriteLine($"{g.Size}\r\n");
            file.WriteLine("Marker states:");
            string vMarkedStates = "";
            bool vFirst = true;
            for (var s = 0; s < g._statesList[0].Length; ++s)
            {
                if (!g._statesList[0][s].IsMarked)
                    continue;
                if (vFirst)
                {
                    vFirst = false;
                    vMarkedStates = s.ToString();
                }
                else
                    vMarkedStates += "\r\n" + s;
            }
            file.WriteLine($"{vMarkedStates}\r\n");
            file.WriteLine("Vocal states:\r\n");
            file.WriteLine("Transitions:");
            for (var s = 0; s < g._statesList[0].Length; ++s)
                foreach (var t in g._adjacencyList[0][s])
                    file.WriteLine($"{s,-5} {vEventsMaps[t.e],-3} {t.s,-5}");
            file.Close();
        }
    }

    /// <summary>
    /// Converts the automaton to an FSM file and writes it to disk.
    /// </summary>
    /// <param name="fileName">The file name to write the FSM file.</param>
    public void ToFsmFile(string fileName = null)
    {
        fileName ??= Name;
        fileName = fileName.Replace('|', '_');
        if (!fileName.EndsWith(".fsm"))
            fileName += ".fsm";

        using var writer = new StreamWriter(fileName);
        AutomatonToFsm(writer);
        writer.Close();
    }

    /// <summary>
    /// Gets an FSM formatted string representation of the automaton.
    /// </summary>
    public string ToFsm
    {
        get
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            AutomatonToFsm(writer);
            writer.Close();
            return (new StreamReader(stream)).ReadToEnd();
        }
    }

    /// <summary>
    /// Writes the automaton in FSM format to the specified stream writer.
    /// </summary>
    /// <param name="writer">The stream writer to output FSM data.</param>
    private void AutomatonToFsm(StreamWriter writer)
    {
        int n = _statesList.Count;
        var pos = new int[n];
        writer.Write($"{Size}\n\n");

        // Local function to write a state and its transitions.
        var writeState = new Action<AbstractState, Transition[]>((s, transitions) =>
        {
            writer.Write($"{s}\t{(s.IsMarked ? 1 : 0)}\t{transitions.Length}\n");
            foreach (var t in transitions)
                writer.Write($"{t.Trigger}\t{t.Destination}\t{(t.Trigger.IsControllable ? "c" : "uc")}\to\n");
            writer.Write("\n");
        });

        if (IsEmpty())
            return;

        if (_validStates != null)
        {
            writeState(ComposeState(pos), GetTransitionsFromState(pos).ToArray());
            foreach (var sT in _validStates)
            {
                sT.Key.Get(pos, _bits, _maxSize);
                for (var i = 0; i < n; ++i)
                {
                    if (pos[i] == 0)
                        continue;
                    writeState(ComposeState(pos), GetTransitionsFromState(pos).ToArray());
                    break;
                }
            }
        }
        else
        {
            do
                writeState(ComposeState(pos), GetTransitionsFromState(pos).ToArray());
            while (IncrementPosition(pos));
        }
    }

    /// <summary>
    /// Converts the automaton to a Wmod file and writes it to disk.
    /// </summary>
    /// <param name="filename">The output file path.</param>
    /// <param name="plants">A collection of plant automata.</param>
    /// <param name="specifications">A collection of specification automata.</param>
    /// <param name="moduleName">The module name to assign.</param>
    public static void ToWmodFile(string filename, IEnumerable<DFA> plants, IEnumerable<DFA> specifications, string moduleName = "UltraDES")
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t",
            NewLineChars = "\r\n"
        };

        using (var writer = XmlWriter.Create(filename, settings))
            AutomatonToWmod(plants, specifications, moduleName, writer);

        GC.Collect();
    }

    /// <summary>
    /// Converts the automata to a Wmod formatted string.
    /// </summary>
    /// <param name="plants">A collection of plant automata.</param>
    /// <param name="specifications">A collection of specification automata.</param>
    /// <param name="moduleName">The module name to assign.</param>
    /// <returns>A string containing the Wmod formatted data.</returns>
    public static string ToWmodString(IEnumerable<DFA> plants, IEnumerable<DFA> specifications, string moduleName = "UltraDES")
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t",
            NewLineChars = "\r\n"
        };

        var xmlStr = new StringBuilder();
        using (var writer = XmlWriter.Create(xmlStr, settings))
            AutomatonToWmod(plants, specifications, moduleName, writer);

        GC.Collect();
        return xmlStr.ToString();
    }

    /// <summary>
    /// Writes automata data in Wmod XML format to the specified XML writer.
    /// </summary>
    /// <param name="plants">A collection of plant automata.</param>
    /// <param name="specifications">A collection of specification automata.</param>
    /// <param name="moduleName">The module name to assign.</param>
    /// <param name="writer">The XML writer to output Wmod data.</param>
    private static void AutomatonToWmod(IEnumerable<DeterministicFiniteAutomaton> plants, IEnumerable<DeterministicFiniteAutomaton> specifications, string moduleName, XmlWriter writer)
    {
        writer.WriteStartDocument(true);
        writer.WriteStartElement("Module", "http://waters.sourceforge.net/xsd/module");
        writer.WriteAttributeString("Name", moduleName);
        writer.WriteAttributeString("xmlns", "ns2", null, "http://waters.sourceforge.net/xsd/base");

        writer.WriteElementString("ns2", "Comment", null, "By UltraDES");

        writer.WriteStartElement("EventDeclList");

        // Write predefined events.
        writer.WriteStartElement("EventDecl");
        writer.WriteAttributeString("Kind", "PROPOSITION");
        writer.WriteAttributeString("Name", ":accepting");
        writer.WriteEndElement();
        writer.WriteStartElement("EventDecl");
        writer.WriteAttributeString("Kind", "PROPOSITION");
        writer.WriteAttributeString("Name", ":forbidden");
        writer.WriteEndElement();

        var vEvents = new List<AbstractEvent>();
        vEvents = plants.Aggregate(vEvents, (current, v_plant) => current.Union(v_plant.Events).ToList());
        vEvents = specifications.Aggregate(vEvents, (current, v_spec) => current.Union(v_spec.Events).ToList());

        foreach (var vEvent in vEvents)
        {
            writer.WriteStartElement("EventDecl");
            writer.WriteAttributeString("Kind", vEvent.IsControllable ? "CONTROLLABLE" : "UNCONTROLLABLE");
            writer.WriteAttributeString("Name", vEvent.ToString());
            writer.WriteEndElement();
        }
        writer.WriteEndElement(); // End EventDeclList

        writer.WriteStartElement("ComponentList");

        // Local function to add an automaton to the XML.
        void AddAutomaton(DFA G, string kind, XmlWriter w)
        {
            w.WriteStartElement("SimpleComponent");
            w.WriteAttributeString("Kind", kind);
            w.WriteAttributeString("Name", G.Name);
            w.WriteStartElement("Graph");
            w.WriteStartElement("NodeList");
            foreach (var state in G.States)
            {
                w.WriteStartElement("SimpleNode");
                w.WriteAttributeString("Name", state.ToString());
                if (state == G.InitialState)
                    writer.WriteAttributeString("Initial", "true");

                if (state.IsMarked)
                {
                    writer.WriteStartElement("EventList");
                    writer.WriteStartElement("SimpleIdentifier");
                    writer.WriteAttributeString("Name", ":accepting");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // End NodeList

            writer.WriteStartElement("EdgeList");
            foreach (var t in G.Transitions)
            {
                writer.WriteStartElement("Edge");
                writer.WriteAttributeString("Source", t.Origin.ToString());
                writer.WriteAttributeString("Target", t.Destination.ToString());
                writer.WriteStartElement("LabelBlock");
                writer.WriteStartElement("SimpleIdentifier");
                writer.WriteAttributeString("Name", t.Trigger.ToString());
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // End EdgeList

            writer.WriteEndElement(); // End Graph
            writer.WriteEndElement(); // End SimpleComponent
        }

        foreach (var plant in plants)
            AddAutomaton(plant, "PLANT", writer);
        foreach (var spec in specifications)
            AddAutomaton(spec, "SPEC", writer);

        writer.WriteEndElement(); // End ComponentList
        writer.WriteEndElement(); // End Module
        writer.WriteEndDocument();
    }

    /// <summary>
    /// Writes the XML representation of the automaton to the specified file.
    /// </summary>
    /// <param name="filepath">The file path where the XML file will be saved.</param>
    public void ToXMLFile(string filepath)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "\t",
            NewLineChars = "\r\n"
        };

        using (var writer = XmlWriter.Create(filepath, settings))
            AutomatonToXML(writer);

        GC.Collect();
        GC.Collect();
    }

    /// <summary>
    /// Writes the XML representation of the automaton using the provided XML writer.
    /// </summary>
    /// <param name="writer">The XML writer used to output the automaton data.</param>
    private void AutomatonToXML(XmlWriter writer)
    {
        writer.WriteStartElement("Automaton");
        writer.WriteAttributeString("Name", Name);

        writer.WriteStartElement("States");
        var stateDic = new Dictionary<AbstractState, int>();
        int idState = 0;
        foreach (var state in States)
        {
            stateDic.Add(state, idState++);
            writer.WriteStartElement("State");
            writer.WriteAttributeString("Name", state.ToString());
            writer.WriteAttributeString("Marking", state.Marking.ToString());
            writer.WriteAttributeString("Id", stateDic[state].ToString());
            writer.WriteEndElement();
        }
        writer.WriteEndElement(); // End States

        writer.WriteStartElement("InitialState");
        writer.WriteAttributeString("Id", "0");
        writer.WriteEndElement();

        writer.WriteStartElement("Events");
        var eventDic = new Dictionary<AbstractEvent, int>();
        int idEvent = 0;
        foreach (var ev in Events)
        {
            eventDic.Add(ev, idEvent++);
            writer.WriteStartElement("Event");
            writer.WriteAttributeString("Name", ev.ToString());
            writer.WriteAttributeString("Controllability", ev.Controllability.ToString());
            writer.WriteAttributeString("Id", eventDic[ev].ToString());
            writer.WriteEndElement();
        }
        writer.WriteEndElement(); // End Events

        writer.WriteStartElement("Transitions");
        foreach (var t in Transitions)
        {
            writer.WriteStartElement("Transition");
            writer.WriteAttributeString("Origin", stateDic[t.Origin].ToString());
            writer.WriteAttributeString("Trigger", eventDic[t.Trigger].ToString());
            writer.WriteAttributeString("Destination", stateDic[t.Destination].ToString());
            writer.WriteEndElement();
        }
        writer.WriteEndElement(); // End Transitions

        writer.WriteEndElement(); // End Automaton
    }

    /// <summary>
    /// Writes the automata in JSON format to a file.
    /// </summary>
    /// <param name="filepath">The file path where the JSON will be saved.</param>
    /// <param name="automata">A collection of automata to write.</param>
    public static void ToJsonFile(string filepath, params DFA[] automata) =>
        ToJsonFile(filepath, (IEnumerable<DFA>)automata);

    /// <summary>
    /// Writes the automata in JSON format to a file.
    /// </summary>
    /// <param name="filepath">The file path where the JSON will be saved.</param>
    /// <param name="automata">A collection of automata to write.</param>
    public static void ToJsonFile(string filepath, IEnumerable<DFA> automata)
    {
        var json = ToJsonString(automata);
        using var sw = new StreamWriter(filepath, false);
        sw.Write(json);
    }

    /// <summary>
    /// Converts the automata to a JSON string.
    /// </summary>
    /// <param name="automata">A collection of automata.</param>
    /// <returns>A JSON string representing the automata.</returns>
    public static string ToJsonString(params DFA[] automata) =>
        ToJsonString((IEnumerable<DFA>)automata);

    /// <summary>
    /// Converts the automata to a JSON string.
    /// </summary>
    /// <param name="automata">A collection of automata.</param>
    /// <returns>A JSON string representing the automata.</returns>
    public static string ToJsonString(IEnumerable<DFA> automata)
    {
        var elements = new JArray();
        foreach (var G in automata)
        {
            var states = G.States.ToArray();
            var events = G.Events.ToArray();
            var aut = new JObject
            {
                ["name"] = G.Name
            };

            var jstates = new JArray();
            for (var i = 0; i < states.Length; i++)
            {
                var s = new JObject
                {
                    ["name"] = states[i].ToString(),
                    ["marking"] = states[i].IsMarked ? "marked" : "unmarked",
                    ["id"] = i
                };
                jstates.Add(s);
            }
            aut["states"] = jstates;
            aut["initial_state"] = Array.IndexOf(states, G.InitialState);

            var jevents = new JArray();
            for (var i = 0; i < events.Length; i++)
            {
                var ev = new JObject
                {
                    ["name"] = events[i].ToString(),
                    ["controllability"] = events[i].IsControllable ? "controllable" : "uncontrollable",
                    ["id"] = i
                };
                jevents.Add(ev);
            }
            aut["events"] = jevents;

            var jtrans = new JArray();
            foreach (var trans in G.Transitions)
            {
                var t = new JObject
                {
                    ["origin"] = Array.IndexOf(states, trans.Origin),
                    ["trigger"] = Array.IndexOf(events, trans.Trigger),
                    ["destination"] = Array.IndexOf(states, trans.Destination)
                };
                jtrans.Add(t);
            }
            aut["transitions"] = jtrans;

            elements.Add(aut);
        }

        return elements.ToString();
    }

    /// <summary>
    /// Reads automata from a JSON formatted string.
    /// </summary>
    /// <param name="jsonStr">A JSON string containing automata data.</param>
    /// <returns>An enumerable collection of automata.</returns>
    public static IEnumerable<DFA> FromJsonString(string jsonStr)
    {
        var elements = JArray.Parse(jsonStr);
        foreach (var aut in elements)
        {
            var states = ((JArray)aut["states"]).ToDictionary(
                s => int.Parse(s["id"].ToString()),
                s => new State(s["name"].ToString(),
                    s["marking"].ToString() == "marked" ? Marking.Marked : Marking.Unmarked));

            var events = ((JArray)aut["events"]).ToDictionary(
                ev => int.Parse(ev["id"].ToString()),
                ev => new Event(ev["name"].ToString(),
                    ev["controllability"].ToString() == "controllable"
                        ? UltraDES.Controllability.Controllable
                        : UltraDES.Controllability.Uncontrollable));

            var initialState = states[int.Parse(aut["initial_state"].ToString())];
            var name = aut["name"].ToString();

            var transitions = aut["transitions"].Select(t => new Transition(
                states[int.Parse(t["origin"].ToString())],
                events[int.Parse(t["trigger"].ToString())],
                states[int.Parse(t["destination"].ToString())])
            );
            yield return new DFA(transitions, initialState, name);
        }
    }

    /// <summary>
    /// Reads automata from a JSON file.
    /// </summary>
    /// <param name="filepath">The JSON file path.</param>
    /// <returns>An enumerable collection of automata loaded from the file.</returns>
    public static IEnumerable<DFA> FromJsonFile(string filepath)
    {
        using var sr = new StreamReader(filepath);
        return FromJsonString(sr.ReadToEnd());
    }
}
