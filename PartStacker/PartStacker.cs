using System.Windows.Forms;
using PartStacker.FormComponents;
using PartStacker.Geometry;

namespace PartStacker
{
    using Rotation = Action<Mesh>;

    public class PartStacker
    {
        private readonly static Rotation[][] RotationSets;

        public class Parameters
        {
            // `Parameters` must be a class, not a struct, because of reference semantics

            public required int InitialTriangles;
            public required PartsListItem[] BaseParts;

            public required Action<double, double> SetProgress;
            public required Action<bool, Mesh> FinishStacking;
            public required Action<Mesh, int, int, int> DisplayMesh;
            public required double Resolution;
            
            public required double xMin, xMax;
            public required double yMin, yMax;
            public required double zMin, zMax;
        }

        Parameters Params;
        Thread Thread;
        public bool Running { get; private set; }
        public Mesh Result { get; private set; }

        public void Stop()
        {
            if (Running)
            {
                Running = false;
                Thread.Join();
            }
        }

        Mesh[][] Meshes;
        int[][,,] Voxels;
        bool[,,] Space;

        public PartStacker(Parameters parameters)
        {
            Params = parameters;
            Thread = new Thread(Stacker);
            Running = true;
            Thread.Start();
        }
        private void SetProgress( double progress, double total)
        {
            Params.SetProgress(progress, total);
        }

        private void Stacker()
        {
            bool? b = Stacker_Sub();
            if (b.HasValue)
                Params.FinishStacking(b.Value, Result);
            Running = false;
        }

