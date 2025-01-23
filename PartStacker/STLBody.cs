using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using PartStacker.Geometry;

namespace PartStacker
{
    public class STLBody : ICloneable
    {
        public List<Triangle> Triangles;

        public Tuple<int, int, int> box;
        public Vector size;

        public STLBody(int InitialTriangles)
        {
            Triangles = new List<Triangle>(InitialTriangles);
        }

        public STLBody()
        {
            Triangles = new List<Triangle>();
        }

        public Tuple<int, int, int> CalcBox()
        {
            float[] min = new float[] { float.MaxValue, float.MaxValue, float.MaxValue };
            float[] max = new float[] { float.MinValue, float.MinValue, float.MinValue };

            foreach (Triangle t in Triangles)
            {
                for (int j = 0; j < 3; j++)
                {
                    min[0] = (float)Math.Min(t.Vertices[j].X, min[0]);
                    max[0] = (float)Math.Max(t.Vertices[j].X, max[0]);

                    min[1] = (float)Math.Min(t.Vertices[j].Y, min[1]);
                    max[1] = (float)Math.Max(t.Vertices[j].Y, max[1]);

                    min[2] = (float)Math.Min(t.Vertices[j].Z, min[2]);
                    max[2] = (float)Math.Max(t.Vertices[j].Z, max[2]);
                }
            }

            this.Translate(new Vector(-min[0], -min[1], -min[2]));

            size = new Vector(max[0] - min[0], max[1] - min[1], max[2] - min[2]);
            box = new Tuple<int, int, int>((int)Math.Ceiling(max[0] - min[0] + 2), (int)Math.Ceiling(max[1] - min[1] + 2), (int)Math.Ceiling(max[2] - min[2] + 2));
            return box;
        }

        public bool TranslateAndAdd(STLBody target, Vector offset)
        {
            bool ok = true;

            int originalCount = target.Triangles.Count;
            foreach (Triangle t in Triangles)
            {
                target.Triangles.Add(new Triangle(t.Normal, t.Vertices[0] + offset, t.Vertices[1] + offset, t.Vertices[2] + offset));

                //for (int i = 0; i < originalCount; i++)
                    //if (target.Triangles[i].triangle_intersect(target.Triangles[target.Triangles.Count - 1]))
                        //ok = false;
            }

            return ok;
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
                float db = 0.1f / Math.Max(Math.Abs(b.X), Math.Max(Math.Abs(b.Z), Math.Abs(b.Y)));
                float dc = 0.1f / Math.Max(Math.Abs(c.X), Math.Max(Math.Abs(c.Z), Math.Abs(c.Y)));

                // Voxelize
                for (float p = 0; p < 1; p += db)
                    for (float q = 0; q < 1 - p; q += dc)
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

        public void ExportSTL(string targetFile)
        {
            BinaryWriter bw = new BinaryWriter(new FileStream(targetFile, FileMode.OpenOrCreate));

            for (int i = 0; i < 80; i++)
                bw.Write((byte)0);

            int test = Marshal.SizeOf(typeof(Triangle));

            bw.Write((uint)Triangles.Count);

            byte[] buff = new byte[50];
            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);

            foreach (Triangle t in Triangles)
            {
                Marshal.StructureToPtr(t, handle.AddrOfPinnedObject(), false);
                bw.Write(buff);
            }

            handle.Free();
            bw.Close();
        }

        public static STLBody FromSTL(string file)
        {
            STLBody result = new STLBody();

            result.AppendSTL(file);

            return result;
        }

