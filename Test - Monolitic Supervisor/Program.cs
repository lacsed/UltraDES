/*****************************************************************************************
 *   UltraDES is an open source library for modeling, analisys and control of Discrete 
 *   Event Systems,it has been developed at LACSED|UFMG (http://www.lacsed.eng.ufmg.br)
 *   More informations and download at https://github.com/lucasvra/UltraDES
 *****************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UltraDES;

namespace Monolithic
{
    internal class Program
    {
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

            int max = clusters;

            var evs = Enumerable.Range(1, max).SelectMany(i => Enumerable.Range(0, 9).Select(
                k => new Event(String.Format("{0}|{1}", i, k),
                    k % 2 == 0
                        ? Controllability.Uncontrollable
                        : Controllability.Controllable))).ToList();

            for (int i = 1; i <= max; i++)
            {
                var e = Enumerable.Range(0, 9).Select(
                    k => new Event(String.Format("{0}|{1}", i, k),
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
                        s[0], String.Format("R{0}", i));

                var Ci = new DeterministicFiniteAutomaton(new[]
                {
                    new Transition(s[0], e[7], s[1]),
                    new Transition(s[1], e[8], s[0]),
                },
                s[0], String.Format("C{0}", i));

                var Ei = new DeterministicFiniteAutomaton(new[]
                {
                    new Transition(s[0], e[2], s[1]),
                    new Transition(s[1], e[7], s[0]),
                    new Transition(s[0], e[8], s[2]),
                    new Transition(s[2], e[5], s[0])
                },
                s[0], String.Format("E{0}", i));

                plants.Add(Ri);
                plants.Add(Ci);
                specs.Add(Ei);
            }

            for (int i = 1; i < max; i++)
            {
                var e61 = new Event(String.Format("{0}|6", i),
                    Controllability.Uncontrollable);
                var e31 = new Event(String.Format("{0}|3", i),
                    Controllability.Controllable);
                var e12 = new Event(String.Format("{0}|1", i + 1),
                    Controllability.Controllable);
                var e42 = new Event(String.Format("{0}|4", i + 1),
                    Controllability.Uncontrollable);

                var Eij = new DeterministicFiniteAutomaton(new[]
                {
                    new Transition(s[0], e61, s[1]),
                    new Transition(s[1], e12, s[0]),
                    new Transition(s[0], e42, s[2]),
                    new Transition(s[2], e31, s[0])
                },
                s[0], String.Format("E{0}_{1}", i, i + 1));

                specs.Add(Eij);
            }
        }

        private static void FSM(out List<DeterministicFiniteAutomaton> plants, out List<DeterministicFiniteAutomaton> specs)
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
            specs = new[] { e1, e2, e3, e4, e5, e6, e7, e8 }.ToList();
        }

        private static void Main()
        {

            List<DeterministicFiniteAutomaton> plants;
            List<DeterministicFiniteAutomaton> specs;

            //ClusterTool(3, out plants, out specs);
            FSM(out plants, out specs);

            Console.WriteLine("Supervisor:");
            var timer = new Stopwatch();
            timer.Start();
            var sup = DeterministicFiniteAutomaton.MonolithicSupervisor(plants, specs, true);
            timer.Stop();
            Console.WriteLine("\tStates: {0}", sup.Size);
            Console.WriteLine("\tTransitions: {0}", sup.Transitions.Count());
            Console.WriteLine("\tComputation Time: {0}", timer.ElapsedMilliseconds / 1000.0);

            Console.WriteLine("\nSupervisor Projection (Removing first and last event):");
            timer.Restart();
            var proj = sup.Projection(new[] { sup.Events.First(), sup.Events.Last() });
            timer.Stop();
            Console.WriteLine("\tStates: {0}", proj.States.Count()); // proj.States.Count() == proj.Size
            Console.WriteLine("\tTransitions: {0}", proj.Transitions.Count());
            Console.WriteLine("\tComputation Time: {0}", timer.ElapsedMilliseconds / 1000.0);

            Console.ReadLine();
        }
    }
}
