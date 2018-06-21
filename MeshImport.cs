using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HelmetVolumes
{
    static partial class CMS
    {       
        public static Mesh ContourMesh(Mesh inputMesh, float res)
        {
            System.Diagnostics.Stopwatch
                importTime = new System.Diagnostics.Stopwatch(),
                voxelTime = new System.Diagnostics.Stopwatch();

            if (res <= 0) res = 1;
            int[] inTris = inputMesh.triangles;
            Vector3[] inVerts = inputMesh.vertices;

            scale = GetScale(inVerts, inTris, res);
            step = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            delta = new Vector3(scale.x / 2f, scale.y / 2f, scale.z / 2f);

            importTime.Start();
            List<Octant>[][] octants = GetOctants(inTris, inVerts);
            importTime.Stop();

            voxelTime.Start();
            List<Vector3> vertices;
            List<Segment>[][][] segments;
            GetSegments(octants, out vertices, out segments);

            List<Square>[][][] squares = GetSquares(segments);
            List<int> triangles = GetCubes(vertices, squares);
            voxelTime.Stop();

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            octantSize = "Octant Size: (" + scale.x + ", " + scale.y + ", " + scale.z + ")\n"; 

            importVoxTime =
            (
                "Import Time: " + importTime.ElapsedMilliseconds + "ms\n" +
                "Voxel Time: " + voxelTime.ElapsedMilliseconds + "ms\n"
            );

            return mesh;
        }

        private static Vector3 GetScale(Vector3[] v, int[] t, float res)
        {
            Vector3 avg = Vector3.zero;

            for (int n = 0; n < t.Length; n += 3)
            {
                avg.x += GetMax(v[t[n]].x, v[t[n + 1]].x, v[t[n + 2]].x) - GetMin(v[t[n]].x, v[t[n + 1]].x, v[t[n + 2]].x);
                avg.y += GetMax(v[t[n]].y, v[t[n + 1]].y, v[t[n + 2]].y) - GetMin(v[t[n]].y, v[t[n + 1]].y, v[t[n + 2]].y);
                avg.z += GetMax(v[t[n]].z, v[t[n + 1]].z, v[t[n + 2]].z) - GetMin(v[t[n]].z, v[t[n + 1]].z, v[t[n + 2]].z);
            }

            avg /= (t.Length / 3);
            avg *= res * .5f;

            return avg;
        }

        private static float GetMin(float a, float b, float c)
        {
            float min = float.MaxValue;

            if (a < min) min = a;
            if (b < min) min = b;
            if (c < min) min = c;

            return min;
        }

        private static float GetMax(float a, float b, float c)
        {
            float max = float.MinValue;

            if (a > max) max = a;
            if (b > max) max = b;
            if (c > max) max = c;

            return max;
        }

        private static List<Octant>[][] GetOctants(int[] triangles, Vector3[] vertices)
        {
            int length, width, height;
            List<Octant>[][] octants;
            List<float>[][] hMapZ, hMapY, hMapX;

            GetDimensions(vertices, out length, out width, out height);
            hMapZ = GetHeightMapZ(length, width, triangles, vertices);
            hMapY = GetHeightMapY(length, height, triangles, vertices);
            hMapX = GetHeightMapX(width, height, triangles, vertices);

            octants = new List<Octant>[length + 2][];

            for (int x = 0; x < octants.Length; x++)
                octants[x] = new List<Octant>[width + 2];

            for (int x = 0; x < octants.Length; x++)
            {
                octants[x][0] = new List<Octant>(0);
                octants[x][width + 1] = new List<Octant>(0);
            }

            for (int y = 1; y < octants[0].Length - 1; y++)
            {
                octants[0][y] = new List<Octant>(0);
                octants[length + 1][y] = new List<Octant>(0);
            }

            for (int x = 0; x < length; x++)
                for (int y = 0; y < width; y++)
                    octants[x + 1][y + 1] = new List<Octant>();

            GetOctantsZ(octants, hMapZ, length, width);
            GetOctantsY(octants, hMapY, length, height, width);
            GetOctantsX(octants, hMapX, width, height, length);

            return octants;
        }

        private static void GetDimensions(Vector3[] vertices, out int length, out int width, out int height)
        {
            float
                 xMin = float.MaxValue, xMax = float.MinValue,
                 yMin = float.MaxValue, yMax = float.MinValue,
                 zMin = float.MaxValue, zMax = float.MinValue;

            foreach (Vector3 v in vertices)
            {
                if (v.x < xMin) xMin = v.x;
                if (v.y < yMin) yMin = v.y;
                if (v.z < zMin) zMin = v.z;
                if (v.x > xMax) xMax = v.x;
                if (v.y > yMax) yMax = v.y;
                if (v.z > zMax) zMax = v.z;
            }

            for (int n = 0; n < vertices.Length; n++)
            {
                vertices[n].x -= xMin;
                vertices[n].y -= yMin;
                vertices[n].z -= zMin;
            }

            xMax -= xMin;
            yMax -= yMin;
            zMax -= zMin;
            maximums = new Vector3(xMax, yMax, zMax);

            length = (int)(xMax * step.x) + 1;
            width = (int)(yMax * step.y) + 1;
            height = (int)(zMax * step.z) + 1;

            dimensions =
            (
                "Length: " + length + ", Width: " + width + ", Height: " + height + "\n" +
                "Maximums: (" + xMax + ", " + yMax + ", " + zMax + ")\n"
            );
        }

        private static void GetOctantsZ(List<Octant>[][] octants, List<float>[][] hMapZ, int length, int width)
        {
            Octant last;
            int n, z2, z3;

            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    n = 1;
                    last = Octant.zero;

                    for (int z = 0; z < hMapZ[x][y].Count; z++)
                    {
                        z2 = (int)(hMapZ[x][y][z] * step.z);

                        if (z2 > last.range)
                        {
                            if (n > 1)
                            {
                                last.z /= n;
                                n = 1;
                            }

                            z3 = GetHeight(octants[x + 1][y + 1], z2);

                            if (z3 == int.MinValue)
                            {
                                last = new Octant(x * scale.x, y * scale.y, hMapZ[x][y][z], z2);
                                octants[x + 1][y + 1].Add(last);
                            }
                            else
                            {
                                last = octants[x + 1][y + 1][z3];
                                last.z += hMapZ[x][y][z];
                                n++;
                            }
                        }
                        else
                        {
                            last.z += hMapZ[x][y][z];
                            n++;
                        }
                    }

                    if (n > 1) last.z /= n;
                }
            }

            for (int x = 1; x < length + 1; x++)
                for (int y = 1; y < width + 1; y++)
                    octants[x][y].Sort((a, b) => a.range.CompareTo(b.range));
        }

        private static void GetOctantsY(List<Octant>[][] octants, List<float>[][] hMapY, int length, int height, int width)
        {
            Octant lastPoint;
            int last, n, z2, y2;

            for (int x = 0; x < length; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    n = 1; z2 = int.MinValue;
                    last = int.MinValue; lastPoint = Octant.zero;

                    for (int y = 0; y < hMapY[x][z].Count; y++)
                    {
                        y2 = (int)(hMapY[x][z][y] * step.y);

                        if (last < y2)
                        {
                            if (n > 1)
                            {
                                lastPoint.y /= n;
                                n = 1;
                            }

                            last = y2;
                            z2 = GetHeight(octants[x + 1][y2 + 1], z);

                            if (z2 == int.MinValue)
                            {
                                lastPoint = new Octant(x * scale.x, hMapY[x][z][y], z * scale.z, z);
                                octants[x + 1][y2 + 1].Add(lastPoint);
                            }
                            else
                            {
                                lastPoint = octants[x + 1][y2 + 1][z2];
                                lastPoint.y += hMapY[x][z][y];
                                n++;
                            }
                        }
                        else
                        {
                            lastPoint.y += hMapY[x][z][y];
                            n++;
                        }
                    }

                    if (n > 1) lastPoint.y /= n;
                }
            }

            for (int x = 1; x < length + 1; x++)
                for (int y = 1; y < width + 1; y++)
                    octants[x][y].Sort((a, b) => a.range.CompareTo(b.range));
        }

        private static void GetOctantsX(List<Octant>[][] octants, List<float>[][] hMapX, int width, int height, int length)
        {
            Octant lastPoint;
            int last, n, x2, z2;

            for (int y = 0; y < width; y++)
            {
                for (int z = 0; z < height; z++)
                {
                    n = 1; z2 = int.MinValue;
                    last = int.MinValue; lastPoint = Octant.zero;

                    for (int x = 0; x < hMapX[y][z].Count; x++)
                    {
                        x2 = (int)(hMapX[y][z][x] * step.x);

                        if (last < x2)
                        {
                            if (n > 1)
                            {
                                lastPoint.x /= n;
                                n = 1;
                            }

                            last = x2;
                            z2 = GetHeight(octants[x2 + 1][y + 1], z);

                            if (z2 == int.MinValue)
                            {
                                lastPoint = new Octant(hMapX[y][z][x], y * scale.y, z * scale.z, z);
                                octants[x2 + 1][y + 1].Add(lastPoint);
                            }
                            else
                            {
                                lastPoint = octants[x2 + 1][y + 1][z2];
                                lastPoint.x += hMapX[y][z][x];
                                n++;
                            }
                        }
                        else
                        {
                            lastPoint.x += hMapX[y][z][x];
                            n++;
                        }
                    }

                    if (n > 1) lastPoint.x /= n;
                }
            }

            for (int x = 1; x < length + 1; x++)
                for (int y = 1; y < width + 1; y++)
                    octants[x][y].Sort((a, b) => a.range.CompareTo(b.range));
        }

        private static bool ContainsRepetition(List<Octant>[][] octants)
        {
            int last;

            for (int x = 0; x < octants.Length; x++)
                for (int y = 0; y < octants[0].Length; y++)
                {
                    last = int.MinValue;

                    foreach (Octant z in octants[x][y])
                        if (z.range > last)
                            last = z.range;
                        else
                            return true;
                }

            return false;
        }

        private static int GetHeight(List<Octant> column, int z)
        {
            for (int n = 0; n < column.Count; n++)
                if (column[n].range > z)
                    return int.MinValue;
                else if (column[n].range == z)
                    return n;

            return int.MinValue;
        }

        private class Triangle
        {
            public readonly Vector3 a, b, c;
            public readonly int xMin, xMax, yMin, yMax, zMin, zMax;

            public Triangle(int[] triangles, Vector3[] vertices, int n)
            {
                a = vertices[triangles[n]];
                b = vertices[triangles[n + 1]];
                c = vertices[triangles[n + 2]];

                xMin = (int)(GetMin(a.x, b.x, c.x) * step.x);
                yMin = (int)(GetMin(a.y, b.y, c.y) * step.y);
                zMin = (int)(GetMin(a.z, b.z, c.z) * step.z);

                xMax = (int)(GetMax(a.x, b.x, c.x) * step.x);
                yMax = (int)(GetMax(a.y, b.y, c.y) * step.y);
                zMax = (int)(GetMax(a.z, b.z, c.z) * step.z);
            }
        }

        private static List<float>[][] GetHeightMapZ(int length, int width, int[] triangles, Vector3[] vertices)
        {
            float z, x2;
            List<float>[][] hMapZ = new List<float>[length][];
            Triangle triangle;

            for (int x = 0; x < length; x++)
            {
                hMapZ[x] = new List<float>[width];

                for (int y = 0; y < width; y++)
                    hMapZ[x][y] = new List<float>();
            }

            for (int n = 0; n < triangles.Length; n += 3)
            {
                triangle = new Triangle(triangles, vertices, n);

                for (int x = triangle.xMin; x <= triangle.xMax; x++)
                {
                    x2 = x * scale.x;

                    for (int y = triangle.yMin; y <= triangle.yMax; y++)
                    {
                        z = GetHeightZ(triangle, x2, y * scale.y);

                        if (z != float.MinValue && z >= 0 && z <= maximums.z)
                            hMapZ[x][y].Add(z);
                    }
                }
            }

            for (int x = 0; x < length; x++)
                for (int y = 0; y < width; y++)
                    hMapZ[x][y].Sort((a, b) => a.CompareTo(b));

            return hMapZ;
        }

        private static List<float>[][] GetHeightMapY(int length, int height, int[] triangles, Vector3[] vertices)
        {
            float y, x2;
            List<float>[][] hMapY = new List<float>[length][];
            Triangle triangle;

            for (int x = 0; x < length; x++)
            {
                hMapY[x] = new List<float>[height];

                for (int z = 0; z < height; z++)
                    hMapY[x][z] = new List<float>();
            }

            for (int n = 0; n < triangles.Length; n += 3)
            {
                triangle = new Triangle(triangles, vertices, n);

                for (int x = triangle.xMin; x <= triangle.xMax; x++)
                {
                    x2 = x * scale.x;

                    for (int z = triangle.zMin; z <= triangle.zMax; z++)
                    {
                        y = GetHeightY(triangle, x2, z * scale.z);

                        if (y != float.MinValue && y >= 0 && y <= maximums.y)
                            hMapY[x][z].Add(y);
                    }
                }
            }

            for (int x = 0; x < length; x++)
                for (int z = 0; z < height; z++)
                    hMapY[x][z].Sort((a, b) => a.CompareTo(b));

            return hMapY;
        }

        private static List<float>[][] GetHeightMapX(int width, int height, int[] triangles, Vector3[] vertices)
        {
            float x, y2;
            List<float>[][] hMapX = new List<float>[width][];
            Triangle triangle;

            for (int y = 0; y < width; y++)
            {
                hMapX[y] = new List<float>[height];

                for (int z = 0; z < height; z++)
                    hMapX[y][z] = new List<float>();
            }

            for (int n = 0; n < triangles.Length; n += 3)
            {
                triangle = new Triangle(triangles, vertices, n);

                for (int y = triangle.yMin; y <= triangle.yMax; y++)
                {
                    y2 = y * scale.y;

                    for (int z = triangle.zMin; z <= triangle.zMax; z++)
                    {
                        x = GetHeightX(triangle, y2, z * scale.z);

                        if (x != float.MinValue && x >= 0 && x <= maximums.x)
                            hMapX[y][z].Add(x);
                    }
                }
            }

            for (int y = 0; y < width; y++)
                for (int z = 0; z < height; z++)
                    hMapX[y][z].Sort((a, b) => a.CompareTo(b));

            return hMapX;
        }

        private static float GetHeightZ(Triangle t, float x, float y)
        {
            float w1, w2;

            w1 = ((x - t.a.x) * (t.c.y - t.a.y) - (y - t.a.y) * (t.c.x - t.a.x))
                / ((t.b.x - t.a.x) * (t.c.y - t.a.y) - (t.b.y - t.a.y) * (t.c.x - t.a.x));
            w2 = ((y - t.a.y) - w1 * (t.b.y - t.a.y)) / (t.c.y - t.a.y);

            if ((w1 >= 0f && w1 <= 1f) && (w2 >= 0f && w2 <= 1f))
                return t.a.z + w1 * (t.b.z - t.a.z) + w2 * (t.c.z - t.a.z);

            w1 = ((x - t.c.x) * (t.b.y - t.c.y) - (y - t.c.y) * (t.b.x - t.c.x))
                / ((t.a.x - t.c.x) * (t.b.y - t.c.y) - (t.a.y - t.c.y) * (t.b.x - t.c.x));
            w2 = ((y - t.c.y) - w1 * (t.a.y - t.c.y)) / (t.b.y - t.c.y);

            if ((w1 >= 0f && w1 <= 1f) && (w2 >= 0f && w2 <= 1f))
                return t.c.z + w1 * (t.a.z - t.c.z) + w2 * (t.b.z - t.c.z);

            w1 = ((x - t.b.x) * (t.a.y - t.b.y) - (y - t.b.y) * (t.a.x - t.b.x))
                / ((t.c.x - t.b.x) * (t.a.y - t.b.y) - (t.c.y - t.b.y) * (t.a.x - t.b.x));
            w2 = ((y - t.b.y) - w1 * (t.c.y - t.b.y)) / (t.a.y - t.b.y);

            if ((w1 >= 0f && w1 <= 1f) && (w2 >= 0f && w2 <= 1f))
                return t.b.z + w1 * (t.c.z - t.b.z) + w2 * (t.a.z - t.b.z);

            return float.MinValue;
        }

        private static float GetHeightY(Triangle t, float x, float z)
        {
            float w1, w2, y;

            w1 = ((x - t.a.x) * (t.c.z - t.a.z) - (z - t.a.z) * (t.c.x - t.a.x))
                / ((t.b.x - t.a.x) * (t.c.z - t.a.z) - (t.b.z - t.a.z) * (t.c.x - t.a.x));
            w2 = ((z - t.a.z) - w1 * (t.b.z - t.a.z)) / (t.c.z - t.a.z);

            if ((w1 >= 0f && w1 <= 1f) && (w2 >= 0f && w2 <= 1f))
                return t.a.y + w1 * (t.b.y - t.a.y) + w2 * (t.c.y - t.a.y);

            w1 = ((x - t.c.x) * (t.b.z - t.c.z) - (z - t.c.z) * (t.b.x - t.c.x))
                / ((t.a.x - t.c.x) * (t.b.z - t.c.z) - (t.a.z - t.c.z) * (t.b.x - t.c.x));
            w2 = ((z - t.c.z) - w1 * (t.a.z - t.c.z)) / (t.b.z - t.c.z);

            if ((w1 >= 0f && w1 <= 1f) && (w2 >= 0f && w2 <= 1f))
                return t.c.y + w1 * (t.a.y - t.c.y) + w2 * (t.b.y - t.c.y);

            w1 = ((x - t.b.x) * (t.a.z - t.b.z) - (z - t.b.z) * (t.a.x - t.b.x))
                / ((t.c.x - t.b.x) * (t.a.z - t.b.z) - (t.c.z - t.b.z) * (t.a.x - t.b.x));
            w2 = ((z - t.b.z) - w1 * (t.c.z - t.b.z)) / (t.a.z - t.b.z);

            if ((w1 >= 0f && w1 <= 1f) && (w2 >= 0f && w2 <= 1f))
                return t.b.y + w1 * (t.c.y - t.b.y) + w2 * (t.a.y - t.b.y);

            return float.MinValue;
        }

        private static float GetHeightX(Triangle t, float y, float z)
        {
            float w1, w2;

            w1 = ((z - t.a.z) * (t.c.y - t.a.y) - (y - t.a.y) * (t.c.z - t.a.z))
                / ((t.b.z - t.a.z) * (t.c.y - t.a.y) - (t.b.y - t.a.y) * (t.c.z - t.a.z));
            w2 = ((y - t.a.y) - w1 * (t.b.y - t.a.y)) / (t.c.y - t.a.y);

            if ((w1 >= 0f && w1 <= 1f) && (w2 >= 0f && w2 <= 1f))
                return t.a.x + w1 * (t.b.x - t.a.x) + w2 * (t.c.x - t.a.x);

            w1 = ((z - t.c.z) * (t.b.y - t.c.y) - (y - t.c.y) * (t.b.z - t.c.z))
                / ((t.a.z - t.c.z) * (t.b.y - t.c.y) - (t.a.y - t.c.y) * (t.b.z - t.c.z));
            w2 = ((y - t.c.y) - w1 * (t.a.y - t.c.y)) / (t.b.y - t.c.y);

            if ((w1 >= 0f && w1 <= 1f) && (w2 >= 0f && w2 <= 1f))
                return t.c.x + w1 * (t.a.x - t.c.x) + w2 * (t.b.x - t.c.x);

            w1 = ((z - t.b.z) * (t.a.y - t.b.y) - (y - t.b.y) * (t.a.z - t.b.z))
                / ((t.c.z - t.b.z) * (t.a.y - t.b.y) - (t.c.y - t.b.y) * (t.a.z - t.b.z));
            w2 = ((y - t.b.y) - w1 * (t.c.y - t.b.y)) / (t.a.y - t.b.y);

            if ((w1 >= 0f && w1 <= 1f) && (w2 >= 0f && w2 <= 1f))
                return t.b.x + w1 * (t.c.x - t.b.x) + w2 * (t.a.x - t.b.x);

            return float.MinValue;
        }
    }
}