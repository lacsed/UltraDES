using System.Windows;

namespace UltraDES
{
    public static class Constants
    {
        public static int NUMBER_OF_DIGITS_TO_ROUND = 2;       //Numeo de casas decimais quando necessário arredondar algum numero
        public static int STATE_RADIUS = 25;
        public static double SPRING_CONSTANT = 0.02893;//0.00063;
        public static double SPRING_LENGTH = 7 * STATE_RADIUS;
        public static double CONSTANT_OF_REPULSION = 220 * STATE_RADIUS; // valor original: 5000
        public static double REPULSION_RADIUS = 60000 * STATE_RADIUS;

        public static int AREA_LIMIT_OFFSET = 100;    //offset que garante area de desnho.
        public static double DELTA = 5;           // Taxa de deslocamento do estado (SUBSTITUI PARAMETRODESLOCAMENTO)

        public static int MAX_ITERATIONS = 100;   // numreo maximo de iteração

        public static Vector STOP_CRITERION = new Vector(0.01, 0.01); // modulo da força resulstante que deseja que o programa pare de rodar.

        // constante relacionadas ao desenho

        public static int TRANSITION_OFFSET = 8; // define a distancia entre duas transições
        public static int TEXT_OFFSET = TRANSITION_OFFSET+2; // define a transição entre o texto (nome da transição) e a seta
        public static int DISTANCE = 00; //distancia da borda do estado para o incio\fim da seta
    }
}
