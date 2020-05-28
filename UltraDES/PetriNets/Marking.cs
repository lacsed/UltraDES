// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-22-2020

using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES.PetriNets
{
    /// <summary>
    /// Class Marking.
    /// Implements the <see cref="System.IEquatable{UltraDES.PetriNets.Marking}" />
    /// </summary>
    /// <seealso cref="System.IEquatable{UltraDES.PetriNets.Marking}" />
    public class Marking:IEquatable<Marking>
    {
        /// <summary>
        /// The dic
        /// </summary>
        private readonly Dictionary<Place, uint?> _dic;

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public IEnumerable<(Place place, uint? val)> Values => _dic.Select(kvp => (kvp.Key, kvp.Value));

        /// <summary>
        /// Initializes a new instance of the <see cref="Marking"/> class.
        /// </summary>
        /// <param name="marking">The marking.</param>
        public Marking(IEnumerable<(Place place, uint val)> marking)
        {
            _dic = marking.ToDictionary(m => m.place, m => (uint?)m.val);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Marking"/> class.
        /// </summary>
        /// <param name="marking">The marking.</param>
        public Marking(IEnumerable<(Place place, uint? val)> marking)
        {
            _dic = marking.ToDictionary(m => m.place, m => m.val);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Marking"/> class.
        /// </summary>
        /// <param name="dic">The dic.</param>
        private Marking(Dictionary<Place, uint?> dic)
        {
            _dic = dic;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
        public bool Equals(Marking other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return !_dic.Except(other._dic).Any();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Marking) obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return (_dic != null ? _dic.GetHashCode() : 0);
        }

        /// <summary>
        /// Gets the <see cref="System.Nullable{System.UInt32}"/> with the specified p.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>System.Nullable&lt;System.UInt32&gt;.</returns>
        public uint? this[Place p] => _dic.ContainsKey(p) ? _dic[p] : 0u;

        /// <summary>
        /// Updates the specified p.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="value">The value.</param>
        /// <returns>Marking.</returns>
        public Marking Update(Place p, uint? value)
        {
            var dic = new Dictionary<Place, uint?>(_dic);
            if (dic.ContainsKey(p)) dic[p] = value;
            else dic.Add(p, value);

            return new Marking(dic);
        }

        /// <summary>
        /// Implements the == operator.
        /// </summary>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(Marking t1, Marking t2) => ReferenceEquals(t1, t2) || (!ReferenceEquals(t1, null) && t1.Equals(t2));
        /// <summary>
        /// Implements the != operator.
        /// </summary>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(Marking t1, Marking t2) => !(t1 == t2);

        /// <summary>
        /// Implements the &lt; operator.
        /// </summary>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="Exception">The markings must have the same places</exception>
        public static bool operator <(Marking t1, Marking t2)
        {
            if(t1._dic.Keys.Except(t2._dic.Keys).Any() || t2._dic.Keys.Except(t1._dic.Keys).Any())
                throw new Exception("The markings must have the same places");

            return t1._dic.Keys.All(p => t1[p] != null && (t2[p] == null || t1[p] < t2[p]));
        }

        /// <summary>
        /// Implements the &lt;= operator.
        /// </summary>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="Exception">The markings must have the same places</exception>
        public static bool operator <=(Marking t1, Marking t2)
        {
            if(t1._dic.Keys.Except(t2._dic.Keys).Any() || t2._dic.Keys.Except(t1._dic.Keys).Any())
                throw new Exception("The markings must have the same places");

            return t1._dic.Keys.All(p => t1[p] != null && (t2[p] == null || t1[p] <= t2[p]));
        }

        /// <summary>
        /// Implements the &gt;= operator.
        /// </summary>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="Exception">The markings must have the same places</exception>
        public static bool operator >=(Marking t1, Marking t2)
        {
            if (t1._dic.Keys.Except(t2._dic.Keys).Any() || t2._dic.Keys.Except(t1._dic.Keys).Any())
                throw new Exception("The markings must have the same places");

            return t1._dic.Keys.All(p => t2[p] != null && (t1[p] == null || t1[p] >= t2[p]));
        }

        /// <summary>
        /// Implements the &gt; operator.
        /// </summary>
        /// <param name="t1">The t1.</param>
        /// <param name="t2">The t2.</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="Exception">The markings must have the same places</exception>
        public static bool operator >(Marking t1, Marking t2)
        {
            if (t1._dic.Keys.Except(t2._dic.Keys).Any() || t2._dic.Keys.Except(t1._dic.Keys).Any())
                throw new Exception("The markings must have the same places");

            return t1._dic.Keys.All(p => t2[p] != null && (t1[p] == null || t1[p] > t2[p]));
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() => _dic.Aggregate("", (current, kvp) => current + $"{kvp.Key}: {(kvp.Value==null? "ω" : kvp.Value.ToString())} | ").Trim(' ', '|');
    }


}
