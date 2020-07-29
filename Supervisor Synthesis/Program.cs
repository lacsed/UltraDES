using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UltraDES;

namespace SupervisorSynthesis
{
    class Program
    {
        // Creates the plants and specifications of ITL problem. 
        // The automata are saved in 'plants' and 'specs'
        private static void ITL(out List<DeterministicFiniteAutomaton> plants, out List<DeterministicFiniteAutomaton> specs)
        {
            var s =
                Enumerable.Range(0, 6)
                    .Select(i =>
                        new State(i.ToString(),
                            i == 0
                                ? Marking.Marked
                                : Marking.Unmarked)
                    ).ToArray();


            var e =
               Enumerable.Range(0, 100)
                   .Select(i =>
                       new Event(i.ToString(),
                           i % 2 != 0
                               ? Controllability.Controllable
                               : Controllability.Uncontrollable)
                   ).ToArray();

            //plants

            //M1
            var M1 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[1], s[1]),
                    new Transition(s[1], e[2], s[0])
                },
                s[0], "M1");

            var M2 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[3], s[1]),
                    new Transition(s[1], e[4], s[0])
                },
                s[0], "M2");

            var M3 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[5], s[1]),
                    new Transition(s[1], e[6], s[0])
                },
                s[0], "M3");

            var M4 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[7], s[1]),
                    new Transition(s[1], e[8], s[0])
                },
                s[0], "M4");

            var M5 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[9], s[1]),
                    new Transition(s[1], e[10], s[0])
                },
                s[0], "M5");

            var M6 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[11], s[1]),
                    new Transition(s[1], e[12], s[0])
                },
                s[0], "M6");

            //Specifications

            var e1 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[2], s[1]),
                    new Transition(s[1], e[3], s[0])
                },
                s[0], "E1");

            var e2 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[6], s[1]),
                    new Transition(s[1], e[7], s[0])
                },
                s[0], "E2");

            var e3 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[4], s[1]),
                    new Transition(s[0], e[8], s[1]),
                    new Transition(s[1], e[9], s[0])
                },
                s[0], "E3");

            var e4 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[10], s[1]),
                    new Transition(s[1], e[11], s[0])
                },
                s[0], "E4");

            plants = new[] { M1, M2, M3, M4, M5, M6 }.ToList();
            specs = new[] { e1, e2, e3, e4 }.ToList();
        }

        // Creates the plants and specifications of Cluster Tools problem. 
        // You can choose how many clusters to use.
        // The automata are saved in 'plants' and 'specs'
        private static void ClusterTool(int clusters, out List<DeterministicFiniteAutomaton> plants, out List<DeterministicFiniteAutomaton> specs)
        {
            var s = Enumerable.Range(0, 4).Select(
                k => new State(k.ToString(),
                    k == 0
                        ? Marking.Marked
                        : Marking.Unmarked))
                .ToArray();

            plants = new List<DeterministicFiniteAutomaton>();
            specs = new List<DeterministicFiniteAutomaton>();

            var max = clusters;

            var evs = Enumerable.Range(1, max).SelectMany(i => Enumerable.Range(0, 9).Select(
                k => new Event($"{i}|{k}",
                    k % 2 == 0
                        ? Controllability.Uncontrollable
                        : Controllability.Controllable))).ToList();

            for (var i = 1; i <= max; i++)
            {
                var e = Enumerable.Range(0, 9).Select(
                    k => new Event($"{i}|{k}",
                        k % 2 == 0
                            ? Controllability.Uncontrollable
                            : Controllability.Controllable))
                    .ToArray();

                var Ri = new DeterministicFiniteAutomaton(
                    i != max
                        ? new[]
                        {
                            new Transition(s[0], e[1], s[1]),
                            new Transition(s[1], e[2], s[0]),
                            new Transition(s[0], e[3], s[2]),
                            new Transition(s[2], e[4], s[0]),
                            new Transition(s[0], e[5], s[3]),
                            new Transition(s[3], e[6], s[0])
                        }
                        : new[]
                        {
                            new Transition(s[0], e[1], s[1]),
                            new Transition(s[1], e[2], s[0]),
                            new Transition(s[0], e[5], s[2]),
                            new Transition(s[2], e[4], s[0]),
                        },
                        s[0], $"R{i}");

                var Ci = new DeterministicFiniteAutomaton(new[]
                {
                    new Transition(s[0], e[7], s[1]),
                    new Transition(s[1], e[8], s[0]),
                },
                s[0], $"C{i}");

                var Ei = new DeterministicFiniteAutomaton(new[]
                {
                    new Transition(s[0], e[2], s[1]),
                    new Transition(s[1], e[7], s[0]),
                    new Transition(s[0], e[8], s[2]),
                    new Transition(s[2], e[5], s[0])
                },
                s[0], $"E{i}");

                plants.Add(Ri);
                plants.Add(Ci);
                specs.Add(Ei);
            }

            for (var i = 1; i < max; i++)
            {
                var e61 = new Event($"{i}|6",
                    Controllability.Uncontrollable);
                var e31 = new Event($"{i}|3",
                    Controllability.Controllable);
                var e12 = new Event($"{i + 1}|1",
                    Controllability.Controllable);
                var e42 = new Event($"{i + 1}|4",
                    Controllability.Uncontrollable);

                var Eij = new DeterministicFiniteAutomaton(new[]
                {
                    new Transition(s[0], e61, s[1]),
                    new Transition(s[1], e12, s[0]),
                    new Transition(s[0], e42, s[2]),
                    new Transition(s[2], e31, s[0])
                },
                s[0], $"E{i}_{i + 1}");

                specs.Add(Eij);
            }
        }

        // Creates the plants and specifications of FMS problem. 
        // The automata are saved in 'plants' and 'specs'
        private static void FMS(out List<DeterministicFiniteAutomaton> plants, out List<DeterministicFiniteAutomaton> specs, bool conflicting = false)
        {
            var s =
                Enumerable.Range(0, 6)
                    .Select(i =>
                        new State(i.ToString(),
                            i == 0
                                ? Marking.Marked
                                : Marking.Unmarked)
                    ).ToArray();

            // Creating Events (0 to 100)
            var e =
                Enumerable.Range(0, 100)
                    .Select(i =>
                        new Event(i.ToString(),
                            i % 2 != 0
                                ? Controllability.Controllable
                                : Controllability.Uncontrollable)
                    ).ToArray();

            //----------------------------
            // Plants
            //----------------------------


            // C1
            var c1 = new DeterministicFiniteAutomaton(
                new[]
                {
                    new Transition(s[0], e[11], s[1]),
                    new Transition(s[1], e[12], s[0])
                },
                s[0], "C1");

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
            specs = conflicting
                ? new[] {e1, e2, e3, e4, e5, e6, e7, e8}.ToList()
                : new[] {e1, e2, e3, e4, e5, e6, e7.ParallelCompositionWith(e8)}.ToList();
        }
        static void Main(string[] args)
        {
            ComputingMonolithicSupervisor();

            ComputingModularSupervisor();

            ComputingModularReducedSupervisor();

            // this is used to prevent the program from closing immediately
            Console.ReadLine();
        }

        private static void ComputingMonolithicSupervisor()
        {
            // Choose one option below
            ClusterTool(4, out var plants, out var specs);

            Console.WriteLine("Monolithic Supervisor:");
            var timer = new Stopwatch(); // to measure time
            timer.Start();
            // computes the monolithic supervisor and stores the resulting automaton in 'sup'
            var sup = DeterministicFiniteAutomaton.MonolithicSupervisor(plants, specs, true);
            timer.Stop();

            // shows information about supervisor and the elapsed time
            Console.WriteLine($"\tStates: {sup.Size}");
            Console.WriteLine($"\tTransitions: {sup.Transitions.Count()}");
            Console.WriteLine($"\tComputation Time: {timer.ElapsedMilliseconds / 1000.0}");
        }

        private static void ComputingModularSupervisor()
        {
            // Choose one option below
            FMS(out var plants, out var specs);

            Console.WriteLine("Modular Supervisor:");
            var timer = new Stopwatch(); // to measure time
            timer.Start();
            // computes the monolithic supervisor and stores the resulting automaton in 'sup'
            var sups = DeterministicFiniteAutomaton.LocalModularSupervisor(plants, specs);
            timer.Stop();

            foreach (var s in sups)
            {
                Console.WriteLine($"\tSupervisor: {s}");
                Console.WriteLine($"\t-States: {s.Size}");
                Console.WriteLine($"\t-Transitions: {s.Transitions.Count()}");
            }
            Console.WriteLine($"\tComputation Time: {timer.ElapsedMilliseconds / 1000.0}");

        }

        private static void ComputingModularReducedSupervisor()
        {
            // Choose one option below
            FMS(out var plants, out var specs);

            Console.WriteLine("Modular Reduced Supervisor:");
            var timer = new Stopwatch(); // to measure time
            timer.Start();
            // computes the monolithic supervisor and stores the resulting automaton in 'sup'
            var sups = DeterministicFiniteAutomaton.LocalModularReducedSupervisor(plants, specs);
            timer.Stop();

            foreach (var s in sups)
            {
                Console.WriteLine($"\tSupervisor: {s}");
                Console.WriteLine($"\t-States: {s.Size}");
                Console.WriteLine($"\t-Transitions: {s.Transitions.Count()}");
            }
            Console.WriteLine($"\tComputation Time: {timer.ElapsedMilliseconds / 1000.0}");

        }
    }
}
