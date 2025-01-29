using UltraDES;
using UltraDES.Opacity;
using UltraDES.Diagnosability;
using System.Diagnostics;

TestObserver();
TestDiagnosability();
TestISOOpacity();
TestCSOOpacity();
TestIFSOpacity();
TestKStepsOpacity();



//     Tests with Observer.
//     Includes the creation of states, events, transitions, and observer construction.
//     Reference: Tests for the construction and display of the automaton's observer.
static void TestObserver()
{
    Console.WriteLine("=== Test Observer ===");

    // Definition of States
    var q1 = new State("q1");
    var q2 = new State("q2");
    var q3 = new State("q3");
    var q4 = new State("q4");
    var q5 = new State("q5");

    // Definition of Events
    var a = new Event("a", Controllability.Controllable);
    var b = new Event("b", Controllability.Controllable);
    var c = new Event("c", Controllability.Controllable);
    var f = new Event("f", Controllability.Controllable); // Reusing event 'f'

    // Definition of Transitions
    var transitions = new[]
    {
            new Transition(q1, f, q3),
            new Transition(q1, b, q2),
            new Transition(q2, a, q5),
            new Transition(q5, c, q5),
            new Transition(q4, b, q5),
            new Transition(q3, a, q4)
        };

    // Creation of Automaton G
    var G = new DeterministicFiniteAutomaton(transitions, q1, "G");

    // Definition of Observable and Unobservable Events
    var observableEvents = new HashSet<AbstractEvent> { a, b, c };
    var unobservableEvents = new HashSet<AbstractEvent> { f };

    // Parallel Composition of Rot with G
    var N = new State("N");
    var Y = new State("Y");
    var Rot = new DeterministicFiniteAutomaton(new[]
    {
            new Transition(N, f, Y),
            new Transition(Y, f, Y)
        }, N, "Labeler");

    var K = Rot.ParallelCompositionWith(G);

    // Display of Automata
    G.ShowAutomaton();
    Rot.ShowAutomaton();
    K.ShowAutomaton();

    // Time Measurement for Observer Construction
    var timer = Stopwatch.StartNew();
    var observer = DiagnosticsAlgoritms.CreateObserver(K, unobservableEvents);
    timer.Stop();

    // Display of Observer Construction Time
    Console.WriteLine($"Observer construction time: {timer.ElapsedMilliseconds} ms");
    Console.WriteLine();
}


//     Diagnosability Verification.
//     Executes the diagnosability verification using the constructed observer.
//     Reference: Verification of diagnostic properties of the system.
static void TestDiagnosability()
{
    Console.WriteLine("=== Test Diagnosability ===");

    // Definition of States
    var q1 = new State("q1");
    var q2 = new State("q2");
    var q3 = new State("q3");
    var q4 = new State("q4");
    var q5 = new State("q5");

    // Definition of Events
    var a = new Event("a", Controllability.Controllable);
    var b = new Event("b", Controllability.Controllable);
    var c = new Event("c", Controllability.Controllable);
    var f = new Event("f", Controllability.Controllable);

    // Definition of Transitions
    var transitions = new[]
    {
            new Transition(q1, f, q3),
            new Transition(q1, b, q2),
            new Transition(q2, a, q5),
            new Transition(q5, c, q5),
            new Transition(q4, b, q5),
            new Transition(q3, a, q4)
        };

    // Creation of Automaton G
    var G = new DeterministicFiniteAutomaton(transitions, q1, "G");

    // Definition of Unobservables
    var unobservableEvents = new HashSet<AbstractEvent> { f };

    // Creation of Labeler Automaton and Parallel Composition
    var N = new State("N");
    var Y = new State("Y");
    var Rot = new DeterministicFiniteAutomaton(new[]
    {
            new Transition(N, f, Y),
            new Transition(Y, f, Y)
        }, N, "Labeler");

    var K = Rot.ParallelCompositionWith(G);

    // Time Measurement for Observer Construction
    var timer = Stopwatch.StartNew();
    var observer = DiagnosticsAlgoritms.CreateObserver(K, unobservableEvents);
    timer.Stop();

    Console.WriteLine($"Observer construction time: {timer.ElapsedMilliseconds} ms");

    // Time Measurement for Diagnosability Verification
    timer = Stopwatch.StartNew();
    var result = DiagnosticsAlgoritms.IsDiagnosable(observer);
    timer.Stop();

    // Display of Results
    Console.WriteLine($"Diagnosability result: {result}");
    Console.WriteLine($"Diagnosability verification time: {timer.ElapsedMilliseconds} ms");
    Console.WriteLine();
}


