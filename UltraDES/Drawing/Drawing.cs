using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;

namespace UltraDES
{
    internal static class Drawing
    {
        public static Vector RoundVector(Vector v)
        {
            double x = Math.Round(v.X, Constants.NUMBER_OF_DIGITS_TO_ROUND);
            double y = Math.Round(v.Y, Constants.NUMBER_OF_DIGITS_TO_ROUND);
            return new Vector(x, y);
        }

        public static string round(double p, int precision = -1)
        {
            if (precision == -1) precision = Constants.NUMBER_OF_DIGITS_TO_ROUND;
            return Math.Round(p, precision).ToString().Replace(',', '.');
        }

        //coloca o automato em na configuração inicial na forma de um circulo
        private static void initialConfiguration(Dictionary<string, DrawingState> statesList, double radius, Vector center)
        {
            double angleUnit = (2 * Math.PI) / (statesList.Count());
            int pos = 1;

            foreach (var item in statesList)
            {
                if (item.Value.initialState)
                {
                    item.Value.position = new Vector(center.X - radius, center.Y);
                }
                else
                {
                    var x = center.X - radius * Math.Cos(pos * angleUnit);
                    var y = center.Y - radius * Math.Sin(pos * angleUnit);
                    item.Value.position = new Vector(x, y);
                    ++pos;
                }
            }
        }

        //Calcula o offset (com sinal) baseado em um angulo de inclinação (em radianos) entre dois estados
        public static Vector getOffset(int offset, double theta)
        {
            double xd = 0, yd = 0;
            double margin = 0.3;

            if (theta > margin && theta < Math.PI - margin)
            {
                xd = -offset;

            }
            else if (theta > Math.PI + margin && theta < 2 * Math.PI - margin)
            {
                xd = offset;
            }
            else
            {
                xd = 0;
            }

            if ((theta >= 0 && theta < Math.PI / 2 - margin) || (theta > 3 * Math.PI / 2 + margin && theta < 2 * Math.PI))
            {
                yd = -offset;
            }
            else if (theta > Math.PI / 2 + margin && theta < 3 * Math.PI / 2 - margin)
            {
                yd = offset;
            }
            else
            {
                yd = 0;
            }

            Vector spacing = new Vector(xd, yd);
            return spacing;

        }

        private static void getMaxAndMin(Dictionary<string, DrawingState> states, out Vector max, out Vector min)
        {
            min = new Vector(10000, 10000);
            max = new Vector();

            foreach (var item in states)
            {
                if (item.Value.position.Y < min.Y)
                {
                    min.Y = item.Value.position.Y;
                }

                if (item.Value.position.X < min.X)
                {
                    min.X = item.Value.position.X;
                }

                if (item.Value.position.Y > max.Y)
                {
                    max.Y = item.Value.position.Y;
                }

                if (item.Value.position.X > max.X)
                {
                    max.X = item.Value.position.X;
                }
            }
        }

        //gera arquivo de desenho do autômato
        public static void drawSVG(DeterministicFiniteAutomaton G, string fileName, bool openAfterFinish = true)
        {
            Dictionary<string, DrawingState> statesList = prepare(G);
            Vector max, min;
            getMaxAndMin(statesList, out max, out min);

            foreach (var item in statesList)
            {
                var y = item.Value.position.Y - min.Y + Constants.AREA_LIMIT_OFFSET;
                var x = item.Value.position.X - min.X + Constants.AREA_LIMIT_OFFSET;
                item.Value.position = new Vector(x, y);
            }

            if (!fileName.EndsWith(".svg"))
            {
                fileName += ".svg";
            }

            StreamWriter writer = new StreamWriter(fileName);

            //cria cabeçalho
            FigureStream.WriteSVGHeader(writer, max.X + Constants.AREA_LIMIT_OFFSET, 
                                      max.Y - min.Y + 2 * Constants.AREA_LIMIT_OFFSET);

            foreach (var item in statesList)
            {
                FigureStream.drawSVGState(writer, item.Value, Constants.STATE_RADIUS);
            }

            FigureStream.drawFigureSVG(writer, statesList);

            //termina arquivo
            FigureStream.WriteSVGEnd(writer);
            writer.Close();

            if (openAfterFinish) System.Diagnostics.Process.Start(fileName);
        }

