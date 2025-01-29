using System.Collections.ObjectModel;
using PartStacker.Geometry;

namespace PartStacker.Geometry
{
    public class Mesh
    {
        public ReadOnlyCollection<Triangle> Triangles => _triangles.AsReadOnly();

        public Tuple<int, int, int> box;
        public Vector size;

        private List<Triangle> _triangles = new();

        public Mesh() { }

        public Mesh(int initialTriangles)
        {
            _triangles = new(initialTriangles);
        }

        public Mesh(List<Triangle> triangles)
        {
            _triangles = new(triangles);
        }

        public Tuple<int, int, int> CalcBox()
        {
            Point3 min = new(double.MaxValue);
            Point3 max = new(double.MinValue);

            var allVertices = Triangles.SelectMany(t => t.Vertices);
            foreach (Point3 p in allVertices)
            {
                min.X = Math.Min(p.X, min.X);
                max.X = Math.Max(p.X, max.X);

                min.Y = Math.Min(p.Y, min.Y);
                max.Y = Math.Max(p.Y, max.Y);

                min.Z = Math.Min(p.Z, min.Z);
                max.Z = Math.Max(p.Z, max.Z);
            }

            Translate(Point3.Origin - min);

            size = max - min;
            box = new Tuple<int, int, int>((int)Math.Ceiling(size.X + 2), (int)Math.Ceiling(size.Y + 2), (int)Math.Ceiling(size.Z + 2));
            return box;
        }

        public bool Add(Mesh source, Vector offset)
        {
            var toAdd = source._triangles.Select(t => t.Translated(offset));
            _triangles.AddRange(toAdd);
            return true;
        }

