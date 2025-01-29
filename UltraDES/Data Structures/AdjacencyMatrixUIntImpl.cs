using System;
using System.Collections.Generic;

namespace UltraDES;

internal sealed class AdjacencyMatrixUIntImpl : IAdjacencyMatrixImplementation
{
    private readonly SortedList<int, int>[] _internal;
    private readonly uint[] _eventMask; // 1 bit por evento, total 32 bits

    public int Length => _internal.Length;
    public int EventsNum { get; }

    public AdjacencyMatrixUIntImpl(int states, int eventsNum, bool preAllocate = false)
    {
        if (eventsNum > 32)
            throw new ArgumentException("AdjacencyMatrixUIntImpl suporta no máximo 32 eventos.", nameof(eventsNum));

        EventsNum = eventsNum;
        _internal = new SortedList<int, int>[states];
        _eventMask = new uint[states];

        if (preAllocate)
        {
            for (int i = 0; i < states; i++)
            {
                _internal[i] = new SortedList<int, int>();
                _eventMask[i] = 0U;
            }
        }
    }

    // Indexador [s,e]: retorna destino ou -1 se não existe
    public int this[int s, int e] => HasEvent(s, e) ? _internal[s][e] : -1;

    // Indexador [s]: retorna SortedList, cria se null
    public SortedList<int, int> this[int s] => _internal[s] ??= new SortedList<int, int>();

    public bool HasEvent(int s, int e)
    {
        uint mask = 1U << e;
        return (_eventMask[s] & mask) != 0;
    }

    public void Add(int origin, (int, int)[] values)
    {
        if (_internal[origin] == null)
        {
            _internal[origin] = new SortedList<int, int>(values.Length);
            _eventMask[origin] = 0U;
        }

        foreach (var tuple in values)
        {
            int e = tuple.Item1;
            int dest = tuple.Item2;

            uint mask = 1U << e;
            if ((_eventMask[origin] & mask) == 0)
            {
                _internal[origin].Add(e, dest);
                _eventMask[origin] |= mask;
            }
            else
            {
                if (_internal[origin][e] != dest)
                    throw new Exception("Automaton is not deterministic.");
            }
        }
    }

    public void Add(int origin, int e, int dest)
    {
        if (_internal[origin] == null)
        {
            _internal[origin] = new SortedList<int, int>();
            _eventMask[origin] = 0U;
        }

        uint mask = 1U << e;
        if ((_eventMask[origin] & mask) == 0)
        {
            _internal[origin].Add(e, dest);
            _eventMask[origin] |= mask;
        }
        else
        {
            if (_internal[origin][e] != dest)
                throw new Exception("Automaton is not deterministic.");
        }
    }

    public void Remove(int origin, int e)
    {
        _internal[origin]?.Remove(e);
        uint mask = 1U << e;
        _eventMask[origin] &= ~mask;
    }

    public IAdjacencyMatrixImplementation Clone()
    {
        var clone = new AdjacencyMatrixUIntImpl(Length, EventsNum);
        for (int i = 0; i < Length; i++)
        {
            clone._eventMask[i] = _eventMask[i];
            if (_internal[i] != null)
            {
                // Cria nova SortedList e clona
                clone._internal[i] = new SortedList<int, int>();
                foreach (var kv in _internal[i])
                {
                    clone._internal[i].Add(kv.Key, kv.Value);
                }
            }
        }

        return clone;
    }

    public void TrimExcess()
    {
        foreach (var sl in _internal)
        {
            sl?.TrimExcess();
        }
    }
}