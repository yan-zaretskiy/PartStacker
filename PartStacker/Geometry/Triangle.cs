namespace PartStacker.Geometry
{
    public struct Triangle
    {
        public Vector Normal;
        public Point3 v1, v2, v3;

        public Triangle(Vector normal, Point3 v1, Point3 v2, Point3 v3)
        {
            this.Normal = normal;

            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }

        public Triangle Mirrored()
        {
            return new Triangle(Normal.MirroredX(), v1.MirroredX(), v3.MirroredX(), v2.MirroredX());
        }

        public Triangle Rotated(Vector axis, float angle)
        {
            return new Triangle(Normal.Rotated(axis, angle), v1.Rotated(axis, angle, Point3.Origin), v2.Rotated(axis, angle, Point3.Origin), v3.Rotated(axis, angle, Point3.Origin));
        }

        public Triangle Translated(Vector offset)
        {
            return new Triangle(Normal, v1 + offset, v2 + offset, v3 + offset);
        }

        public Triangle Scaled(float factor)
        {
            return new Triangle(Normal, v1.Scaled(factor), v2.Scaled(factor), v3.Scaled(factor));
        }

        public Point3[] Vertices => [v1, v2, v3];
    }
}