using System.Runtime.InteropServices;
using PartStacker.Geometry;

namespace PartStacker.MeshFile
{
    public class STL
    {
        public static Mesh From(string fromFile)
        {
            BinaryReader br = new BinaryReader(new FileStream(fromFile, FileMode.Open));
            string header = new String(br.ReadChars(80));
            int tCount = br.ReadInt32();

            Mesh mesh = new();

            //if (header.Trim().StartsWith("solid")) // ASCI STL
            if (br.BaseStream.Length != 84 + tCount * 50)
            {
                br.Close();

                string[] lines = File.ReadAllLines(fromFile);

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
                    mesh.Triangles.Add(new Triangle(norm, ParsePoint(v1), ParsePoint(v2), ParsePoint(v3)));
                }
            }
            else // Binary STL
            {
                IntPtr ptr = Marshal.AllocHGlobal(50);

                while (tCount-- > 0)
                {
                    Marshal.Copy(br.ReadBytes(50), 0, ptr, 50);
                    mesh.Triangles.Add((Triangle)Marshal.PtrToStructure(ptr, typeof(Triangle)));
                }

                Marshal.FreeHGlobal(ptr);
                br.Close();
            }

            return mesh;
        }

        public static void To(Mesh mesh, string toFile)
        {
            BinaryWriter bw = new BinaryWriter(new FileStream(toFile, FileMode.OpenOrCreate));

            for (int i = 0; i < 80; i++)
                bw.Write((byte)0);

            int test = Marshal.SizeOf(typeof(Triangle));

            bw.Write((uint)mesh.Triangles.Count);

            byte[] buff = new byte[50];
            GCHandle handle = GCHandle.Alloc(buff, GCHandleType.Pinned);

            foreach (Triangle t in mesh.Triangles)
            {
                Marshal.StructureToPtr(t, handle.AddrOfPinnedObject(), false);
                bw.Write(buff);
            }

            handle.Free();
            bw.Close();
        }
    }
}
