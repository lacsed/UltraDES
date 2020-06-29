// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using static System.Math;

namespace UltraDES
{
    /// <summary>
    /// Class Drawing.
    /// </summary>
    internal static class Drawing
    {
        /// <summary>
        /// Rounds the vector.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns>Vector.</returns>
        public static Vector RoundVector(Vector v)
        {
            double x = Round(v.X, Constants.NUMBER_OF_DIGITS_TO_ROUND);
            double y = Round(v.Y, Constants.NUMBER_OF_DIGITS_TO_ROUND);
            return new Vector(x, y);
        }

        /// <summary>
        /// Rounds the specified p.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="precision">The precision.</param>
        /// <returns>System.String.</returns>
        public static string round(double p, int precision = -1)
        {
            if (precision == -1) precision = Constants.NUMBER_OF_DIGITS_TO_ROUND;
            return Round(p, precision).ToString(CultureInfo.InvariantCulture).Replace(',', '.');
        }

        //coloca o automato em na configuração inicial na forma de um circulo
        /// <summary>
        /// Initials the configuration.
        /// </summary>
        /// <param name="statesList">The states list.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="center">The center.</param>
        private static void initialConfiguration(Dictionary<string, DrawingState> statesList, double radius, Vector center)
        {
            var angleUnit = (2 * PI) / (statesList.Count());
            var pos = 1;

            foreach (var item in statesList)
            {
                if (item.Value.initialState)
                {
                    item.Value.position = new Vector(center.X - radius, center.Y);
                }
                else
                {
                    var x = center.X - radius * Cos(pos * angleUnit);
                    var y = center.Y - radius * Sin(pos * angleUnit);
                    item.Value.position = new Vector(x, y);
                    ++pos;
                }
            }
        }

        //Calcula o offset (com sinal) baseado em um angulo de inclinação (em radianos) entre dois estados
        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="theta">The theta.</param>
        /// <returns>Vector.</returns>
        public static Vector getOffset(int offset, double theta)
        {
            double xd, yd;
            var margin = 0.3;

            if (theta > margin && theta < PI - margin)
                xd = -offset;
            else if (theta > PI + margin && theta < 2 * PI - margin)
                xd = offset;
            else
                xd = 0;

            if ((theta >= 0 && theta < PI / 2 - margin) || (theta > 3 * PI / 2 + margin && theta < 2 * PI))
                yd = -offset;
            else if (theta > PI / 2 + margin && theta < 3 * PI / 2 - margin)
                yd = offset;
            else
                yd = 0;

            var spacing = new Vector(xd, yd);
            return spacing;

        }

        /// <summary>
        /// Gets the maximum and minimum.
        /// </summary>
        /// <param name="states">The states.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="min">The minimum.</param>
        private static void getMaxAndMin(Dictionary<string, DrawingState> states, out Vector max, out Vector min)
        {
            min = new Vector(10000, 10000);
            max = new Vector();

            foreach (var item in states)
            {
                if (item.Value.position.Y < min.Y) min.Y = item.Value.position.Y;

                if (item.Value.position.X < min.X) min.X = item.Value.position.X;

                if (item.Value.position.Y > max.Y) max.Y = item.Value.position.Y;

                if (item.Value.position.X > max.X) max.X = item.Value.position.X;
            }
        }

        //gera arquivo de desenho do autômato
        /// <summary>
        /// Draws the SVG.
        /// </summary>
        /// <param name="G">The g.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="openAfterFinish">if set to <c>true</c> [open after finish].</param>
        public static void drawSVG(DeterministicFiniteAutomaton G, string fileName, bool openAfterFinish = true)
        {
            var statesList = prepare(G);
            getMaxAndMin(statesList, out var max, out var min);

            foreach (var item in statesList)
            {
                var y = item.Value.position.Y - min.Y + Constants.AREA_LIMIT_OFFSET;
                var x = item.Value.position.X - min.X + Constants.AREA_LIMIT_OFFSET;
                item.Value.position = new Vector(x, y);
            }

            if (!fileName.EndsWith(".svg")) fileName += ".svg";

            var writer = new StreamWriter(fileName);

            //cria cabeçalho
            FigureStream.WriteSVGHeader(writer, max.X + Constants.AREA_LIMIT_OFFSET, 
                                      max.Y - min.Y + 2 * Constants.AREA_LIMIT_OFFSET);

            foreach (var item in statesList) FigureStream.drawSVGState(writer, item.Value, Constants.STATE_RADIUS);

            FigureStream.drawFigureSVG(writer, statesList);

            //termina arquivo
            FigureStream.WriteSVGEnd(writer);
            writer.Close();

            if (openAfterFinish) Process.Start(fileName);
        }

