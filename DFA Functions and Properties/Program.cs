using System;
using System.Collections.Generic;
using System.Linq;
using UltraDES;

// Top-level statements
TestProperties();
TestIteratingOverProperties();
TestMethods();
TestHandlingFiles();

Console.WriteLine("\n\nProgram finished.");
Console.ReadLine();

void FSM(out List<DeterministicFiniteAutomaton> plants, out List<DeterministicFiniteAutomaton> specs)
{
    var s = new List<State>();
    for (var i = 0; i < 6; i++)
        s.Add(i == 0 ? new State(i.ToString(), Marking.Marked) : new State(i.ToString()));

    // Creating Events (0 to 100)
    var e = new List<Event>();
    for (var i = 0; i < 100; ++i)
        e.Add(i % 2 != 0
            ? new Event($"e{i}", Controllability.Controllable)
            : new Event($"e{i}", Controllability.Uncontrollable));

    //----------------------------
    // Plants
    //----------------------------

    // C1
    var transC1 = new List<Transition>();
    transC1.Add(new Transition(s[0], e[11], s[1]));
    transC1.Add(new Transition(s[1], e[12], s[0]));

    var c1 = new DeterministicFiniteAutomaton(transC1, s[0], "C1");

    // C2
    var c2 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[21], s[1]),
            new Transition(s[1], e[22], s[0])
        },
        s[0], "C2");

    // Milling
    var milling = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[41], s[1]),
            new Transition(s[1], e[42], s[0])
        },
        s[0], "Milling");

    // MP
    var mp = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[81], s[1]),
            new Transition(s[1], e[82], s[0])
        },
        s[0], "MP");

    // Lathe
    var lathe = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[51], s[1]),
            new Transition(s[1], e[52], s[0]),
            new Transition(s[0], e[53], s[2]),
            new Transition(s[2], e[54], s[0])
        },
        s[0], "Lathe");

    // C3
    var c3 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[71], s[1]),
            new Transition(s[1], e[72], s[0]),
            new Transition(s[0], e[73], s[2]),
            new Transition(s[2], e[74], s[0])
        },
        s[0], "C3");

    // Robot
    var robot = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[31], s[1]),
            new Transition(s[1], e[32], s[0]),
            new Transition(s[0], e[33], s[2]),
            new Transition(s[2], e[34], s[0]),
            new Transition(s[0], e[35], s[3]),
            new Transition(s[3], e[36], s[0]),
            new Transition(s[0], e[37], s[4]),
            new Transition(s[4], e[38], s[0]),
            new Transition(s[0], e[39], s[5]),
            new Transition(s[5], e[30], s[0])
        },
        s[0], "Robot");

    // MM
    var mm = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[61], s[1]),
            new Transition(s[1], e[63], s[2]),
            new Transition(s[1], e[65], s[3]),
            new Transition(s[2], e[64], s[0]),
            new Transition(s[3], e[66], s[0])
        },
        s[0], "MM");

    //----------------------------
    // Specifications
    //----------------------------

    // E1
    var e1 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[12], s[1]),
            new Transition(s[1], e[31], s[0])
        },
        s[0], "E1");

    // E2
    var e2 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[22], s[1]),
            new Transition(s[1], e[33], s[0])
        },
        s[0], "E2");

    // E5
    var e5 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[36], s[1]),
            new Transition(s[1], e[61], s[0])
        },
        s[0], "E5");

    // E6
    var e6 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[38], s[1]),
            new Transition(s[1], e[63], s[0])
        },
        s[0], "E6");

    // E3
    var e3 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[32], s[1]),
            new Transition(s[1], e[41], s[0]),
            new Transition(s[0], e[42], s[2]),
            new Transition(s[2], e[35], s[0])
        },
        s[0], "E3");

    // E7
    var e7 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[30], s[1]),
            new Transition(s[1], e[71], s[0]),
            new Transition(s[0], e[74], s[2]),
            new Transition(s[2], e[65], s[0])
        },
        s[0], "E7");

    // E8
    var e8 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[72], s[1]),
            new Transition(s[1], e[81], s[0]),
            new Transition(s[0], e[82], s[2]),
            new Transition(s[2], e[73], s[0])
        },
        s[0], "E8");

    // E4
    var e4 = new DeterministicFiniteAutomaton(
        new[]
        {
            new Transition(s[0], e[34], s[1]),
            new Transition(s[1], e[51], s[0]),
            new Transition(s[1], e[53], s[0]),
            new Transition(s[0], e[52], s[2]),
            new Transition(s[2], e[37], s[0]),
            new Transition(s[0], e[54], s[3]),
            new Transition(s[3], e[39], s[0])
        },
        s[0], "E4");

    plants = new[] { c1, c2, milling, lathe, robot, mm, c3, mp }.ToList();
    specs = new[] { e1, e2, e3, e4, e5, e6, e7, e8 }.ToList();
}

