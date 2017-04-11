using System;
using System.Windows;
using System.Collections.Generic;

namespace UltraDES
{
    public class DrawingState : State
    {
        public Vector position { get; set; }
        public bool initialState { get; set; }

        public Dictionary<DrawingState, Tuple<string, int>> destinationStates;
        public Dictionary<DrawingState, int> originStates;

        public DrawingState(string name, Marking marking) : base(name, marking)
        {
            this.position = new Vector();
            this.initialState = false;
            this.destinationStates = new Dictionary<DrawingState, Tuple<string, int>>();
            this.originStates = new Dictionary<DrawingState, int>();
        }

        // verifica se o estado dado eh igual a algum anterior 
        public bool isAOrigin(DrawingState state)
        {
            return originStates.ContainsKey(state);
        }

        // verifica se o estado dado eh igual a algum na lista de destino
        public bool IsADestination(DrawingState state)
        {
            return destinationStates.ContainsKey(state);
        } 

        //insere um estado na lita de estado de destino
        public void addDestination (DrawingState state, string eventName)
        {
            Tuple<string, int> value;
            if (destinationStates.TryGetValue(state, out value))
            {
                value = new Tuple<string, int>(value.Item1 + ", " + eventName, value.Item2 + 1);
                destinationStates[state] = value;
            }
            else
            {
                destinationStates.Add(state, new Tuple<string, int>(eventName, 1));
            }
        }

        //insere um estado na lista de estado de anterior
        public void addOrigin(DrawingState state)
        {
            int value;
            if(originStates.TryGetValue(state, out value))
            {
                originStates[state] = value + 1;
            }
            else
            {
                originStates.Add(state, 1);
            }
        }

        //calcula a força de atraçao total dado o comprimento da mola, uma constatne e o estado de referencia.
        public Vector attractionForce( double springConstant, double springLength)
        {
            Vector springLengthVector = new Vector();       //comprimento da mola representado como um vetor;
            Vector distance = new Vector();
            double theta;

            Vector destinationForce = new Vector(0, 0);
            Vector originForce = new Vector(0, 0);

            foreach (var item in this.destinationStates)
            {
                if (!this.Equals(item.Key))
                {
                    distance = item.Key.position - this.position;
                    theta = Math.Atan2(distance.Y, distance.X);
                    springLengthVector.X = springLength * Math.Cos(theta);
                    springLengthVector.Y = springLength * Math.Sin(theta);

                    destinationForce += springConstant * (distance - springLengthVector) * item.Value.Item2;
                }
            }

            foreach (var item in this.originStates)
            {
                if (!this.Equals(item.Key))
                {
                    distance = item.Key.position - this.position;
                    theta = Math.Atan2(distance.Y, distance.X);
                    springLengthVector.X = springLength * Math.Cos(theta);
                    springLengthVector.Y = springLength * Math.Sin(theta);

                    originForce += springConstant * (distance - springLengthVector) * item.Value;
                }
            }

            return (destinationForce + originForce);
        }

        //calcula a força de repulsão total dado uma constante, o estado de referencia e uma lista de todos os estados
        public Vector repulsionForce(double repulsionConstant, Dictionary<string, DrawingState> statesList)
        {
            double forceModule;
            Vector resultantForce = new Vector(0, 0);
            Vector force = new Vector(0, 0);
            Vector distance = new Vector();
            double theta;                // radianos


            foreach (var item in statesList)
            {
                distance = item.Value.position - this.position;

                if (!this.Equals(item.Value) && distance.Length < Constants.REPULSION_RADIUS)
                {
                    theta = Math.Atan2(distance.Y, distance.X);              //angulo da direçao da força
                    forceModule = repulsionConstant / (distance.Length * distance.Length);

                    //define a direção da força utilizano o metodo sign
                    force.X = -Math.Sign(distance.X) * Math.Abs(forceModule * Math.Cos(theta));
                    force.Y = -Math.Sign(distance.Y) * Math.Abs(forceModule * Math.Sin(theta));

                    resultantForce += force;
                }
            }

            return resultantForce;
        }
    }
}
