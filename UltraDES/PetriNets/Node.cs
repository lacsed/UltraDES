using System;
using System.Collections.Generic;
using System.Text;

namespace UltraDES.PetriNets
{
    public abstract class Node
    {
        protected readonly string _alias;

        protected Node(string alias)
        {
            _alias = alias;
        }
    }
}
