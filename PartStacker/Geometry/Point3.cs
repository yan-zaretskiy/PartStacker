namespace PartStacker.Geometry
{
    public struct Point3
    {
        public float X, Y, Z;

        public readonly static Point3 Origin = new Point3(0, 0, 0);

        public Point3(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Vector Vector => new Vector(X, Y, Z);

        public static Point3 operator+(Point3 A, Vector B) => new Point3(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        public static Point3 operator+(Vector A, Point3 B) => new Point3(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        public static Vector operator-(Point3 A, Point3 B) => new Vector(A.X - B.X, A.Y - B.Y, A.Z - B.Z);

        public Point3 MirrorX() => new Point3(-X, Y, Z);
        public Point3 Scale(float factor) => new Point3(X * factor, Y * factor, Z * factor);
        public Point3 Rotate(Vector axis, float angle, Point3 about) => (this - about).Rotate(axis, angle) + about;
    }
}
