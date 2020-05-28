// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace UltraDES
{
    /// <summary>
    /// Class DrawingState.
    /// Implements the <see cref="UltraDES.State" />
    /// </summary>
    /// <seealso cref="UltraDES.State" />
    public class DrawingState : State
    {
        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        public Vector position { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [initial state].
        /// </summary>
        /// <value><c>true</c> if [initial state]; otherwise, <c>false</c>.</value>
        public bool initialState { get; set; }

        /// <summary>
        /// The destination states
        /// </summary>
        public Dictionary<DrawingState, Tuple<string, int>> destinationStates;
        /// <summary>
        /// The origin states
        /// </summary>
        public Dictionary<DrawingState, int> originStates;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingState"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="marking">The marking.</param>
        public DrawingState(string name, Marking marking) : base(name, marking)
        {
            this.position = new Vector();
            this.initialState = false;
            this.destinationStates = new Dictionary<DrawingState, Tuple<string, int>>();
            this.originStates = new Dictionary<DrawingState, int>();
        }

        // verifica se o estado dado eh igual a algum anterior 
        /// <summary>
        /// Determines whether [is a origin] [the specified state].
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns><c>true</c> if [is a origin] [the specified state]; otherwise, <c>false</c>.</returns>
        public bool isAOrigin(DrawingState state)
        {
            return originStates.ContainsKey(state);
        }

        // verifica se o estado dado eh igual a algum na lista de destino
        /// <summary>
        /// Determines whether [is a destination] [the specified state].
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns><c>true</c> if [is a destination] [the specified state]; otherwise, <c>false</c>.</returns>
        public bool IsADestination(DrawingState state)
        {
            return destinationStates.ContainsKey(state);
        }

        //insere um estado na lita de estado de destino
        /// <summary>
        /// Adds the destination.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="eventName">Name of the event.</param>
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
        /// <summary>
        /// Adds the origin.
        /// </summary>
        /// <param name="state">The state.</param>
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
        /// <summary>
        /// Attractions the force.
        /// </summary>
        /// <param name="springConstant">The spring constant.</param>
        /// <param name="springLength">Length of the spring.</param>
        /// <returns>Vector.</returns>
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
        /// <summary>
        /// Repulsions the force.
        /// </summary>
        /// <param name="repulsionConstant">The repulsion constant.</param>
        /// <param name="statesList">The states list.</param>
        /// <returns>Vector.</returns>
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