        private bool? Stacker_Sub()
        {
            Params.BaseParts = Params.BaseParts.OrderBy(p => p.Volume).ToArray();
            Result = new Mesh(Params.InitialTriangles);

            double triangles = 0;
            double scale = 1 / Params.Resolution;
            int totalParts = 0;

            foreach (PartsListItem p in Params.BaseParts)
            {
                triangles += p.Triangles * RotationSets[p.RotationIndex].Length;
                totalParts += p.Quantity;
            }

            Meshes = new Mesh[Params.BaseParts.Length][];
            Voxels = new int[Params.BaseParts.Length][, ,];

            double progress = 0;
            for (int i = 0; i < Params.BaseParts.Length; i++)
            {
                Rotation baseRotation = (Mesh m) => { };

                if (Params.BaseParts[i].RotateMinBox)
                {
                    List<Triangle> originalTriangles = Params.BaseParts[i].BasePart.Triangles.ToList();

                    List<Triangle> toRemove = new List<Triangle>();
                    for (int j = 0; j < originalTriangles.Count; j++)
                    {
                        if (j % 15 != 0)
                            toRemove.Add(originalTriangles[j]);
                    }

                    foreach (Triangle t in toRemove)
                        originalTriangles.Remove(t);

                    Mesh original = new(originalTriangles);

                    int best = int.MaxValue; int bestA = 0, bestB = 0;

                    for (int a = 0; a < 360; a += 9)
                    {
                        original.Rotate(new Vector(1, 0, 0), 9);
                        for (int b = 0; b < 360; b += 9)
                        {
                            original.Rotate(new Vector(0, 1, 0), 9);
                            Tuple<int, int, int> box = original.CalcBox();
                            int volume = box.Item1 * box.Item2 * box.Item3;
                            if (volume < best)
                            {
                                best = volume;
                                bestA = a; bestB = b;
                            }
                        }
                    }

                    baseRotation = (Mesh m) => { m.Rotate(new Vector(1, 0, 0), bestA); m.Rotate(new Vector(0, 1, 0), bestB); };
                }

                // Set up array of parts
                Meshes[i] = new Mesh[RotationSets[Params.BaseParts[i].RotationIndex].Length];

                // Track bounding box size
                int maxBX = 1, maxBY = 1, maxBZ = 1;

                // Calculate all the rotations
                for (int j = 0; j < Meshes[i].Length; j++)
                {
                    if (!Running)
                        return null;

                    Mesh thisPart = Params.BaseParts[i].BasePart.Clone();
                    thisPart.Scale(scale);
                    RotationSets[Params.BaseParts[i].RotationIndex][j](thisPart);
                    baseRotation(thisPart);

                    Tuple<int, int, int> max = thisPart.CalcBox();
                    maxBX = Math.Max(max.Item1, maxBX); maxBY = Math.Max(max.Item2, maxBY); maxBZ = Math.Max(max.Item3, maxBZ);

                    Meshes[i][j] = thisPart;

                    progress += Params.BaseParts[i].Triangles / 2;
                    SetProgress(progress, triangles);
                }

                // Initialize space size to appropriate dimensions
                Voxels[i] = new int[maxBX, maxBY, maxBZ];

                // Voxelize each rotated instance of this part
                int index = 1;
                for (int j = 0; j < Meshes[i].Length; j++)
                {
                    if (!Running)
                        return null;

                    Meshes[i][j].Voxelize(Voxels[i], index, Params.BaseParts[i].MinHole);
                    index *= 2;

                    progress += Params.BaseParts[i].Triangles / 2;
                    SetProgress(progress, triangles);
                }
            }

            int maxX = (int)(scale * Params.xMin);
            int maxY = (int)(scale * Params.yMin);
            int maxZ = (int)(scale * Params.zMin);
            Space = new bool[(int)Math.Max(maxX, (scale * Params.xMax)), (int)Math.Max(maxY, (scale * Params.yMax)), (int)Math.Max(maxZ, (scale * Params.zMax))];

            SetProgress(0, 1);

            int currentCount = 0;
            int pIndex = Meshes.Length - 1;
            while (pIndex >= 0)
            {
                int oldP = pIndex;

                pIndex = TryPlace(pIndex, maxX, maxY, maxZ, totalParts, ref currentCount);
                if (pIndex < 0)
                    break; // Done!

                // If pIndex has not changed it means there are no more ways to place an instance of the current part in the box: it must be enlarged
                if (pIndex == oldP)
                {
                    int best = int.MaxValue;
                    int newX = Space.GetLength(0), newY = Space.GetLength(1), newZ = Space.GetLength(2);

                    int minBX = int.MaxValue, minBY = int.MaxValue, minBZ = int.MaxValue;
                    foreach (Mesh p in Meshes[pIndex])
                    {
                        minBX = Math.Min(p.box.Item1, minBX);
                        minBY = Math.Min(p.box.Item2, minBY);
                        minBZ = Math.Min(p.box.Item3, minBZ);
                    }

                    for (int s = 0; s < Space.GetLength(0) + Space.GetLength(1) + Space.GetLength(2) - minBX - minBY - minBZ; s++)
                        for (int r = Math.Max(0, s - Space.GetLength(2) - minBZ); r <= Math.Min(s, Space.GetLength(0) + Space.GetLength(1) - minBX - minBY); r++)
                        {
                            int z = s - r;
                            if (Math.Max(z + minBZ, maxZ) * maxY * maxX > best)
                                break;

                            for (int x = Math.Max(0, r - Space.GetLength(1) - minBY); x <= Math.Min(r, Space.GetLength(0) - minBZ); x++)
                            {
                                int y = r - x;
                                if (Math.Max(x + minBX, maxX) * Math.Max(y + minBY, maxY) * Math.Max(z + minBZ, maxZ) > best)
                                    continue;

                                Rotation[] rotations = RotationSets[Params.BaseParts[pIndex].RotationIndex];

                                // Calculate which orientations fit in bounding box
                                int index = 1;
                                int possible = 0;
                                for (int i = 0; i < rotations.Length; i++)
                                {
                                    if (x + Meshes[pIndex][i].box.Item1 < Space.GetLength(0) && y + Meshes[pIndex][i].box.Item2 < Space.GetLength(1) && z + Meshes[pIndex][i].box.Item3 < Space.GetLength(2))
                                        possible |= index;
                                    index *= 2;
                                }
                                
                                possible = CanPlace(possible, Voxels[pIndex], x, y, z);

                                index = 1;
                                if (possible != 0) // If it fits, figure out which rotation to use
                                    for (int i = 0; i < rotations.Length; i++)
                                    {
                                        if ((possible & index) != 0)
                                        {
                                            int newbox = Math.Max(maxX, x + Meshes[pIndex][i].box.Item1) * Math.Max(maxY, y + Meshes[pIndex][i].box.Item2) * Math.Max(maxZ, z + Meshes[pIndex][i].box.Item3);
                                            if (newbox < best)
                                            {
                                                best = newbox;
                                                newX = x + Meshes[pIndex][i].box.Item1;
                                                newY = y + Meshes[pIndex][i].box.Item2;
                                                newZ = z + Meshes[pIndex][i].box.Item3;
                                            }
                                        }
                                        index *= 2;
                                    }
                            }
                        }

                    if (best == int.MaxValue)
                    {
                        Result = null;
                        return false;
                    }

                    maxX = Math.Max(maxX, newX + 2);
                    maxY = Math.Max(maxY, newY + 2);
                    maxZ = Math.Max(maxZ, newZ + 2);
                }

                if (!Running)
                    return null;
            }

            Result.Scale(1 / scale);
            return true;
        }

