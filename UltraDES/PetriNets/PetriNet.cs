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
using Weight = System.Collections.Generic.Dictionary<UltraDES.PetriNets.Node, System.Collections.Generic.Dictionary<UltraDES.PetriNets.Node, uint>>;

namespace UltraDES.PetriNets
{
    /// <summary>
    /// Class PetriNet.
    /// </summary>
    public class PetriNet
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// The places
        /// </summary>
        private readonly HashSet<Place> _places = new HashSet<Place>();
        /// <summary>
        /// The transitions
        /// </summary>
        private readonly HashSet<Transition> _transitions = new HashSet<Transition>();
        /// <summary>
        /// The weights
        /// </summary>
        private readonly Weight _weights = new Dictionary<Node, Dictionary<Node, uint>>();

        /// <summary>
        /// Gets the places.
        /// </summary>
        /// <value>The places.</value>
        public IEnumerable<Place> Places => _places;
        /// <summary>
        /// Gets the transitions.
        /// </summary>
        /// <value>The transitions.</value>
        public IEnumerable<Transition> Transitions => _transitions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PetriNet"/> class.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="outputs">The outputs.</param>
        /// <param name="name">The name.</param>
        public PetriNet(IEnumerable<(Place p, Transition t, uint weight)> inputs, IEnumerable<(Place p, Transition t, uint weight)> outputs, string name)
        :this(inputs.Select(i => ((Node)i.p, (Node)i.t, i.weight)).Union(outputs.Select(o => ((Node)o.t,(Node)o.p,o.weight ))), name)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PetriNet"/> class.
        /// </summary>
        /// <param name="arcs">The arcs.</param>
        /// <param name="name">The name.</param>
        public PetriNet(IEnumerable<(Node x, Node y, uint weight)> arcs, string name)
        {
            Name = name;
            foreach (var (x, y,w) in arcs)
            {
                if (x is Place)
                {
                    _places.Add(x as Place);
                    if(!(y is Transition)) throw new Exception("An arc should be composed of a Place and a Transition");
                    _transitions.Add(y as Transition);
                }
                else
                {
                    _transitions.Add(x as Transition);
                    if (!(y is Place)) throw new Exception("An arc should be composed of a Place and a Transition");
                    _places.Add(y as Place);
                }

                if (!_weights.ContainsKey(x)) _weights.Add(x, new Dictionary<Node, uint>(1));
                _weights[x].Add(y, w);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PetriNet"/> class.
        /// </summary>
        /// <param name="arcs">The arcs.</param>
        /// <param name="name">The name.</param>
        public PetriNet(IEnumerable<Arc> arcs, string name) : this(arcs.Select(a => (a.N1, a.N2, a.Weight)), name) { }

        public PetriNet(IEnumerable<(Node x, Node y)> arcs, string name)
        {
            Name = name;
            foreach (var (x, y) in arcs)
            {
                var w = 0u;
                if (x is Place)
                {
                    _places.Add(x as Place);
                    if (!(y is Transition)) throw new Exception("An arc should be composed of a Place and a Transition");
                    _transitions.Add(y as Transition);
                }
                else
                {
                    _transitions.Add(x as Transition);
                    if (!(y is Place)) throw new Exception("An arc should be composed of a Place and a Transition");
                    _places.Add(y as Place);
                }

                if (!_weights.ContainsKey(x)) _weights.Add(x, new Dictionary<Node, uint>(1));
                _weights[x].Add(y, w);
            }
        }

        public IEnumerable<(Node x, Node y)> Arcs => _weights.SelectMany(kvp1 => kvp1.Value.Select(kvp2 => (kvp1.Key, kvp2.Key)));
        //public IEnumerable<(Node y, Node x)> InverseArcs => _weights.SelectMany(kvp1 => kvp1.Value.Select(kvp2 => (kvp2.Key, kvp1.Key)));
        public IEnumerable<Node> Inputs(Node b) => Arcs.Where(arc => arc.y == b && Weight(arc.x,arc.y)>0).Select(arc => arc.x);
        public IEnumerable<Node> Outputs(Node b) => Arcs.Where(arc => arc.x == b && Weight(arc.x, arc.y) > 0).Select(arc => arc.y);
        public IEnumerable<Transition> Inputs(Place p) => Inputs(p as Node).OfType<Transition>();
        public IEnumerable<Transition> Outputs(Place p) => Outputs(p as Node).OfType<Transition>();
        public IEnumerable<Place> Inputs(Transition p) => Inputs(p as Node).OfType<Place>();
        public IEnumerable<Place> Outputs(Transition p) => Outputs(p as Node).OfType<Place>();

        public uint Weight(Node x, Node y) => _weights.ContainsKey(x) ? _weights[x].ContainsKey(y) ? _weights[x][y] : 0u : 0u;
        public uint Input(Place p, Transition t) => Weight(p, t);
        public uint Output(Place p, Transition t) => Weight(t, p);
        public IEnumerable<Transition> EnabledTransitions(Marking m) => _transitions.Where(t => Inputs(t).All(p => m[p] >= _weights[p][t] || m[p] == null));

        public Marking Fire(Marking m, Transition t) =>
            Outputs(t).Aggregate(Inputs(t).Aggregate(m, (mark, p) => mark.Update(p, mark[p] - _weights[p][t])),
                (mark, p) => mark.Update(p, mark[p] + _weights[t][p]));
        public Marking Fire(Marking m, IEnumerable<Transition> transitions) => transitions.Aggregate(m, Fire);

        public bool IsSiphon(IEnumerable<Place> places)
        {
            var sInput = new HashSet<Transition>(places.SelectMany(Inputs));
            var sOutput = new HashSet<Transition>(places.SelectMany(Outputs));

            return sOutput.IsSubsetOf(sInput);
        }

        public bool IsTrap(IEnumerable<Place> places)
        {
            var sInput = new HashSet<Transition>(places.SelectMany(Inputs));
            var sOutput = new HashSet<Transition>(places.SelectMany(Outputs));

            return sInput.IsSubsetOf(sOutput);
        }

        public IEnumerable<(Marking o, Transition t, Marking d)> CoverabilityTree(Marking m0)
        {
            var v0 = new HashSet<(Place p, uint? v)>(m0.Values);
            var V = new HashSet<HashSet<(Place p, uint? v)>>(HashSet<(Place p, uint? v)>.CreateSetComparer()) { v0 };
            var A = new List<(HashSet<(Place p, uint? v)> o, Transition t, HashSet<(Place p, uint? v)> d)>();
            var mu = new Dictionary<HashSet<(Place p, uint? v)>, Marking>(HashSet<(Place p, uint? v)>.CreateSetComparer()) { { v0, m0 } };

            var unprocessed = new Queue<HashSet<(Place p, uint? v)>>();
            unprocessed.Enqueue(v0);

            while (unprocessed.Count > 0)
            {
                var v = unprocessed.Peek();
                if (V.Except(unprocessed).Any(u => mu[u] == mu[v]))
                {
                    unprocessed.Dequeue();
                    continue;
                }

                foreach (var t in EnabledTransitions(mu[v]))
                {
                    var M = Fire(mu[v], t);
                    var w = new HashSet<(Place p, uint? v)>(M.Values);
                    V.Add(w);
                    A.Add((v, t, w));
                    unprocessed.Enqueue(w);

                    mu[w] = M;

                    var aux = A.Select(a => (a.o, a.d)).VerticesBetween(v0, v, (i, j) => i.SetEquals(j)).Where(x => mu[x] <= M).ToList();
                    if (!aux.Any()) continue;


                    var u = aux.First();
                    foreach (var p in _places)
                    {
                        mu[w] = mu[w].Update(p,
                            mu[u][p] < M[p] || (mu[u][p] != null && M[p] == null) ? null : M[p]);
                    }

                }

                unprocessed.Dequeue();
            }

            return A.Select(a => (new Marking(a.o), a.t, new Marking(a.d))).ToList();

        }

        public IEnumerable<(Marking o, Transition t, Marking d)> CoverabilityGraph(Marking m0)
        {
            var v0 = new HashSet<(Place p, uint? v)>(m0.Values);
            var V = new HashSet<HashSet<(Place p, uint? v)>>(HashSet<(Place p, uint? v)>.CreateSetComparer()) { v0 };
            var A = new List<(HashSet<(Place p, uint? v)> o, Transition t, HashSet<(Place p, uint? v)> d)>();
            var mu = new Dictionary<HashSet<(Place p, uint? v)>, Marking>(HashSet<(Place p, uint? v)>.CreateSetComparer()) { { v0, m0 } };

            var unprocessed = new Queue<HashSet<(Place p, uint? v)>>();
            unprocessed.Enqueue(v0);

            while (unprocessed.Count > 0)
            {
                var v = unprocessed.Dequeue();

                foreach (var t in EnabledTransitions(mu[v]))
                {
                    var _M = Fire(mu[v], t);
                    Marking M = _M;

                    var aux = A.Select(a => (a.o, a.d)).VerticesBetween(v0, v, (i, j) => i.SetEquals(j)).Where(x => mu[x] <= _M).ToList();
                    if (aux.Any())
                    {
                        var u = aux.First();
                        foreach (var p in _places)
                        {
                            M = M.Update(p, mu[u][p] < _M[p] || (mu[u][p] != null && _M[p] == null) ? null : _M[p]);
                        }
                    }

                    aux = V.Where(_v => mu[_v] == M).ToList();
                    if (!aux.Any())
                    {
                        var w = new HashSet<(Place p, uint? v)>(_M.Values);
                        V.Add(w);
                        mu.Add(w, M);
                        unprocessed.Enqueue(w);
                        A.Add((v, t, w));
                    }
                    else
                    {
                        var w = aux.First();
                        A.Add((v, t, w));
                    }
                }
            }

            return A.Select(a => (new Marking(a.o), a.t, new Marking(a.d))).ToList();

        }

        public IEnumerable<(Marking o, Transition t , Marking d)> ReachabilityTree(Marking m0)
        {
            var aux = CoverabilityTree(m0);

            return aux.Any(a => a.o.Values.Any(v => v.val == null) || a.d.Values.Any(v => v.val == null)) ? null : aux;
        }

        public int[,] IncidenceMatrix(out Dictionary<Place,int> placeIdx, out Dictionary<Transition, int> transitionIdx)
        {
            var p = _places.ToArray();
            var t = _transitions.ToArray();

            placeIdx = Enumerable.Range(0, p.Length).ToDictionary(i => p[i], i => i);
            transitionIdx = Enumerable.Range(0, t.Length).ToDictionary(j => t[j], j => j);

            var m = new int[p.Length, t.Length];

            for (int i = 0; i < p.Length; i++)
            {
                for (int j = 0; j < t.Length; j++)
                {
                    m[i, j] = (int) Weight(p[i], t[j]) - (int) Weight(t[j], p[i]);
                }   
            }

            return m;
        }


        public string ToDotCode(Marking m = null)
        {
            if(m == null) m = new Marking(_places.Select(p => (p, 0u)));

            var dot = @"digraph 
{
    
forcelabels = TRUE;
splines = FALSE;
";

            dot += "node [shape = circle, label = \"\"];\n";
            foreach (var p in _places)
            {
                if (m[p] == null) dot += $"\"{p}\" [ xlabel = <{p}>, label = \"ω\" ];\n";
                else if (m[p] > 0) dot += $"\"{p}\" [ xlabel = <{p}>, label = \"{m[p]}\" ];\n";
                else dot += $"\"{p}\" [ xlabel = <{p}>, label = \"\" ];\n";
            }

            dot += "node [shape = rectangle, style = filled, color = black, label = \"\",  width = 0.5, height = 0.05];\n";
            foreach (var t in _transitions)
            {
                dot += $"\"{t}\" [ xlabel = <{t}> ];\n";
            }

            foreach (var (x,y) in Arcs)
            {
                if (Weight(x, y) > 1) dot += $"\"{x}\" -> \"{y}\" [ label = \"{Weight(x, y)}\" ];\n";
                else dot += $"\"{x}\" -> \"{y}\" [ label = \"\" ];\n";
            }

            dot += "}";
            return dot;
        }

        public void ShowPetriNet(string name, Marking m = null)
        {
            Draw.ShowDotCode(ToDotCode(m), name);
        }

        public static PetriNet operator +(PetriNet p1, PetriNet p2)
        {
            return new PetriNet(p1.Arcs.Union(p2.Arcs), $"{p1.Name}+{p2.Name}");
        }

    }
}
