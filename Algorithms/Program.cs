using UltraDES;
using System.Collections.Generic;
using System;

// Top-level statements
TestObserverPropertyVerify();
TestObserverPropertySearch();
return;

void TestObserverPropertySearch()
{
    Console.WriteLine("=== Test Observer Property Search ===");

    // Definition of States
    var q0 = new State("0", Marking.Unmarked);
    var q1 = new State("1", Marking.Unmarked);
    var q2 = new State("2", Marking.Unmarked);
    var q3 = new State("3", Marking.Unmarked);
    var q4 = new State("4", Marking.Marked);
    var q5 = new State("5", Marking.Unmarked);
    var q6 = new State("6", Marking.Unmarked);

    var states = new[] { q0, q1, q2, q3, q4, q5, q6 };


    // Definition of Events
    var gamma = new Event("gamma", Controllability.Controllable);
    var beta = new Event("beta", Controllability.Controllable);
    var lambda = new Event("lambda", Controllability.Controllable);
    var omega = new Event("omega", Controllability.Controllable);
    var y = new Event("y", Controllability.Controllable);

    var events = new Dictionary<string, Event>()
    {
        {"gamma", gamma},
        {"beta", beta},
        {"lambda", lambda},
        {"omega", omega},
        {"y", y}
    };

    // Definition of Transitions
    var transitions = new Transition[]
    {
        (q0, events["lambda"], q1), (q1, events["lambda"], q1),
        (q1, events["beta"], q2), (q2, events["gamma"], q3),
        (q3, events["gamma"], q1), (q3, events["lambda"], q4)
    };

    // Creation of Automaton G
    var G = new DeterministicFiniteAutomaton(transitions, q0, "G");

    G.ShowAutomaton("G");

    // Observer Property Search
    var Vg = G.ObserverPropertySearch(new[] { events["lambda"] });
    Vg.ShowAutomaton("Vg");
    Console.WriteLine();
}

void TestObserverPropertyVerify()
{
    Console.WriteLine("=== Test Observer Property Verify ===");

    // Definition of States
    var q0 = new State("0", Marking.Unmarked);
    var q1 = new State("1", Marking.Unmarked);
    var q2 = new State("2", Marking.Unmarked);
    var q3 = new State("3", Marking.Unmarked);
    var q4 = new State("4", Marking.Marked);
    var q5 = new State("5", Marking.Unmarked);
    var q6 = new State("6", Marking.Unmarked);

    var states = new[] { q0, q1, q2, q3, q4, q5, q6 };

    // Definition of Events
    var a = new Event("a", Controllability.Controllable);
    var b = new Event("b", Controllability.Controllable);
    var x = new Event("x", Controllability.Controllable);
    var z = new Event("z", Controllability.Controllable);
    var y = new Event("y", Controllability.Controllable);

    var events = new Dictionary<string, Event>()
    {
        {"a", a},
        {"b", b},
        {"x", x},
        {"z", z},
        {"y", y}
    };


    // Definition of Transitions
    var transitions = new Transition[]
    {
        (q0, events["a"], q1),
        (q1, events["a"], q1),
        (q1, events["x"], q2),
        (q2, events["y"], q3),
        (q3, events["y"], q1),
        (q3, events["a"], q4)
    };

    // Creation of Automaton G
    var G = new DeterministicFiniteAutomaton(transitions, q0, "G");

    G.ShowAutomaton("G");

    // Observer Property Verify
    G.ObserverPropertyVerify(new[] { events["a"] }, out var Vg, false);
    Vg.ShowAutomaton("Vg");
    Console.WriteLine();
}