        private int TryPlace(int p, int maxX, int maxY, int maxZ, int totalParts, ref int currentCount)
        {
            for (int s = 0; s <= maxX + maxY + maxZ; s++)
                for (int r = Math.Max(0, s - maxZ); r <= Math.Min(s, maxX + maxY); r++)
                {
                    int z = s - r;
                    for (int x = Math.Max(0, r - maxY); x <= Math.Min(r, maxX); x++)
                    {
                        int y = r - x;
                        Rotation[] rotations = RotationSets[Params.BaseParts[p].RotationIndex];

                        // Calculate which orientations fit in bounding box
                        int index = 1;
                        int possible = 0;
                        for (int i = 0; i < rotations.Length; i++)
                        {
                            if (x + Meshes[p][i].box.Item1 < maxX && y + Meshes[p][i].box.Item2 < maxY && z + Meshes[p][i].box.Item3 < maxZ)
                                possible |= index;
                            index *= 2;
                        }

                        possible = CanPlace(possible, Voxels[p], x, y, z);

                        if (possible != 0) // If it fits, figure out which rotation to use
                        {
                            index = 1;
                            for (int i = 0; i < rotations.Length; i++)
                                if ((possible & index) != 0)
                                {
                                    if (!Result.Add(Meshes[p][i], new Vector(x, y, z))) // Add to result
                                    {
                                        MessageBox.Show("Intersecting triangles error!");
                                    }
                                    Place(index, Voxels[p], x, y, z); // Mark voxels as occupied

                                    currentCount++;
                                    SetProgress(currentCount, totalParts);
                                    Params.DisplayMesh(Result, maxX, maxY, maxZ);

                                    Params.BaseParts[p].Remaining--; // Move to next instance of part
                                    if (Params.BaseParts[p].Remaining == 0) // All instances placed, try next part
                                    {
                                        p--;
                                        if (p < 0)
                                            return -1;
                                    }

                                    break;
                                }
                                else
                                    index *= 2;
                        }
                    }
                }

            // Reached the end of the box, return the part we're currently at.
            return p;
        }

        private int CanPlace(int possible, int[, ,] obj, int x, int y, int z)
        {
            int maxI = Math.Min(x + obj.GetLength(0), Space.GetLength(0)), maxJ = Math.Min(y + obj.GetLength(1), Space.GetLength(1)), maxK = Math.Min(z + obj.GetLength(2), Space.GetLength(2));

            for (int i = x; i < maxI; i++)
                for (int j = y; j < maxJ; j++)
                    for (int k = z; k < maxK; k++)
                        if (Space[i, j, k])
                        {
                            possible = possible & (possible ^ obj[i - x, j - y, k - z]);
                            if (possible == 0)
                                return 0;
                        }

            return possible;
        }

