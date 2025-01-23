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

        public Triangle Mirror()
        {
            return new Triangle(Normal.MirrorX(), v1.MirrorX(), v3.MirrorX(), v2.MirrorX());
        }

        public Triangle Rotate(Vector axis, float angle)
        {
            return new Triangle(Normal.Rotate(axis, angle), v1.Rotate(axis, angle, Point3.Origin), v2.Rotate(axis, angle, Point3.Origin), v3.Rotate(axis, angle, Point3.Origin));
        }

        public Triangle Translate(Vector offset)
        {
            return new Triangle(Normal, v1 + offset, v2 + offset, v3 + offset);
        }

        public Triangle Scale(float factor)
        {
            return new Triangle(Normal, v1.Scale(factor), v2.Scale(factor), v3.Scale(factor));
        }

        public Point3[] Vertices => [v1, v2, v3];
    }
}