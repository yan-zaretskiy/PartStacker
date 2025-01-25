namespace PartStacker.Geometry
{
    public class Sinterbox
    {
        public struct Parameters
        {
            public double Clearance;
            public double Thickness;
            public double Width;
            public double Spacing;
            public void Deconstruct(out double clearance, out double thickness, out double width, out double spacing)
            {
                clearance = Clearance;
                thickness = Thickness;
                width = Width;
                spacing = Spacing;
            }
        }

        public static void GenerateInto(ref List<Triangle> triangles, Vector size, Parameters parameters)
        {
            MakePositions(size, parameters, out List<double> positionsX, out List<double> positionsY, out List<double> positionsZ);
            Point3 lowerBound = new(positionsX[1], positionsY[1], positionsZ[1]);
            Point3 upperBound = new(positionsX[positionsX.Count - 2], positionsY[positionsY.Count - 2], positionsZ[positionsZ.Count - 2]);
            double thickness = parameters.Thickness;

            // XY sides
            {
                Point3[,] lowerXY = new Point3[positionsX.Count, positionsY.Count];
                Point3[,] upperXY = new Point3[positionsX.Count, positionsY.Count];
                for (int x = 0; x < positionsX.Count; ++x)
                {
                    for (int y = 0; y < positionsY.Count; ++y)
                    {
                        lowerXY[x, y] = new(positionsX[x], positionsY[y], lowerBound.Z);
                        upperXY[x, y] = new(positionsX[x], positionsY[y], upperBound.Z);
                    }
                }
                AddSide(ref triangles, lowerXY, (Vector.UnitZ, Vector.UnitX, Vector.UnitY), thickness);
                AddSide(ref triangles, upperXY, (-Vector.UnitZ, Vector.UnitX, Vector.UnitY), thickness);
            }

            // ZX sides
            // Note, doing "XZ" instead of "ZX" makes the triangles face the wrong way
            {
                Point3[,] lowerZX = new Point3[positionsZ.Count, positionsX.Count];
                Point3[,] upperZX = new Point3[positionsZ.Count, positionsX.Count];
                for (int z = 0; z < positionsZ.Count; ++z)
                {
                    for (int x = 0; x < positionsX.Count; ++x)
                    {
                        lowerZX[z, x] = new(positionsX[x], lowerBound.Y, positionsZ[z]);
                        upperZX[z, x] = new(positionsX[x], upperBound.Y, positionsZ[z]);
                    }
                }
                AddSide(ref triangles, lowerZX, (Vector.UnitY, Vector.UnitZ, Vector.UnitX), thickness);
                AddSide(ref triangles, upperZX, (-Vector.UnitY, Vector.UnitZ, Vector.UnitX), thickness);
            }

            // YZ sides
            {
                Point3[,] lowerYZ = new Point3[positionsY.Count, positionsZ.Count];
                Point3[,] upperYZ = new Point3[positionsY.Count, positionsZ.Count];
                for (int y = 0; y < positionsY.Count; ++y)
                {
                    for (int z = 0; z < positionsZ.Count; ++z)
                    {
                        lowerYZ[y, z] = new(lowerBound.X, positionsY[y], positionsZ[z]);
                        upperYZ[y, z] = new(upperBound.X, positionsY[y], positionsZ[z]);
                    }
                }
                AddSide(ref triangles, lowerYZ, (Vector.UnitX, Vector.UnitY, Vector.UnitZ), thickness);
                AddSide(ref triangles, upperYZ, (-Vector.UnitX, Vector.UnitY, Vector.UnitZ), thickness);
            }
        }

        private static void AddSide(ref List<Triangle> triangles, Point3[,] points, ValueTuple<Vector, Vector, Vector> directions, double thicknessValue)
        {
            var (normal, dir1, dir2) = directions;
            Vector thickness = normal * -thicknessValue;

            Func<Vector, Point3, Point3, Point3, Triangle> triangle = (normal.X + normal.Y + normal.Z > 0)
                ? ((Vector n, Point3 v1, Point3 v2, Point3 v3) => new Triangle(n, v1, v2, v3))
                : ((Vector n, Point3 v1, Point3 v2, Point3 v3) => new Triangle(n, v1, v3, v2));

            for (int i = 0; i < points.GetLength(0) - 1; ++i)
            {
                for (int j = 0; j < points.GetLength(1) - 1; ++j)
                {
                    var p00 = points[i, j];
                    var p01 = points[i, j + 1];
                    var p10 = points[i + 1, j];
                    var p11 = points[i + 1, j + 1];
                    if (i % 2 == 1 && j % 2 == 1)
                    {
                        triangles.Add(triangle(dir2, p00, p10, p00 + thickness));
                        triangles.Add(triangle(dir2, p10 + thickness, p00 + thickness, p10));
                        triangles.Add(triangle(-dir2, p01, p01 + thickness, p11));
                        triangles.Add(triangle(-dir2, p11 + thickness, p11, p01 + thickness));
                        // Flip i and j, and reverse the order
                        triangles.Add(triangle(dir1, p00, p00 + thickness, p01));
                        triangles.Add(triangle(dir1, p01 + thickness, p01, p00 + thickness));
                        triangles.Add(triangle(-dir1, p10, p11, p10 + thickness));
                        triangles.Add(triangle(-dir1, p11 + thickness, p10 + thickness, p11));
                    }
                    else
                    {
                        if (i != 0 && j != 0 && i != points.GetLength(0) - 2 && j != points.GetLength(1) - 2)
                        {
                            triangles.Add(triangle(normal, p00, p10, p01));
                            triangles.Add(triangle(normal, p11, p01, p10));
                        }
                        triangles.Add(triangle(-normal, p00 + thickness, p01 + thickness, p10 + thickness));
                        triangles.Add(triangle(-normal, p11 + thickness, p10 + thickness, p01 + thickness));
                    }
                }
            }
        }

        private static void MakePositions(Vector size, Parameters parameters, out List<double> xs, out List<double> ys, out List<double> zs)
        {
            var (Clearance, Thickness, Width, DesiredSpacing) = parameters;

            // Number of bars in the given direction
            Vector N = (size + (2 * Clearance) - DesiredSpacing) / (Width + DesiredSpacing);
            N = new((int)N.X, (int)N.Y, (int)N.Z); // Round down

            Vector _numerators = size + (2 * Clearance) - (N * Width);
            Vector RealSpacing = new(
                _numerators.X / (N.X + 1),
                _numerators.Y / (N.Y + 1),
                _numerators.Z / (N.Z + 1)
            );

            Point3 lowerBound = new(-Clearance);
            Point3 upperBound = new(
                -Clearance + N.X * (RealSpacing.X + Width) + RealSpacing.X,
                -Clearance + N.Y * (RealSpacing.Y + Width) + RealSpacing.Y,
                -Clearance + N.Z * (RealSpacing.Z + Width) + RealSpacing.Z
            );

            var makePositions = (out List<double> ps, Func<Point3, double> pElem, Func<Vector, double> vElem) =>
            {
                ps = new(4 + ((int)vElem(N) * 2));
                ps.Add(pElem(lowerBound) - Thickness);
                for (double pos = pElem(lowerBound); pos <= pElem(upperBound); pos += (vElem(RealSpacing) + Width))
                {
                    ps.Add(pos);
                    ps.Add(pos + vElem(RealSpacing));
                }
                ps.Add(pElem(upperBound) + Thickness);
            };

            makePositions(out xs, (p) => p.X, (v) => v.X);
            makePositions(out ys, (p) => p.Y, (v) => v.Y);
            makePositions(out zs, (p) => p.Z, (v) => v.Z);
        }
    }
}
