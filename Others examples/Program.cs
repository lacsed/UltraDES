using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UltraDES;

namespace Examples
{
    class Program
    {
        private static void FSM(out List<DeterministicFiniteAutomaton> plants, out List<DeterministicFiniteAutomaton> specs)
        {
            var s = new List<State>(); // or State[] s = new State[6];
            for (int i = 0; i < 6; i++)
            {
                if (i == 0) s.Add(new State(i.ToString(), Marking.Marked));
                else        s.Add(new State(i.ToString(), Marking.Unmarked));
            }

            // Creating Events (0 to 100)
            var e = new List<Event>(); // or Event[] e = new Event[100];
            for (var i = 0; i < 100; ++i)
            {
                if (i % 2 != 0)
                {
                    e.Add(new Event(String.Format("e{0}", i), Controllability.Controllable));
                }
                else
                {
                    e.Add(new Event(String.Format("e{0}", i), Controllability.Uncontrollable));
                }
            }

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

        private static void IteratingOverProperties()
        {
            List<DeterministicFiniteAutomaton> plants, specs;
            FSM(out plants, out specs);

            var G = plants.Where(s => s.Name == "Robot").First();

            Console.WriteLine("\nAutomaton: {0}", G.ToString()); // or G.Name

            Console.WriteLine("\nStates: {0}", G.States.Count());
            // Or: Console.WriteLine("States: {0}", G.Size);
            foreach (var s in G.States) // iterates over all states
            {
                Console.WriteLine("\tState: {0}", s.ToString()); // prints state's name.
            }

            Console.WriteLine("\nEvents: {0}", G.Events.Count());
            foreach (var e in G.Events) // iterates over all event
            {
                if (e.IsControllable)
                {
                    Console.WriteLine("\tEvent: {0} (controllable)", e.ToString()); // prints event's name.
                }
                else
                {
                    Console.WriteLine("\tEvent: {0} (uncontrollable)", e.ToString()); // prints event's name.
                }
            }

            Console.WriteLine("\nTransitions: {0}", G.Transitions.Count());

            foreach (var t in G.Transitions) // iterates over all transictions
            {
                // you can use t.Origin, t.Trigger and t.Destination to get information about the transiction
                Console.WriteLine("\tTransition: {0}", t.ToString()); // prints '{Origin} --{event}-> {Destination}'
            }

            Console.WriteLine("\nUncontrollable Events: {0}", G.UncontrollableEvents.Count());
            foreach (var e in G.UncontrollableEvents)
            {
                Console.WriteLine("\tUncontrollable event: {0}", e.ToString());
            }

            Console.WriteLine("\nMarked States: {0}", G.MarkedStates.Count());
            foreach (var s in G.MarkedStates)
            {
                Console.WriteLine("\tMarked State: {0}", s.ToString());
            }

            // if you need access some random position you can not use '[]' (Ex: G.Events[i])
            // To do this, you need use 'ElementAt' (Ex: G.Events.ElementAt(i)).
            // But if you need repeat this operation several times your code will be slow. 
            // To acelerate your program, you can store the information (states, events, etc) in a variable, 
            // You can do like this:
            var transitionsList = G.Transitions.ToList();
            var transitionsArray = G.Transitions.ToArray();
            // and then get the random positions: transitionsList[i] or transitionsArray[i]

            // Notice: store a list or array will use more memory, 
            //         therefore do this only if 'foreach' does not meet your need.

        }

        private static void Properties()
        {
            List<DeterministicFiniteAutomaton> plants, specs;
            FSM(out plants, out specs);

            var G = plants.Where(s => s.Name == "Robot").First();

            Console.WriteLine("Automaton: {0}", G.ToString()); // or G.Name

            Console.WriteLine("\tInitial state: {0}", G.InitialState.ToString());

            Console.WriteLine("\tAccessible Part: {0} states", G.AccessiblePart.Size);
            Console.WriteLine("\tCoaccessible Part: {0} states", G.CoaccessiblePart.Size);
            Console.WriteLine("\tTrim: {0} states", G.Trim.Size);

            Console.WriteLine("\tMinimal: {0} states", G.Minimal.Size);
            Console.WriteLine("\tPrefix Closure: {0} states", G.PrefixClosure.Size);
        }

        private static void Methods()
        {
            List<DeterministicFiniteAutomaton> plants, specs;
            FSM(out plants, out specs);

            Console.WriteLine("\nFSM\n");

            var Plant = DeterministicFiniteAutomaton.ParallelComposition(plants);
            var Specification = DeterministicFiniteAutomaton.ParallelComposition(specs);
            var K = Plant.ParallelCompositionWith(Specification);

            Console.WriteLine("\tPlant: {0} states", Plant.Size);
            Console.WriteLine("\tSpecification: {0} states", Specification.Size);
            Console.WriteLine("\tK: {0} states", K.Size);

            // Computes the supervisor using the global plant and specification
            var S = DeterministicFiniteAutomaton.MonolithicSupervisor(
                        new[] { Plant }, // global plant
                        new[] { Specification }, // global specification
                        true
                    );

            Console.WriteLine("\tSupervisor: {0} states", S.Size);

            // Computes the supervisor using all plants and specifications.
            S = DeterministicFiniteAutomaton.MonolithicSupervisor(plants, specs, true);
            Console.WriteLine("\tSupervisor (method 2): {0} states", S.Size);

            var proj = S.Projection(S.UncontrollableEvents);
            Console.WriteLine("\tProjection: {0} states", proj.Size);

            Console.WriteLine("------------------------------------------------------");

            var P = Plant.ProductWith(Specification);
            Console.WriteLine("\tProduct 1: {0} state{1}", P.Size, P.Size > 1 ? "s" : "");
            P = DeterministicFiniteAutomaton.Product(plants);
            Console.WriteLine("\tProduct 2: {0} state{1}", P.Size, P.Size > 1 ? "s" : "");
            P = Plant.ProductWith(specs);
            Console.WriteLine("\tProduct 3: {0} state{1}", P.Size, P.Size > 1 ? "s" : "");
        }

        private static void HandlingFiles()
        {
            List<DeterministicFiniteAutomaton> plants, specs;
            FSM(out plants, out specs);

            var robot = plants.Where(s => s.Name == "Robot").First();

            var Plant = DeterministicFiniteAutomaton.ParallelComposition(plants);
            var Specification = DeterministicFiniteAutomaton.ParallelComposition(specs);

            // Exporting a automaton to a ADS file (TCT)
            robot.ToAdsFile("ROBOT.ADS");

            DeterministicFiniteAutomaton.ToAdsFile(
                    new[] { Plant, Specification },
                    new[] { "G.ADS", "E.ADS" }
            );

            // Notice: Export all automata calling the method 'ToAdsFile' just once.
            // This is necessary to generate unique number for each event.

            // Plant.ToAdsFile("G.ADS");
            // Specification.ToAdsFile("E.ADS");

            // The code above can generate unexpected results because differents events can receive 
            // a same number when exported to ADS files.

            // -------------------------------------------------------------

            // Reading a automaton from a ADS file
            robot = DeterministicFiniteAutomaton.FromAdsFile("ROBOT.ADS");

            // Exporting to a WMod File (Supremica)
            DeterministicFiniteAutomaton.ToWmodFile("FSM.wmod", plants, specs);

            // Importing plants and specifications from WMod file
            // (Will be saved in plants and specs)
            DeterministicFiniteAutomaton.FromWmodFile("FSM.wmod", out plants, out specs);

            // Exporting to a XML File
            robot.ToXMLFile("robot.xml");

            // Building a automaton from a xml file
            robot = DeterministicFiniteAutomaton.FromXMLFile("robot.xml");

            // Serializes the automaton to the file (stores in the file (binary mode)).
            Plant.SerializeAutomaton("Plant.bin");

            Plant = DeterministicFiniteAutomaton.DeserializeAutomaton("Plant.bin");

            // If you wish view some automaton you can use:
            plants.ForEach(g => g.drawSVGFigure());
        }

        static void Main(string[] args)
        {
            Properties();
            IteratingOverProperties();
            Methods();
            HandlingFiles();

            Console.WriteLine("\n\nProgram finished.");
            Console.ReadLine();
        }
    }
}