        //gera arquivo de desenho do autômato
        public static void drawLatexFigure(DeterministicFiniteAutomaton G, string fileName, bool openAfterFinish = true)
        {
            string fontSize = "normalsize";
            Dictionary<string, DrawingState> statesList = prepare(G);
            Vector max, min;
            getMaxAndMin(statesList, out max, out min);

            foreach (var item in statesList)
            {
                var y = item.Value.position.Y - min.Y + Constants.AREA_LIMIT_OFFSET;
                var x = item.Value.position.X - min.X + Constants.AREA_LIMIT_OFFSET;
                item.Value.position = new Vector(x, y);
            }

            if (!fileName.EndsWith(".txt") && !fileName.EndsWith(".tex"))
            {
                fileName += ".txt";
            }

            StreamWriter writer = new StreamWriter(fileName);

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

            if (openAfterFinish) System.Diagnostics.Process.Start(fileName);
        }

        //Simula a dinamica de força do sistema
        private static Dictionary<string, DrawingState> prepare(DeterministicFiniteAutomaton G)
        {
            //CRIANDO MÉTODO QUE SERA UTILIZADO PARA O DESENVOLVIMENTO DA BIBLIOTECA

            var drawingStatesList = new Dictionary<string, DrawingState>();           // lista de estados com cordenadas

            //Cria uma lista de estados com parametros de posição para serem desenhados
            foreach (var item in G.States)
            {
                DrawingState state = new DrawingState(item.ToString(), item.Marking);
                state.initialState = item.Equals(G.InitialState);
                drawingStatesList.Add(item.ToString(), state);
            }

            // aloca os estados dentro de um circulo de raio e centro que serão determinados

            double radius = Constants.SPRING_LENGTH * drawingStatesList.Count();
            Vector center = new Vector(radius + Constants.AREA_LIMIT_OFFSET, radius + Constants.AREA_LIMIT_OFFSET);

            initialConfiguration(drawingStatesList, radius, center);

            // Atraves da lista de transiçoes associa para cada estado quais sao os estados de origem e qual era o anterior;

            string transitionName;

            foreach (var item in G.Transitions)
            {
                DrawingState state;
                if (drawingStatesList.TryGetValue(item.Origin.ToString(), out state))
                {
                    transitionName = item.Trigger.ToString() + ((item.IsControllableTransition) ? "" : "'");
                    state.addDestination(drawingStatesList[item.Destination.ToString()], transitionName);
                }
                if (drawingStatesList.TryGetValue(item.Destination.ToString(), out state))
                {
                    state.addOrigin(drawingStatesList[item.Origin.ToString()]);
                }
            }


            Vector repulsionForce, attractionForce, position;
            Vector force = new Vector();
            Vector biggestForce = new Vector();
           
            for(int j = 0; j < Constants.MAX_ITERATIONS; ++j)
            {
                force.X = 0;
                force.Y = 0;

                foreach (var item in drawingStatesList)
                {
                    attractionForce = item.Value.attractionForce(Constants.SPRING_CONSTANT, Constants.SPRING_LENGTH);
                    repulsionForce = item.Value.repulsionForce(Constants.CONSTANT_OF_REPULSION, drawingStatesList);
                    force = attractionForce + repulsionForce;

                    if(force.Length > biggestForce.Length)
                    {
                        biggestForce = force;
                    }

                    // desloca estado para uma posição melhor
                    if (!item.Value.initialState)
                    {
                        position = item.Value.position + Constants.DELTA * force;
                        position.X = Math.Max(position.X, Constants.AREA_LIMIT_OFFSET);
                        position.X = Math.Min(position.X, Constants.AREA_LIMIT_OFFSET + 2 * radius);
                        position.Y = Math.Max(position.Y, Constants.AREA_LIMIT_OFFSET);
                        position.Y = Math.Min(position.Y, Constants.AREA_LIMIT_OFFSET + 2 * radius);
                        item.Value.position = position;
                    }
                }
                if (biggestForce.Length <= Constants.STOP_CRITERION.Length) break;
            }
            return drawingStatesList;
        }

    }
}