//     Verification of ISO Opacity (Initial-State Opacity).
//     This test verifies the initial state opacity of the system.
//     Reference: Definition of Initial-State Opacity.
static void TestISOOpacity()
{
    Console.WriteLine("=== Test ISO Opacity (Initial-State Opacity) ===");

    // Definition of States
    var q0 = new State("q0");
    var q1 = new State("q1");
    var q2 = new State("q2");
    var q3 = new State("q3");
    var q4 = new State("q4");

    // Definition of Events
    var a = new Event("a", Controllability.Controllable);
    var b = new Event("b", Controllability.Controllable);
    var t = new Event("t", Controllability.Controllable);

    // Definition of Transitions (ISO Example)
    var isoTransitions = new[]
    {
            new Transition(q0, t, q2),
            new Transition(q0, b, q1),
            new Transition(q1, b, q4),
            new Transition(q0, a, q3),
            new Transition(q3, b, q4),
            new Transition(q4, a, q4),
            new Transition(q2, a, q2)
        };
    var G_iso = new DeterministicFiniteAutomaton(isoTransitions, q0, "ISO_Example");

    // Display of ISO Automaton
    G_iso.ShowAutomaton();

    var unobservableEvents = new HashSet<AbstractEvent> { t };


    // Time Measurement for Initial-State Opacity Verification
    var timer = Stopwatch.StartNew();
    var isISOpaque = OpacityAlgorithms.InitialStateOpacity(G_iso, unobservableEvents, [q0], out var estimator);
    timer.Stop();

    estimator.ShowAutomaton();

    // Display of Result
    Console.WriteLine($"ISO Opacity: {isISOpaque}");
    Console.WriteLine($"ISO Opacity verification time: {timer.ElapsedMilliseconds} ms");
    Console.WriteLine();
}


//     Verification of CSO Opacity (Current-Step Opacity).
//     This test verifies the opacity at the current step of the system.
//     Reference: Definition of Current-Step Opacity.
static void TestCSOOpacity()
{
    Console.WriteLine("=== Test CSO Opacity (Current-Step Opacity) ===");

    // Definition of States
    var q0 = new State("q0");
    var q1 = new State("q1");
    var q2 = new State("q2");
    var q3 = new State("q3");
    var q4 = new State("q4");

    // Definition of Events
    var a = new Event("a", Controllability.Controllable);
    var b = new Event("b", Controllability.Controllable);
    var c = new Event("c", Controllability.Controllable);

    // Definition of Transitions (CSO Example)
    var csoTransitions = new[]
    {
            new Transition(q0, a, q1),
            new Transition(q0, c, q2),
            new Transition(q1, b, q3),
            new Transition(q2, b, q4)
        };
    var G_cso = new DeterministicFiniteAutomaton(csoTransitions, q0, "CSO_Example");

    // Display of CSO Automaton
    G_cso.ShowAutomaton();

    // Definition of Observable and Unobservable Events
    var unobservableEvents = new HashSet<AbstractEvent> { c };

    // Time Measurement for Current-Step Opacity Verification
    var timer = Stopwatch.StartNew();
    var isCSOpaque = OpacityAlgorithms.CurrentStepOpacity(G_cso, unobservableEvents, [q1], out var estimator);
    timer.Stop();

    estimator.ShowAutomaton();

    // Display of Result
    Console.WriteLine($"CSO Opacity: {isCSOpaque}");
    Console.WriteLine($"CSO Opacity verification time: {timer.ElapsedMilliseconds} ms");
    Console.WriteLine();
}


