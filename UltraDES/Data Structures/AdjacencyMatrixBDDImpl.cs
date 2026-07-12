using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
    [Serializable]
    internal sealed class AdjacencyMatrixBDDImpl : IAdjacencyMatrixImplementation
    {
        private readonly MTBDD[] _stateFunctions;
        public int Length { get; }
        public int EventsNum { get; }
        private readonly int numVars; // numero de bits para representar eventsNum

        public AdjacencyMatrixBDDImpl(int states, int eventsNum, bool preAllocate = false)
        {
            Length = states;
            EventsNum = eventsNum;
            numVars = (int)Math.Ceiling(Math.Log(eventsNum, 2));

            _stateFunctions = new MTBDD[states];

            if (preAllocate)
            {
                for (int i = 0; i < states; i++)
                {
                    // Todos terminais inicial = -1
                    _stateFunctions[i] = MTBDD.FullTree(0, numVars, -1);
                }
            }
        }

        public int this[int s, int e]
        {
            get
            {
                if (s < 0 || s >= Length || e < 0 || e >= EventsNum)
                    throw new IndexOutOfRangeException();

                // Se ainda não existe, cria
                _stateFunctions[s] ??= MTBDD.FullTree(0, numVars, -1);
                // Avalia
                return _stateFunctions[s].Evaluate(e, numVars);
            }
        }

        public List<(int e, int s)> this[int s]
        {
            get
            {
                var transitions = new List<(int e, int s)>();
                if (s < 0 || s >= Length)
                    throw new IndexOutOfRangeException();

                var func = _stateFunctions[s] ?? MTBDD.FullTree(0, numVars, -1);
                func.CollectNonDefault(0, numVars, EventsNum, -1, transitions);
                return transitions;
            }
        }


        public bool HasEvent(int s, int e)
        {
            if (s < 0 || s >= Length || e < 0 || e >= EventsNum)
                throw new IndexOutOfRangeException();

            return _stateFunctions[s] != null && _stateFunctions[s].Evaluate(e, numVars) != -1;
        }

        public void Add(int origin, int e, int dest)
        {
            if (origin < 0 || origin >= Length || e < 0 || e >= EventsNum)
                throw new IndexOutOfRangeException();

            _stateFunctions[origin] ??= MTBDD.FullTree(0, numVars, -1);

            var current = _stateFunctions[origin].Evaluate(e, numVars);
            if (current == -1)
            {
                // update
                _stateFunctions[origin] = _stateFunctions[origin].Update(e, dest, 0, numVars);
            }
            else if (current != dest)
            {
                throw new Exception("Automaton is not deterministic.");
            }
        }

        public void Add(int origin, (int, int)[] values)
        {
            if (origin < 0 || origin >= Length)
                throw new IndexOutOfRangeException();

            if (_stateFunctions[origin] != null)
            {
                foreach (var tuple in values)
                    Add(origin, tuple.Item1, tuple.Item2);

                return;
            }

            var transitions = new Dictionary<int, int>(values.Length);
            foreach (var (e, dest) in values)
            {
                if (e < 0 || e >= EventsNum)
                    throw new IndexOutOfRangeException();

                if (transitions.TryGetValue(e, out var current))
                {
                    if (current != dest)
                        throw new Exception("Automaton is not deterministic.");
                }
                else
                {
                    transitions.Add(e, dest);
                }
            }

            _stateFunctions[origin] = BuildSparseTree(transitions.Select(kvp => (kvp.Key, kvp.Value)).ToList(), 0);
        }

        private MTBDD BuildSparseTree(List<(int e, int dest)> values, int level)
        {
            if (values.Count == 0)
                return MTBDD.Terminal(-1);

            if (level == numVars)
                return MTBDD.Terminal(values[0].dest);

            var low = new List<(int e, int dest)>();
            var high = new List<(int e, int dest)>();

            foreach (var value in values)
            {
                var bit = (value.e >> (numVars - 1 - level)) & 1;
                if (bit == 0)
                    low.Add(value);
                else
                    high.Add(value);
            }

            return MTBDD.Node(level, BuildSparseTree(low, level + 1), BuildSparseTree(high, level + 1));
        }

        public void Remove(int origin, int e)
        {
            if (origin < 0 || origin >= Length || e < 0 || e >= EventsNum)
                throw new IndexOutOfRangeException();

            if (_stateFunctions[origin] == null)
                return;

            // define de volta para -1
            _stateFunctions[origin] = _stateFunctions[origin].Update(e, -1, 0, numVars);
        }

        public IAdjacencyMatrixImplementation Clone()
        {
            var clone = new AdjacencyMatrixBDDImpl(Length, EventsNum);
            for (int i = 0; i < Length; i++)
            {
                // MTBDD é imutável, podemos compartilhar a mesma referência
                clone._stateFunctions[i] = _stateFunctions[i];
            }
            return clone;
        }

        public void TrimExcess()
        {
            // Se quisermos, poderíamos tentar reordenar variáveis ou 
            // forçar minimizações adicionais. Mas no momento, nada é feito.
        }
    }
}
