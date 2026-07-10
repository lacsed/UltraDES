using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UltraDES;

var implementations = args.Length > 0
    ? args
    : new[] { "Auto", "UShort", "UInt", "BitMask", "BitArray", "BoolArray" };

const int warmupIterations = 1;
const int measuredIterations = 3;
var results = new List<BenchmarkResult>();
var failures = new List<(string Implementation, string Error)>();

foreach (var implementation in implementations)
{
    Console.WriteLine($"Running {implementation}...");
    Environment.SetEnvironmentVariable("ULTRADES_ADJACENCY_MATRIX_IMPL", implementation);

    try
    {
        for (var i = 0; i < warmupIterations; i++)
            RunScenario();

        for (var i = 0; i < measuredIterations; i++)
            results.Add(RunMeasured(implementation, i + 1));
    }
    catch (Exception ex)
    {
        failures.Add((implementation, ex.Message));
        Console.WriteLine($"  skipped: {ex.Message}");
    }
}

if (results.Count == 0)
{
    Console.WriteLine("No implementation completed the benchmark.");
    return;
}

var baseline = results.First();
var correctnessFailures = results
    .Where(r => r.MonolithicStates != baseline.MonolithicStates
        || r.ModularConflicting != baseline.ModularConflicting
        || !r.ModularSupervisorStates.SequenceEqual(baseline.ModularSupervisorStates))
    .ToArray();

Console.WriteLine();
Console.WriteLine("Correctness: " + (correctnessFailures.Length == 0 ? "OK" : "FAILED"));
if (correctnessFailures.Length > 0)
{
    foreach (var failure in correctnessFailures)
        Console.WriteLine($"  {failure.Implementation}: monolithic={failure.MonolithicStates}, modular=[{string.Join(',', failure.ModularSupervisorStates)}], conflicting={failure.ModularConflicting}");
}

Console.WriteLine();
Console.WriteLine("Implementation | Mono ms | Mono KB | Modular ms | Modular KB | Mono states | Modular states | Modular conflicting");
Console.WriteLine("--- | ---: | ---: | ---: | ---: | ---: | --- | ---");
foreach (var group in results.GroupBy(r => r.Implementation))
{
    var monoMs = group.Average(r => r.MonolithicElapsed.TotalMilliseconds);
    var monoKb = group.Average(r => r.MonolithicAllocatedBytes) / 1024.0;
    var modularMs = group.Average(r => r.ModularElapsed.TotalMilliseconds);
    var modularKb = group.Average(r => r.ModularAllocatedBytes) / 1024.0;
    var sample = group.First();
    Console.WriteLine($"{group.Key} | {monoMs:F3} | {monoKb:F1} | {modularMs:F3} | {modularKb:F1} | {sample.MonolithicStates} | [{string.Join(',', sample.ModularSupervisorStates)}] | {sample.ModularConflicting}");
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Skipped implementations:");
    foreach (var failure in failures)
        Console.WriteLine($"- {failure.Implementation}: {failure.Error}");
}

BenchmarkResult RunMeasured(string implementation, int iteration)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    FSM(out var plantsForMonolithic, out var specsForMonolithic);
    var beforeMono = GC.GetAllocatedBytesForCurrentThread();
    var monoStopwatch = Stopwatch.StartNew();
    var monolithic = DeterministicFiniteAutomaton.MonolithicSupervisor(plantsForMonolithic, specsForMonolithic, true);
    monoStopwatch.Stop();
    var monoAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeMono;

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    FSM(out var plantsForModular, out var specsForModular);
    var beforeModular = GC.GetAllocatedBytesForCurrentThread();
    var modularStopwatch = Stopwatch.StartNew();
    var modular = LocalModularSupervisorIncludingConflictStatus(plantsForModular, specsForModular, out var modularConflicting);
    modularStopwatch.Stop();
    var modularAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeModular;

    return new BenchmarkResult(
        implementation,
        iteration,
        monoStopwatch.Elapsed,
        monoAllocated,
        modularStopwatch.Elapsed,
        modularAllocated,
        monolithic.Size,
        modular.Select(s => s.Size).ToArray(),
        modularConflicting);
}

void RunScenario()
{
    FSM(out var plants, out var specs);
    _ = DeterministicFiniteAutomaton.MonolithicSupervisor(plants, specs, true);
    FSM(out plants, out specs);
    _ = LocalModularSupervisorIncludingConflictStatus(plants, specs, out _);
}

DeterministicFiniteAutomaton[] LocalModularSupervisorIncludingConflictStatus(
    IEnumerable<DeterministicFiniteAutomaton> plants,
    IEnumerable<DeterministicFiniteAutomaton> specifications,
    out bool isConflicting)
{
    var plantArray = plants.ToArray();
    var supervisors = specifications
        .Select(specification =>
        {
            var specificationEvents = specification.Events.ToHashSet();
            var localPlants = plantArray.Where(plant => plant.Events.Any(specificationEvents.Contains));
            var localPlant = DeterministicFiniteAutomaton.ParallelComposition(localPlants);
            return DeterministicFiniteAutomaton.MonolithicSupervisor(new[] { localPlant }, new[] { specification });
        })
        .ToArray();

    isConflicting = DeterministicFiniteAutomaton.IsConflicting(supervisors);
    return supervisors;
}

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



record BenchmarkResult(
    string Implementation,
    int Iteration,
    TimeSpan MonolithicElapsed,
    long MonolithicAllocatedBytes,
    TimeSpan ModularElapsed,
    long ModularAllocatedBytes,
    long MonolithicStates,
    long[] ModularSupervisorStates,
    bool ModularConflicting);