void TestIteratingOverProperties()
{
    FSM(out var plants, out _);

    var G = plants.First(s => s.Name == "Robot");

    Console.WriteLine("\nAutomaton: {0}", G);

    Console.WriteLine("\nStates: {0}", G.States.Count());
    foreach (var s in G.States)
        Console.WriteLine("\tState: {0}", s);

    Console.WriteLine("\nEvents: {0}", G.Events.Count());
    foreach (var e in G.Events)
        Console.WriteLine(e.IsControllable ? $"\tEvent: {e} (controllable)" : $"\tEvent: {e} (uncontrollable)");

    Console.WriteLine($"\nTransitions: {G.Transitions.Count()}");

    foreach (var t in G.Transitions)
        Console.WriteLine($"\tTransition: {t}");

    Console.WriteLine("\nUncontrollable Events: {0}", G.UncontrollableEvents.Count());
    foreach (var e in G.UncontrollableEvents) Console.WriteLine("\tUncontrollable event: {0}", e);

    Console.WriteLine("\nMarked States: {0}", G.MarkedStates.Count());
    foreach (var s in G.MarkedStates) Console.WriteLine("\tMarked State: {0}", s);

    var transitionsList = G.Transitions.ToList();
    var transitionsArray = G.Transitions.ToArray();
}

void TestProperties()
{
    FSM(out var plants, out _);

    var G = plants.First(s => s.Name == "Robot");

    Console.WriteLine($"Automaton: {G}");

    Console.WriteLine($"\tInitial state: {G.InitialState}");

    Console.WriteLine($"\tAccessible Part: {G.AccessiblePart.Size} states");
    Console.WriteLine($"\tCoaccessible Part: {G.CoaccessiblePart.Size} states");
    Console.WriteLine($"\tTrim: {G.Trim.Size} states");

    Console.WriteLine($"\tMinimal: {G.Minimal.Size} states");
    Console.WriteLine($"\tPrefix Closure: {G.PrefixClosure.Size} states");
}

void ShowDisablement(DeterministicFiniteAutomaton S, DeterministicFiniteAutomaton G, int limit)
{
    var statesAndEventsList = S.DisabledEvents(G);
    var i = 0;
    foreach (var pairStateEventList in statesAndEventsList)
    {
        Console.WriteLine($"\tState: {pairStateEventList.Key}");

        foreach (var _event in pairStateEventList.Value) Console.WriteLine($"\t\tEvent: {_event}");
        Console.Write("\n");

        if (++i >= limit) break;
    }
}

void TestMethods()
{
    FSM(out var plants, out var specs);

    Console.WriteLine("\nFSM\n");

    var Plant = DeterministicFiniteAutomaton.ParallelComposition(plants);
    var Specification = DeterministicFiniteAutomaton.ParallelComposition(specs);
    var K = Plant.ParallelCompositionWith(Specification);

    Console.WriteLine($"\tPlant: {Plant.Size} states");
    Console.WriteLine($"\tSpecification: {Specification.Size} states");
    Console.WriteLine($"\tK: {K.Size} states");

    // Controllability
    Console.WriteLine(K.IsControllable(Plant) ? "\tK is controllable" : "\tK is not controllable");

    // Computes the supervisor using the global plant and specification
    var S = DeterministicFiniteAutomaton.MonolithicSupervisor(
        [Plant], // global plant
        [Specification]
    );

    Console.WriteLine($"\tSupervisor: {S.Size} states");

    // Computes the supervisor using all plants and specifications.
    S = DeterministicFiniteAutomaton.MonolithicSupervisor(plants, specs);
    Console.WriteLine($"\tSupervisor (method 2): {S.Size} states");

    var proj = S.Projection(S.UncontrollableEvents);
    Console.WriteLine($"\tProjection: {proj.Size} states");

    S = S.SimplifyStatesName();
    Console.WriteLine("\tDisabled Events (first 5):");
    ShowDisablement(S, Plant, 5);

    Console.WriteLine("------------------------------------------------------");

    var P = Plant.ProductWith(Specification);
    Console.WriteLine($"\tProduct 1: {P.Size} state{(P.Size > 1 ? "s" : "")}");
    P = DeterministicFiniteAutomaton.Product(plants);
    Console.WriteLine($"\tProduct 2: {P.Size} state{(P.Size > 1 ? "s" : "")}");
    P = Plant.ProductWith(specs);
    Console.WriteLine($"\tProduct 3: {P.Size} state{(P.Size > 1 ? "s" : "")}");
}

void TestHandlingFiles()
{
    FSM(out var plants, out var specs);

    var robot = plants.First(s => s.Name == "Robot");

    var Plant = DeterministicFiniteAutomaton.ParallelComposition(plants);
    var Specification = DeterministicFiniteAutomaton.ParallelComposition(specs);

    // Exporting a automaton to a ADS file (TCT)
    robot.ToAdsFile("ROBOT.ADS");

    DeterministicFiniteAutomaton.ToAdsFile(
        [Plant, Specification],
        ["G.ADS", "E.ADS"]
    );

    // Reading a automaton from a ADS file
    robot = DeterministicFiniteAutomaton.FromAdsFile("ROBOT.ADS");

    // Exporting to a WMod File (Supremica)
    DeterministicFiniteAutomaton.ToWmodFile("FSM.wmod", plants, specs);

    // Importing plants and specifications from WMod file
    DeterministicFiniteAutomaton.FromWmodFile("FSM.wmod", out plants, out specs);

    // Exporting to a XML File
    robot.ToXMLFile("robot.xml");

    // Building a automaton from a xml file
    robot = DeterministicFiniteAutomaton.FromXMLFile("robot.xml");

    // Serializes the automaton to the file (stores in the file (binary mode)).
    Plant.SerializeAutomaton("Plant.bin");

    Plant = DeterministicFiniteAutomaton.DeserializeAutomaton("Plant.bin");

    plants.ForEach(g => g.drawSVGFigure(null, false));
}