using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES.PetriNets
{
    public class Marking:IEquatable<Marking>
    {
        private readonly Dictionary<Place, uint?> _dic;

        public IEnumerable<(Place place, uint? val)> Values => _dic.Select(kvp => (kvp.Key, kvp.Value));

        public Marking(IEnumerable<(Place place, uint val)> marking)
        {
            _dic = marking.ToDictionary(m => m.place, m => (uint?)m.val);
        }

        public Marking(IEnumerable<(Place place, uint? val)> marking)
        {
            _dic = marking.ToDictionary(m => m.place, m => m.val);
        }
        private Marking(Dictionary<Place, uint?> dic)
        {
            _dic = dic;
        }

        public bool Equals(Marking other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return !_dic.Except(other._dic).Any();
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Marking) obj);
        }

        public override int GetHashCode()
        {
            return (_dic != null ? _dic.GetHashCode() : 0);
        }

        public uint? this[Place p] => _dic.ContainsKey(p) ? _dic[p] : 0u;

        public Marking Update(Place p, uint? value)
        {
            var dic = new Dictionary<Place, uint?>(_dic);
            if (dic.ContainsKey(p)) dic[p] = value;
            else dic.Add(p, value);

            return new Marking(dic);
        }

        public static bool operator ==(Marking t1, Marking t2) => ReferenceEquals(t1, t2) || (!ReferenceEquals(t1, null) && t1.Equals(t2));
        public static bool operator !=(Marking t1, Marking t2) => !(t1 == t2);

        public static bool operator <(Marking t1, Marking t2)
        {
            if(t1._dic.Keys.Except(t2._dic.Keys).Any() || t2._dic.Keys.Except(t1._dic.Keys).Any())
                throw new Exception("The markings must have the same places");

            return t1._dic.Keys.All(p => t1[p] != null && (t2[p] == null || t1[p] < t2[p]));
        }

        public static bool operator <=(Marking t1, Marking t2)
        {
            if(t1._dic.Keys.Except(t2._dic.Keys).Any() || t2._dic.Keys.Except(t1._dic.Keys).Any())
                throw new Exception("The markings must have the same places");

            return t1._dic.Keys.All(p => t1[p] != null && (t2[p] == null || t1[p] <= t2[p]));
        }

        public static bool operator >=(Marking t1, Marking t2)
        {
            if (t1._dic.Keys.Except(t2._dic.Keys).Any() || t2._dic.Keys.Except(t1._dic.Keys).Any())
                throw new Exception("The markings must have the same places");

            return t1._dic.Keys.All(p => t2[p] != null && (t1[p] == null || t1[p] >= t2[p]));
        }

        public static bool operator >(Marking t1, Marking t2)
        {
            if (t1._dic.Keys.Except(t2._dic.Keys).Any() || t2._dic.Keys.Except(t1._dic.Keys).Any())
                throw new Exception("The markings must have the same places");

            return t1._dic.Keys.All(p => t2[p] != null && (t1[p] == null || t1[p] > t2[p]));
        }

        public override string ToString() => _dic.Aggregate("", (current, kvp) => current + $"{kvp.Key}: {(kvp.Value==null? "ω" : kvp.Value.ToString())} | ").Trim(' ', '|');
    }


}
