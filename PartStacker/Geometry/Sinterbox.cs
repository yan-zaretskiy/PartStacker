using Microsoft.Xna.Framework;

namespace PartStacker.Geometry
{
    public class Sinterbox
    {
        public static void GenerateInto(ref List<Triangle> triangles, Vector size, Parameters parameters)
        {
            AddPlaneX(ref triangles, size, parameters);
            AddPlaneY(ref triangles, size, parameters);
            AddPlaneZ(ref triangles, size, parameters);
        }

        public struct Parameters
        {
            public float Clearance;
            public float Thickness;
            public float Width;
            public float Spacing;
            public void Deconstruct(out float clearance, out float thickness, out float width, out float spacing)
            {
                clearance = Clearance;
                thickness = Thickness;
                width = Width;
                spacing = Spacing;
            }
        }

        private static void AddBox(ref List<Triangle> triangles, float X, float Y, float Z, float sx, float sy, float sz)
        {
            Matrix transform = (Matrix.CreateTranslation(new Vector3(-0.5f, -0.5f, -0.5f)) * Matrix.CreateScale(sx, sy, sz)) * Matrix.CreateTranslation(new Vector3(X + (sx / 2f), Y + (sy / 2f), Z + (sz / 2f)));
            triangles.AddRange(CreateCube(transform));
        }

        private static void AddPlaneX(ref List<Triangle> triangles, Vector size, Parameters parameters)
        {
            var (sx, sy, sz) = size;
            var (Clearance, Thickness, Width, Spacing) = parameters;
            int num = (int)(((sy + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            int num2 = (int)(((sz + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            for (int i = 0; i <= num; i++)
            {
                AddBox(ref triangles, -(Thickness + Clearance), -(Thickness + Clearance) + ((i * ((sy + (2f * (Clearance + Thickness))) - Width)) / ((float)num)), -(Thickness + Clearance), Thickness, Width, sz + (2f * (Clearance + Thickness)));
                AddBox(ref triangles, sx + Clearance, -(Thickness + Clearance) + ((i * ((sy + (2f * (Clearance + Thickness))) - Width)) / ((float)num)), -(Thickness + Clearance), Thickness, Width, sz + (2f * (Clearance + Thickness)));
            }
            for (int j = 0; j <= num2; j++)
            {
                AddBox(ref triangles, -(Thickness + Clearance), -(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sz + (2f * (Clearance + Thickness))) - Width)) / ((float)num2)), Thickness, sy + (2f * (Clearance + Thickness)), Width);
                AddBox(ref triangles, sx + Clearance, -(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sz + (2f * (Clearance + Thickness))) - Width)) / ((float)num2)), Thickness, sy + (2f * (Clearance + Thickness)), Width);
            }
        }

