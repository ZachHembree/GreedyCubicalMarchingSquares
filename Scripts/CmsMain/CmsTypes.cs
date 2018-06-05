using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace CmsMain
{
    public static partial class Volume
    {
        /// <summary>
        /// Defines the intersection between a ray and surface within a given volume.
        /// </summary>
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

            public Octant(int _x, int _y, float _z, int _range) // just pass in the scale
            {
                x = _x * scale.x;
                y = _y * scale.y;
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

                    return hash;
                }
            }
        }

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

        private class Segment
        {
            public static readonly Segment zero = new Segment(null, null);
            public readonly DirEdge start, end;
            public readonly int diagonal;
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
                    diagonal = (start.dir + end.dir) % 2;
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

            public int GetOppositeDir(Edge edge) // get rid of this
            {
                if (start.edge == edge)
                    return end.dir;
                else if (end.edge == edge)
                    return start.dir;
                else
                    return 0;
            }

            public bool TryGetOppositeEdge(int sideUsed, ref DirEdge next)
            {
                if (used != -2 && used != sideUsed)
                {
                    if (start.edge == next.edge)
                        next = end;
                    else if (end.edge == next.edge)
                        next = start;
                    else
                    {
                        next = DirEdge.zero;
                        return false;
                    }

                    used = used == -1 ? sideUsed : -2;
                    return true;
                }
                else
                {
                    next = DirEdge.zero;
                    return false;
                }
            }

            public Segment GetSegByDir(int edgeDir)
            {
                if (start.dir == edgeDir && start.edge.segments[planeDirs[edgeDir]] != null)
                    return start.edge.segments[planeDirs[edgeDir]];
                else if (end.dir == edgeDir && end.edge.segments[planeDirs[edgeDir]] != null)
                    return end.edge.segments[planeDirs[edgeDir]];
                else
                    return zero;
            }
        }

        private struct Side
        {
            public Segment seg;
            public DirEdge next;
            public int dir;
            public bool complete;

            public Side(Segment _seg, DirEdge _next, int _dir)
            {
                seg = _seg;
                next = _next;
                dir = _dir;
                complete = false;
            }

            public bool TryGetStart(ref Side left, ref Side right)
            {
                DirEdge opp = seg.GetOppositeEdge(next.edge);

                if (next.dir < 2)
                {
                    left.seg = seg;
                    left.next = next;
                    left.dir = dir;
                    left.complete = false;

                    right.seg = opp.edge.segments[edgeDirs[dir][opp.dir]];
                    right.next = right.seg.GetOppositeEdge(opp.edge); 
                    right.dir = cubeDirs[opp.dir];
                    right.complete = false;

                    return right.next.dir < 2;
                }
                else if (opp.dir < 2)
                {
                    left.seg = next.edge.segments[edgeDirs[dir][next.dir]];
                    left.next = left.seg.GetOppositeEdge(next.edge);
                    left.dir = cubeDirs[next.dir];
                    left.complete = false;

                    right.seg = seg;
                    right.next = opp;
                    right.dir = dir;
                    right.complete = false;

                    return left.next.dir < 2;
                }

                return false;
            }

            public bool TryGetHexStart(ref Side left, ref Side right)
            {
                if (seg.start.dir > 1 && seg.end.dir > 1)
                {
                    DirEdge opp = seg.GetOppositeEdge(next.edge);

                    left.seg = next.edge.segments[edgeDirs[dir][next.dir]];
                    left.next = left.seg.GetOppositeEdge(next.edge);
                    left.dir = cubeDirs[next.dir];
                    left.complete = false;

                    right.seg = opp.edge.segments[edgeDirs[dir][opp.dir]];
                    right.next = right.seg.GetOppositeEdge(opp.edge);
                    right.dir = cubeDirs[opp.dir];
                    right.complete = false;

                    return left.next.dir < 2 && right.next.dir < 2;
                }

                return false;
            }

            public void GetNextSide()
            {
                seg = next.edge.segments[edgeDirs[dir][next.dir]];
                dir = cubeDirs[next.dir];
                next = seg.GetOppositeEdge(next.edge);
                complete = false;
            }

            public void GetNextSide(bool diagonal)
            {
                Segment comp = next.edge.segments[edgeDirs[dir][next.dir]];

                if (diagonal || seg.diagonal == comp.diagonal)
                {
                    seg = comp;
                    dir = cubeDirs[next.dir];
                    next = seg.GetOppositeEdge(next.edge);
                    complete = false;
                }
                else
                    next = DirEdge.zero;
            }

            public bool TryGetNextSide()
            {
                seg = next.edge.segments[edgeDirs[dir][next.dir]];
                dir = cubeDirs[next.dir];

                return seg.TryGetOppositeEdge(dir, ref next);
            }

            public bool TryGetCompliment()
            {
                if (!complete)
                {
                    Segment comp = next.edge.segments[planeDirs[next.dir]];
                    //Debug.Log("Diagonal: (" + (comp.diagonal + ", " + seg.diagonal) + "), Comp Used: " + comp.IsUsed(dir));

                    if (comp.diagonal == seg.diagonal && !comp.IsUsed(dir))
                    {
                        if (seg.start.edge == comp.end.edge && seg.end.edge.conf == comp.start.edge.conf)
                        {
                            seg = comp;
                            next = comp.start;

                            return true;
                        }
                        else if (seg.end.edge == comp.start.edge && seg.start.edge.conf == comp.end.edge.conf)
                        {
                            seg = comp;
                            next = comp.end;

                            return true;
                        }
                    }
                }

                complete = true;
                return false;
            }
        }
    }
}