        public void AppendSTL(string file)
        {
            BinaryReader br = new BinaryReader(new FileStream(file, FileMode.Open));
            string header = new String(br.ReadChars(80));
            int tCount = br.ReadInt32();

            //if (header.Trim().StartsWith("solid")) // ASCI STL
            if (br.BaseStream.Length != 84 + tCount * 50)
            {
                br.Close();

                string[] lines = System.IO.File.ReadAllLines(file);

                for (int i = 1; i < lines.Length - 1; i += 7)
                {
                    string[] normal = lines[i].Split(' ');
                    string[] v1 = lines[i + 2].Split(' ');
                    string[] v2 = lines[i + 3].Split(' ');
                    string[] v3 = lines[i + 4].Split(' ');

                    var ParsePoint = (string[] s) =>
                    {
                        Point3 result = new();
                        result.X = float.Parse(s[s.Length - 3]);
                        result.Y = float.Parse(s[s.Length - 2]);
                        result.Z = float.Parse(s[s.Length - 1]);
                        return result;
                    };

                    Point3 normalAsPoint = ParsePoint(normal);
                    Vector norm = new Vector(normalAsPoint.X, normalAsPoint.Y, normalAsPoint.Z);
                    this.Triangles.Add(new Triangle(norm, ParsePoint(v1), ParsePoint(v2), ParsePoint(v3)));
                }
            }
            else // Binary STL
            {
                IntPtr ptr = Marshal.AllocHGlobal(50);

                while (tCount-- > 0)
                {
                    Marshal.Copy(br.ReadBytes(50), 0, ptr, 50);
                    Triangles.Add((Triangle)Marshal.PtrToStructure(ptr, typeof(Triangle)));
                }

                Marshal.FreeHGlobal(ptr);
                br.Close();
            }
        }

        public float Volume()
        {
            float volume = 0;
            foreach (Triangle t in Triangles)
                volume += t.v1.Vector.Dot(t.v2.Vector.Cross(t.v3.Vector));

            return volume / 6;
        }

        public void Mirror()
        {
            for (int i = 0; i < Triangles.Count; i++)
                Triangles[i] = Triangles[i].Mirror();
        }

        public void Rotate(Vector axis, float angle)
        {
            for (int i = 0; i < Triangles.Count; i++)
                Triangles[i] = Triangles[i].Rotate(axis, angle);
        }

        public void Scale(float factor)
        {
            for (int i = 0; i < Triangles.Count; i++)
                Triangles[i] = Triangles[i].Scale(factor);
        }

        public void Translate(Vector offset)
        {
            for(int i = 0; i < Triangles.Count; i++)
                Triangles[i] = Triangles[i].Translate(offset);
        }

        public object Clone()
        {
            STLBody newBody = new STLBody(this.Triangles.Count);
            foreach (Triangle t in this.Triangles)
                newBody.Triangles.Add(t);
            return newBody;
        }

        private void AddBox(float X, float Y, float Z, float sx, float sy, float sz)
        {
            Matrix transform = (Matrix.CreateTranslation(new Vector3(-0.5f, -0.5f, -0.5f)) * Matrix.CreateScale(sx, sy, sz)) * Matrix.CreateTranslation(new Vector3(X + (sx / 2f), Y + (sy / 2f), Z + (sz / 2f)));
            foreach (Triangle triangle in this.CreateCube(transform))
            {
                this.Triangles.Insert(this.Triangles.Count, triangle);
            }
        }

        private void AddPlaneX(float sx, float sy, float sz, float Clearance, float Thickness, float Width, float Spacing)
        {
            int num = (int) (((sy + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            int num2 = (int) (((sz + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            for (int i = 0; i <= num; i++)
            {
                this.AddBox(-(Thickness + Clearance), -(Thickness + Clearance) + ((i * ((sy + (2f * (Clearance + Thickness))) - Width)) / ((float) num)), -(Thickness + Clearance), Thickness, Width, sz + (2f * (Clearance + Thickness)));
                this.AddBox(sx + Clearance, -(Thickness + Clearance) + ((i * ((sy + (2f * (Clearance + Thickness))) - Width)) / ((float) num)), -(Thickness + Clearance), Thickness, Width, sz + (2f * (Clearance + Thickness)));
            }
            for (int j = 0; j <= num2; j++)
            {
                this.AddBox(-(Thickness + Clearance), -(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sz + (2f * (Clearance + Thickness))) - Width)) / ((float) num2)), Thickness, sy + (2f * (Clearance + Thickness)), Width);
                this.AddBox(sx + Clearance, -(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sz + (2f * (Clearance + Thickness))) - Width)) / ((float) num2)), Thickness, sy + (2f * (Clearance + Thickness)), Width);
            }
        }

