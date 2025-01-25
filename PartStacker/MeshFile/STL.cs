using System.Runtime.CompilerServices;
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

            List<Triangle> triangles = new();

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
                    triangles.Add(new Triangle(norm, ParsePoint(v1), ParsePoint(v2), ParsePoint(v3)));
                }
            }
            else // Binary STL
            {
                byte[] buff = new byte[50];
                while (tCount-- > 0)
                {
                    br.Read(buff, 0, 50);
                    // This is very verbose, but it's fast
                    triangles.Add(new Triangle(
                        new Vector(
                            Unsafe.As<byte, float>(ref buff[4 * 0]),
                            Unsafe.As<byte, float>(ref buff[4 * 1]),
                            Unsafe.As<byte, float>(ref buff[4 * 2])
                        ),
                        new Point3(
                            Unsafe.As<byte, float>(ref buff[4 * 3]),
                            Unsafe.As<byte, float>(ref buff[4 * 4]),
                            Unsafe.As<byte, float>(ref buff[4 * 5])
                        ),
                        new Point3(
                            Unsafe.As<byte, float>(ref buff[4 * 6]),
                            Unsafe.As<byte, float>(ref buff[4 * 7]),
                            Unsafe.As<byte, float>(ref buff[4 * 8])
                        ),
                        new Point3(
                            Unsafe.As<byte, float>(ref buff[4 * 9]),
                            Unsafe.As<byte, float>(ref buff[4 * 10]),
                            Unsafe.As<byte, float>(ref buff[4 * 11])
                        )
                    ));
                }
                br.Close();
            }

            return new Mesh(triangles);
        }

        public static void To(Mesh mesh, string toFile)
        {
            using (BinaryWriter bw = new BinaryWriter(new FileStream(toFile, FileMode.OpenOrCreate)))
            {
                for (int i = 0; i < 80; i++)
                    bw.Write((byte)0);
                bw.Write((uint)mesh.Triangles.Count);
                byte[] buff = new byte[50];
                foreach (Triangle t in mesh.Triangles)
                {
                    Unsafe.As<byte, float>(ref buff[4 * 0]) = (float)t.Normal.X;
                    Unsafe.As<byte, float>(ref buff[4 * 1]) = (float)t.Normal.Y;
                    Unsafe.As<byte, float>(ref buff[4 * 2]) = (float)t.Normal.Z;
                    Unsafe.As<byte, float>(ref buff[4 * 3]) = (float)t.v1.X;
                    Unsafe.As<byte, float>(ref buff[4 * 4]) = (float)t.v1.Y;
                    Unsafe.As<byte, float>(ref buff[4 * 5]) = (float)t.v1.Z;
                    Unsafe.As<byte, float>(ref buff[4 * 6]) = (float)t.v2.X;
                    Unsafe.As<byte, float>(ref buff[4 * 7]) = (float)t.v2.Y;
                    Unsafe.As<byte, float>(ref buff[4 * 8]) = (float)t.v2.Z;
                    Unsafe.As<byte, float>(ref buff[4 * 9]) = (float)t.v3.X;
                    Unsafe.As<byte, float>(ref buff[4 * 10]) = (float)t.v3.Y;
                    Unsafe.As<byte, float>(ref buff[4 * 11]) = (float)t.v3.Z;
                    bw.Write(buff);
                }
            }
        }
    }
}