        //gera arquivo de desenho do autômato
        /// <summary>
        /// Draws the latex figure.
        /// </summary>
        /// <param name="G">The g.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="openAfterFinish">if set to <c>true</c> [open after finish].</param>
        public static void drawLatexFigure(DeterministicFiniteAutomaton G, string fileName, bool openAfterFinish = true)
        {
            const string fontSize = "normalsize";
            var statesList = prepare(G);
            getMaxAndMin(statesList, out var max, out var min);

            foreach (var item in statesList)
            {
                var y = item.Value.position.Y - min.Y + Constants.AREA_LIMIT_OFFSET;
                var x = item.Value.position.X - min.X + Constants.AREA_LIMIT_OFFSET;
                item.Value.position = new Vector(x, y);
            }

            if (!fileName.EndsWith(".txt") && !fileName.EndsWith(".tex")) fileName += ".txt";

            var writer = new StreamWriter(fileName);

            //cria cabeçalho
            FigureStream.WriteLatexHeader(writer, max.X + Constants.AREA_LIMIT_OFFSET,
                                      max.Y - max.Y + 2 * Constants.AREA_LIMIT_OFFSET);

            //inverte as cordenas y para desenho no latex
            foreach (var item in statesList)
            {
                item.Value.position = new Vector(item.Value.position.X, -item.Value.position.Y);
                FigureStream.drawLatexState(writer, item.Value, Constants.STATE_RADIUS, fontSize);
            }

            FigureStream.drawFigureLatex(writer, statesList, fontSize);

            //termina arquivo
            FigureStream.WriteLatexEnd(writer);
            writer.Close();

            if (openAfterFinish) Process.Start(fileName);
        }

        //Simula a dinamica de força do sistema
        /// <summary>
        /// Prepares the specified g.
        /// </summary>
        /// <param name="G">The g.</param>
        /// <returns>Dictionary&lt;System.String, DrawingState&gt;.</returns>
        private static Dictionary<string, DrawingState> prepare(DeterministicFiniteAutomaton G)
        {
            //CRIANDO MÉTODO QUE SERA UTILIZADO PARA O DESENVOLVIMENTO DA BIBLIOTECA

            var drawingStatesList = new Dictionary<string, DrawingState>();           // lista de estados com cordenadas

            //Cria uma lista de estados com parametros de posição para serem desenhados
            foreach (var item in G.States)
            {
                var state = new DrawingState(item.ToString(), item.Marking) {initialState = item.Equals(G.InitialState)};
                drawingStatesList.Add(item.ToString(), state);
            }

            // aloca os estados dentro de um circulo de raio e centro que serão determinados

            var radius = Constants.SPRING_LENGTH * drawingStatesList.Count;
            var center = new Vector(radius + Constants.AREA_LIMIT_OFFSET, radius + Constants.AREA_LIMIT_OFFSET);

            initialConfiguration(drawingStatesList, radius, center);

            // Atraves da lista de transiçoes associa para cada estado quais sao os estados de origem e qual era o anterior;

            foreach (var item in G.Transitions)
            {
                if (drawingStatesList.TryGetValue(item.Origin.ToString(), out var state))
                {
                    var transitionName = item.Trigger + ((item.IsControllableTransition) ? "" : "'");
                    state.addDestination(drawingStatesList[item.Destination.ToString()], transitionName);
                }
                if (drawingStatesList.TryGetValue(item.Destination.ToString(), out state))
                    state.addOrigin(drawingStatesList[item.Origin.ToString()]);
            }


            var force = new Vector();
            var biggestForce = new Vector();
           
            for(int j = 0; j < Constants.MAX_ITERATIONS; ++j)
            {
                force.X = 0;
                force.Y = 0;

                foreach (var item in drawingStatesList)
                {
                    var attractionForce = item.Value.attractionForce(Constants.SPRING_CONSTANT, Constants.SPRING_LENGTH);
                    var repulsionForce = item.Value.repulsionForce(Constants.CONSTANT_OF_REPULSION, drawingStatesList);
                    force = attractionForce + repulsionForce;

                    if(force.Length > biggestForce.Length)
                    {
                        biggestForce = force;
                    }

                    // desloca estado para uma posição melhor
                    if (item.Value.initialState) continue;
                    var position = item.Value.position + Constants.DELTA * force;
                    position.X = Max(position.X, Constants.AREA_LIMIT_OFFSET);
                    position.X = Min(position.X, Constants.AREA_LIMIT_OFFSET + 2 * radius);
                    position.Y = Max(position.Y, Constants.AREA_LIMIT_OFFSET);
                    position.Y = Min(position.Y, Constants.AREA_LIMIT_OFFSET + 2 * radius);
                    item.Value.position = position;
                }
                if (biggestForce.Length <= Constants.STOP_CRITERION.Length) break;
            }
            return drawingStatesList;
        }

    }
}
