using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
    public static class ObserverAlgorithms
    {
        public static bool ObserverPropertyVerify(this DeterministicFiniteAutomaton G, AbstractEvent[] relevantArray, out (IEnumerable<Transition> transitions, AbstractState initial) Vg, bool returnOnDead = true)
        {
            var tau = new Event("Tau", Controllability.Controllable);
            var dead = new State("DEAD", Marking.Marked);
            bool findDead = false;
            var relevant = new HashSet<AbstractEvent>(relevantArray) {tau};
            var nonrelevant = new HashSet<AbstractEvent>(G.Events.Except(relevant));
            var transitionsAux = G.Transitions.Union(G.MarkedStates.Select(q => (Transition) (q, tau, q))).ToList();

            var M = StronglyConnectedComponentsAutomaton((transitionsAux, G.InitialState), nonrelevant);

            var transitions = M.transitions.GroupBy(t => t.Origin).ToDictionary(g => g.Key, g => g.ToArray());

            var Q = new HashSet<(AbstractState, AbstractState)>();
            var Qt = new HashSet<(AbstractState, AbstractState)>() {(M.initial, M.initial) };
            var VgTransitions = new HashSet<Transition>();

            Vg = (VgTransitions, G.InitialState);

            while (Qt.Except(Q).Any())
            {
                var Qtemp = new HashSet<(AbstractState, AbstractState)>();
                foreach (var q in Qt.Except(Q))
                {
                    Q.Add(q);

                    var (q1, q2) = q;

                    var origin = q1 == q2
                        ? q1
                        : (new[] { q1, q2 }).OrderBy(qq => qq.ToString())
                        .Aggregate((a, b) => a.MergeWith(b));

                    var Enq1 = new HashSet<AbstractEvent>(transitions.ContainsKey(q1)
                        ? transitions[q1].Select(t => t.Trigger)
                        : new AbstractEvent[0]).Distinct(); 
                    var Enq2 = new HashSet<AbstractEvent>(transitions.ContainsKey(q2)
                        ? transitions[q2].Select(t => t.Trigger)
                        : new AbstractEvent[0]).Distinct();

                    foreach (var sigma in Enq1.Union(Enq2))
                    {
                        if (relevant.Contains(sigma))
                        {
                            if (Enq1.Contains(sigma) && Enq2.Contains(sigma))
                            {
                                var d1s = transitions[q1].Where(t => t.Trigger == sigma).Select(t => t.Destination);
                                var d2s = transitions[q2].Where(t => t.Trigger == sigma).Select(t => t.Destination);

                              

                                foreach (var d2 in d2s)
                                {
                                    foreach (var d1 in d1s)
                                    {
                                        var destination = d1 == d2
                                            ? d1
                                            : (new[] { d1, d2 }).OrderBy(qq => qq.ToString())
                                            .Aggregate((a, b) => a.MergeWith(b));
                                        Qtemp.Add((d1, d2));
                                        VgTransitions.Add((origin, sigma, destination));
                                    }
                                }
                            }
                            else if ((Enq1.Contains(sigma) && !Enq2.Intersect(nonrelevant).Any()) ||
                                     (Enq2.Contains(sigma) && !Enq1.Intersect(nonrelevant).Any()))
                            {
                                Qtemp.Add((dead, dead));
                                VgTransitions.Add((origin, sigma, dead));
                            }
                        }
                        else
                        {
                            if (Enq1.Contains(sigma))
                            {
                                var d1s = transitions[q1].Where(t => t.Trigger == sigma).Select(t => t.Destination);
                                foreach (var d1 in d1s)
                                {
                                    var destination = d1 == q2
                                        ? d1
                                        : (new[] { d1, q2 }).OrderBy(qq => qq.ToString())
                                        .Aggregate((a, b) => a.MergeWith(b));

                                    Qtemp.Add((d1, q2));
                                    VgTransitions.Add((origin, sigma, destination));
                                }
                                
                            }

                            if (Enq2.Contains(sigma))
                            { 
                                var d2s = transitions[q2].Where(t => t.Trigger == sigma).Select(t => t.Destination);
                                foreach (var d2 in d2s)
                                {
                                    var destination = q1 == d2
                                        ? q1
                                        : (new[] { q1, d2 }).OrderBy(qq => qq.ToString())
                                        .Aggregate((a, b) => a.MergeWith(b));

                                    Qtemp.Add((q1, d2));
                                    VgTransitions.Add((origin, sigma, destination));
                                }
                            }
                        }
                    }
                }

                Qt.UnionWith(Qtemp);
                if (Qtemp.Contains((dead, dead)))
                {
                    if (returnOnDead) return false;
                    else findDead = true;
                }
            }

            return !findDead;
        }

        public static (IEnumerable<Transition> transitions, AbstractState initial) StronglyConnectedComponentsAutomaton((List<Transition> transitions, AbstractState initial) G, HashSet<AbstractEvent> nonrelevant)
        {
            var trans = G.transitions.Select(t => (Transition) (t.Origin.Flatten, t.Trigger, t.Destination.Flatten)).ToList();
            var states = trans.SelectMany(t => new[] { t.Origin, t.Destination }).Distinct().ToArray();
            var partitions = TarjanSCC(trans.Where(t => nonrelevant.Contains(t.Trigger)).ToList());
            var macroStates = partitions.Select(p => p.Aggregate((a, b) => a.MergeWith(b))).ToList();
            macroStates.AddRange(states.Except(partitions.SelectMany(p => p)).Select(q => q.Flatten));

            var initial = macroStates.Single(q => q == G.initial.Flatten || (q is AbstractCompoundState state && state.S.Contains(G.initial.Flatten)));

            var transitions = new List<Transition>();
            foreach (var t in trans)
            {
                var origin = macroStates.Single(q => q == t.Origin || (q is AbstractCompoundState state && state.S.Contains(t.Origin)));
                var destination = macroStates.Single(q => q == t.Destination || (q is AbstractCompoundState state && state.S.Contains(t.Destination)));
                if (!nonrelevant.Contains(t.Trigger) || (origin != destination))
                {
                    transitions.Add((origin, t.Trigger, destination));
                }
            }

            return (transitions, initial);
        }

        public static List<List<AbstractState>> TarjanSCC(IEnumerable<Transition> transitions)
        {
            var components = new List<List<AbstractState>>();
            var stateIndex = new Dictionary<AbstractState, int>();
            var stateLowlink = new Dictionary<AbstractState, int>();
            var states = transitions.SelectMany(t => new[] { t.Origin, t.Destination }).Distinct().ToArray();
            
            var index = 0;
            var S = new Stack<AbstractState>();

            void StrongConnect(AbstractState v)
            {
                // Set the depth index for v to the smallest unused index
                stateIndex.Add(v, index);
                stateLowlink.Add(v, index);

                index = index + 1;
                S.Push(v);

                // Consider successors of v
                foreach (var w in transitions.Where(t => t.Origin == v).Select(t => t.Destination))
                {
                    if (!stateIndex.ContainsKey(w))
                    {
                        // Successor w has not yet been visited; recurse on it
                        StrongConnect(w);
                        stateLowlink[v] = (int) Math.Min(stateLowlink[v], stateLowlink[w]);
                    }
                    else if (S.Contains(w))
                    {
                        // Successor w is in stack S and hence in the current SCC
                        // If w is not on stack, then (v, w) is a cross-edge in the DFS tree and must be ignored
                        // Note: The next line may look odd - but is correct.
                        // It says w.index not w.lowlink; that is deliberate and from the original paper
                        stateLowlink[v] = (int)Math.Min(stateLowlink[v], stateIndex[w]);
                    }
                }

                // If v is a root node, pop the stack and generate an SCC
                if (stateIndex[v] == stateLowlink[v])
                {
                    var component = new List<AbstractState>();
                    AbstractState w;
                    do
                    {
                        w = S.Pop();
                        component.Add(w);
                    } while (w != v);
                    components.Add(component);
                }
                    
            }

            foreach (var s in states)
            {
                if (!stateIndex.ContainsKey(s))
                {
                    StrongConnect(s);
                }
            }

            return components;
        }
        
    }
}
