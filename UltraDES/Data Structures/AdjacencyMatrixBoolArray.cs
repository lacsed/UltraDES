using System;
using System.Collections.Generic;

namespace UltraDES
{
    /// <summary>
    /// Implementação que usa bool[] por estado para marcar quais eventos existem.
    /// Só será utilizada se a quantidade de estados for menor que 1000 (critério dado).
    /// </summary>
    internal sealed class AdjacencyMatrixBoolArrayImpl : IAdjacencyMatrixImplementation
    {
        private readonly SortedList<int, int>[] _internal;
        private readonly bool[][] _events;  // Para cada estado, um array de bool com tamanho EventsNum

        public int Length => _internal.Length;
        public int EventsNum { get; }

        public AdjacencyMatrixBoolArrayImpl(int states, int eventsNum, bool preAllocate = false)
        {
            EventsNum = eventsNum;
            _internal = new SortedList<int, int>[states];
            _events = new bool[states][];

            if (preAllocate)
            {
                for (int s = 0; s < states; s++)
                {
                    _internal[s] = new SortedList<int, int>();
                    _events[s] = new bool[eventsNum];
                }
            }
        }

        /// <summary>
        /// Indexador [s, e]: retorna destino ou -1 se não existir transição
        /// </summary>
        public int this[int s, int e]
            => HasEvent(s, e) ? _internal[s][e] : -1;

        /// <summary>
        /// Indexador [s]: retorna a SortedList<evento, destino> para o estado 's' (criando se for null)
        /// </summary>
        public SortedList<int, int> this[int s]
            => _internal[s] ??= new SortedList<int, int>();

        /// <summary>
        /// Verifica se existe o evento 'e' no estado 's'
        /// </summary>
        public bool HasEvent(int s, int e)
        {
            // se _events[s] ainda não foi criado, não tem evento
            if (_events[s] == null) return false;
            return _events[s][e];
        }

        /// <summary>
        /// Adiciona múltiplos pares (evento, destino) para o estado 'origin'
        /// </summary>
        public void Add(int origin, (int, int)[] values)
        {
            if (_internal[origin] == null)
            {
                _internal[origin] = new SortedList<int, int>(values.Length);
                _events[origin] = new bool[EventsNum];
            }

            foreach (var (evt, dest) in values)
            {
                if (!_events[origin][evt])
                {
                    _internal[origin].Add(evt, dest);
                    _events[origin][evt] = true;
                }
                else
                {
                    // se já existe, checa determinismo
                    if (_internal[origin][evt] != dest)
                        throw new Exception("Automaton is not deterministic.");
                }
            }
        }

        /// <summary>
        /// Adiciona um par (evento, destino) ao estado 'origin'
        /// </summary>
        public void Add(int origin, int e, int dest)
        {
            if (_internal[origin] == null)
            {
                _internal[origin] = new SortedList<int, int>();
                _events[origin] = new bool[EventsNum];
            }

            if (!_events[origin][e])
            {
                _internal[origin].Add(e, dest);
                _events[origin][e] = true;
            }
            else
            {
                // se já existe, checa determinismo
                if (_internal[origin][e] != dest)
                    throw new Exception("Automaton is not deterministic.");
            }
        }

        /// <summary>
        /// Remove o evento 'e' do estado 'origin'
        /// </summary>
        public void Remove(int origin, int e)
        {
            _events[origin][e] = false;
            _internal[origin]?.Remove(e);
        }

        /// <summary>
        /// Clona a matriz de adjacência
        /// </summary>
        public IAdjacencyMatrixImplementation Clone()
        {
            var clone = new AdjacencyMatrixBoolArrayImpl(Length, EventsNum);
            for (int s = 0; s < Length; s++)
            {
                if (_internal[s] != null)
                {
                    // clona a SortedList
                    clone._internal[s] = new SortedList<int, int>();
                    foreach (var kv in _internal[s])
                    {
                        clone._internal[s].Add(kv.Key, kv.Value);
                    }
                }
                // clona o array de bool
                if (_events[s] != null)
                {
                    clone._events[s] = new bool[EventsNum];
                    Array.Copy(_events[s], clone._events[s], EventsNum);
                }
            }
            return clone;
        }

        /// <summary>
        /// Solicita às coleções internas que liberem memória extra
        /// </summary>
        public void TrimExcess()
        {
            foreach (var sl in _internal)
            {
                sl?.TrimExcess();
            }
        }
    }
}
