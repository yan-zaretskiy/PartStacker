using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PartStacker_Final
{
    public struct Triangle
    {
        public Point3 Normal;
        public Point3 v1, v2, v3;
        public ushort Attribute;

        public Triangle(Point3 normal, Point3 v1, Point3 v2, Point3 v3, ushort attribute)
        {
            this.Normal = normal;

            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;

            this.Attribute = 0;
        }

        public Triangle(Point3 normal, Point3 v1, Point3 v2, Point3 v3)
            : this(normal, v1, v2, v3, 0)
        {

        }

        public Triangle Mirror()
        {
            return new Triangle(Normal.MirrorIT(), v1.Mirror(), v3.Mirror(), v2.Mirror());
        }

        public Triangle Rotate(Point3 axis, float angle)
        {
            return new Triangle(Normal.Rotate(axis, angle), v1.Rotate(axis, angle), v2.Rotate(axis, angle), v3.Rotate(axis, angle), Attribute);
        }

        public Triangle Translate(Point3 offset)
        {
            return new Triangle(Normal, v1 + offset, v2 + offset, v3 + offset, Attribute);
        }

        public Triangle Scale(float factor)
        {
            return new Triangle(Normal, v1 * factor, v2 * factor, v3 * factor, Attribute);
        }

        public Point3[] Vertices
        {
            get { return new Point3[] { v1, v2, v3 }; }
        }
    }
}