//     Verification of IFSO Opacity (Infinite-Step Opacity).
//     This test verifies the opacity at infinite steps of the system.
//     Reference: Definition of Infinite-Step Opacity.
static void TestIFSOpacity()
{
    Console.WriteLine("=== Test IFSO Opacity (Infinite-Step Opacity) ===");

    // Definition of States
    var q0 = new State("q0");
    var q1 = new State("q1");
    var q2 = new State("q2");
    var q3 = new State("q3");

    // Definition of Events
    var a = new Event("a", Controllability.Controllable);
    var b = new Event("b", Controllability.Controllable);
    var t = new Event("t", Controllability.Controllable);

    // Definition of Transitions (IFSO Example)
    var ifsoTransitions = new[]
    {
            new Transition(q0, a, q0),
            new Transition(q0, t, q2),
            new Transition(q2, a, q1),
            new Transition(q1, b, q0),
            new Transition(q1, t, q3),
            new Transition(q3, b, q1)
        };
    var G_ifso = new DeterministicFiniteAutomaton(ifsoTransitions, q0, "IFSO_Example");

    // Display of IFSO Automaton
    G_ifso.ShowAutomaton();

    // Definition of Observable and Unobservable Events
    var unobservableEvents = new HashSet<AbstractEvent> { t };

    // Time Measurement for Infinite-Step Opacity Verification
    var timer = Stopwatch.StartNew();
    var isIFSOpaque = OpacityAlgorithms.InitialFinalStateOpacity(G_ifso, unobservableEvents, [(q0, q3)], out var estimator);
    timer.Stop();

    estimator.ShowAutomaton();

    // Display of Result
    Console.WriteLine($"IFSO Opacity: {isIFSOpaque}");
    Console.WriteLine($"IFSO Opacity verification time: {timer.ElapsedMilliseconds} ms");
    Console.WriteLine();
}


//     Verification of K-Steps Opacity.
//     This test verifies the system's opacity with a limitation of K steps.
//     Reference: Definition of K-Steps Opacity.
static void TestKStepsOpacity()
{
    Console.WriteLine("=== Test K-Steps Opacity ===");

    // Definition of States
    var q0 = new State("q0");
    var q1 = new State("q1");
    var q2 = new State("q2");
    var q3 = new State("q3");
    var q4 = new State("q4");
    var q5 = new State("q5");
    var q6 = new State("q6");
    var q7 = new State("q7");

    // Definition of Events
    var a = new Event("a", Controllability.Controllable);
    var b = new Event("b", Controllability.Controllable);
    var c = new Event("c", Controllability.Controllable);
    var t = new Event("t", Controllability.Controllable);

    // Definition of Transitions (K-Steps Example)
    var kstepsTransitions = new[]
    {
            new Transition(q0, t, q1),
            new Transition(q1, a, q2),
            new Transition(q2, b, q3),
            new Transition(q3, a, q4),
            new Transition(q4, c, q2),
            new Transition(q0, a, q5),
            new Transition(q5, b, q6),
            new Transition(q6, a, q7),
            new Transition(q7, c, q5)
        };
    var G_ksteps = new DeterministicFiniteAutomaton(kstepsTransitions, q0, "KSteps_Example");

    // Display of K-Steps Automaton
    G_ksteps.ShowAutomaton();

    // Definition of Observable and Unobservable Events
    var unobservableEvents = new HashSet<AbstractEvent> { t };


    // Definition of K (Number of Steps)
    var k = 2;

    // Time Measurement for K-Steps Opacity Verification
    var timer = Stopwatch.StartNew();
    var isKStepsOpaque = OpacityAlgorithms.KStepsOpacity(G_ksteps, unobservableEvents, [q1, q2], k, out var estimator);
    timer.Stop();

    estimator.ShowAutomaton();

    // Display of Result
    Console.WriteLine($"K-Steps Opacity (K={k}): {isKStepsOpaque}");
    Console.WriteLine($"K-Steps Opacity verification time: {timer.ElapsedMilliseconds} ms");
    Console.WriteLine();
}