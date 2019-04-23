using System;

namespace UltraDES
{
    public partial struct Vector
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Vector(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Length => Math.Sqrt(X * X + Y * Y);

        public static Vector operator -(Vector vector)
        {
            return new Vector(-vector.X, -vector.Y);
        }

        public Vector Negate => new Vector(-X, -Y);


        public static Vector operator +(Vector v1, Vector v2)
        {
            return new Vector(v1.X + v2.X,v1.Y + v2.Y);
        }

        public static Vector operator -(Vector v1, Vector v2)
        {
            return new Vector(v1.X - v2.X,v1.Y - v2.Y);
        }

        public static Vector operator *(Vector vector, double scalar)
        {
            return new Vector(vector.X * scalar,vector.Y * scalar);
        }

        public static Vector operator *(double scalar, Vector vector)
        {
            return new Vector(vector.X * scalar,vector.Y * scalar);
        }

        public static Vector operator /(Vector vector, double scalar)
        {
            return vector * (1.0 / scalar);
        }

        public static double operator *(Vector v1, Vector v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

    }
}