        public int Voxelize(int[, ,] voxels, int index, int carverSize)
        {
            bool[, ,] actualTriangles = new bool[voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)];
            bool[, ,] visited = new bool[voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)];
            bool[, ,] carved = new bool[voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)];

            // First render each part, placing voxels at the position of each triangle
            foreach (Triangle t in Triangles)
            {
                Vector a = t.Vertices[0].Vector;
                Vector b = t.Vertices[1] - t.Vertices[0];
                Vector c = t.Vertices[2] - t.Vertices[0];

                // Approximate the step size
                double db = 0.1 / Math.Max(Math.Abs(b.X), Math.Max(Math.Abs(b.Z), Math.Abs(b.Y)));
                double dc = 0.1 / Math.Max(Math.Abs(c.X), Math.Max(Math.Abs(c.Z), Math.Abs(c.Y)));

                // Voxelize
                for (double p = 0; p < 1; p += db)
                    for (double q = 0; q < 1 - p; q += dc)
                    {
                        Vector pos = a + p * b + q * c;
                        actualTriangles[(int)Math.Round(pos.X), (int)Math.Round(pos.Y), (int)Math.Round(pos.Z)] = true;
                    }
            }

            Stack<Tuple<int, int, int>> stack = new Stack<Tuple<int, int, int>>();

            if (carverSize > 0)
            {
                // Carving step
                for (int x = 0; x < voxels.GetLength(0); x++)
                    for (int y = 0; y < voxels.GetLength(1); y++)
                        for (int z = 0; z < voxels.GetLength(2); z++)
                            if (x == 0 || y == 0 || z == 0 || x == voxels.GetLength(0) - carverSize || y == voxels.GetLength(1) - carverSize || z == voxels.GetLength(2) - carverSize)
                                stack.Push(new Tuple<int, int, int>(x, y, z));

                while (stack.Count > 0)
                {
                    Tuple<int, int, int> pt = stack.Pop();
                    int x = pt.Item1, y = pt.Item2, z = pt.Item3;

                    // Check if we need to do work here
                    if (x < 0 || y < 0 || z < 0 || x > voxels.GetLength(0) - carverSize || y > voxels.GetLength(1) - carverSize || z > voxels.GetLength(2) - carverSize || visited[x, y, z])
                        continue;
                    visited[x, y, z] = true;

                    bool good = true;
                    for (int i = 0; i < carverSize && good; i++)
                        for (int j = 0; j < carverSize; j++)
                            for (int k = 0; k < carverSize; k++)
                                if (actualTriangles[x + i, y + j, z + k])
                                    good = false;

                    if (!good)
                        continue;

                    for (int i = 0; i < carverSize && good; i++)
                        for (int j = 0; j < carverSize; j++)
                            for (int k = 0; k < carverSize; k++)
                                carved[x + i, y + j, z + k] = true;

                    stack.Push(new Tuple<int, int, int>(x - 1, y, z));
                    stack.Push(new Tuple<int, int, int>(x + 1, y, z));

                    stack.Push(new Tuple<int, int, int>(x, y - 1, z));
                    stack.Push(new Tuple<int, int, int>(x, y + 1, z));

                    stack.Push(new Tuple<int, int, int>(x, y, z - 1));
                    stack.Push(new Tuple<int, int, int>(x, y, z + 1));
                }

                #region convexivy
                // Make convex in z-direction
                for (int x = 0; x < voxels.GetLength(0); x++)
                    for (int y = 0; y < voxels.GetLength(1); y++)
                    {
                        int minV = int.MaxValue; int maxV = int.MinValue;

                        for (int z = 0; z < voxels.GetLength(2); z++)
                            if (actualTriangles[x, y, z])
                            {
                                minV = Math.Min(z, minV);
                                maxV = Math.Max(z, maxV);
                            }

                        for (int z = minV; z < maxV; z++)
                            actualTriangles[x, y, z] = true;
                    }

                // Make convex in y-direction
                for (int x = 0; x < voxels.GetLength(0); x++)
                    for (int z = 0; z < voxels.GetLength(2); z++)
                    {
                        int minV = int.MaxValue; int maxV = int.MinValue;

                        for (int y = 0; y < voxels.GetLength(1); y++)
                            if (actualTriangles[x, y, z])
                            {
                                minV = Math.Min(y, minV);
                                maxV = Math.Max(y, maxV);
                            }

                        for (int y = minV; y < maxV; y++)
                            actualTriangles[x, y, z] = true;
                    }

                // Make convex in x-direction
                for (int z = 0; z < voxels.GetLength(2); z++)
                    for (int y = 0; y < voxels.GetLength(1); y++)
                    {
                        int minV = int.MaxValue; int maxV = int.MinValue;

                        for (int x = 0; x < voxels.GetLength(0); x++)
                            if (actualTriangles[x, y, z])
                            {
                                minV = Math.Min(x, minV);
                                maxV = Math.Max(x, maxV);
                            }

                        for (int x = minV; x < maxV; x++)
                            actualTriangles[x, y, z] = true;
                    }
                #endregion
            }

            // Expand by one voxel in all directions            
            for (int x = 0; x < voxels.GetLength(0) - 1; x++)
                for (int y = 0; y < voxels.GetLength(1) - 1; y++)
                    for (int z = 0; z < voxels.GetLength(2) - 1; z++)
                        if (!carved[x, y, z] && actualTriangles[x, y, z])
                        {
                            voxels[x + 1, y + 1, z + 1] |= index;
                            voxels[x + 1, y + 1, z] |= index;
                            voxels[x + 1, y, z + 1] |= index;
                            voxels[x + 1, y, z] |= index;
                            voxels[x, y + 1, z] |= index;
                            voxels[x, y + 1, z + 1] |= index;
                            voxels[x, y, z + 1] |= index;
                        }

            // Calculate and return volume
            int volume = 0;
            for (int x = 0; x < voxels.GetLength(0); x++)
                for (int y = 0; y < voxels.GetLength(1); y++)
                    for (int z = 0; z < voxels.GetLength(2); z++)
                        if ((voxels[x, y, z] & index) != 0)
                            volume++;
            return volume;
        }

        public double Volume()
        {
            double volume = 0;
            foreach (Triangle t in Triangles)
                volume += t.v1.Vector.Dot(t.v2.Vector.Cross(t.v3.Vector));

            return volume / 6;
        }

        public void Mirror()
        {
            for (int i = 0; i < _triangles.Count; i++)
                _triangles[i] = _triangles[i].Mirrored();
        }

        public void Rotate(Vector axis, double angle)
        {
            for (int i = 0; i < _triangles.Count; i++)
                _triangles[i] = _triangles[i].Rotated(axis, angle);
        }

        public void Scale(double factor)
        {
            for (int i = 0; i < _triangles.Count; i++)
                _triangles[i] = _triangles[i].Scaled(factor);
        }

        public void Translate(Vector offset)
        {
            for (int i = 0; i < _triangles.Count; i++)
                _triangles[i] = _triangles[i].Translated(offset);
        }

        public Mesh Clone()
        {
            return new(_triangles);
        }

        public void AddSinterbox(Sinterbox.Parameters parameters)
        {
            Sinterbox.GenerateInto(ref _triangles, size, parameters);
        }
    }
}