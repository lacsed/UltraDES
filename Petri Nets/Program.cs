using System;
using UltraDES;
using UltraDES.PetriNets;
using Marking = UltraDES.PetriNets.Marking;
using Transition = UltraDES.PetriNets.Transition;

namespace Petri_Nets
{
    class Program
    {
        static void Main(string[] args)
        {
            var p1 = new Place("p1");
            var p2 = new Place("p2");
            var p3 = new Place("p3");
            var p4 = new Place("p4");

            var t1 = new Transition("t1");
            var t2 = new Transition("t2");
            var t3 = new Transition("t3");
            var t4 = new Transition("t4");

            var P = new PetriNet(new (Node, Node, uint)[]
            {
                (t1, p2, 1u),
                (t2, p3, 1u),
                (t3, p3, 1u),
                (t4, p2, 2u),
                (p1, t1, 1u),
                (p1, t2, 1u),
                (p2, t3, 1u),
                (p3, t4, 1u)
            }, "P");

            var m = new Marking(new[]
            {
                (p1, 1u),
                (p2, 0u),
                (p3, 0u),
            });
            
            P.ShowPetriNet("net1");
            P.CoverabilityGraph(m).ShowGraph("coverability");
        }
    }
}
