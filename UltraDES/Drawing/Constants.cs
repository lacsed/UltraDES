// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020

namespace UltraDES
{
    /// <summary>
    /// Class Constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The number of digits to round
        /// </summary>
        public static int NUMBER_OF_DIGITS_TO_ROUND = 2;      
        /// <summary>
        /// The state radius
        /// </summary>
        public static int STATE_RADIUS = 25;
        /// <summary>
        /// The spring constant
        /// </summary>
        public static double SPRING_CONSTANT = 0.02893;//0.00063;
        /// <summary>
        /// The spring length
        /// </summary>
        public static double SPRING_LENGTH = 7 * STATE_RADIUS;
        /// <summary>
        /// The constant of repulsion
        /// </summary>
        public static double CONSTANT_OF_REPULSION = 220 * STATE_RADIUS; // valor original: 5000
        /// <summary>
        /// The repulsion radius
        /// </summary>
        public static double REPULSION_RADIUS = 60000 * STATE_RADIUS;

        /// <summary>
        /// The area limit offset
        /// </summary>
        public static int AREA_LIMIT_OFFSET = 100;    //offset que garante area de desnho.
        /// <summary>
        /// The delta
        /// </summary>
        public static double DELTA = 5;           // Taxa de deslocamento do estado (SUBSTITUI PARAMETRODESLOCAMENTO)

        /// <summary>
        /// The maximum iterations
        /// </summary>
        public static int MAX_ITERATIONS = 100;   // numreo maximo de iteração

        /// <summary>
        /// The stop criterion
        /// </summary>
        public static Vector STOP_CRITERION = new Vector(0.01, 0.01); // modulo da força resulstante que deseja que o programa pare de rodar.

        /// <summary>
        /// The transition offset
        /// </summary>
        public static int TRANSITION_OFFSET = 8; // define a distancia entre duas transições
        /// <summary>
        /// The text offset
        /// </summary>
        public static int TEXT_OFFSET = TRANSITION_OFFSET+2; // define a transição entre o texto (nome da transição) e a seta
        /// <summary>
        /// The distance
        /// </summary>
        public static int DISTANCE = 00; //distancia da borda do estado para o incio\fim da seta
    }
}
