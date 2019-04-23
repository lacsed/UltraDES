<Query Kind="Statements">
  <Reference Relative="..\..\..\..\source\repos\UltraDES_nuget\UltraDES\bin\Debug\netstandard2.0\UltraDES.dll">C:\Users\Lucas\source\repos\UltraDES_nuget\UltraDES\bin\Debug\netstandard2.0\UltraDES.dll</Reference>
  <Namespace>UltraDES</Namespace>
</Query>

var s = Enumerable.Range(0, 6).Select(i => new State($"s{i}", i == 0 ? Marking.Marked : Marking.Unmarked)).ToArray();

var e = Enumerable.Range(0, 100).Select(i => new Event($"e{i}", i % 2 != 0 ? Controllability.Controllable : Controllability.Uncontrollable)).ToArray();


//plants

//M1
var M1 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[1], s[1]),
		new Transition(s[1], e[2], s[0])
	},
	s[0], "M1");

var M2 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[3], s[1]),
		new Transition(s[1], e[4], s[0])
	},
	s[0], "M2");

var M3 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[5], s[1]),
		new Transition(s[1], e[6], s[0])
	},
	s[0], "M3");

var M4 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[7], s[1]),
		new Transition(s[1], e[8], s[0])
	},
	s[0], "M4");

var M5 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[9], s[1]),
		new Transition(s[1], e[10], s[0])
	},
	s[0], "M5");

var M6 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[11], s[1]),
		new Transition(s[1], e[12], s[0])
	},
	s[0], "M6");

//Specifications

var e1 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[2], s[1]),
		new Transition(s[1], e[3], s[0])
	},
	s[0], "E1");

var e2 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[6], s[1]),
		new Transition(s[1], e[7], s[0])
	},
	s[0], "E2");

var e3 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[4], s[1]),
		new Transition(s[0], e[8], s[1]),
		new Transition(s[1], e[9], s[0])
	},
	s[0], "E3");

var e4 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[10], s[1]),
		new Transition(s[1], e[11], s[0])
	},
	s[0], "E4");

var plants = new[] { M1, M2, M3, M4, M5, M6 };
var specs = new[] { e1, e2, e3, e4 };

Console.WriteLine("Supervisor:");
var timer = new Stopwatch();
timer.Start();
var sup = DeterministicFiniteAutomaton.MonolithicSupervisor(plants, specs, true);
timer.Stop();

Console.WriteLine("\tStates: {0}", sup.Size);
Console.WriteLine("\tTransitions: {0}", sup.Transitions.Count());
Console.WriteLine("\tComputation Time: {0}", timer.ElapsedMilliseconds / 1000.0);
