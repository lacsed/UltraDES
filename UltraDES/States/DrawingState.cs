using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltraDES
{
    public class DrawingState : State
    {
        public Vector position { get; set; }
        public bool initialState { get; set; }

        public Dictionary<DrawingState, Tuple<string, int>> estadosDestino;
        public Dictionary<DrawingState, int> estadosAnterior;

        public DrawingState(string name, Marking marking) : base(name, marking)
        {
            this.position = new Vector();
            this.initialState = false;
            this.estadosDestino = new Dictionary<DrawingState, Tuple<string, int>>();
            this.estadosAnterior = new Dictionary<DrawingState, int>();
        }

        // verifica se o estado dado eh igual a algum anterior 
        public bool IgualAnterior(DrawingState estado)
        {
            return estadosAnterior.ContainsKey(estado);
        }

        // verifica se o estado dado eh igual a algum na lista de destino
        public bool IgualDestino(DrawingState estado)
        {
            return estadosDestino.ContainsKey(estado);
        } 

        //insere um estado na lita de estado de destino
        public void addDestination (DrawingState estado, string eventName)
        {
            Tuple<string, int> value;
            if (estadosDestino.TryGetValue(estado, out value))
            {
                value = new Tuple<string, int>(value.Item1 + ", " + eventName, value.Item2 + 1);
                estadosDestino[estado] = value;
            }
            else
            {
                estadosDestino.Add(estado, new Tuple<string, int>(eventName, 1));
            }
        }

        //insere um estado na lista de estado de anterior
        public void addOrigin(DrawingState estado)
        {
            int value;
            if(estadosAnterior.TryGetValue(estado, out value))
            {
                estadosAnterior[estado] = value + 1;
            }
            else
            {
                estadosAnterior.Add(estado, 1);
            }
        }

        //calcula a força de atraçao total dado o comprimento da mola, uma constatne e o estado de referencia.
        public Vector forcaAtracao( double constanteMola, double comprimentoMola)
        {
            Vector vetorComprimentoMola = new Vector();       //comprimento da mola representado como um vetor;
            Vector distancia = new Vector();
            double teta;

            Vector forcaSaidaDestino = new Vector(0, 0);
            Vector forcaSaidaAnterior = new Vector(0, 0);

            foreach (var item in this.estadosDestino)
            {
                if (!this.Equals(item.Key))
                {
                    distancia = item.Key.position - this.position;
                    teta = Math.Atan2(distancia.Y, distancia.X);
                    vetorComprimentoMola.X = comprimentoMola * Math.Cos(teta);
                    vetorComprimentoMola.Y = comprimentoMola * Math.Sin(teta);

                    forcaSaidaDestino += constanteMola * (distancia - vetorComprimentoMola) * item.Value.Item2;
                }
            }

            foreach (var item in this.estadosAnterior)
            {
                if (!this.Equals(item.Key))
                {
                    distancia = item.Key.position - this.position;
                    teta = Math.Atan2(distancia.Y, distancia.X);
                    vetorComprimentoMola.X = comprimentoMola * Math.Cos(teta);
                    vetorComprimentoMola.Y = comprimentoMola * Math.Sin(teta);

                    forcaSaidaAnterior += constanteMola * (distancia - vetorComprimentoMola) * item.Value;
                }
            }

            return (forcaSaidaDestino + forcaSaidaAnterior);
        }

        //calcula a força de repulsão total dado uma constante, o estado de referencia e uma lista de todos os estados
        public Vector forcaRepulsao(double constanteRepulsao, Dictionary<string, DrawingState> listaEstados)
        {
            double forcaModulo;
            Vector forcaResultante = new Vector(0, 0);
            Vector forca = new Vector(0, 0);
            Vector distancia = new Vector();
            double teta;                // radianos


            foreach (var item in listaEstados)
            {
                distancia = item.Value.position - this.position;

                if (!this.Equals(item.Value) && distancia.Length < Constants.REPULSION_RADIUS)
                {
                    teta = Math.Atan2(distancia.Y, distancia.X);              //angulo da direçao da força
                    forcaModulo = constanteRepulsao / (distancia.Length * distancia.Length);

                    //define a direção da força utilizano o metodo sign
                    forca.X = -Math.Sign(distancia.X) * Math.Abs(forcaModulo * Math.Cos(teta));
                    forca.Y = -Math.Sign(distancia.Y) * Math.Abs(forcaModulo * Math.Sin(teta));

                    forcaResultante += forca;
                }
            }

            return forcaResultante;
        }
    }
}
