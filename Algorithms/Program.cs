using System.Linq;
using UltraDES;

namespace Algorithms
{
    class Program
    {
        static void Main(string[] args)
        {
            ObserverPropertyVerifyExample();
            ObserverPropertySearchExample();
        }

       
        private static void ObserverPropertySearchExample()
        {

            var states = Enumerable.Range(0, 7).Select(n => new State($"{n}", n == 4 ? Marking.Marked : Marking.Unmarked))
                .ToArray();
            var events =
                (new[] {"gamma", "beta", "lambda", "omega", "y"}).ToDictionary(n => n,
                    n => new Event(n, Controllability.Controllable));

            var G = new DeterministicFiniteAutomaton(new Transition[]
            {
                (states[0], events["lambda"], states[1]),
                (states[1], events["lambda"], states[1]),
                (states[1], events["beta"], states[2]),
                (states[2], events["gamma"], states[3]),
                (states[3], events["gamma"], states[1]),
                (states[3], events["lambda"], states[4])
            }, states[0], "G");

            G.showAutomaton("G");

            var Vg = G.ObserverPropertySearch(new[] {events["lambda"]});
            Vg.showAutomaton();
        }

        private static void ObserverPropertyVerifyExample()
        {
            var states = Enumerable.Range(0, 7)
                .Select(n => new State($"{n}", n == 4 ? Marking.Marked : Marking.Unmarked)).ToArray();
            var events =
                (new[] {"a", "b", "x", "z", "y"}).ToDictionary(n => n, n => new Event(n, Controllability.Controllable));

            var G = new DeterministicFiniteAutomaton(new Transition[]
            {
                (states[0], events["a"], states[1]),
                (states[1], events["a"], states[1]),
                (states[1], events["x"], states[2]),
                (states[2], events["y"], states[3]),
                (states[3], events["y"], states[1]),
                (states[3], events["a"], states[4])
            }, states[0], "G");


            G.showAutomaton("G");

            G.ObserverPropertyVerify(new[] {events["a"]}, out var Vg, false);
            Vg.showAutomaton();

        }

    }
}
