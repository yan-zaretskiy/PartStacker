namespace PartStacker.Geometry
{
    public struct Vector
    {
        public double X, Y, Z;

        public Vector(double X, double Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public void Deconstruct(out double x, out double y, out double z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public readonly static Vector UnitX = new(1, 0, 0);
        public readonly static Vector UnitY = new(0, 1, 0);
        public readonly static Vector UnitZ = new(0, 0, 1);

        public static Vector operator+(Vector A, Vector B) => new Vector(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        public static Vector operator+(Vector A, double l) => new Vector(A.X + l, A.Y + l, A.Z + l);
        public static Vector operator-(Vector A, Vector B) => new Vector(A.X - B.X, A.Y - B.Y, A.Z - B.Z);
        public static Vector operator-(Vector A, double l) => new Vector(A.X - l, A.Y - l, A.Z - l);
        public static Vector operator-(Vector A) => new Vector(-A.X, -A.Y, -A.Z);
        public static Vector operator*(Vector A, double l) => new Vector(A.X * l, A.Y * l, A.Z * l);
        public static Vector operator*(double l, Vector A) => new Vector(A.X * l, A.Y * l, A.Z * l);
        public static Vector operator/(Vector A, double l) => new Vector(A.X / l, A.Y / l, A.Z / l);

        public double Length => Math.Sqrt(Dot(this));
        public Vector Normalized => this / Length;

        public double Dot(Vector other) => X * other.X + Y * other.Y + Z * other.Z;
        public Vector Cross(Vector other) => new Vector(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);
        public Vector MirroredX() => new Vector(-X, Y, Z);

        public Vector Rotated(Vector axis, double angle)
        {
            Vector result = new Vector(0, 0, 0);

            axis = axis.Normalized;

            angle = angle / 180 * Math.PI;

            double tr = t(angle);
            double cos = c(angle);
            double sin = s(angle);

            result.X = a1(angle, axis, tr, cos) * X + a2(angle, axis, tr, sin) * Y + a3(angle, axis, tr, sin) * Z;
            result.Y = b1(angle, axis, tr, sin) * X + b2(angle, axis, tr, cos) * Y + b3(angle, axis, tr, sin) * Z;
            result.Z = c1(angle, axis, tr, sin) * X + c2(angle, axis, tr, sin) * Y + c3(angle, axis, tr, cos) * Z;

            return result;
        }

        #region rotation stuff
        private static double t(double angle)
        {
            return 1 - Math.Cos((double)angle);
        }

        private static double c(double angle)
        {
            return Math.Cos((double)angle);
        }

        private static double s(double angle)
        {
            return Math.Sin((double)angle);
        }

        private static double a1(double angle, Vector axis, double tr, double cos)
        {
            return (tr * axis.X * axis.X) + cos;
        }

        private static double a2(double angle, Vector axis, double tr, double sin)
        {
            return (tr * axis.X * axis.Y) - (sin * axis.Z);
        }

        private static double a3(double angle, Vector axis, double tr, double sin)
        {
            return (tr * axis.X * axis.Z) + (sin * axis.Y);
        }

        private static double b1(double angle, Vector axis, double tr, double sin)
        {
            return (tr * axis.X * axis.Y) + (sin * axis.Z);
        }

        private static double b2(double angle, Vector axis, double tr, double cos)
        {
            return (tr * axis.Y * axis.Y) + cos;
        }

        private static double b3(double angle, Vector axis, double tr, double sin)
        {
            return (tr * axis.Y * axis.Z) - (sin * axis.X);
        }

        private static double c1(double angle, Vector axis, double tr, double sin)
        {
            return (tr * axis.X * axis.Z) - (sin * axis.Y);
        }

        private static double c2(double angle, Vector axis, double tr, double sin)
        {
            return (tr * axis.Y * axis.Z) + (sin * axis.X);
        }

        private static double c3(double angle, Vector axis, double tr, double cos)
        {
            return (tr * axis.Z * axis.Z) + cos;
        }
        #endregion
    }
}
