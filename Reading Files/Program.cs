using UltraDES;
using DFA = UltraDES.DeterministicFiniteAutomaton;

DFA.FromWmodFile("Medium-Schedule-1-Automata.wmod", out var plants, out var specifications);

DFA.MonolithicSupervisor(plants, specifications);

Console.WriteLine("Supervisor generated successfully.");