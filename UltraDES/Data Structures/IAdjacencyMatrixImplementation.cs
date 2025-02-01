using System;
using System.Collections.Generic;

namespace UltraDES
{
    internal interface IAdjacencyMatrixImplementation
    {
        int Length { get; }
        int EventsNum { get; }

        // Indexador [s, e] => retorna o destino ou -1
        int this[int s, int e] { get; }

        // Indexador [s] => retorna a SortedList (com os pares <evento, destino>)
        List<(int e, int s)> this[int s] { get; }
        bool HasEvent(int s, int e);

        void Add(int origin, (int, int)[] values);
        void Add(int origin, int e, int dest);
        void Remove(int origin, int e);

        IAdjacencyMatrixImplementation Clone();
        void TrimExcess();
    }
}
