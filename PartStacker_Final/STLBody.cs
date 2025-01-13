using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PartStacker_Final
{
    public class STLBody : ICloneable
    {
        public List<Triangle> Triangles;

        public Tuple<int, int, int> box;
        public Point3 size;

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

            this.Translate(new Point3(-min[0], -min[1], -min[2]));

            size = new Point3(max[0] - min[0], max[1] - min[1], max[2] - min[2]);
            box = new Tuple<int, int, int>((int)Math.Ceiling(max[0] - min[0] + 2), (int)Math.Ceiling(max[1] - min[1] + 2), (int)Math.Ceiling(max[2] - min[2] + 2));
            return box;
        }

        public bool TranslateAndAdd(STLBody target, Point3 offset)
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
                Point3 a = t.Vertices[0];
                Point3 b = t.Vertices[1] - t.Vertices[0];
                Point3 c = t.Vertices[2] - t.Vertices[0];

                // Approximate the step size
                float db = 0.1f / Math.Max(Math.Abs(b.X), Math.Max(Math.Abs(b.Z), Math.Abs(b.Y)));
                float dc = 0.1f / Math.Max(Math.Abs(c.X), Math.Max(Math.Abs(c.Z), Math.Abs(c.Y)));

                // Voxelize
                for (float p = 0; p < 1; p += db)
                    for (float q = 0; q < 1 - p; q += dc)
                    {
                        Point3 pos = a + p * b + q * c;
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
                    string[] normal = lines[i].Replace('.', ',').Split(' ');
                    string[] v1 = lines[i + 2].Replace('.', ',').Split(' ');
                    string[] v2 = lines[i + 3].Replace('.', ',').Split(' ');
                    string[] v3 = lines[i + 4].Replace('.', ',').Split(' ');

                    this.Triangles.Add(new Triangle(Point3.fromStringArray(normal), Point3.fromStringArray(v1), Point3.fromStringArray(v2), Point3.fromStringArray(v3)));
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
                volume += t.v1.Dot(t.v2.Cross(t.v3));

            return volume / 6;
        }

        public void Mirror()
        {
            for (int i = 0; i < Triangles.Count; i++)
                Triangles[i] = Triangles[i].Mirror();
        }

        public void Rotate(Point3 axis, float angle)
        {
            for (int i = 0; i < Triangles.Count; i++)
                Triangles[i] = Triangles[i].Rotate(axis, angle);
        }

        public void Scale(float factor)
        {
            for (int i = 0; i < Triangles.Count; i++)
                Triangles[i] = Triangles[i].Scale(factor);
        }

        public void Translate(Point3 offset)
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

        public void SetAsModel(ModelViewerControl mvc)
        {
            mvc.TriangleCount = 0;
            mvc.triangles = null;
            GC.AddMemoryPressure(Triangles.Count * 3 * 40 + 1);
            GC.Collect();

            VertexBuffer[] buffers = new VertexBuffer[Triangles.Count / 500000 + 1];
            int[] TriangleCounts = new int[Triangles.Count / 500000 + 1];

            VertexPositionColorNormal[] buffer = new VertexPositionColorNormal[Triangles.Count * 3];

            for (int i = 0; i < Triangles.Count; i++)
            {
                buffer[3 * i + 0] = new VertexPositionColorNormal(Triangles[i].v1.XNAVector3, Microsoft.Xna.Framework.Color.White, Triangles[i].Normal.XNAVector3);
                buffer[3 * i + 1] = new VertexPositionColorNormal(Triangles[i].v2.XNAVector3, Microsoft.Xna.Framework.Color.White, Triangles[i].Normal.XNAVector3);
                buffer[3 * i + 2] = new VertexPositionColorNormal(Triangles[i].v3.XNAVector3, Microsoft.Xna.Framework.Color.White, Triangles[i].Normal.XNAVector3);
            }

            mvc.triangles = buffer;
            mvc.TriangleCount = Triangles.Count;
            mvc.BB = size.XNAVector3;
            mvc.Invalidate();
        }

        public void SetAsModel(ModelViewerControl mvc, int[,,] voxels, int volume)
        {
            mvc.TriangleCount = 0;
            mvc.triangles = null;
            GC.AddMemoryPressure(40 * (Triangles.Count * 3 + volume * 36));
            GC.Collect();

            VertexPositionColorNormal[] buffer = new VertexPositionColorNormal[Triangles.Count * 3 + volume * 36];

            for (int i = 0; i < Triangles.Count; i++)
            {
                buffer[3 * i + 0] = new VertexPositionColorNormal(Triangles[i].v1.XNAVector3, Microsoft.Xna.Framework.Color.White, Triangles[i].Normal.XNAVector3);
                buffer[3 * i + 1] = new VertexPositionColorNormal(Triangles[i].v2.XNAVector3, Microsoft.Xna.Framework.Color.White, Triangles[i].Normal.XNAVector3);
                buffer[3 * i + 2] = new VertexPositionColorNormal(Triangles[i].v3.XNAVector3, Microsoft.Xna.Framework.Color.White, Triangles[i].Normal.XNAVector3);
            }

            int pos = Triangles.Count * 3;
            Color c = Color.Aqua;
            c.A = (byte)(256 * 0.8f);

            for(int x = 0; x < voxels.GetLength(0); x++)
                for (int y = 0; y < voxels.GetLength(1); y++)
                    for (int z = 0; z < voxels.GetLength(2); z++)
                        if (voxels[x, y, z] == 1)
                        {
                            float xt = (float)x - 1.0f;
                            float yt = (float)y - 1.0f;
                            float zt = (float)z - 1.0f;

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(-1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 0), c, new Vector3(-1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 1), c, new Vector3(-1, 0, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(-1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 1), c, new Vector3(-1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 1), c, new Vector3(-1, 0, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 0), c, new Vector3(1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 0), c, new Vector3(1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(1, 0, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 0), c, new Vector3(1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 1), c, new Vector3(1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(1, 0, 0));


                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(0, -1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 0), c, new Vector3(0, -1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 1), c, new Vector3(0, -1, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(0, -1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 1), c, new Vector3(0, -1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 1), c, new Vector3(0, -1, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 0), c, new Vector3(0, 1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 0), c, new Vector3(0, 1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(0, 1, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 0), c, new Vector3(0, 1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 1), c, new Vector3(0, 1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(0, 1, 0));


                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(0, 0, -1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 0), c, new Vector3(0, 0, -1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 0), c, new Vector3(0, 0, -1));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(0, 0, -1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 0), c, new Vector3(0, 0, -1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 0), c, new Vector3(0, 0, -1));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 1), c, new Vector3(0, 0, 1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 1), c, new Vector3(0, 0, 1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(0, 0, 1));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 1), c, new Vector3(0, 0, 1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 1), c, new Vector3(0, 0, 1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(0, 0, 1));
                        }
            
            
            mvc.TriangleCount = buffer.Length / 3;
            mvc.triangles = buffer;
            mvc.BB = size.XNAVector3;
            mvc.Invalidate();
        }
    }
}