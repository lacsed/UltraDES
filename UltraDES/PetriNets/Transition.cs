using System;

namespace UltraDES.PetriNets
{
    [Serializable]
    public class Transition: Node, IEquatable<Transition>
    {


        public Transition(string alias):base(alias)
        { }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Transition) obj);
        }

        public bool Equals(Transition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _alias == other._alias;
        }

        public static bool operator ==(Transition t1, Transition t2) => ReferenceEquals(t1, t2) || (!ReferenceEquals(t1, null) && t1.Equals(t2));
        public static bool operator !=(Transition t1, Transition t2) => !(t1 == t2);

        public override int GetHashCode()
        {
            return (_alias != null ? _alias.GetHashCode() : 0);
        }

        public override string ToString() => _alias;

    }


}
