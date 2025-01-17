using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PartStacker
{
    public struct Point3
    {
        public float X, Y, Z;

        public Point3(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public static Point3 fromStringArray(string[] s)
        {
            Point3 result = new Point3(0, 0, 0);
            result.X = float.Parse(s[s.Length - 3], new System.Globalization.CultureInfo("nl-NL"));
            result.Y = float.Parse(s[s.Length - 2], new System.Globalization.CultureInfo("nl-NL"));
            result.Z = float.Parse(s[s.Length - 1], new System.Globalization.CultureInfo("nl-NL"));

            return result;
        }

        override public string ToString()
        {
            return X.ToString("e").Replace(',', '.') + " " + Y.ToString("e").Replace(',', '.') + " " + Z.ToString("e").Replace(',', '.');
        }

        public static Point3 operator +(Point3 A, Point3 B)
        {
            return new Point3(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        }

        public static Point3 operator -(Point3 A, Point3 B)
        {
            return A + (-1) * B;
        }

        public static Point3 operator /(Point3 A, float l)
        {
            return A * (1 / l);
        }

        public static Point3 operator *(Point3 A, float l)
        {
            return new Point3(A.X * l, A.Y * l, A.Z * l);
        }

        public static Point3 operator *(float l, Point3 A)
        {
            return A * l;
        }

        public Point3 Normalized
        {
            get { return this / this.Length; }
        }

        public static bool operator ==(Point3 A, Point3 B)
        {
            return (A - B).Length < 0.0001;
        }

        public static bool operator !=(Point3 A, Point3 B)
        {
            return !(A == B);
        }

        public float Length
        {
            get { return (float)Math.Sqrt(this.Dot(this)); }
        }

        public float Dot(Point3 other)
        {
            return this.X * other.X + this.Y * other.Y + this.Z * other.Z;
        }

        public Point3 Cross(Point3 other)
        {
            return new Point3(this.Y * other.Z - this.Z * other.Y, this.Z * other.X - this.X * other.Z, this.X * other.Y - this.Y * other.X);
        }

        public Point3 Mirror()
        {
            return new Point3(-this.X, this.Y, this.Z);
        }

        public Point3 MirrorIT()
        {
            return new Point3(-this.X, this.Y, this.Z);
        }

        public Point3 Rotate(Point3 axis, float angle)
        {
            Point3 result = new Point3(0, 0, 0);

            axis = axis.Normalized;

            angle = angle / 180 * (float)Math.PI;

            float tr = t(angle);
            float cos = c(angle);
            float sin = s(angle);

            result.X = a1(angle, axis, tr, cos) * this.X + a2(angle, axis, tr, sin) * this.Y + a3(angle, axis, tr, sin) * this.Z;
            result.Y = b1(angle, axis, tr, sin) * this.X + b2(angle, axis, tr, cos) * this.Y + b3(angle, axis, tr, sin) * this.Z;
            result.Z = c1(angle, axis, tr, sin) * this.X + c2(angle, axis, tr, sin) * this.Y + c3(angle, axis, tr, cos) * this.Z;

            return result;
        }

        public Microsoft.Xna.Framework.Vector3 XNAVector3
        {
            get { return new Microsoft.Xna.Framework.Vector3(X, Y, Z); }
        }

        public float[] Coords
        {
            get { return new float[] { X, Y, Z }; }
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

        private static float a1(float angle, Point3 axis, float tr, float cos)
        {
            return (tr * axis.X * axis.X) + cos;
        }

        private static float a2(float angle, Point3 axis, float tr, float sin)
        {
            return (tr * axis.X * axis.Y) - (sin * axis.Z);
        }

        private static float a3(float angle, Point3 axis, float tr, float sin)
        {
            return (tr * axis.X * axis.Z) + (sin * axis.Y);
        }

        private static float b1(float angle, Point3 axis, float tr, float sin)
        {
            return (tr * axis.X * axis.Y) + (sin * axis.Z);
        }

        private static float b2(float angle, Point3 axis, float tr, float cos)
        {
            return (tr * axis.Y * axis.Y) + cos;
        }

        private static float b3(float angle, Point3 axis, float tr, float sin)
        {
            return (tr * axis.Y * axis.Z) - (sin * axis.X);
        }

        private static float c1(float angle, Point3 axis, float tr, float sin)
        {
            return (tr * axis.X * axis.Z) - (sin * axis.Y);
        }

        private static float c2(float angle, Point3 axis, float tr, float sin)
        {
            return (tr * axis.Y * axis.Z) + (sin * axis.X);
        }

        private static float c3(float angle, Point3 axis, float tr, float cos)
        {
            return (tr * axis.Z * axis.Z) + cos;
        }
        #endregion
    }
}
