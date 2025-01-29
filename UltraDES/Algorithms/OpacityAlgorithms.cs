using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES.Opacity
{
    public static class OpacityAlgorithms
    {
        #region Projection and Language-Based Opacity

        public static bool LanguageBasedOpacity(RegularExpression secretLanguage, RegularExpression nonSecretLanguage,
            HashSet<AbstractEvent> unobservableEvents )
        {
            var projectedSecret = secretLanguage.Projection(unobservableEvents);
            var projectedNonSecret = nonSecretLanguage.Projection(unobservableEvents);

            var Gs = projectedSecret.ToDFA.Minimal;
            var Gns = projectedNonSecret.ToDFA.Minimal;

            return DeterministicFiniteAutomaton.Isomorphism(Gs, Gns);
        }

        #endregion

        #region Auxiliary Methods for Building Estimators

        /// <summary>
        /// Creates a DFA (estimator) from a history of transitions.
        /// </summary>
        /// <param name="transitionsHistory">List of transitions collected during BFS.</param>
        /// <param name="automatonName">Name to be assigned to the estimator.</param>
        /// <returns>Returns the constructed deterministic automaton.</returns>
        private static DeterministicFiniteAutomaton BuildAutomatonFromTransitions(
            IEnumerable<Transition> transitionsHistory,
            string automatonName)
        {
            var transitions = transitionsHistory.ToList();
            var states = new HashSet<AbstractState>();

            // Collect all states (origin and destination).
            foreach (var t in transitions)
            {
                states.Add(t.Origin);
                states.Add(t.Destination);
            }

            // Define the initial state as the first found origin state
            var initialState = transitions.FirstOrDefault()?.Origin;

            return new DeterministicFiniteAutomaton(
                transitions.Select(t => (t.Origin, t.Trigger, t.Destination)),
                initialState,
                automatonName
            );
        }

        /// <summary>
        /// Auxiliary BFS function for mapping pairs of states (used in ISO and IFSO).
        /// </summary>
        /// <param name="automaton">The base automaton.</param>
        /// <param name="unobservableEvents">Set of non-observable events.</param>
        /// <param name="initialMapping">Initial mapping (pairs of states).</param>
        /// <returns>Returns the history of transitions and the history of generated mappings.</returns>
        private static (List<Transition> transitionsHistory,
                        List<HashSet<(AbstractState, AbstractState)>> mappingHistory)
            BfsPairMapping(
                DeterministicFiniteAutomaton automaton,
                HashSet<AbstractEvent> unobservableEvents,
                HashSet<(AbstractState, AbstractState)> initialMapping)
        {
            var observableEvents = new HashSet<AbstractEvent>(automaton.Events.Except(unobservableEvents));

            var transitionsHistory = new List<Transition>();
            var mappingHistory = new List<HashSet<(AbstractState, AbstractState)>> { initialMapping };

            var queue = new Queue<HashSet<(AbstractState, AbstractState)>>();
            queue.Enqueue(initialMapping);

            while (queue.Count > 0)
            {
                var currentMapping = queue.Dequeue();

                // For each observable event, apply and generate new mapping
                foreach (var obsEvent in observableEvents)
                {
                    var newMapping = new HashSet<(AbstractState, AbstractState)>();

                    // Iterate through each pair (s1, s2) of the current mapping
                    foreach (var (state1, state2) in currentMapping)
                    {
                        // Insert expansion logic here (similar to your original code)
                        // --------------------------------------------------------
                        var internalQueue = new Queue<AbstractState>();
                        internalQueue.Enqueue(state2);
                        var visited = new HashSet<AbstractState>(); // controls visited

                        while (internalQueue.Count > 0)
                        {
                            var st = internalQueue.Dequeue();

                            // 1) First try transition with obsEvent
                            var tObs = automaton.TransitionFunction(st, obsEvent);
                            if (tObs?.Value != null)
                            {
                                var pair = (state1, tObs.Value);
                                if (newMapping.Add(pair)) visited.Add(tObs.Value);

                                // 2) Try chaining non-observable events
                                foreach (var nObsEv in unobservableEvents)
                                {
                                    var tNObs = automaton.TransitionFunction(tObs.Value, nObsEv);
                                    if (tNObs?.Value != null && !visited.Contains(tNObs.Value))
                                    {
                                        internalQueue.Enqueue(tNObs.Value);
                                        visited.Add(tNObs.Value);
                                    }
                                }
                            }

                            // 3) Can also try first one (or more) non-observable events in st
                            foreach (var nObsEv in unobservableEvents)
                            {
                                var tNObs = automaton.TransitionFunction(st, nObsEv);
                                if (tNObs?.Value == null) continue;

                                // After a non-observable, try the obsEvent
                                var tAfterNObs = automaton.TransitionFunction(tNObs.Value, obsEvent);
                                if (tAfterNObs?.Value == null || visited.Contains(tAfterNObs.Value)) continue;
                                var pair2 = (state1, tAfterNObs.Value);
                                newMapping.Add(pair2);

                                internalQueue.Enqueue(tAfterNObs.Value);
                                visited.Add(tAfterNObs.Value);
                            }
                        }
                        // --------------------------------------------------------
                    }

                    if (newMapping.Count == 0)
                        continue;

                    // Create "synthetic" states to represent each mapping
                    var s1 = new State(string.Join(", ", currentMapping.Select(p => $"({p.Item1}, {p.Item2})")));
                    var s2 = new State(string.Join(", ", newMapping.Select(p => $"({p.Item1}, {p.Item2})")));

                    var newTransition = new Transition(s1, obsEvent, s2);

                    if (transitionsHistory.Contains(newTransition)) continue;
                    transitionsHistory.Add(newTransition);
                    mappingHistory.Add(newMapping);
                    queue.Enqueue(newMapping);
                }
            }

            return (transitionsHistory, mappingHistory);
        }

        /// <summary>
        /// Auxiliary BFS function for mapping individual states (used in CurrentStepOpacity).
        /// </summary>
        private static (List<Transition> transitionsHistory,
                        List<HashSet<AbstractState>> mappingHistory)
            BfsSingleStateMapping(
                DeterministicFiniteAutomaton automaton,
                HashSet<AbstractEvent> unobservableEvents,
                HashSet<AbstractState> initialMapping)
        {
            var observableEvents = new HashSet<AbstractEvent>(automaton.Events.Except(unobservableEvents));

            var transitionsHistory = new List<Transition>();
            var mappingHistory = new List<HashSet<AbstractState>> { initialMapping };

            var queue = new Queue<HashSet<AbstractState>>();
            queue.Enqueue(initialMapping);

            while (queue.Count > 0)
            {
                var currentMapping = queue.Dequeue();

                foreach (var obsEvent in observableEvents)
                {
                    var newMapping = new HashSet<AbstractState>();

                    // Expand each state in the mapping
                    foreach (var st in currentMapping)
                    {
                        var internalQueue = new Queue<AbstractState>();
                        internalQueue.Enqueue(st);
                        var visited = new HashSet<AbstractState>();

                        while (internalQueue.Count > 0)
                        {
                            var stNow = internalQueue.Dequeue();

                            // 1) Apply the observable event directly
                            var tObs = automaton.TransitionFunction(stNow, obsEvent);
                            if (tObs?.Value != null && newMapping.Add(tObs.Value))
                            {
                                visited.Add(tObs.Value);

                                // Chain non-observable events
                                foreach (var nObsEv in unobservableEvents)
                                {
                                    var tNObs = automaton.TransitionFunction(tObs.Value, nObsEv);
                                    if (tNObs?.Value == null || visited.Contains(tNObs.Value)) continue;
                                    internalQueue.Enqueue(tNObs.Value);
                                    visited.Add(tNObs.Value);
                                }
                            }

                            // 2) Try non-observable in stNow before obsEvent
                            foreach (var nObsEv in unobservableEvents)
                            {
                                var tNObs = automaton.TransitionFunction(stNow, nObsEv);
                                if (tNObs?.Value == null) continue;

                                var tAfterNObs = automaton.TransitionFunction(tNObs.Value, obsEvent);
                                if (tAfterNObs?.Value == null || visited.Contains(tAfterNObs.Value)) continue;
                                newMapping.Add(tAfterNObs.Value);
                                internalQueue.Enqueue(tAfterNObs.Value);
                                visited.Add(tAfterNObs.Value);
                            }
                        }
                    }

                    if (newMapping.Count == 0)
                        continue;

                    var s1 = new State(string.Join(", ", currentMapping.Select(x => $"({x})")));
                    var s2 = new State(string.Join(", ", newMapping.Select(x => $"({x})")));

                    var newTransition = new Transition(s1, obsEvent, s2);

                    if (transitionsHistory.Contains(newTransition)) continue;
                    transitionsHistory.Add(newTransition);
                    mappingHistory.Add(newMapping);
                    queue.Enqueue(newMapping);
                }
            }

            return (transitionsHistory, mappingHistory);
        }

        /// <summary>
        /// Auxiliary BFS function for mapping lists of states (used in KStepsOpacity).
        /// </summary>
        private static (List<Transition> transitionsHistory,
                        List<HashSet<List<AbstractState>>>)
            BfsKStepMapping(
                DeterministicFiniteAutomaton automaton,
                HashSet<AbstractEvent> unobservableEvents,
                HashSet<List<AbstractState>> initialMapping,
                int k)
        {
            var observableEvents = new HashSet<AbstractEvent>(automaton.Events.Except(unobservableEvents));

            var transitionsHistory = new List<Transition>();
            var mappingHistory = new List<HashSet<List<AbstractState>>> { initialMapping };

            var queue = new Queue<HashSet<List<AbstractState>>>();
            queue.Enqueue(initialMapping);

            while (queue.Count > 0)
            {
                var currentMapping = queue.Dequeue();

                foreach (var obsEvent in observableEvents)
                {
                    var newMapping = new HashSet<List<AbstractState>>();

                    foreach (var stateList in currentMapping)
                    {
                        // Last state in the list
                        var lastState = stateList[^1];

                        // Internal BFS
                        var internalQueue = new Queue<AbstractState>();
                        internalQueue.Enqueue(lastState);
                        var visited = new HashSet<AbstractState>();

                        while (internalQueue.Count > 0)
                        {
                            var stNow = internalQueue.Dequeue();

                            // 1) Apply obsEvent to stNow
                            var tObs = automaton.TransitionFunction(stNow, obsEvent);
                            if (tObs?.Value != null)
                            {
                                var shiftedList = ShiftAndAppendState(stateList, tObs.Value, k);
                                if (!ContainsList(newMapping, shiftedList))
                                {
                                    newMapping.Add(shiftedList);
                                    visited.Add(tObs.Value);

                                    // Chain non-observable events
                                    foreach (var nObsEv in unobservableEvents)
                                    {
                                        var tNObs = automaton.TransitionFunction(tObs.Value, nObsEv);
                                        if (tNObs?.Value == null || visited.Contains(tNObs.Value)) continue;
                                        internalQueue.Enqueue(tNObs.Value);
                                        visited.Add(tNObs.Value);
                                    }
                                }
                            }

                            // 2) Try applying non-observable to stNow, then obsEvent
                            foreach (var nObsEv in unobservableEvents)
                            {
                                var tNObs = automaton.TransitionFunction(stNow, nObsEv);
                                if (tNObs?.Value == null) continue;

                                var tAfterNObs = automaton.TransitionFunction(tNObs.Value, obsEvent);
                                if (tAfterNObs?.Value == null || visited.Contains(tAfterNObs.Value)) continue;
                                var shiftedList2 = ShiftAndAppendState(stateList, tAfterNObs.Value, k);
                                if (ContainsList(newMapping, shiftedList2)) continue;
                                newMapping.Add(shiftedList2);
                                internalQueue.Enqueue(tAfterNObs.Value);
                                visited.Add(tAfterNObs.Value);
                            }
                        }
                    }

                    if (newMapping.Count == 0)
                        continue;

                    var s1 = new State(string.Join(", ", currentMapping.Select(p => $"({string.Join(",", p)})")));
                    var s2 = new State(string.Join(", ", newMapping.Select(p => $"({string.Join(",", p)})")));

                    var newTransition = new Transition(s1, obsEvent, s2);

                    if (transitionsHistory.Contains(newTransition)) continue;
                    transitionsHistory.Add(newTransition);
                    mappingHistory.Add(newMapping);
                    queue.Enqueue(newMapping);
                }
            }

            return (transitionsHistory, mappingHistory);
        }

        /// <summary>
        /// Shifts the list of states by removing the first and adding <paramref name="newState"/> at the end.
        /// </summary>
        private static List<AbstractState> ShiftAndAppendState(List<AbstractState> original, AbstractState newState, int k)
        {
            // Remove the "oldest" state and add the new one at the end.
            // Original has size k+1
            var shifted = new List<AbstractState>(original.Skip(1)) { newState };
            return shifted;
        }

        /// <summary>
        /// Checks if a set of lists contains a list equal to the provided one.
        /// </summary>
        private static bool ContainsList(HashSet<List<AbstractState>> setOfLists, List<AbstractState> list)
            => setOfLists.Any(item => item.SequenceEqual(list));

        #endregion

        #region Initial State Opacity (ISO)

        /// <summary>
        /// Checks Initial State Opacity (ISO). Internally creates an estimator with pairs of states
        /// and then checks if, in all possible mappings, there is "covering" of secret states.
        /// </summary>
        /// <param name="automaton">Deterministic finite automaton.</param>
        /// <param name="unobservableEvents">Set of non-observable events.</param>
        /// <param name="secretStates">Set of secret states.</param>
        /// <param name="estimator">Returns the generated estimator (auxiliary DFA).</param>
        /// <returns>True if the system is opaque with respect to the initial state; false otherwise.</returns>
        public static bool InitialStateOpacity(
            DeterministicFiniteAutomaton automaton,
            HashSet<AbstractEvent> unobservableEvents,
            HashSet<AbstractState> secretStates,
            out DeterministicFiniteAutomaton estimator)
        {
            // Initial mapping: all pairs (st, st)
            var initialMapping = new HashSet<(AbstractState, AbstractState)>();
            foreach (var st in automaton.States)
                initialMapping.Add((st, st));

            // Execute the encapsulated BFS
            var (transitionsHistory, mappingHistory) =
                BfsPairMapping(automaton, unobservableEvents, initialMapping);

            // Build the estimator
            estimator = BuildAutomatonFromTransitions(transitionsHistory, "ISOAutomaton");

            // Decision criterion: check if each mapping has at least one initial state that is not secret
            // (i.e., if *all* in the pair are secret, opacity is violated).
            // Example check, adapting your code's verification:
            // "returns true if every mapping contains some state1 that is not secret".
            return mappingHistory.All(
                mapping => mapping.Select(m => m.Item1).Except(secretStates).Any()
            );
        }

        #endregion

        #region Initial-Final State Opacity (IFSO)

        /// <summary>
        /// Checks Initial-Final State Opacity (IFSO). Creates the same estimator of pairs (s1, s2),
        /// but now compares with secret pairs (initial, final).
        /// </summary>
        /// <param name="automaton">Deterministic finite automaton.</param>
        /// <param name="unobservableEvents">Set of non-observable events.</param>
        /// <param name="secretPairs">Set of secret (initial, final) state pairs.</param>
        /// <param name="estimator">Returns the generated estimator.</param>
        /// <returns>True if the system is opaque with respect to initial and final states; false otherwise.</returns>
        public static bool InitialFinalStateOpacity(
            DeterministicFiniteAutomaton automaton,
            HashSet<AbstractEvent> unobservableEvents,
            HashSet<(AbstractState initial, AbstractState final)> secretPairs,
            out DeterministicFiniteAutomaton estimator)
        {
            // Initial mapping: all pairs (s, s)
            var initialMapping = new HashSet<(AbstractState, AbstractState)>();
            foreach (var st in automaton.States)
                initialMapping.Add((st, st));

            var (transitionsHistory, mappingHistory) =
                BfsPairMapping(automaton, unobservableEvents, initialMapping);

            estimator = BuildAutomatonFromTransitions(transitionsHistory, "IFSOAutomaton");

            // Criterion: there must be no mapping that includes secret pairs
            return !mappingHistory.Any(mapping =>
                mapping.Any(pair => secretPairs.Contains((pair.Item1, pair.Item2)))
            );
        }

        #endregion

        #region Current Step Opacity

        /// <summary>
        /// Checks Current Step Opacity,
        /// by building an estimator with simple state mappings.
        /// </summary>
        /// <param name="automaton">Deterministic finite automaton.</param>
        /// <param name="unobservableEvents">Set of non-observable events.</param>
        /// <param name="secretStates">Set of secret states.</param>
        /// <param name="estimator">Returns the generated estimator.</param>
        /// <returns>True if the system is opaque in the current step; false otherwise.</returns>
        public static bool CurrentStepOpacity(
            DeterministicFiniteAutomaton automaton,
            HashSet<AbstractEvent> unobservableEvents,
            HashSet<AbstractState> secretStates,
            out DeterministicFiniteAutomaton estimator)
        {
            // Initial mapping: each state mapped to itself (set of all states).
            var initialMapping = new HashSet<AbstractState>(automaton.States);

            var (transitionsHistory, mappingHistory) =
                BfsSingleStateMapping(automaton, unobservableEvents, initialMapping);

            estimator = BuildAutomatonFromTransitions(transitionsHistory, "CurrentStepAutomaton");

            // Criterion: there must be no complete mapping within the secret set
            // (if a mapping is entirely secret, opacity is violated).
            return !mappingHistory.Any(mapping => mapping.IsSubsetOf(secretStates));
        }

        #endregion

        #region K-Steps Opacity

        /// <summary>
        /// Checks opacity considering K steps. Builds an estimator
        /// tracking lists of states of length <c>k+1</c>.
        /// </summary>
        /// <param name="automaton">Deterministic finite automaton.</param>
        /// <param name="unobservableEvents">Set of non-observable events.</param>
        /// <param name="secretStates">Set of secret states.</param>
        /// <param name="k">Number of steps to consider (K >= 0).</param>
        /// <param name="estimator">Returns the generated estimator.</param>
        /// <returns>True if the system is K-opaque; false otherwise.</returns>
        public static bool KStepsOpacity(
            DeterministicFiniteAutomaton automaton,
            HashSet<AbstractEvent> unobservableEvents,
            HashSet<AbstractState> secretStates,
            int k,
            out DeterministicFiniteAutomaton estimator)
        {
            if (k < 0)
                throw new ArgumentOutOfRangeException(nameof(k), "k cannot be negative.");

            // If k=0, reuse the CurrentStepOpacity logic
            if (k == 0)
                return CurrentStepOpacity(automaton, unobservableEvents, secretStates, out estimator);

            // Initial mapping: For each state, create a list of size k+1 with repetitions of that state.
            var initialMapping = new HashSet<List<AbstractState>>();
            foreach (var st in automaton.States)
            {
                var list = new List<AbstractState>();
                for (int i = 0; i < k + 1; i++)
                    list.Add(st);
                initialMapping.Add(list);
            }

            var (transitionsHistory, mappingHistory) =
                BfsKStepMapping(automaton, unobservableEvents, initialMapping, k);

            estimator = BuildAutomatonFromTransitions(transitionsHistory, "KStepsAutomaton");

            // Violation criterion: there exists a mapping where *all* 
            // the "initial states" of the lists (position 0) are secret
            // Example check from your code:
            return !mappingHistory.Any(mapping =>
                mapping.All(seq => secretStates.Contains(seq[0]))
            );
        }

        #endregion
    }
}
