using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace CmsMain
{
    /// <summary>
    /// Stores vertex and triangle information necessary to create a new mesh.
    /// </summary>
    public class MeshData
    {
        private readonly Vector3[] vertices;
        private readonly int[] triangles;

        public MeshData(IList<Vector3> _vertices, IList<int> _triangles)
        {
            vertices = new Vector3[_vertices.Count];
            triangles = new int[_triangles.Count];
            _vertices.CopyTo(vertices, 0);
            _triangles.CopyTo(triangles, 0);
        }

        /// <summary>
        /// Instantiates a new mesh using the vertex and triangle data used to instantiate the MeshData
        /// object. This method can only be called from the main thread.
        /// </summary>
        public Mesh GetMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;

            return mesh;
        }
    }

    /// <summary>
    /// Defines the intersection between a surface and a volume of a given size.
    /// </summary>
    public class Octant
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

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Octant b = (Octant)obj;

            return (b.range == range && x == b.x && y == b.y && z == b.z);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + range.GetHashCode();
                hash = hash * 23 + x.GetHashCode();
                hash = hash * 23 + y.GetHashCode();
                hash = hash * 23 + z.GetHashCode();

                return hash;
            }
        }

        public static Octant operator +(Octant a, Octant b)
        {
            if (a.range == b.range)
                return new Octant(a.x + b.x, a.y + b.y, a.z + b.z, a.range);
            else
                throw new Exception("Cannot perform addition on Octants with different ranges.");
        }

        public static Octant operator -(Octant a, Octant b)
        {
            if (a.range == b.range)
                return new Octant(a.x - b.x, a.y - b.y, a.z - b.z, a.range);
            else
                throw new Exception("Cannot perform subtraction on Octants with different ranges.");
        }

        public static Octant operator *(Octant a, float b) =>
            new Octant(a.x * b, a.y * b, a.z * b, a.range);

        public static Octant operator *(Octant a, int b) =>
            new Octant(a.x * b, a.y * b, a.z * b, a.range);

        public static Octant operator /(Octant a, float b) =>
            new Octant(a.x / b, a.y / b, a.z / b, a.range);

        public static Octant operator /(Octant a, int b) =>
            new Octant(a.x / b, a.y / b, a.z / b, a.range);
    }

    public partial class Volume
    {
        /// <summary>
        /// Defines the direction of the edge of intersecting Segments and stores the
        /// location of those intersecting Segments.
        /// </summary>
        private class Edge
        {
            public static readonly Edge zero = new Edge(int.MaxValue, -1, 0);
            public bool used;
            public int index;
            public readonly int z, conf;
            public readonly Segment[] segments;

            public Edge(int _z, int _index, int _conf)
            {
                used = false;
                z = _z;
                index = _index;
                conf = _conf;
                segments = new Segment[4];
            }
        }

        /// <summary>
        /// Defines the position of a given Edge in relation to other edges on a given face.
        /// </summary>
        private class DirEdge
        {
            public static readonly DirEdge zero = new DirEdge(int.MaxValue, null);
            public readonly Edge edge;
            public readonly int dir;

            public DirEdge(int _dir, Edge _seg)
            {
                dir = _dir;
                edge = _seg;
            }
        }

        /// <summary>
        /// Defines a line segment on a face.
        /// </summary>
        private class Segment
        {
            public static readonly Segment zero = new Segment(null, null);
            public readonly DirEdge start, end;
            public readonly bool diagonal;
            public readonly int comp;
            public int used;

            public Segment(DirEdge _start, DirEdge _end)
            {
                start = _start;
                end = _end;
                used = -1;

                if (start != null && end != null)
                {
                    start.edge.segments[start.dir] = this;
                    end.edge.segments[end.dir] = this;
                    comp = start.edge.conf + end.edge.conf;
                    diagonal = (start.dir + end.dir) % 2 == 1;
                }
            }

            public bool IsUsed(int dir) =>
                (used == -2 || used == dir);

            public DirEdge GetOppositeEdge(Edge edge)
            {
                if (start.edge == edge)
                    return end;
                else if (end.edge == edge)
                    return start;
                else
                    return DirEdge.zero;
            }

            public DirEdge TryGetOppositeEdge(int sideUsed, Edge next)
            {
                if (used != -2 && used != sideUsed)
                    if (start.edge == next)
                    {
                        used = used == -1 ? sideUsed : -2;
                        return end;
                    }
                    else if (end.edge == next)
                    {
                        used = used == -1 ? sideUsed : -2;
                        return start;
                    }

                return DirEdge.zero;
            }
        }

        private struct Side
        {
            public Segment Seg { get; private set; }
            public DirEdge Next { get; private set; }
            public int Dir { get; private set; }
            public bool EndFound { get; private set; }

            public Side(Segment _seg, DirEdge _next, int _dir)
            {
                Seg = _seg;
                Next = _next;
                Dir = _dir;
                EndFound = false;
            }

            public void ReverseDirection()
            {
                Dir = Dir == 0 ? 1 : 0;
                EndFound = false;

                if (Dir == 0)
                    Next = Seg.start;
                else
                    Next = Seg.end;
            }

            public int TryGetStart(ref Side left, ref Side right)
            {
                Side center = this;
                DirEdge opp = Seg.GetOppositeEdge(Next.edge);

                if (center.CanGetEnd())
                {
                    left = center;

                    right.Seg = opp.edge.segments[edgeDirs[Dir][opp.dir]];
                    right.Next = right.Seg.GetOppositeEdge(opp.edge); 
                    right.Dir = cubeDirs[opp.dir];
                    right.EndFound = false;

                    return right.CanGetEnd() ? 2 : 1;
                }

                center.Next = opp;

                if (center.CanGetEnd())
                {
                    left.Seg = Next.edge.segments[edgeDirs[Dir][Next.dir]];
                    left.Next = left.Seg.GetOppositeEdge(Next.edge);
                    left.Dir = cubeDirs[Next.dir];
                    left.EndFound = false;

                    right = center;

                    return left.CanGetEnd() ? 2 : 1;
                }

                return 0;
            }

            public void GetNextFace()
            {
                Seg = Next.edge.segments[edgeDirs[Dir][Next.dir]];
                Dir = cubeDirs[Next.dir];
                Next = Seg.GetOppositeEdge(Next.edge);
                EndFound = false;
            }

            public bool TryGetNextFace()
            {
                Seg = Next.edge.segments[edgeDirs[Dir][Next.dir]];
                Dir = cubeDirs[Next.dir];
                Next = Seg.TryGetOppositeEdge(Dir, Next.edge);

                return Next != DirEdge.zero;
            }

            public bool CanGetEnd()
            {
                Segment end = Next.edge.segments[planeDirs[Next.dir]];
                return !end.IsUsed(Dir) && (Seg.diagonal == end.diagonal) && (Seg.comp == end.comp);
            }

            public void GetEnd()
            {
                if (!EndFound)
                {
                    Segment end = Next.edge.segments[planeDirs[Next.dir]];

                    if (!end.IsUsed(Dir))
                    {
                        Seg = end;
                        Next = end.GetOppositeEdge(Next.edge);
                        EndFound = false;
                    }
                }

                EndFound = true;
            }

            public bool TryGetEnd()
            {
                if (!EndFound)
                {
                    Segment end = Next.edge.segments[planeDirs[Next.dir]];

                    if (!end.IsUsed(Dir) && (Seg.diagonal == end.diagonal) && (Seg.comp == end.comp))
                    {
                        Seg = end;
                        Next = end.GetOppositeEdge(Next.edge);
                        return true;
                    }
                }

                EndFound = true;
                return false;
            }            
        }
    }
}