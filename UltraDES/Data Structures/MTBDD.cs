using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace UltraDES
{
    /// <summary>
    /// Estrutura-auxiliar para lookup do unique table
    /// Armazena (variable, low, high) e gera hash de forma eficiente
    /// </summary>
    internal readonly struct NodeKey(int variable, MTBDD low, MTBDD high) : IEquatable<NodeKey>
    {
        public readonly int Variable = variable;
        public readonly MTBDD Low = low;
        public readonly MTBDD High = high;

        public bool Equals(NodeKey other)
        {
            return Variable == other.Variable
                && Low == other.Low
                && High == other.High;
        }

        public override bool Equals(object obj) => obj is NodeKey nk && Equals(nk);

        public override int GetHashCode()
        {
            // Combine the hashes (HashCode.Combine é .NET Standard 2.1+)
            return HashCode.Combine(Variable, Low, High);
        }
    }

    /// <summary>
    /// Gerenciador global de nós do BDD (unique table).
    /// </summary>
    internal sealed class MTBDDManager
    {
        // Tabela para nós internos (variable != -1).
        private readonly ConcurrentDictionary<NodeKey, MTBDD> _uniqueTable = new();

        // Tabela para nós terminais.
        private readonly ConcurrentDictionary<int, MTBDD> _terminals = new();

        // Singleton do Manager (pode ser 'static' se quiser global).
        public static MTBDDManager Instance { get; } = new();

        private MTBDDManager() { }

        /// <summary>
        /// Obtém nó terminal (interning). Se já existir, retorna o existente.
        /// </summary>
        public MTBDD Terminal(int value)
        {
            // Se muitos valores inteiros diferentes forem possíveis, talvez seja
            // mais eficiente criar nós sem caching. Mas aqui fazemos caching mesmo assim.
            return _terminals.GetOrAdd(value, v => new MTBDD(v));
        }

        /// <summary>
        /// Cria (ou obtém) nó interno com redução automática
        /// - se low == high, retorna low
        /// - se ambos terminais e valor for igual, retorna low
        /// - senão, busca em unique table
        /// </summary>
        public MTBDD Node(int variable, MTBDD low, MTBDD high)
        {
            // Aplicar redução
            if (ReferenceEquals(low, high))
                return low;
            if (low.IsTerminal && high.IsTerminal && low.Value == high.Value)
                return low;

            // Caso geral, consulta unique table
            var key = new NodeKey(variable, low, high);
            return _uniqueTable.GetOrAdd(key, k => new MTBDD(k.Variable, k.Low, k.High));
        }
    }

    /// <summary>
    /// Uma implementação de Multi-Terminal BDD (MTBDD) com unique table
    /// e técnicas para reduzir uso de memória e melhorar performance.
    /// 
    /// Note que todos os construtores são 'internal', pois a criação 
    /// passa pelo MTBDDManager para manter a canonicidade.
    /// 
    /// Observe também que mantemos a API pública pedida (Terminal, Node, etc.)
    /// mas internamente redirecionamos para o Manager.
    /// </summary>
    internal sealed class MTBDD
    {
        #region Campos
        public bool IsTerminal { get; }
        public int Value { get; }       // Valor, se terminal
        public int Variable { get; }    // Índice da variável, se não-terminal
        public MTBDD Low { get; }
        public MTBDD High { get; }
        #endregion

        #region Construtores internos

        // Nó terminal
        internal MTBDD(int value)
        {
            IsTerminal = true;
            Value = value;
        }

        // Nó não-terminal
        internal MTBDD(int variable, MTBDD low, MTBDD high)
        {
            IsTerminal = false;
            Variable = variable;
            Low = low;
            High = high;
        }

        #endregion

        #region Criação de Nós (API externa)

        // redireciona para o Manager
        public static MTBDD Terminal(int value)
            => MTBDDManager.Instance.Terminal(value);

        public static MTBDD Node(int variable, MTBDD low, MTBDD high)
            => MTBDDManager.Instance.Node(variable, low, high);

        /// <summary>
        /// Constrói uma árvore completa (para níveis de 'level' até 'numVars')
        /// com todas as folhas com o mesmo valor. 
        /// Se reduce for true, aproveita Node(...) com redução canônica.
        /// </summary>
        public static MTBDD FullTree(int level, int numVars, int value, bool reduce = true)
        {
            if (level == numVars)
                return Terminal(value);

            // Aqui podemos construir iterativamente ou recursivamente.
            // Exemplo recursivo (cuidado com muito numVars) - mas podemos
            // mitigar usando tail recursion ou abordagem BFS.
            var left = FullTree(level + 1, numVars, value, reduce);
            var right = FullTree(level + 1, numVars, value, reduce);

            return reduce ? Node(level, left, right) :
                // constrói nó sem usar manager
                new MTBDD(level, left, right);
        }

        #endregion

        #region Evaluate (iterativo)

        /// <summary>
        /// Avalia a função BDD para um inteiro 'e' de 'numVars' bits.
        /// Implementação iterativa para evitar stack overflow em árvores grandes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Evaluate(int e, int numVars)
        {
            var current = this;
            while (!current.IsTerminal)
            {
                int bit = (e >> (numVars - 1 - current.Variable)) & 1;
                current = (bit == 0) ? current.Low : current.High;
            }
            return current.Value;
        }

        #endregion

        #region Update (recursivo + gerenciado)

        /// <summary>
        /// Atualiza a função (imutavelmente) definindo f(e) = dest, a partir do nível 'level'.
        /// Retorna nova árvore com a atualização.
        /// </summary>
        public MTBDD Update(int e, int dest, int level, int numVars)
        {
            // Se chegamos no fim, cria (ou pega) nó terminal
            if (level == numVars)
                return Terminal(dest);

            // Se o nó atual é terminal e ainda precisamos expandir,
            // expandimos sem redução para então atualizar.
            if (IsTerminal)
            {
                var full = FullTree(level, numVars, this.Value, reduce: false);
                return full.Update(e, dest, level, numVars);
            }

            // Se a variável do nó atual é maior que 'level', 
            // significa que não há nível para 'level' e precisamos expandir:
            if (Variable > level)
            {
                var full = FullTree(level, numVars, this.Evaluate(e, numVars), reduce: false);
                return full.Update(e, dest, level, numVars);
            }

            // Se a variável do nó == level, vamos descer no filho certo
            if (Variable == level)
            {
                int bitAtLevel = (e >> (numVars - 1 - level)) & 1;
                if (bitAtLevel == 0)
                {
                    var newLow = Low.Update(e, dest, level + 1, numVars);
                    return Node(Variable, newLow, High);
                }
                else
                {
                    var newHigh = High.Update(e, dest, level + 1, numVars);
                    return Node(Variable, Low, newHigh);
                }
            }
            else
            {
                // Caso improbable se Variable < level, mas podemos tratar
                // recursivamente chamando update no próximo nível
                // ou simplesmente retomar Evaluate e forçar um patch.
                // Aqui faremos recursão:
                var newLow2 = Low.Update(e, dest, level, numVars);
                var newHigh2 = High.Update(e, dest, level, numVars);
                return Node(Variable, newLow2, newHigh2);
            }
        }

        #endregion

        #region Equality & HashCode

        public override bool Equals(object obj)
        {
            // Como temos interning, podemos usar ReferenceEquals 
            // para diferenciar nós distintos. 
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            // Se IsTerminal, podemos devolver Value.GetHashCode();
            // caso contrário, combinamos (Variable, Low, High).
            // Entretanto, se está no Unique Table, raramente 
            // chamaremos GetHashCode manualmente. 
            return IsTerminal ? Value.GetHashCode()
                              : HashCode.Combine(Variable, Low, High);
        }

        #endregion
    }
}
