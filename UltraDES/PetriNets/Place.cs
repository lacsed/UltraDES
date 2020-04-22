using System;

namespace UltraDES.PetriNets
{
    [Serializable]
    public class Place : Node, IEquatable<Place>
    {
        public Place(string alias):base(alias)
        { }
        public static bool operator ==(Place t1, Place t2) => ReferenceEquals(t1, t2) || (!ReferenceEquals(t1, null) && t1.Equals(t2));
        public static bool operator !=(Place t1, Place t2) => !(t1 == t2);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Place) obj);
        }
        public override int GetHashCode()
        {
            return (_alias != null ? _alias.GetHashCode() : 0);
        }

        public override string ToString() => _alias;

        public bool Equals(Place other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _alias == other._alias;
        }
    }
}
