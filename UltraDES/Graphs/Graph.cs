using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace UltraDES
{
    public static class Graph
    {
        public static IEnumerable<(TNode o, TNode d)> ToUnlabeledEdges<TNode, TLabel>(this IEnumerable<(TNode o, TLabel l, TNode d)> edges) => edges.Select(e => (e.o, e.d));
        public static IEnumerable<(T o, T d)> ReverseEdges<T>(this IEnumerable<(T o, T d)> edges) => edges.Select(e => (e.d, e.o)).ToList();
        public static IEnumerable<T> BreadthFirstSearch<T>(this IEnumerable<(T o, T d)> edges, T v0) => edges.BreadthFirstSearch(v0, (i, j) => i.Equals(j));
        public static IEnumerable<T> BreadthFirstSearch<T>(this IEnumerable<(T o, T d)> edges, T v0, Func<T,T,bool> equals)
        {
            var visited = new List<T>();
            var frontier = new List<T> {v0};

            while (frontier.Any())
            {
                var newFrontier = new List<T>();
                visited.AddRange(frontier);

                foreach (var v1 in frontier)
                {
                    foreach (var (_, v2) in edges.AsParallel().Where(t => equals(t.o, v1)))
                    {
                        if(visited.AsParallel().Any(v => equals(v, v2))) continue;
                        if(!newFrontier.AsParallel().Any(v => equals(v, v2))) newFrontier.Add(v2);
                    }
                }

                frontier = newFrontier;
            }

            return visited;
        }
        public static IEnumerable<T> VerticesBetween<T>(this IEnumerable<(T o, T d)> edges, T v1, T v2) => edges.VerticesBetween(v1, v2, (i, j) => i.Equals(j));
        public static IEnumerable<T> VerticesBetween<T>(this IEnumerable<(T o, T d)> edges, T v1, T v2, Func<T, T, bool> equals) => BreadthFirstSearch(edges, v1, @equals).Intersect(BreadthFirstSearch(edges.ReverseEdges(), v2, @equals));
        public static void ShowGraph<TNode>(this IEnumerable<(TNode o, TNode d)> edges, string name) => Draw.ShowDotCode(edges.ToDotCode(), name);
        public static void ShowGraph<TNode, TLabel>(this IEnumerable<(TNode o, TLabel l, TNode d)> edges, string name) => Draw.ShowDotCode(edges.ToDotCode(), name);
        public static string ToDotCode<TNode>(this IEnumerable<(TNode o, TNode d)> edges) => edges.Select(e => (e.o, "", e.d)).ToDotCode();
        public static string ToDotCode<TNode, TLabel>(this IEnumerable<(TNode o, TLabel l, TNode d)> edges)
        {
            var nodes = edges.Select(e => e.o).Distinct().Union(edges.Select(e => e.d).Distinct()).ToList();

            var dot = @"digraph 
{
    
forcelabels = TRUE;
splines = FALSE;
";

            dot += "node [shape = rectangle, label = \"\"];\n";
            foreach (var n in nodes)
            {
                dot += $"\"{n}\" [ label = \"{n}\" ];\n";
            }



            foreach (var (o, l,d) in edges)
            {
                dot += $"\"{o}\" -> \"{d}\" [ label = \"{l}\" ];\n";
            }

            dot += "}";
            return dot;
        }
        public static List<List<TNode>> StronglyConnectedComponents<TNode, TLabel>(this IEnumerable<(TNode o, TLabel l, TNode d)> edges, Func<TNode, TNode, bool> equals) =>
            edges.ToUnlabeledEdges().StronglyConnectedComponents(equals);
        public static List<List<TNode>> StronglyConnectedComponents<TNode>(this IEnumerable<(TNode Origin, TNode Destination)> edges, Func<TNode, TNode, bool> equals)
        {
            //Tarjan Algorithm
            var components = new List<List<TNode>>();
            var stateIndex = new Dictionary<TNode, int>();
            var stateLowlink = new Dictionary<TNode, int>();
            var states = edges.SelectMany(t => new[] { t.Origin, t.Destination }).Distinct().ToArray();

            var index = 0;
            var S = new Stack<TNode>();

            void StrongConnect(TNode v)
            {
                stateIndex.Add(v, index);
                stateLowlink.Add(v, index);

                index += 1;
                S.Push(v);

                foreach (var w in edges.Where(t => equals(t.Origin, v)).Select(t => t.Destination))
                {
                    if (!stateIndex.ContainsKey(w))
                    {
                        StrongConnect(w);
                        stateLowlink[v] = (int)Math.Min(stateLowlink[v], stateLowlink[w]);
                    }
                    else if (S.Contains(w))
                    {
                        stateLowlink[v] = (int)Math.Min(stateLowlink[v], stateIndex[w]);
                    }
                }

                if (stateIndex[v] == stateLowlink[v])
                {
                    var component = new List<TNode>();
                    TNode w;
                    do
                    {
                        w = S.Pop();
                        component.Add(w);
                    } while (!equals(w, v));
                    components.Add(component);
                }

            }

            foreach (var s in states)
            {
                if (!stateIndex.ContainsKey(s)) StrongConnect(s);
            }

            return components;
        }

    }
}
