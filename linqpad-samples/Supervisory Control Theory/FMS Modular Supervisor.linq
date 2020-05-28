<Query Kind="Statements">
  <Reference Relative="..\..\..\..\source\repos\UltraDES_nuget\UltraDES\bin\Debug\netstandard2.0\UltraDES.dll">C:\Users\Lucas\source\repos\UltraDES_nuget\UltraDES\bin\Debug\netstandard2.0\UltraDES.dll</Reference>
  <Namespace>UltraDES</Namespace>
</Query>


var s = Enumerable.Range(0, 6).Select(i => new State($"s{i}", i == 0 ? Marking.Marked : Marking.Unmarked)).ToArray();
var e = Enumerable.Range(0, 100).Select(i => new Event($"e{i}", i % 2 != 0 ? Controllability.Controllable : Controllability.Uncontrollable)).ToArray();

//----------------------------
// Plants
//----------------------------

// C1
var c1 = new DeterministicFiniteAutomaton(
    new[]
    {
        new Transition(s[0], e[11], s[1]),
        new Transition(s[1], e[12], s[0])
    },
    s[0], "C1");

// C2
var c2 = new DeterministicFiniteAutomaton(
    new[]
    {
        new Transition(s[0], e[21], s[1]),
        new Transition(s[1], e[22], s[0])
    },
    s[0], "C2");

// Milling
var milling = new DeterministicFiniteAutomaton(
    new[]
    {
        new Transition(s[0], e[41], s[1]),
        new Transition(s[1], e[42], s[0])
    },
    s[0], "Milling");

// MP
var mp = new DeterministicFiniteAutomaton(
    new[]
    {
        new Transition(s[0], e[81], s[1]),
        new Transition(s[1], e[82], s[0])
    },
    s[0], "MP");

// Lathe
var lathe = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[51], s[1]),
		new Transition(s[1], e[52], s[0]),
		new Transition(s[0], e[53], s[2]),
		new Transition(s[2], e[54], s[0])
	},
	s[0], "Lathe");

// C3
var c3 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[71], s[1]),
		new Transition(s[1], e[72], s[0]),
		new Transition(s[0], e[73], s[2]),
		new Transition(s[2], e[74], s[0])
	},
	s[0], "C3");

// Robot
var robot = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[31], s[1]),
		new Transition(s[1], e[32], s[0]),
		new Transition(s[0], e[33], s[2]),
		new Transition(s[2], e[34], s[0]),
		new Transition(s[0], e[35], s[3]),
		new Transition(s[3], e[36], s[0]),
		new Transition(s[0], e[37], s[4]),
		new Transition(s[4], e[38], s[0]),
		new Transition(s[0], e[39], s[5]),
		new Transition(s[5], e[30], s[0])
	},
	s[0], "Robot");

// MM
var mm = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[61], s[1]),
		new Transition(s[1], e[63], s[2]),
		new Transition(s[1], e[65], s[3]),
		new Transition(s[2], e[64], s[0]),
		new Transition(s[3], e[66], s[0])
	},
	s[0], "MM");

//----------------------------
// Specifications
//----------------------------

// E1
var e1 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[12], s[1]),
		new Transition(s[1], e[31], s[0])
	},
	s[0], "E1");

// E2
var e2 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[22], s[1]),
		new Transition(s[1], e[33], s[0])
	},
	s[0], "E2");

// E5
var e5 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[36], s[1]),
		new Transition(s[1], e[61], s[0])
	},
	s[0], "E5");

// E6
var e6 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[38], s[1]),
		new Transition(s[1], e[63], s[0])
	},
	s[0], "E6");

// E3
var e3 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[32], s[1]),
		new Transition(s[1], e[41], s[0]),
		new Transition(s[0], e[42], s[2]),
		new Transition(s[2], e[35], s[0])
	},
	s[0], "E3");

// E7
var e7 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[30], s[1]),
		new Transition(s[1], e[71], s[0]),
		new Transition(s[0], e[74], s[2]),
		new Transition(s[2], e[65], s[0])
	},
	s[0], "E7");

// E8
var e8 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[72], s[1]),
		new Transition(s[1], e[81], s[0]),
		new Transition(s[0], e[82], s[2]),
		new Transition(s[2], e[73], s[0])
	},
	s[0], "E8");

// E4
var e4 = new DeterministicFiniteAutomaton(
	new[]
	{
		new Transition(s[0], e[34], s[1]),
		new Transition(s[1], e[51], s[0]),
		new Transition(s[1], e[53], s[0]),
		new Transition(s[0], e[52], s[2]),
		new Transition(s[2], e[37], s[0]),
		new Transition(s[0], e[54], s[3]),
		new Transition(s[3], e[39], s[0])
	},
	s[0], "E4");

// Computing the confict solving specification 
var e78 = e7.ParallelCompositionWith(e8);

var timer = new Stopwatch();
timer.Start();

var sups = DeterministicFiniteAutomaton.LocalModularSupervisor(
	new[] { c1, c2, milling, lathe, robot, mm, c3, mp }, // Plants
	new[] { e1, e2, e3, e4, e5, e6, e78 }, // Specifications
	out List<DeterministicFiniteAutomaton> plants).ToArray(); // Modular Plant
timer.Stop();

Console.WriteLine("Computation Time: {0}", timer.ElapsedMilliseconds / 1000.0);

sups[0].ShowAutomaton();