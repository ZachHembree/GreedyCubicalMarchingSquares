using System;  
using System.Collections.Generic;
using UnityEngine;

namespace HelmetVolumes
{
    public static class CMS
    {
        private static int div;
        private static float zRes, xyRes = 0.5f;

        public static Mesh ContourMesh(float[][][] heightMap, int _div = 2)
        {
            div = _div;
            zRes = 1.0f / _div;

            List<Vector3> vertices;
            List<Segment>[][][] segments;
            GetSegments(GetOctants(heightMap), out vertices, out segments);

            List<Square>[][][] squares = new List<Square>[3][][]
            {
                GetSquaresX(segments),
                GetSquaresY(segments),
                GetSquaresZ(segments)
            };

            List<int> triangles;
            GetCubes(vertices, squares, out triangles);

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            return mesh;
        }

        private class Octant
        {
            public readonly int range;
            public readonly float height;

            public Octant(float _height)
            {
                range = (int)(_height * div);
                height = _height;
            }
        }

        private static Octant[][][] GetOctants(float[][][] heightMap)
        {
            int length = heightMap.Length, width = heightMap[0].Length;
            Octant[][][] octants = new Octant[length + 2][][];

            for (int x = 0; x < octants.Length; x++)
                octants[x] = new Octant[width + 2][];

            for (int x = 0; x < octants.Length; x++)
            {
                octants[x][0] = new Octant[0];
                octants[x][width + 1] = new Octant[0];
            }

            for (int y = 1; y < octants[0].Length - 1; y++)
            {
                octants[0][y] = new Octant[0];
                octants[length + 1][y] = new Octant[0];
            }

            for (int x = 0; x < length; x++)
                for (int y = 0; y < width; y++)
                    if (heightMap[x][y] != null)
                    {
                        octants[x + 1][y + 1] = new Octant[heightMap[x][y].Length];

                        for (int z = 0; z < heightMap[x][y].Length; z++)
                            octants[x + 1][y + 1][z] = new Octant(heightMap[x][y][z]);
                    }
                    else
                        octants[x + 1][y + 1] = new Octant[0];

            return octants;
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

        private static void GetSegments(Octant[][][] octants, out List<Vector3> vertices, out List<Segment>[][][] segments)
        {
            int length = octants.Length, width = octants[1].Length, height,
                index = 0, start, s1, s2, x1, x2, y1, y2;
            float xyDelta = xyRes / 2,
                  xPos, yPos;
            vertices = new List<Vector3>();
            segments = new List<Segment>[3][][]
            { new List<Segment>[length - 1][], new List<Segment>[length - 1][], new List<Segment>[length - 1][] };

            for (int x = 0; x < length - 1; x++)
            {
                xPos = (x - 1) * xyRes;
                segments[0][x] = new List<Segment>[width - 1];
                segments[1][x] = new List<Segment>[width - 1];
                segments[2][x] = new List<Segment>[width - 1];

                for (int y = 0; y < width - 1; y++)
                {
                    height = octants[x][y] != null ? 2 * octants[x][y].Length : 0;
                    segments[0][x][y] = new List<Segment>(height);
                    segments[1][x][y] = new List<Segment>(height);
                    segments[2][x][y] = new List<Segment>(height);

                    yPos = (y - 1) * xyRes;
                    x1 = 0; x2 = 0; y1 = 0; y2 = 0;

                    while (x1 < octants[x][y].Length || x2 < octants[x + 1][y].Length)
                    {
                        s1 = x1 < octants[x][y].Length ? octants[x][y][x1].range : int.MaxValue;
                        s2 = x2 < octants[x + 1][y].Length ? octants[x + 1][y][x2].range : int.MaxValue;

                        if (s1 < s2)
                        {
                            segments[0][x][y].Add(new Segment(s1, index, 1));
                            vertices.Add(new Vector3((xPos + xyDelta), yPos, s1 * zRes));
                            x1++; index++;
                        }
                        else if (s1 > s2)
                        {
                            segments[0][x][y].Add(new Segment(s2, index, 2));
                            vertices.Add(new Vector3((xPos + xyDelta), yPos, s2 * zRes));
                            x2++; index++;
                        }
                        else
                        {
                            segments[0][x][y].Add(new Segment(s1, -1, 3));
                            x1++; x2++;
                        }
                    }

                    while (y1 < octants[x][y].Length || y2 < octants[x][y + 1].Length)
                    {
                        s1 = y1 < octants[x][y].Length ? octants[x][y][y1].range : int.MaxValue;
                        s2 = y2 < octants[x][y + 1].Length ? octants[x][y + 1][y2].range : int.MaxValue;

                        if (s1 < s2)
                        {
                            segments[1][x][y].Add(new Segment(s1, index, 1));
                            vertices.Add(new Vector3(xPos, (yPos + xyDelta), s1 * zRes));
                            y1++; index++;
                        }
                        else if (s1 > s2)
                        {
                            segments[1][x][y].Add(new Segment(s2, index, 2));
                            vertices.Add(new Vector3(xPos, (yPos + xyDelta), s2 * zRes));
                            y2++; index++;
                        }
                        else
                        {
                            segments[1][x][y].Add(new Segment(s1, -1, 3));
                            y1++; y2++;
                        }
                    }

                    for (int z = 0; z < octants[x][y].Length; z++)
                    {
                        start = octants[x][y][z].range;

                        if (z == 0 || (octants[x][y][z - 1].range != start - 1))
                        {
                            vertices.Add(new Vector3(xPos, yPos, octants[x][y][z].height - zRes));
                            segments[2][x][y].Add(new Segment(start - 1, index, 2));
                            index++;
                        }

                        if ((z + 1 == octants[x][y].Length) || (octants[x][y][z + 1].range != start + 1))
                        {
                            vertices.Add(new Vector3(xPos, yPos, octants[x][y][z].height));
                            segments[2][x][y].Add(new Segment(start, index, 1));
                            index++;
                        }
                        else
                            segments[2][x][y].Add(new Segment(start, -1, 3));
                    }
                }
            }

            Debug.Log("Starting Vertices: " + vertices.Count);
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
                            if (sides[n].index != -1)
                            {
                                edges[count] = new Edge(n, sides[n].index);
                                count++;
                            }

                        if (count != edges.Length)
                            throw new Exception("[Edge Error] Supplied segments do not form a face. (" + (count - edges.Length) + ")");
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

        // X
        private static List<Square>[][] GetSquaresX(List<Segment>[][][] segments)
        {
            int length = segments[0].Length, width = segments[0][0].Length,
                start, last, z1, z2;
            Segment[] sides = new Segment[4];
            List<Square>[][] squares = new List<Square>[length][];

            for (int x = 0; x < segments[0].Length; x++)
            {
                squares[x] = new List<Square>[width];

                for (int y = 0; y < segments[0][x].Length; y++)
                {
                    last = int.MinValue;
                    z1 = 0; z2 = 0;
                    squares[x][y] = new List<Square>(2 * segments[0][x][y].Count);

                    for (int z = 0; z < segments[0][x][y].Count; z++)
                    {
                        start = segments[0][x][y][z].z;

                        if (last != start - 1)
                        {
                            start--;

                            sides[0] = segments[0][x][y][z];
                            sides[1] = GetSegment(segments[2], x + 1, y, start, ref z1);
                            sides[2] = Segment.zero;
                            sides[3] = GetSegment(segments[2], x, y, start, ref z2);

                            squares[x][y].Add(new Square(sides, start));
                            start++;
                        }

                        sides[0] = GetNextSegment(segments[0][x][y], z + 1, start + 1);
                        sides[1] = GetSegment(segments[2], x + 1, y, start, ref z1);
                        sides[2] = segments[0][x][y][z];
                        sides[3] = GetSegment(segments[2], x, y, start, ref z2);

                        squares[x][y].Add(new Square(sides, start));
                        last = start;
                    }
                }
            }

            return squares;
        }

        // Y
        private static List<Square>[][] GetSquaresY(List<Segment>[][][] segments)
        {
            int length = segments[1].Length, width = segments[1][0].Length,
                start, last, z1, z2;
            Segment[] sides = new Segment[4];
            List<Square>[][] squares = new List<Square>[length][];

            for (int x = 0; x < segments[1].Length; x++)
            {
                squares[x] = new List<Square>[width];

                for (int y = 0; y < segments[1][x].Length; y++)
                {
                    last = int.MinValue;
                    z1 = 0; z2 = 0;
                    squares[x][y] = new List<Square>(2 * segments[1][x][y].Count);

                    for (int z = 0; z < segments[1][x][y].Count; z++)
                    {
                        start = segments[1][x][y][z].z;

                        if (last != start - 1)
                        {
                            start--;

                            sides[0] = Segment.zero;
                            sides[1] = GetSegment(segments[2], x, y + 1, start, ref z1);
                            sides[2] = segments[1][x][y][z];
                            sides[3] = GetSegment(segments[2], x, y, start, ref z2);

                            squares[x][y].Add(new Square(sides, start));
                            start++;
                        }

                        sides[0] = segments[1][x][y][z];
                        sides[1] = GetSegment(segments[2], x, y + 1, start, ref z1);
                        sides[2] = GetNextSegment(segments[1][x][y], z + 1, start + 1);
                        sides[3] = GetSegment(segments[2], x, y, start, ref z2);

                        squares[x][y].Add(new Square(sides, start));
                        last = start;
                    }
                }
            }

            return squares;
        }

        // Z
        private static List<Square>[][] GetSquaresZ(List<Segment>[][][] segments)
        {
            int length = segments[0].Length, width = segments[0][0].Length,
                start, z1, z2, y1, y2;
            Segment x1, x2;
            Segment[] sides = new Segment[4];
            List<Square>[][] squares = new List<Square>[length][];

            for (int x = 0; x < segments[0].Length; x++)
            {
                squares[x] = new List<Square>[width];

                for (int y = 0; y < segments[0][x].Length; y++)
                {
                    start = int.MinValue; z1 = 0; z2 = 0; y1 = 0; y2 = 0;
                    squares[x][y] = new List<Square>(2 * segments[0][x][y].Count);

                    while (start != int.MaxValue)
                    {
                        x1 = z1 < segments[0][x][y].Count ? segments[0][x][y][z1] : Segment.zero;
                        x2 = (y + 1 < segments[0][x].Length && z2 < segments[0][x][y + 1].Count) ? segments[0][x][y + 1][z2] : Segment.zero;

                        if (x1.z < x2.z) { z1++; start = x1.z; x2 = Segment.zero; }
                        else if (x1.z > x2.z) { z2++; start = x2.z; x1 = Segment.zero; }
                        else { z1++; z2++; start = x1.z; }

                        if (start != int.MaxValue)
                        {
                            sides[0] = x2;
                            sides[1] = GetSegment(segments[1], x + 1, y, start, ref y1);
                            sides[2] = x1;
                            sides[3] = GetSegment(segments[1], x, y, start, ref y2);

                            squares[x][y].Add(new Square(sides, start));
                        }
                    }
                }
            }

            return squares;
        }

        private static Segment GetSegment(List<Segment>[][] segs, int x, int y, int z, ref int i)
        {
            if (x >= 0 && x < segs.Length && y >= 0 && y < segs[0].Length)
            {
                for (int n = i; n < segs[x][y].Count; n++)
                {
                    if (segs[x][y][n].z == z)
                    {
                        i = n;
                        return segs[x][y][n];
                    }
                    else if (segs[x][y][n].z > z)
                    {
                        i = n;
                        return Segment.zero;
                    }
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

        private static void GetCubes(List<Vector3> vertices, List<Square>[][][] squares, out List<int> triangles)
        {
            int start, last, x1, x2, y1, y2;
            int[] indices = new int[12];
            Square[] faces = new Square[6];
            triangles = new List<int>(squares[2].Length * squares[2][0].Length * 12);

            for (int x = 0; x < squares[2].Length; x++)
            {
                for (int y = 0; y < squares[2][x].Length; y++)
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
            }
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
