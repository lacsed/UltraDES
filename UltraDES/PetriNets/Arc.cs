using System;
using System.Collections.Generic;
using System.Text;

namespace UltraDES.PetriNets
{
    public class Arc
    {
        public Arc(Node n1, Node n2, uint weight = 1)
        {
            N1 = n1;
            N2 = n2;
            Weight = weight;
        }

        public Node N1 { get; }
        public Node N2 { get; }
        public uint Weight { get; }

        public static implicit operator (Node n1, Node n2, uint weight)(Arc t) => (t.N1, t.N2, t.Weight);

        public void Deconstruct(out Node n1, out Node n2, out uint weight)
        {
            n1 = N1;
            n2 = N2;
            weight = Weight;
        }
    }
}
