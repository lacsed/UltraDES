using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES
{
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

                // Percorre todos os eventos possíveis e checa Evaluate
                var func = _stateFunctions[s] ?? MTBDD.FullTree(0, numVars, -1);
                for (int e = 0; e < EventsNum; e++)
                {
                    var dest = func.Evaluate(e, numVars);
                    if (dest != -1)
                        transitions.Add((e, dest));
                }
                return transitions;
            }
        }


        public bool HasEvent(int s, int e) => (this[s, e] != -1);

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
            foreach (var tuple in values)
            {
                Add(origin, tuple.Item1, tuple.Item2);
            }
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
