namespace PartStacker.Geometry
{
    public struct Vector
    {
        public float X, Y, Z;

        public Vector(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public void Deconstruct(out float x, out float y, out float z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public readonly static Vector UnitX = new(1, 0, 0);
        public readonly static Vector UnitY = new(0, 1, 0);
        public readonly static Vector UnitZ = new(0, 0, 1);

        public static Vector operator+(Vector A, Vector B) => new Vector(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        public static Vector operator+(Vector A, float l) => new Vector(A.X + l, A.Y + l, A.Z + l);
        public static Vector operator-(Vector A, Vector B) => new Vector(A.X - B.X, A.Y - B.Y, A.Z - B.Z);
        public static Vector operator-(Vector A, float l) => new Vector(A.X - l, A.Y - l, A.Z - l);
        public static Vector operator-(Vector A) => new Vector(-A.X, -A.Y, -A.Z);
        public static Vector operator*(Vector A, float l) => new Vector(A.X * l, A.Y * l, A.Z * l);
        public static Vector operator*(float l, Vector A) => new Vector(A.X * l, A.Y * l, A.Z * l);
        public static Vector operator/(Vector A, float l) => new Vector(A.X / l, A.Y / l, A.Z / l);

        public float Length => (float)Math.Sqrt(Dot(this));
        public Vector Normalized => this / Length;

        public float Dot(Vector other) => X * other.X + Y * other.Y + Z * other.Z;
        public Vector Cross(Vector other) => new Vector(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);
        public Vector MirroredX() => new Vector(-X, Y, Z);

        public Vector Rotated(Vector axis, float angle)
        {
            Vector result = new Vector(0, 0, 0);

            axis = axis.Normalized;

            angle = angle / 180 * (float)Math.PI;

            float tr = t(angle);
            float cos = c(angle);
            float sin = s(angle);

            result.X = a1(angle, axis, tr, cos) * X + a2(angle, axis, tr, sin) * Y + a3(angle, axis, tr, sin) * Z;
            result.Y = b1(angle, axis, tr, sin) * X + b2(angle, axis, tr, cos) * Y + b3(angle, axis, tr, sin) * Z;
            result.Z = c1(angle, axis, tr, sin) * X + c2(angle, axis, tr, sin) * Y + c3(angle, axis, tr, cos) * Z;

            return result;
        }

        #region rotation stuff
        private static float t(float angle)
        {
            return 1 - (float)Math.Cos((double)angle);
        }

        private static float c(float angle)
        {
            return (float)Math.Cos((double)angle);
        }

        private static float s(float angle)
        {
            return (float)Math.Sin((double)angle);
        }

        private static float a1(float angle, Vector axis, float tr, float cos)
        {
            return (tr * axis.X * axis.X) + cos;
        }

        private static float a2(float angle, Vector axis, float tr, float sin)
        {
            return (tr * axis.X * axis.Y) - (sin * axis.Z);
        }

        private static float a3(float angle, Vector axis, float tr, float sin)
        {
            return (tr * axis.X * axis.Z) + (sin * axis.Y);
        }

        private static float b1(float angle, Vector axis, float tr, float sin)
        {
            return (tr * axis.X * axis.Y) + (sin * axis.Z);
        }

        private static float b2(float angle, Vector axis, float tr, float cos)
        {
            return (tr * axis.Y * axis.Y) + cos;
        }

        private static float b3(float angle, Vector axis, float tr, float sin)
        {
            return (tr * axis.Y * axis.Z) - (sin * axis.X);
        }

        private static float c1(float angle, Vector axis, float tr, float sin)
        {
            return (tr * axis.X * axis.Z) - (sin * axis.Y);
        }

        private static float c2(float angle, Vector axis, float tr, float sin)
        {
            return (tr * axis.Y * axis.Z) + (sin * axis.X);
        }

        private static float c3(float angle, Vector axis, float tr, float cos)
        {
            return (tr * axis.Z * axis.Z) + cos;
        }
        #endregion
    }
}
