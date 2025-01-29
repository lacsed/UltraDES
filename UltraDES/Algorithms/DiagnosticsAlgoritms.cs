using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UltraDES.Diagnosability
{
    public static class DiagnosticsAlgoritms
    {
        /// <summary>
        /// Creates an observer automaton from a given DFA (automaton), considering observable and unobservable events.
        /// </summary>
        /// <param name="automaton">Original deterministic finite automaton.</param>
        /// <param name="nonObservableEvents">Set of unobservable events.</param>
        /// <returns>The resulting observer automaton.</returns>
        public static DeterministicFiniteAutomaton CreateObserver(
            DeterministicFiniteAutomaton automaton,
            HashSet<AbstractEvent> nonObservableEvents)
        {
            //var observableEvents = new HashSet<AbstractEvent>(automaton.Events.Except(nonObservableEvents));

            // 1) Compute the initial compound state.
            var initialCompoundState = URS(new HashSet<AbstractState> { automaton.InitialState }, automaton, nonObservableEvents);

            // Console.WriteLine($"Estado inicial composto: {initialCompoundState}");

            // 2) Perform BFS-like expansion on the queue of compound states.
            var allCompoundStates = new HashSet<CompoundState> { initialCompoundState };
            var queue = new Queue<CompoundState>();
            queue.Enqueue(initialCompoundState);

            var transitionsList = new List<Transition>();
            var loopCount = 0;

            while (queue.Count > 0)
            {
                var currentCompoundState = queue.Dequeue();

                loopCount++;


                // The set of active events
                var activeEvents = new HashSet<AbstractEvent>(automaton.Events.Cast<AbstractEvent>());

                // Process each possible event
                foreach (var evt in activeEvents)
                {
                    // Expand from currentCompoundState using the event evt
                    ProcessStateExpansion(
                        automaton,
                        evt,
                        currentCompoundState,
                        allCompoundStates,
                        queue,
                        transitionsList,
                        nonObservableEvents
                    );
                }
            }

            // 3) Build final automaton from the transitions
            var finalAutomaton = BuildAutomaton(automaton, transitionsList, initialCompoundState);

            return finalAutomaton;
        }

        /// <summary>
        /// Computes the UR set for a single state, collecting reachable states by unobservable events.
        /// </summary>
        private static CompoundState UR(
            AbstractState estado,
            DeterministicFiniteAutomaton G,
            HashSet<AbstractEvent> eventosNaoObservaveis)
        {
            var alcance = new HashSet<AbstractState> { estado };

            // For each unobservable event, see if there's a transition
            foreach (var transicoes in eventosNaoObservaveis.Select(e => G.TransitionFunction(estado, e).Value).OfType<AbstractState>())
            {
                if (transicoes is not AbstractState proximoEstado) continue;
                alcance.Add(proximoEstado);
            }

            // Check if the set of states includes any marked state
            var isMarked = G.MarkedStates.Any(alcance.Contains);
            return new CompoundState(alcance.ToArray(), isMarked);
        }

        /// <summary>
        /// Computes the UR set for a set of states. This is effectively
        /// the union of UR(estado, G, unobs) for all states in 'estados'.
        /// </summary>
        private static CompoundState URS(
            HashSet<AbstractState> estados,
            DeterministicFiniteAutomaton G,
            HashSet<AbstractEvent> eventosNaoObservaveis)
        {
            var alcanceTotal = new HashSet<AbstractState>();

            foreach (var alcanceEstado in estados.Select(state => UR(state, G, eventosNaoObservaveis)))
                alcanceTotal.UnionWith(alcanceEstado.S);

            var isMarked = G.MarkedStates.Any(alcanceTotal.Contains);
            return new CompoundState(alcanceTotal.ToArray(), isMarked);
        }

        /// <summary>
        /// Processes the expansion of the currentCompoundState upon an event. Handles new transitions,
        /// merges with existing compound states, and updates transitionsList accordingly.
        /// </summary>
        private static void ProcessStateExpansion(
            DeterministicFiniteAutomaton G,
            AbstractEvent evt,
            CompoundState currentCompoundState,
            HashSet<CompoundState> allCompoundStates,
            Queue<CompoundState> queue,
            List<Transition> transitionsList,
            HashSet<AbstractEvent> eventosNaoObservaveis)
        {
            // Compute next states
            var nextStatesSet = new HashSet<AbstractState>();

            // For each sub-state in the compound state, if there's a transition, we add the destination
            foreach (var subState in currentCompoundState.S)
            {
                var possibleTransitions = G.TransitionFunction(subState, evt).Value;
                if (possibleTransitions is not AbstractState proxEstado) continue;
                nextStatesSet.Add(proxEstado);
            }

            // Build the new compound state with URS for nextStatesSet
            var newCompoundState = URS(nextStatesSet, G, eventosNaoObservaveis);

            // If it's an unobservable event, we merge the new compound state with the current one
            if (eventosNaoObservaveis.Contains(evt))
            {
                var merged = currentCompoundState.S.Concat(newCompoundState.S)
                                          .Distinct()
                                          .OrderBy(s => s.ToString())
                                          .ToArray();

                var isMarked = G.MarkedStates.Any(st => newCompoundState.S.Contains(st));
                newCompoundState = new CompoundState(merged, isMarked);

                // Debug: "Depois de mesclar: newCompoundState"
            }

            // If the new compound state is non-empty, we proceed
            if (newCompoundState.S.Length <= 0) return;
            // Check if this new compound state is already in allCompoundStates
            if (!allCompoundStates.Any(x => new HashSet<AbstractState>(x.S).SetEquals(newCompoundState.S)))
            {
                // We discovered a new compound state
                allCompoundStates.Add(newCompoundState);
                queue.Enqueue(newCompoundState);
            }

            // Now update transitions if needed.
            UpdateTransitions(
                G,
                currentCompoundState,
                newCompoundState,
                evt,
                transitionsList,
                eventosNaoObservaveis
            );
        }

        /// <summary>
        /// Updates or creates transitions based on the relationship between currentCompoundState and newCompoundState.
        /// Preserves the BFS/merging logic from the original code.
        /// </summary>
        private static void UpdateTransitions(
            DeterministicFiniteAutomaton G,
            CompoundState currentCompoundState,
            CompoundState newCompoundState,
            AbstractEvent evt,
            List<Transition> transitionsList,
            HashSet<AbstractEvent> eventosNaoObservaveis)
        {
            // If newCompoundState extends currentCompoundState's states (all old states plus more)...
            if (currentCompoundState.S.All(st => newCompoundState.S.Contains(st))
                && currentCompoundState.S.Length != newCompoundState.S.Length)
            {
                // We might need to update existing transitions in transitionsList
                UpdateExistingTransitions(
                    G,
                    currentCompoundState,
                    newCompoundState,
                    evt,
                    transitionsList
                );

                // currentCompoundState = newCompoundState (in the original code, B = B_novo).
            }
            // If currentCompoundState includes all of newCompoundState's states, 
            // and evt is an observable event, we create a loop
            else if (newCompoundState.S.All(st => currentCompoundState.S.Contains(st)) && !eventosNaoObservaveis.Contains(evt))
            {
                var loopTransition = new Transition(
                    AsState(G, currentCompoundState),
                    evt,
                    AsState(G, currentCompoundState)
                );
                transitionsList.Add(loopTransition);
            }
            else if (!eventosNaoObservaveis.Contains(evt))
            {
                // Just add a new normal transition
                var newTransition = new Transition(
                    AsState(G, currentCompoundState),
                    evt,
                    AsState(G, newCompoundState)
                );
                transitionsList.Add(newTransition);
            }
        }

        /// <summary>
        /// Updates existing transitions in <c>transitionsList</c> if origin/destination
        /// match the old compound state and must now point to the new compound state.
        /// </summary>
        private static void UpdateExistingTransitions(
            DeterministicFiniteAutomaton G,
            CompoundState oldCompoundState,
            CompoundState newCompoundState,
            AbstractEvent evt,
            List<Transition> transitionsList)
        {
            for (int i = 0; i < transitionsList.Count; i++)
            {
                var trans = transitionsList[i];
                // We check if trans.Origin or trans.Destination is oldCompoundState, 
                // and update to newCompoundState.

                var originMatches = MatchCompoundState(trans.Origin, oldCompoundState);
                var destinationMatches = MatchCompoundState(trans.Destination, oldCompoundState);

                if (originMatches && destinationMatches)
                {
                    // Update both origin & destination
                    var newOriginDest = new Transition(
                        AsState(G, newCompoundState),
                        trans.Trigger,
                        AsState(G, newCompoundState)
                    );
                    transitionsList[i] = newOriginDest;
                }
                else if (originMatches)
                {
                    var newOriginTransition = new Transition(
                        AsState(G, newCompoundState),
                        trans.Trigger,
                        trans.Destination
                    );
                    transitionsList[i] = newOriginTransition;
                }
                else if (destinationMatches)
                {
                    var newDestTransition = new Transition(
                        trans.Origin,
                        trans.Trigger,
                        AsState(G, newCompoundState)
                    );
                    transitionsList[i] = newDestTransition;
                }
            }
        }

        /// <summary>
        /// Checks if the given <c>transitionEndpoint</c> is a CompoundState that matches the given <c>target</c>.
        /// </summary>
        private static bool MatchCompoundState(object transitionEndpoint, CompoundState target)
        {
            if (transitionEndpoint is not CompoundState compound) return false;
            
            var orderedCompound = compound.S.OrderBy(s => s.ToString());
            var orderedTarget = target.S.OrderBy(s => s.ToString());
            return orderedCompound.SequenceEqual(orderedTarget);
        }

        /// <summary>
        /// Converts a CompoundState (or a single CompoundState's S) to <c>State</c> or <c>CompoundState</c>
        /// properly, ensuring correct marking if it is compound.
        /// </summary>
        private static AbstractState AsState(DeterministicFiniteAutomaton G, CompoundState cState)
        {
            // Sort the sub-states by name
            var ordered = cState.S.OrderBy(st => st.ToString()).ToArray();
            var isMarked = G.MarkedStates.Any(st => ordered.Contains(st));

            return new CompoundState(ordered, isMarked ? Marking.Marked : Marking.Unmarked);
        }

        /// <summary>
        /// Builds the final observer automaton from the collected transitions.
        /// </summary>
        private static DeterministicFiniteAutomaton BuildAutomaton(
            DeterministicFiniteAutomaton G,
            List<Transition> transitionsList,
            CompoundState initialCompoundState)
        {
            // We try to find a suitable initial state in transitions that references the original initial state's name
            var possibleInitial = transitionsList
                .Select(t => t.Origin)
                .FirstOrDefault(s => s.ToString().Contains(G.InitialState.ToString()));

            // If none found, fallback to a State version of the initialCompoundState
            var newInitialState = possibleInitial ?? new State(
                initialCompoundState.ToString(),
                Marking.Unmarked
            );

            // Build the new automaton
            var automaton = new DeterministicFiniteAutomaton(
                transitionsList.ToArray(),
                newInitialState,
                "Observer"
            );

            // Return the accessible part of it
            return automaton.AccessiblePart;
        }
        /// <summary>
        /// Determines if the given automaton (observador) is diagnosable.
        /// </summary>
        /// <param name="observador">The DFA observer.</param>
        /// <returns>True if diagnosable, otherwise false.</returns>
        public static bool IsDiagnosable(DeterministicFiniteAutomaton observador)
        {
            // Get all cycles in the DFA.
            var allCycles = GetAllCycles(observador);

            // Check if any cycle contains a state that includes "Y" in its name.
            // If such a cycle exists, the system is not diagnosable for f.
            return allCycles.All(cycle => !cycle.Any(state => state.ToString().Contains("Y")));

            // System is diagnosticable for f
        }

        /// <summary>
        /// Obtains all cycles by checking each state and running a depth-first
        /// cycle detection from that state.
        /// </summary>
        /// <param name="observador">The DFA observer.</param>
        /// <returns>A list of cycles, each cycle is a list of AbstractState.</returns>
        private static List<List<AbstractState>> GetAllCycles(DeterministicFiniteAutomaton observador)
        {
            var cycles = new List<List<AbstractState>>();

            // For every state, we initiate a DFS-based cycle detection
            foreach (var state in observador.States)
            {
                var currentPath = new Stack<AbstractState>();
                ExploreCycles(observador, state, currentPath, cycles);
            }

            return cycles;
        }

        /// <summary>
        /// Recursive DFS to detect cycles. Once a cycle is detected, 
        /// it is added to the 'cycles' list.
        /// </summary>
        /// <param name="observador">The DFA observer.</param>
        /// <param name="currentState">The state we are exploring from.</param>
        /// <param name="currentPath">Stack representing the current path of states.</param>
        /// <param name="cycles">The accumulator of found cycles.</param>
        private static void ExploreCycles(
            DeterministicFiniteAutomaton observador,
            AbstractState currentState,
            Stack<AbstractState> currentPath,
            List<List<AbstractState>> cycles)
        {
            // If we've seen this state before in currentPath, we found a cycle.
            if (currentPath.Contains(currentState))
            {
                // Build the cycle from the path stack until we reach currentState again.
                var cycle = currentPath.Reverse()
                                       .TakeWhile(s => !s.Equals(currentState))
                                       .ToList();
                cycle.Add(currentState);
                cycles.Add(cycle);

                return;
            }

            // Otherwise, continue DFS from the current state.
            currentPath.Push(currentState);

            // Explore transitions that originate from this state.
            foreach (var transition in observador.Transitions.Where(t => t.Origin.Equals(currentState)))
            {
                var nextState = transition.Destination;
                ExploreCycles(observador, nextState, currentPath, cycles);
            }

            // Backtrack
            currentPath.Pop();
        }
    }
}
