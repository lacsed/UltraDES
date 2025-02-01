using System;
using System.Collections.Generic;
using System.Linq;

namespace UltraDES;

internal sealed class AdjacencyMatrixUShortImpl : IAdjacencyMatrixImplementation
{
    private readonly SortedList<int, int>[] _internal;
    private readonly ushort[] _eventMask; // 1 bit por evento, total 16 bits

    public int Length => _internal.Length;
    public int EventsNum { get; }

    public AdjacencyMatrixUShortImpl(int states, int eventsNum, bool preAllocate = false)
    {
        if (eventsNum > 16)
            throw new ArgumentException("AdjacencyMatrixUShortImpl suporta no máximo 16 eventos.", nameof(eventsNum));

        EventsNum = eventsNum;
        _internal = new SortedList<int, int>[states];
        _eventMask = new ushort[states];

        if (!preAllocate) return;
        for (int i = 0; i < states; i++)
        {
            _internal[i] = new SortedList<int, int>();
            _eventMask[i] = 0;
        }
    }

    // Indexador [s,e]: retorna destino ou -1 se não existe
    public int this[int s, int e] => HasEvent(s, e) ? _internal[s][e] : -1;

    // Indexador [s]: retorna SortedList, cria se null
    public List<(int, int)> this[int s] => _internal[s].Select(kvp => (kvp.Key, kvp.Value)).ToList();

    public bool HasEvent(int s, int e)
    {
        var mask = (ushort)(1U << e);
        return (_eventMask[s] & mask) != 0;
    }

    public void Add(int origin, (int, int)[] values)
    {
        if (_internal[origin] == null)
        {
            _internal[origin] = new SortedList<int, int>(values.Length);
            _eventMask[origin] = (ushort)0U;
        }

        foreach (var tuple in values)
        {
            int e = tuple.Item1;
            int dest = tuple.Item2;

            ushort mask = (ushort)(1U << e);
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
            _eventMask[origin] = (ushort)0U;
        }

        ushort mask = (ushort)(1U << e);
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
        ushort mask = (ushort)(1U << e);
        _eventMask[origin] &= (ushort)~mask;
    }

    public IAdjacencyMatrixImplementation Clone()
    {
        var clone = new AdjacencyMatrixUShortImpl(Length, EventsNum);
        for (int i = 0; i < Length; i++)
        {
            clone._eventMask[i] = _eventMask[i];
            if (_internal[i] == null) continue;
            // Cria nova SortedList e clona
            clone._internal[i] = new SortedList<int, int>();
            foreach (var kv in _internal[i])
            {
                clone._internal[i].Add(kv.Key, kv.Value);
            }
        }

        return clone;
    }

    public void TrimExcess()
    {
        foreach (var sl in _internal) sl?.TrimExcess();
    }
}