<Query Kind="Statements">
  <Reference Relative="..\..\..\source\repos\UltraDES_nuget\UltraDES\bin\Debug\netstandard2.0\UltraDES.dll">C:\Users\Lucas\source\repos\UltraDES_nuget\UltraDES\bin\Debug\netstandard2.0\UltraDES.dll</Reference>
  <Namespace>UltraDES</Namespace>
</Query>

// Creating States

State s1 = new State("s1", Marking.Marked);
State s2 = new State("s2", Marking.Unmarked);

// Creating Events

Event e1 = new Event("e1", Controllability.Controllable);
Event e2 = new Event("e2", Controllability.Uncontrollable);

Event e3 = new Event("e3", Controllability.Controllable);
Event e4 = new Event("e4", Controllability.Uncontrollable);

// Creating Automata

var G1 = new DeterministicFiniteAutomaton(new[]
  {
	new Transition(s1, e1, s2),
	new Transition(s2, e2, s1)
  }, s1, "G1");

var G2 = new DeterministicFiniteAutomaton(new[]
{
	new Transition(s1, e3, s2),
	new Transition(s2, e4, s1)
  }, s1, "G2");
  
// Making a Parallel composition
  
var K = G1.ParallelCompositionWith(G2); 
  
// Showing the Automaton
   
K.ShowAutomaton();
