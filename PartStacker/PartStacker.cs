using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PartStacker
{
    partial class MainForm
    {
        STLBody[][] parts;
        int[][, ,] voxels;
        bool[,,] space;
        Part[] baseParts;

        public void Stacker()
        {
            bool? b = Stacker_Sub();
            if (b.HasValue)
                Invoke(() => FinishStacking(b.Value));
            StackerThreadRunning = false;

            parts = null;
            voxels = null;
            space = null;
            baseParts = null;
        }

        public bool? Stacker_Sub()
        {
            float triangles = 0;
            float scale = (float)(1 / MinimumClearance.Value);
            int totalParts = 0;

            foreach (Part p in baseParts)
            {
                triangles += p.Triangles * RotationSets[p.RotationIndex].Length;
                totalParts += p.Quantity;
            }

            parts = new STLBody[baseParts.Length][];
            voxels = new int[baseParts.Length][, ,];

            float progress = 0;
            for (int i = 0; i < baseParts.Length; i++)
            {
                Rotation baseRotation = (STLBody b) => { };

                if (baseParts[i].RotateMinBox)
                {
                    STLBody original = (STLBody)baseParts[i].BasePart.Clone();

                    List<Triangle> toRemove = new List<Triangle>();
                    for (int j = 0; j < original.Triangles.Count; j++)
                    {
                        if (j % 15 != 0)
                            toRemove.Add(original.Triangles[j]);
                    }

                    foreach (Triangle t in toRemove)
                        original.Triangles.Remove(t);

                    int best = int.MaxValue; int bestA = 0, bestB = 0;

                    for (int a = 0; a < 360; a += 9)
                    {
                        original.Rotate(new Point3(1, 0, 0), 9.0f);
                        for (int b = 0; b < 360; b += 9)
                        {
                            original.Rotate(new Point3(0, 1, 0), 9.0f);
                            Tuple<int, int, int> box = original.CalcBox();
                            int volume = box.Item1 * box.Item2 * box.Item3;
                            if (volume < best)
                            {
                                best = volume;
                                bestA = a; bestB = b;
                            }
                        }
                    }

                    baseRotation = (STLBody b) => { b.Rotate(new Point3(1, 0, 0), bestA); b.Rotate(new Point3(0, 1, 0), bestB); };
                }

                // Set up array of parts
                parts[i] = new STLBody[RotationSets[baseParts[i].RotationIndex].Length];

                // Track bounding box size
                int maxBX = 1, maxBY = 1, maxBZ = 1;

                // Calculate all the rotations
                for (int j = 0; j < parts[i].Length; j++)
                {
                    if (!StackerThreadRunning)
                        return null;

                    STLBody thisPart = (STLBody)baseParts[i].BasePart.Clone();
                    thisPart.Scale(scale);
                    RotationSets[baseParts[i].RotationIndex][j](thisPart);
                    baseRotation(thisPart);

                    Tuple<int, int, int> max = thisPart.CalcBox();
                    maxBX = Math.Max(max.Item1, maxBX); maxBY = Math.Max(max.Item2, maxBY); maxBZ = Math.Max(max.Item3, maxBZ);

                    parts[i][j] = thisPart;

                    progress += (float)baseParts[i].Triangles / 2;
                    SetProgress(progress, triangles);
                }

                // Initialize space size to appropriate dimensions
                voxels[i] = new int[maxBX, maxBY, maxBZ];

                // Voxelize each rotated instance of this part
                int index = 1;
                for (int j = 0; j < parts[i].Length; j++)
                {
                    if (!StackerThreadRunning)
                        return null;

                    parts[i][j].Voxelize(voxels[i], index, baseParts[i].MinHole);
                    index *= 2;

                    progress += (float)baseParts[i].Triangles / 2;
                    SetProgress(progress, triangles);
                }
            }

            // Do insertion sort
            for (int i = 1; i < parts.Length; i++)
            {
                int j = i;
                while (baseParts[j].Volume < baseParts[j - 1].Volume)
                {
                    STLBody[] tempBD = parts[j];
                    int[, ,] tempVX = voxels[j];
                    Part tempPT = baseParts[j];

                    parts[j] = parts[j - 1];
                    voxels[j] = voxels[j - 1];
                    baseParts[j] = baseParts[j - 1];

                    parts[j - 1] = tempBD;
                    voxels[j - 1] = tempVX;
                    baseParts[j - 1] = tempPT;

                    j--;
                    if (j == 0) break;
                }
            }

            int maxX = (int)(scale * (float)xMin.Value);
            int maxY = (int)(scale * (float)yMin.Value);
            int maxZ = (int)(scale * (float)zMin.Value);
            space = new bool[(int)Math.Max(maxX, (scale * (float)xMax.Value)), (int)Math.Max(maxY, (scale * (float)yMax.Value)), (int)Math.Max(maxZ, (scale * (float)zMax.Value))];

            SetProgress(0, 1);

            int currentCount = 0;
            int pIndex = parts.Length - 1;
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
                    int newX = space.GetLength(0), newY = space.GetLength(1), newZ = space.GetLength(2);

                    int minBX = int.MaxValue, minBY = int.MaxValue, minBZ = int.MaxValue;
                    foreach (STLBody p in parts[pIndex])
                    {
                        minBX = Math.Min(p.box.Item1, minBX);
                        minBY = Math.Min(p.box.Item2, minBY);
                        minBZ = Math.Min(p.box.Item3, minBZ);
                    }

                    for (int s = 0; s < space.GetLength(0) + space.GetLength(1) + space.GetLength(2) - minBX - minBY - minBZ; s++)
                        for (int r = Math.Max(0, s - space.GetLength(2) - minBZ); r <= Math.Min(s, space.GetLength(0) + space.GetLength(1) - minBX - minBY); r++)
                        {
                            int z = s - r;
                            if (Math.Max(z + minBZ, maxZ) * maxY * maxX > best)
                                break;

                            for (int x = Math.Max(0, r - space.GetLength(1) - minBY); x <= Math.Min(r, space.GetLength(0) - minBZ); x++)
                            {
                                int y = r - x;
                                if (Math.Max(x + minBX, maxX) * Math.Max(y + minBY, maxY) * Math.Max(z + minBZ, maxZ) > best)
                                    continue;

                                Rotation[] rotations = RotationSets[baseParts[pIndex].RotationIndex];

                                // Calculate which orientations fit in bounding box
                                int index = 1;
                                int possible = 0;
                                for (int i = 0; i < rotations.Length; i++)
                                {
                                    if (x + parts[pIndex][i].box.Item1 < space.GetLength(0) && y + parts[pIndex][i].box.Item2 < space.GetLength(1) && z + parts[pIndex][i].box.Item3 < space.GetLength(2))
                                        possible |= index;
                                    index *= 2;
                                }
                                
                                possible = CanPlace(possible, voxels[pIndex], x, y, z);

                                index = 1;
                                if (possible != 0) // If it fits, figure out which rotation to use
                                    for (int i = 0; i < rotations.Length; i++)
                                    {
                                        if ((possible & index) != 0)
                                        {
                                            int newbox = Math.Max(maxX, x + parts[pIndex][i].box.Item1) * Math.Max(maxY, y + parts[pIndex][i].box.Item2) * Math.Max(maxZ, z + parts[pIndex][i].box.Item3);
                                            if (newbox < best)
                                            {
                                                best = newbox;
                                                newX = x + parts[pIndex][i].box.Item1;
                                                newY = y + parts[pIndex][i].box.Item2;
                                                newZ = z + parts[pIndex][i].box.Item3;
                                            }
                                        }
                                        index *= 2;
                                    }
                            }
                        }

                    if (best == int.MaxValue)
                    {
                        result = null;
                        return false;
                    }

                    maxX = Math.Max(maxX, newX + 2);
                    maxY = Math.Max(maxY, newY + 2);
                    maxZ = Math.Max(maxZ, newZ + 2);
                }

                if (!StackerThreadRunning)
                    return null;
            }

            result.Scale(1 / scale);
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
                        Rotation[] rotations = RotationSets[baseParts[p].RotationIndex];

                        // Calculate which orientations fit in bounding box
                        int index = 1;
                        int possible = 0;
                        for (int i = 0; i < rotations.Length; i++)
                        {
                            if (x + parts[p][i].box.Item1 < maxX && y + parts[p][i].box.Item2 < maxY && z + parts[p][i].box.Item3 < maxZ)
                                possible |= index;
                            index *= 2;
                        }

                        possible = CanPlace(possible, voxels[p], x, y, z);

                        if (possible != 0) // If it fits, figure out which rotation to use
                        {
                            index = 1;
                            for (int i = 0; i < rotations.Length; i++)
                                if ((possible & index) != 0)
                                {
                                    if (!parts[p][i].TranslateAndAdd(result, new Point3(x, y, z))) // Add to result
                                    {
                                        MessageBox.Show("Intersecting triangles error!");
                                    }
                                    Place(index, voxels[p], x, y, z); // Mark voxels as occupied

                                    currentCount++;
                                    SetProgress(currentCount, totalParts);
                                    this.Invoke((MethodInvoker)delegate { result.SetAsModel(Display3D); Display3D.BB = new Microsoft.Xna.Framework.Vector3(maxX, maxY, maxZ); });

                                    baseParts[p].Remaining--; // Move to next instance of part
                                    if (baseParts[p].Remaining == 0) // All instances placed, try next part
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
            int maxI = Math.Min(x + obj.GetLength(0), space.GetLength(0)), maxJ = Math.Min(y + obj.GetLength(1), space.GetLength(1)), maxK = Math.Min(z + obj.GetLength(2), space.GetLength(2));

            for (int i = x; i < maxI; i++)
                for (int j = y; j < maxJ; j++)
                    for (int k = z; k < maxK; k++)
                        if (space[i, j, k])
                        {
                            possible = possible & (possible ^ obj[i - x, j - y, k - z]);
                            if (possible == 0)
                                return 0;
                        }

            return possible;
        }

        private void Place(int index, int[, ,] obj, int x, int y, int z)
        {
            int m1 = Math.Min(obj.GetLength(0) + x, space.GetLength(0));
            int m2 = Math.Min(obj.GetLength(1) + y, space.GetLength(1));
            int m3 = Math.Min(obj.GetLength(2) + z, space.GetLength(2));

            for (int i = x; i < m1; i++)
                for (int j = y; j < m2; j++)
                    for (int k = z; k < m3; k++)
                        space[i, j, k] |= (obj[i - x, j - y, k - z] & index) != 0;
        }

        public void SetProgress(float progress, float total)
        {
            if (!StackerThreadRunning)
                return;

            if (Progress.InvokeRequired)
                this.Invoke((MethodInvoker)delegate { SetProgress(progress, total); });
            else
                Progress.Value = (int)(100 * progress / total);
        }
    }
}