        private void AddPlaneY(float sx, float sy, float sz, float Clearance, float Thickness, float Width, float Spacing)
        {
            int num = (int) (((sx + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            int num2 = (int) (((sz + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            for (int i = 0; i <= num; i++)
            {
                this.AddBox(-(Thickness + Clearance) + ((i * ((sx + (2f * (Clearance + Thickness))) - Width)) / ((float) num)), -(Thickness + Clearance), -(Thickness + Clearance), Width, Thickness, sz + (2f * (Clearance + Thickness)));
                this.AddBox(-(Thickness + Clearance) + ((i * ((sx + (2f * (Clearance + Thickness))) - Width)) / ((float) num)), sy + Clearance, -(Thickness + Clearance), Width, Thickness, sz + (2f * (Clearance + Thickness)));
            }
            for (int j = 0; j <= num2; j++)
            {
                this.AddBox(-(Thickness + Clearance), -(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sz + (2f * (Clearance + Thickness))) - Width)) / ((float) num2)), sx + (2f * (Clearance + Thickness)), Thickness, Width);
                this.AddBox(-(Thickness + Clearance), sy + Clearance, -(Thickness + Clearance) + ((j * ((sz + (2f * (Clearance + Thickness))) - Width)) / ((float) num2)), sx + (2f * (Clearance + Thickness)), Thickness, Width);
            }
        }

        private void AddPlaneZ(float sx, float sy, float sz, float Clearance, float Thickness, float Width, float Spacing)
        {
            int num = (int) (((sx + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            int num2 = (int) (((sy + (2f * (Clearance + Thickness))) - Width) / (Spacing + Width));
            for (int i = 0; i <= num; i++)
            {
                this.AddBox(-(Thickness + Clearance) + ((i * ((sx + (2f * (Clearance + Thickness))) - Width)) / ((float) num)), -(Thickness + Clearance), -(Thickness + Clearance), Width, sy + (2f * (Clearance + Thickness)), Thickness);
                this.AddBox(-(Thickness + Clearance) + ((i * ((sx + (2f * (Clearance + Thickness))) - Width)) / ((float) num)), -(Thickness + Clearance), sz + Clearance, Width, sy + (2f * (Clearance + Thickness)), Thickness);
            }
            for (int j = 0; j <= num2; j++)
            {
                this.AddBox(-(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sy + (2f * (Clearance + Thickness))) - Width)) / ((float) num2)), -(Thickness + Clearance), sx + (2f * (Clearance + Thickness)), Width, Thickness);
                this.AddBox(-(Thickness + Clearance), -(Thickness + Clearance) + ((j * ((sy + (2f * (Clearance + Thickness))) - Width)) / ((float) num2)), sz + Clearance, sx + (2f * (Clearance + Thickness)), Width, Thickness);
            }
        }

        private Tuple<float, float, float> CalcBoxExact()
        {
            float[] numArray = new float[] { float.MaxValue, float.MaxValue, float.MaxValue };
            float[] numArray2 = new float[] { float.MinValue, float.MinValue, float.MinValue };
            foreach (Triangle triangle in this.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    numArray[0] = Math.Min(triangle.Vertices[i].X, numArray[0]);
                    numArray2[0] = Math.Max(triangle.Vertices[i].X, numArray2[0]);
                    numArray[1] = Math.Min(triangle.Vertices[i].Y, numArray[1]);
                    numArray2[1] = Math.Max(triangle.Vertices[i].Y, numArray2[1]);
                    numArray[2] = Math.Min(triangle.Vertices[i].Z, numArray[2]);
                    numArray2[2] = Math.Max(triangle.Vertices[i].Z, numArray2[2]);
                }
            }
            this.Translate(new Vector(-numArray[0], -numArray[1], -numArray[2]));
            this.size = new Vector(numArray2[0] - numArray[0], numArray2[1] - numArray[1], numArray2[2] - numArray[2]);
            return new Tuple<float, float, float>(numArray2[0] - numArray[0], numArray2[1] - numArray[1], numArray2[2] - numArray[2]);
        }

        private Triangle[] CreateCube(Matrix transform)
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

        public void SinterBox(float Clearance, float Thickness, float Width, float Spacing)
        {
            Tuple<float, float, float> tuple = this.CalcBoxExact();
            this.AddPlaneX(tuple.Item1, tuple.Item2, tuple.Item3, Clearance, Thickness, Width, Spacing);
            this.AddPlaneY(tuple.Item1, tuple.Item2, tuple.Item3, Clearance, Thickness, Width, Spacing);
            this.AddPlaneZ(tuple.Item1, tuple.Item2, tuple.Item3, Clearance, Thickness, Width, Spacing);
        }
    }
}