        private static void AddPlaneY(ref List<Triangle> triangles, Vector size, Parameters parameters)
        {
            var (sx, sy, sz) = size;
            var (Clearance, Thickness, Width, Spacing) = parameters;
            int num = (int)(((sx + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            int num2 = (int)(((sz + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            for (int i = 0; i <= num; i++)
            {
                AddBox(ref triangles, -(Thickness + Clearance) + ((i * ((sx + (2f * (Clearance + Thickness))) - Width)) / ((float)num)), -(Thickness + Clearance), -(Thickness + Clearance), Width, Thickness, sz + (2f * (Clearance + Thickness)));
                AddBox(ref triangles, -(Thickness + Clearance) + ((i * ((sx + (2f * (Clearance + Thickness))) - Width)) / ((float)num)), sy + Clearance, -(Thickness + Clearance), Width, Thickness, sz + (2f * (Clearance + Thickness)));
            }
            for (int j = 0; j <= num2; j++)
            {
                AddBox(ref triangles, -(Thickness + Clearance), -(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sz + (2f * (Clearance + Thickness))) - Width)) / ((float)num2)), sx + (2f * (Clearance + Thickness)), Thickness, Width);
                AddBox(ref triangles, -(Thickness + Clearance), sy + Clearance, -(Thickness + Clearance) + ((j * ((sz + (2f * (Clearance + Thickness))) - Width)) / ((float)num2)), sx + (2f * (Clearance + Thickness)), Thickness, Width);
            }
        }

        private static void AddPlaneZ(ref List<Triangle> triangles, Vector size, Parameters parameters)
        {
            var (sx, sy, sz) = size;
            var (Clearance, Thickness, Width, Spacing) = parameters;
            int num = (int)(((sx + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            int num2 = (int)(((sy + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            for (int i = 0; i <= num; i++)
            {
                AddBox(ref triangles, -(Thickness + Clearance) + ((i * ((sx + (2f * (Clearance + Thickness))) - Width)) / ((float)num)), -(Thickness + Clearance), -(Thickness + Clearance), Width, sy + (2f * (Clearance + Thickness)), Thickness);
                AddBox(ref triangles, -(Thickness + Clearance) + ((i * ((sx + (2f * (Clearance + Thickness))) - Width)) / ((float)num)), -(Thickness + Clearance), sz + Clearance, Width, sy + (2f * (Clearance + Thickness)), Thickness);
            }
            for (int j = 0; j <= num2; j++)
            {
                AddBox(ref triangles, -(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sy + (2f * (Clearance + Thickness))) - Width)) / ((float)num2)), -(Thickness + Clearance), sx + (2f * (Clearance + Thickness)), Width, Thickness);
                AddBox(ref triangles, -(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sy + (2f * (Clearance + Thickness))) - Width)) / ((float)num2)), sz + Clearance, sx + (2f * (Clearance + Thickness)), Width, Thickness);
            }
        }

        private static Triangle[] CreateCube(Matrix transform)
        {
            Triangle[] triangleArray = new Triangle[12];
            int num = 0;
            triangleArray[num++] = new Triangle(new Vector(-1f, 0f, 0f), new Point3(0f, 0f, 0f), new Point3(0f, 1f, 1f), new Point3(0f, 1f, 0f));
            triangleArray[num++] = new Triangle(new Vector(-1f, 0f, 0f), new Point3(0f, 0f, 0f), new Point3(0f, 0f, 1f), new Point3(0f, 1f, 1f));
            triangleArray[num++] = new Triangle(new Vector(1f, 0f, 0f), new Point3(1f, 0f, 0f), new Point3(1f, 1f, 0f), new Point3(1f, 1f, 1f));
            triangleArray[num++] = new Triangle(new Vector(1f, 0f, 0f), new Point3(1f, 0f, 0f), new Point3(1f, 1f, 1f), new Point3(1f, 0f, 1f));
            triangleArray[num++] = new Triangle(new Vector(0f, -1f, 0f), new Point3(0f, 0f, 0f), new Point3(1f, 0f, 0f), new Point3(1f, 0f, 1f));
            triangleArray[num++] = new Triangle(new Vector(0f, -1f, 0f), new Point3(0f, 0f, 0f), new Point3(1f, 0f, 1f), new Point3(0f, 0f, 1f));
            triangleArray[num++] = new Triangle(new Vector(0f, 1f, 0f), new Point3(0f, 1f, 0f), new Point3(1f, 1f, 1f), new Point3(1f, 1f, 0f));
            triangleArray[num++] = new Triangle(new Vector(0f, 1f, 0f), new Point3(0f, 1f, 0f), new Point3(0f, 1f, 1f), new Point3(1f, 1f, 1f));
            triangleArray[num++] = new Triangle(new Vector(0f, 0f, -1f), new Point3(0f, 0f, 0f), new Point3(1f, 1f, 0f), new Point3(1f, 0f, 0f));
            triangleArray[num++] = new Triangle(new Vector(0f, 0f, -1f), new Point3(0f, 0f, 0f), new Point3(0f, 1f, 0f), new Point3(1f, 1f, 0f));
            triangleArray[num++] = new Triangle(new Vector(0f, 0f, 1f), new Point3(0f, 0f, 1f), new Point3(1f, 0f, 1f), new Point3(1f, 1f, 1f));
            triangleArray[num++] = new Triangle(new Vector(0f, 0f, 1f), new Point3(0f, 0f, 1f), new Point3(1f, 1f, 1f), new Point3(0f, 1f, 1f));
            for (int i = 0; i < 12; i++)
            {
                var ToVector3 = (Point3 point) => new Vector3(point.X, point.Y, point.Z);
                Vector3 p1 = Vector3.Transform(ToVector3(triangleArray[i].v1), transform);
                Point3 point = new Point3(p1.X, p1.Y, p1.Z);
                Vector3 p2 = Vector3.Transform(ToVector3(triangleArray[i].v2), transform);
                Point3 point2 = new Point3(p2.X, p2.Y, p2.Z);
                Vector3 p3 = Vector3.Transform(ToVector3(triangleArray[i].v3), transform);
                Point3 point3 = new Point3(p3.X, p3.Y, p3.Z);
                triangleArray[i] = new Triangle(triangleArray[i].Normal, point, point2, point3);
            }
            return triangleArray;
        }
    }
}
