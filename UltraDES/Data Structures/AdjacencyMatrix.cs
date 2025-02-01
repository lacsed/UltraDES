using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UltraDES;

namespace UltraDES
{

    [Serializable]
    internal sealed class AdjacencyMatrix
    {
        // Mantém uma referência para a implementação escolhida
        private readonly IAdjacencyMatrixImplementation _impl;

        /// <summary>
        /// Construtor principal. Se 'eventsNum' <= 64, usa implementação com bitmask (ulong),
        /// caso contrário, usa BitArray.
        /// O parâmetro 'preAllocate' pode ser repassado a ambas as implementações.
        /// </summary>
        public AdjacencyMatrix(int states, int eventsNum, bool preAllocate = false)
        {
            _impl = eventsNum switch
            {
                <= 16 => new AdjacencyMatrixUShortImpl(states, eventsNum, preAllocate),
                <= 32 => new AdjacencyMatrixUIntImpl(states, eventsNum, preAllocate),
                <= 64 => new AdjacencyMatrixBitMask(states, eventsNum, preAllocate),
                _ => new AdjacencyMatrixBDDImpl(states, eventsNum, preAllocate)
            };
            //_impl = new AdjacencyMatrixBDDImpl(states, eventsNum, preAllocate);
            //_impl = new AdjacencyMatrixBitArrayImpl(states, eventsNum, preAllocate);
        }

        // Construtor usado internamente para clone:
        private AdjacencyMatrix(IAdjacencyMatrixImplementation impl) => _impl = impl;

        /// <summary>
        /// Retorna a quantidade de estados.
        /// </summary>
        public int Length => _impl.Length;

        /// <summary>
        /// Indexador: retorna o destino, ou -1 se não houver transição.
        /// </summary>
        public int this[int s, int e] => _impl[s, e];

        public bool TryGet(int s, int e, out int value)
        {
            value = this[s, e];
            return value != -1;
        }

        /// <summary>
        /// Indexador: retorna a SortedList de transições (evento -> destino) para o estado 's'.
        /// Se não existir, cria e retorna.
        /// </summary>
        public List<(int e, int s)> this[int s] => _impl[s];

        /// <summary>
        /// Verifica se o estado 's' tem o evento 'e'.
        /// </summary>
        public bool HasEvent(int s, int e) => _impl.HasEvent(s, e);

        /// <summary>
        /// Adiciona vários pares (evento, destino) em um único estado.
        /// </summary>
        public void Add(int origin, (int, int)[] values) => _impl.Add(origin, values);

        /// <summary>
        /// Adiciona um par (evento, destino) em um estado.
        /// </summary>
        public void Add(int origin, int e, int dest) => _impl.Add(origin, e, dest);

        /// <summary>
        /// Remove o evento 'e' do estado 'origin'.
        /// </summary>
        public void Remove(int origin, int e) => _impl.Remove(origin, e);

        /// <summary>
        /// Clona toda a matriz de adjacência (cópia profunda).
        /// </summary>
        public AdjacencyMatrix Clone()
        {
            var clonedImpl = _impl.Clone();
            return new AdjacencyMatrix(clonedImpl);
        }

        /// <summary>
        /// Tenta reduzir a quantidade de memória ocupada pelas coleções internas.
        /// Em coleções grandes, pode ser útil.
        /// </summary>
        public void TrimExcess() => _impl.TrimExcess();
    }
}