        private void Place(int index, int[, ,] obj, int x, int y, int z)
        {
            int m1 = Math.Min(obj.GetLength(0) + x, Space.GetLength(0));
            int m2 = Math.Min(obj.GetLength(1) + y, Space.GetLength(1));
            int m3 = Math.Min(obj.GetLength(2) + z, Space.GetLength(2));

            for (int i = x; i < m1; i++)
                for (int j = y; j < m2; j++)
                    for (int k = z; k < m3; k++)
                        Space[i, j, k] |= (obj[i - x, j - y, k - z] & index) != 0;
        }

        static PartStacker()
        {
            RotationSets = new Rotation[3][];

            // No rotation
            RotationSets[0] = [(Mesh m) => { }];

            // Cubic rotations
            RotationSets[1] =
            [
                (Mesh m) => { },
                (Mesh m) => { m.Rotate(new Vector(1, 0, 0), 90); },
                (Mesh m) => { m.Rotate(new Vector(1, 0, 0), 180); },
                (Mesh m) => { m.Rotate(new Vector(1, 0, 0), 270); },
                (Mesh m) => { m.Rotate(new Vector(0, 1, 0), 90); },
                (Mesh m) => { m.Rotate(new Vector(0, 1, 0), 180); },
                (Mesh m) => { m.Rotate(new Vector(0, 1, 0), 270); },
                (Mesh m) => { m.Rotate(new Vector(0, 0, 1), 90); },
                (Mesh m) => { m.Rotate(new Vector(0, 0, 1), 180); },
                (Mesh m) => { m.Rotate(new Vector(0, 0, 1), 270); },
                (Mesh m) => { m.Rotate(new Vector(1, 1, 0), 180); },
                (Mesh m) => { m.Rotate(new Vector(1, -1, 0), 180); },
                (Mesh m) => { m.Rotate(new Vector(0, 1, 1), 180); },
                (Mesh m) => { m.Rotate(new Vector(0, -1, 1), 180); },
                (Mesh m) => { m.Rotate(new Vector(1, 0, 1), 180); },
                (Mesh m) => { m.Rotate(new Vector(1, 0, -1), 180); },
                (Mesh m) => { m.Rotate(new Vector(1, 1, 1), 120); },
                (Mesh m) => { m.Rotate(new Vector(1, 1, 1), 240); },
                (Mesh m) => { m.Rotate(new Vector(-1, 1, 1), 120); },
                (Mesh m) => { m.Rotate(new Vector(-1, 1, 1), 240); },
                (Mesh m) => { m.Rotate(new Vector(1, -1, 1), 120); },
                (Mesh m) => { m.Rotate(new Vector(1, -1, 1), 240); },
                (Mesh m) => { m.Rotate(new Vector(1, 1, -1), 120); },
                (Mesh m) => { m.Rotate(new Vector(1, 1, -1), 240); }
            ];

            //TODO: arbitrary rotations
            RotationSets[2] = new Rotation[32];

            RotationSets[2][0] = (Mesh m) => { };
            RotationSets[2][1] = (Mesh m) => { m.Rotate(new Vector(1, 1, 1), 120); };
            RotationSets[2][2] = (Mesh m) => { m.Rotate(new Vector(1, 1, 1), 240); };
            RotationSets[2][3] = (Mesh m) => { m.Rotate(new Vector(1, 0, 0), 180); };
            RotationSets[2][4] = (Mesh m) => { m.Rotate(new Vector(0, 1, 0), 180); };
            RotationSets[2][5] = (Mesh m) => { m.Rotate(new Vector(0, 0, 1), 180); };

            Random r = new Random();
            for (int i = 6; i < 32; i++)
            {
                double rx = r.NextDouble() * 360;
                double ry = r.NextDouble() * 360;
                double rz = r.NextDouble() * 360;
                RotationSets[2][i] = (Mesh m) => { m.Rotate(new Vector(1, 0, 0), rx); m.Rotate(new Vector(0, 1, 0), ry); m.Rotate(new Vector(0, 0, 1), rz); };
            }
        }
    }
}