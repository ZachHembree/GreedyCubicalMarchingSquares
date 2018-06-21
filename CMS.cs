using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace HelmetVolumes
{
    public static partial class CMS
    {
        public static string importVoxTime, dimensions, octantSize, startVertices;
        private static Vector3 scale, delta, step, maximums;
        //private static float scale.x, scale.y, scale.z, delta.x, delta.y, delta.z, step.x, step.y, step.z;

        private class Octant
        {
            public static readonly Octant zero = new Octant(0, 0, 0, int.MinValue);
            public readonly int range;
            public float x, y, z;

            public Octant(float _x, float _y, float _z, int _range)
            {
                x = _x;
                y = _y;
                z = _z;
                range = _range;
            }
            
            public Octant(int _x, int _y, float _z)
            {
                x = _x * scale.x;
                y = _y * scale.y;
                z = _z;
                range = (int)(_z * step.z);
            }

            public static Octant operator +(Octant a, Octant b) =>
                new Octant(a.x + b.x, a.y + b.y, a.z + b.z, a.range);

            public static Octant operator -(Octant a, Octant b) =>
                new Octant(a.x + b.x, a.y + b.y, a.z + b.z, a.range);

            public static Octant operator /(Octant a, int b) =>
                new Octant(a.x / b, a.y / b, a.z / b, a.range);
        }

        private class Segment
        {
            public static readonly Segment zero = new Segment(int.MaxValue, -1, 0);
            public readonly int z, index, conf;

            public Segment(int _z, int _index, int _conf)
            {
                z = _z;
                index = _index;
                conf = _conf;
            }
        }

        private static void GetSegments(IList<Octant>[][] octants, out List<Vector3> vertices, out List<Segment>[][][] segments)
        {
            int length = octants.Length, width = octants[1].Length, index = 0;
            vertices = new List<Vector3>((length * width * 6) + (length * width / 2));
            segments = new List<Segment>[3][][] { new List<Segment>[length][], new List<Segment>[length][], new List<Segment>[length][] };

            for (int x = 0; x < length; x++)
            {
                segments[0][x] = new List<Segment>[width];
                segments[1][x] = new List<Segment>[width];
                segments[2][x] = new List<Segment>[width];

                segments[0][x][width - 1] = new List<Segment>(0);
                segments[1][x][width - 1] = new List<Segment>(0);
                segments[2][x][width - 1] = new List<Segment>(0);
            }

            for (int y = 0; y < width - 1; y++)
            {
                segments[0][length - 1][y] = new List<Segment>(0);
                segments[1][length - 1][y] = new List<Segment>(0);
                segments[2][length - 1][y] = new List<Segment>(0);
            }

            for (int x = 0; x < length - 1; x++)
                for (int y = 0; y < width - 1; y++)
                {
                    segments[0][x][y] = GetSegmentColumnX(vertices, octants[x][y], octants[x + 1][y], ref index);
                    segments[1][x][y] = GetSegmentColumnY(vertices, octants[x][y], octants[x][y + 1], ref index);
                    segments[2][x][y] = GetSegmentColumnZ(vertices, octants[x][y], ref index);
                }

            startVertices = ("Starting Vertices: " + vertices.Count + "\n");
        }

        private static List<Segment> GetSegmentColumnX(List<Vector3> vertices, IList<Octant> a, IList<Octant> b, ref int index)
        {
            int x1 = 0, x2 = 0, s1, s2;
            List<Segment> segments = new List<Segment>(a.Count + b.Count);

            while (x1 < a.Count || x2 < b.Count)
            {
                s1 = x1 < a.Count ? a[x1].range : int.MaxValue;
                s2 = x2 < b.Count ? b[x2].range : int.MaxValue;

                if (s1 < s2)
                {
                    segments.Add(new Segment(s1, index, 1));
                    vertices.Add(new Vector3((a[x1].x + delta.x), a[x1].y, a[x1].z));
                    x1++; index++;
                }
                else if (s1 > s2)
                {
                    segments.Add(new Segment(s2, index, 2));
                    vertices.Add(new Vector3((b[x2].x - delta.x), b[x2].y, b[x2].z));
                    x2++; index++;
                }
                else
                {
                    segments.Add(new Segment(s1, -1, 3));
                    x1++; x2++;
                }
            }

            return segments;
        }

        private static List<Segment> GetSegmentColumnY(List<Vector3> vertices, IList<Octant> a, IList<Octant> b, ref int index)
        {
            int y1 = 0, y2 = 0, s1, s2;
            List<Segment> segments = new List<Segment>(a.Count + b.Count);

            while (y1 < a.Count || y2 < b.Count)
            {
                s1 = y1 < a.Count ? a[y1].range : int.MaxValue;
                s2 = y2 < b.Count ? b[y2].range : int.MaxValue;

                if (s1 < s2)
                {
                    segments.Add(new Segment(s1, index, 1));
                    vertices.Add(new Vector3(a[y1].x, (a[y1].y + delta.y), a[y1].z));
                    y1++; index++;
                }
                else if (s1 > s2)
                {
                    segments.Add(new Segment(s2, index, 2));
                    vertices.Add(new Vector3(b[y2].x, (b[y2].y - delta.y), b[y2].z));
                    y2++; index++;
                }
                else
                {
                    segments.Add(new Segment(s1, -1, 3));
                    y1++; y2++;
                }
            }

            return segments;
        }

        private static List<Segment> GetSegmentColumnZ(List<Vector3> vertices, IList<Octant> octants, ref int index)
        {
            int start;
            List<Segment> segments = new List<Segment>(octants.Count);

            for (int z = 0; z < octants.Count; z++)
            {
                start = octants[z].range;

                if (z == 0 || (octants[z - 1].range != start - 1))
                {
                    vertices.Add(new Vector3(octants[z].x, octants[z].y, (octants[z].z - delta.z)));
                    segments.Add(new Segment(start - 1, index, 2));
                    index++;
                }

                if ((z + 1 == octants.Count) || (octants[z + 1].range != start + 1))
                {
                    vertices.Add(new Vector3(octants[z].x, octants[z].y, octants[z].z + delta.z));
                    segments.Add(new Segment(start, index, 1));
                    index++;
                }
                else
                    segments.Add(new Segment(start, -1, 3));
            }

            return segments;
        }

        private class Edge
        {
            public readonly int dir, index;

            public Edge(int _dir, int _index)
            {
                dir = _dir;
                index = _index;
            }
        }

        private class Square
        {
            public static readonly Square zero = new Square(null, int.MinValue);
            public readonly bool dir;
            public readonly int z;
            public readonly Edge[] edges;

            public Square(Segment[] sides, int _z)
            {
                if (sides != null)
                {
                    int conf = sides[0].conf + (sides[2].conf * 4);
                    z = _z;

                    if (conf != 0 && conf != 15)
                    {
                        int count = 0;
                        dir = (sides[0].conf == 0 || sides[0].conf == 2);
                        edges = new Edge[(conf == 6 || conf == 9) ? 4 : 2];

                        for (int n = 0; n < 4; n++)
                        {
                            if (sides[n].index != -1)
                            {
                                edges[count] = new Edge(n, sides[n].index);
                                count++;
                            }
                        }

                        if (count != edges.Length)
                        {
                            Debug.Log("[New Square]");

                            for (int n = 0; n < 4; n++)
                                Debug.Log("[Segment] " + +n + ", Conf: " + sides[n].conf + ", Index: " + sides[n].index);

                            throw new Exception("[Edge Error] Supplied segments do not form a face. (" + (count - edges.Length) + ")");
                        }
                    }

                    if (conf == 6)
                    {
                        Edge c = edges[0];
                        edges[0] = edges[2];
                        edges[2] = c;
                    }
                }
            }

            public Edge GetOpposite(int n)
            {
                if (edges != null)
                    if (edges.Length == 4)
                    {
                        if (n == edges[0].index)
                            return edges[1];
                        else if (n == edges[1].index)
                            return edges[0];
                        else if (n == edges[2].index)
                            return edges[3];
                        else if (n == edges[3].index)
                            return edges[2];
                    }
                    else
                    {
                        if (n == edges[0].index)
                            return edges[1];
                        else if (n == edges[1].index)
                            return edges[0];
                    }

                return null;
            }
        }

        private static List<Square>[][][] GetSquares(List<Segment>[][][] segments)
        {
            int length = segments[0].Length - 1, width = segments[0][0].Length - 1;
            List<Square>[][][] squares = new List<Square>[3][][] { new List<Square>[length][], new List<Square>[length][], new List<Square>[length][] };

            for (int x = 0; x < length; x++)
            {
                squares[0][x] = new List<Square>[width];
                squares[1][x] = new List<Square>[width];
                squares[2][x] = new List<Square>[width];

                for (int y = 0; y < width; y++)
                {
                    squares[0][x][y] = GetSquareColumnX(segments[0][x][y], segments[2][x][y], segments[2][x + 1][y]);
                    squares[1][x][y] = GetSquareColumnY(segments[1][x][y], segments[2][x][y], segments[2][x][y + 1]);
                    squares[2][x][y] = GetSquareColumnZ(segments[0][x][y], segments[0][x][y + 1], segments[1][x][y], segments[1][x + 1][y]);
                }
            }

            return squares;
        }

        private static List<Square> GetSquareColumnX(List<Segment> a, List<Segment> b1, List<Segment> b2)
        {
            int start, last = int.MinValue, z1 = 0, z2 = 0;
            Segment[] sides = new Segment[4];
            List<Square> squares = new List<Square>(2 * a.Count);

            for (int z = 0; z < a.Count; z++)
            {
                start = a[z].z;

                if (last != start - 1)
                {
                    start--;

                    sides[0] = a[z];
                    sides[1] = GetSegment(b2, start, ref z1);
                    sides[2] = Segment.zero;
                    sides[3] = GetSegment(b1, start, ref z2);

                    squares.Add(new Square(sides, start));
                    start++;
                }

                sides[0] = GetNextSegment(a, z + 1, start + 1);
                sides[1] = GetSegment(b2, start, ref z1);
                sides[2] = a[z];
                sides[3] = GetSegment(b1, start, ref z2);

                squares.Add(new Square(sides, start));
                last = start;
            }

            return squares;
        }

        private static List<Square> GetSquareColumnY(List<Segment> a, List<Segment> b1, List<Segment> b2)
        {
            int start, last = int.MinValue, z1 = 0, z2 = 0;
            Segment[] sides = new Segment[4];
            List<Square> squares = new List<Square>(2 * a.Count);

            for (int z = 0; z < a.Count; z++)
            {
                start = a[z].z;

                if (last != start - 1)
                {
                    start--;

                    sides[0] = Segment.zero;
                    sides[1] = GetSegment(b2, start, ref z1);
                    sides[2] = a[z];
                    sides[3] = GetSegment(b1, start, ref z2);

                    squares.Add(new Square(sides, start));
                    start++;
                }

                sides[0] = a[z];
                sides[1] = GetSegment(b2, start, ref z1);
                sides[2] = GetNextSegment(a, z + 1, start + 1);
                sides[3] = GetSegment(b1, start, ref z2);

                squares.Add(new Square(sides, start));
                last = start;
            }

            return squares;
        }

        private static List<Square> GetSquareColumnZ(List<Segment> a1, List<Segment> a2, List<Segment> b1, List<Segment> b2)
        {
            int start = int.MinValue, z1 = 0, z2 = 0, y1 = 0, y2 = 0;
            Segment x1, x2;
            Segment[] sides = new Segment[4];
            List<Square> squares = new List<Square>(2 * a1.Count);

            while (start != int.MaxValue)
            {
                x1 = z1 < a1.Count ? a1[z1] : Segment.zero;
                x2 = z2 < a2.Count ? a2[z2] : Segment.zero;

                if (x1.z < x2.z) { z1++; start = x1.z; x2 = Segment.zero; }
                else if (x1.z > x2.z) { z2++; start = x2.z; x1 = Segment.zero; }
                else { z1++; z2++; start = x1.z; }

                if (start != int.MaxValue)
                {
                    sides[0] = x2;
                    sides[1] = GetSegment(b2, start, ref y1);
                    sides[2] = x1;
                    sides[3] = GetSegment(b1, start, ref y2);

                    squares.Add(new Square(sides, start));
                }
            }

            return squares;
        }

        private static Segment GetSegment(List<Segment> segs, int z, ref int i)
        {
            for (int n = i; n < segs.Count; n++)
            {
                if (segs[n].z == z)
                {
                    i = n;
                    return segs[n];
                }
                else if (segs[n].z > z)
                {
                    i = n;
                    return Segment.zero;
                }
            }

            return Segment.zero;
        }

        private static Segment GetNextSegment(List<Segment> segs, int i, int z)
        {
            if (i < segs.Count && segs[i].z == z)
                return segs[i];

            return Segment.zero;
        }

        private static List<int> GetCubes(List<Vector3> vertices, List<Square>[][][] squares)
        {
            int length = squares[2].Length, width = squares[2][0].Length,
                start, last, x1, x2, y1, y2;
            int[] indices = new int[12];
            Square[] faces = new Square[6];
            List<int> triangles = new List<int>(squares[2].Length * squares[2][0].Length * 27);

            for (int x = 0; x < length; x++)
                for (int y = 0; y < width; y++)
                {
                    last = int.MinValue;
                    x1 = 0; x2 = 0; y1 = 0; y2 = 0;

                    for (int z = 0; z < squares[2][x][y].Count; z++)
                    {
                        start = squares[2][x][y][z].z;

                        if (last != start - 1)
                        {
                            start--;

                            faces[0] = GetSquare(squares[0], x, y, start, ref x1);
                            faces[1] = GetSquare(squares[0], x, y + 1, start, ref x2);
                            faces[2] = GetSquare(squares[1], x, y, start, ref y1);
                            faces[3] = GetSquare(squares[1], x + 1, y, start, ref y2);
                            faces[4] = Square.zero;
                            faces[5] = squares[2][x][y][z];

                            GetSurfaces(faces, indices, vertices, triangles);
                            start++;
                        }

                        faces[0] = GetSquare(squares[0], x, y, start, ref x1);
                        faces[1] = GetSquare(squares[0], x, y + 1, start, ref x2);
                        faces[2] = GetSquare(squares[1], x, y, start, ref y1);
                        faces[3] = GetSquare(squares[1], x + 1, y, start, ref y2);
                        faces[4] = squares[2][x][y][z];
                        faces[5] = GetNextSquare(squares[2][x][y], start + 1, z + 1);

                        GetSurfaces(faces, indices, vertices, triangles);
                        last = start;
                    }
                }

            return triangles;
        }

        private static Square GetSquare(List<Square>[][] squares, int x, int y, int z, ref int i)
        {
            if (squares != null && x < squares.Length && y < squares[0].Length)
            {
                for (int n = i; n < squares[x][y].Count; n++)
                    if (squares[x][y][n].z == z)
                    {
                        i = n;
                        return squares[x][y][n];
                    }
                    else if (squares[x][y][n].z > z)
                    {
                        i = n;
                        return Square.zero;
                    }
            }

            return Square.zero;
        }

        private static Square GetNextSquare(List<Square> squares, int z, int i)
        {
            if (i < squares.Count && squares[i].z == z)
                return squares[i];

            return Square.zero;
        }

        private static readonly int[][] adjacentFaces = new int[6][]
        {
            new int[] { 5, 3, 4, 2 },
            new int[] { 5, 3, 4, 2 },
            new int[] { 4, 1, 5, 0 },
            new int[] { 4, 1, 5, 0 },
            new int[] { 1, 3, 0, 2 },
            new int[] { 1, 3, 0, 2 }
        };

        private static void GetSurfaces(Square[] f, int[] indices, List<Vector3> vertices, List<int> triangles)
        {
            int count;
            int[] used = new int[6] { -1, -1, -1, -1, -1, -1 };

            for (int face = 0; face < 6;)
                if (used[face] != -2)
                {
                    if (f[face].edges != null)
                    {
                        count = GetIndices(f, indices, used, face);
                        GetPolys(vertices, triangles, indices, count);
                    }
                    else
                        used[face] = -2;
                }
                else
                    face++;
        }

        private static int GetIndices(Square[] f, int[] indices, int[] used, int face)
        {
            int a = 0, b = 1, count = 0;
            Edge lastEdge;

            if (used[face] >= 0 && used[face] < 2) { a = 2; b = 3; }
            if (face % 2 == 1 ^ f[face].dir) { a = b; b = a - 1; }

            indices[0] = f[face].edges[a].index;
            indices[1] = f[face].edges[b].index;

            lastEdge = f[face].edges[b];
            used[face] = (used[face] == -1 && f[face].edges.Length == 4) ? b : -2;
            count++;

            while (lastEdge != null && lastEdge.index != indices[0])
            {
                count++;
                face = adjacentFaces[face][lastEdge.dir];
                lastEdge = f[face].GetOpposite(lastEdge.index);

                used[face] = (used[face] == -1 && f[face].edges.Length == 4) ? lastEdge.dir : -2;
                indices[count] = lastEdge.index;
            }

            if (lastEdge == null || lastEdge.index != indices[0] || count < 3)
                throw new Exception("[Cube Error] Could not form an enclosed surface. (" + count + ")");

            return count;
        }

        private static void GetPolys(List<Vector3> vertices, List<int> triangles, int[] indices, int count)
        {
            if (count > 4)
            {
                int pos = vertices.Count;

                for (int n = 0; n < count - 1; n++)
                {
                    triangles.Add(pos);
                    triangles.Add(indices[n]);
                    triangles.Add(indices[n + 1]);
                }

                triangles.Add(pos);
                triangles.Add(indices[count - 1]);
                triangles.Add(indices[0]);

                vertices.Add(GetAvgVertex(vertices, indices, count));
            }
            else
            {
                for (int n = 1; n < count - 1; n++)
                {
                    triangles.Add(indices[0]);
                    triangles.Add(indices[n]);
                    triangles.Add(indices[n + 1]);
                }
            }
        }

        private static Vector3 GetAvgVertex(List<Vector3> vertices, int[] indices, int count)
        {
            Vector3 vec = Vector3.zero;

            for (int a = 0; a < count; a++)
                vec += vertices[indices[a]];

            return vec / count;
        }
    }

    /*
        private static void PrintSegments(List<Segment>[][] segments, string name)
        {
            for (int x = 0; x < segments.Length; x++)
                for (int y = 0; y < segments[x].Length; y++)
                    for (int z = 0; z < segments[x][y].Count; z++)
                        Debug.Log("[Segment] " + name + ": (" + x + ", " + y + ", " + segments[x][y][z].z + "), " + segments[x][y][z].conf);
        }

        private static void PrintSquares(List<Square>[][] squares, string name)
        {
            for (int x = 0; x < squares.Length; x++)
                for (int y = 0; y < squares[x].Length; y++)
                    for (int z = 0; z < squares[x][y].Count; z++)
                        Debug.Log("[Square] " + name + ": (" + x + ", " + y + ", " + squares[x][y][z].z + "), " + squares[x][y][z].conf);
        }
    */
}
