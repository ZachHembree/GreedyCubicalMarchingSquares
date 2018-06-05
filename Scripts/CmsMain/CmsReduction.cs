using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace CmsMain
{
    public static partial class Volume
    {
        /// <summary>
        /// Creates a finished and simplified mesh from the provided segments and vertices.
        /// </summary>
        private static List<int> GetReducedMesh(List<Vector3> vertices, List<Edge>[][][] startEdges, List<Segment> segments, out List<Vector3> redVertices) 
        {
            List<Edge> corners = new List<Edge>(12), edges = new List<Edge>(vertices.Count * 2);
            List<int> indices = new List<int>(12), triangles = new List<int>(vertices.Count * 9);
            redVertices = new List<Vector3>(vertices.Count / 2);

            foreach (Segment seg in segments)
            {
                if (!seg.IsUsed(0))
                    GetReducedSurface(new Side(seg, seg.start, 0), corners, edges);

                if (!seg.IsUsed(1))
                    GetReducedSurface(new Side(seg, seg.end, 1), corners, edges);
            }

            for (int x = 0; x < 3; x++)
                for (int y = 0; y < startEdges[x].Length; y++)
                    for (int z = 0; z < startEdges[x][y].Length; z++)
                        foreach (Edge e in startEdges[x][y][z])
                            if (e.used)
                            {
                                redVertices.Add(vertices[e.index]);
                                e.index = redVertices.Count - 1;
                            }

            foreach (Edge e in edges)
                if (e.used)
                    indices.Add(e.index);
                else if (e == Edge.zero)
                {
                    GetPolys(redVertices, triangles, indices);
                    indices.Clear();
                }

            return triangles;
        }

        /// <summary>
        /// Finds a pair of divergent segments to form the basis of the edges of a larger
        /// surface comprised of coplanar segments. 
        /// </summary>
        private static void GetReducedSurface(Side side, List<Edge> corners, List<Edge> edges)
        {
            bool done = false; // change this when you fix it
            Edge end = side.next.edge;
            Side left = new Side(), right = new Side();

            corners.Clear();
            corners.Add(side.next.edge);
            side.GetNextSide();

            while (side.next != DirEdge.zero && side.next.edge != end)
            {
                corners.Add(side.next.edge);

                if (!done)
                    done = side.TryGetStart(ref left, ref right);

                side.GetNextSide();
            }

            if (side.next == DirEdge.zero || side.next.edge != end || corners.Count < 3)
                throw new Exception("[Reduction Error] Could not start surface. (" + corners.Count + ")");

            if (corners.Count <= 4 && left.seg != null)
            {             
                Edge opp = left.seg.GetOppositeEdge(left.next.edge).edge;
                edges.Add(opp);
                opp.used = true;

                GetSurfaceEdges(left, right, edges);
                edges.Add(Edge.zero);
            }
            else
            {
                foreach (Edge e in corners)
                    e.used = true;

                MarkSurfaceUsed(side);
                edges.AddRange(corners);
                edges.Add(Edge.zero);
            }
        }

        /// <summary>
        /// Finds the edges of a given planar surface of arbitrary size.
        /// </summary>
        private static void GetSurfaceEdges(Side left, Side right, List<Edge> edges)
        {
            int countLeft = 1, countRight = 1;
            bool middleDir = false, canExpand = true;
            List<Edge> leftEdges = new List<Edge>(), rightEdges = new List<Edge>();
            List<Edge>[] middleEdges = null;
            List<Side> middleSides = new List<Side>(6);

            while ((!left.complete || !right.complete) && canExpand) 
            {
                if (countLeft < countRight) 
                {
                    canExpand = CanGetMiddleEdges(right, left.next.edge, countLeft, countRight);

                    if (canExpand)
                        middleEdges = GetMiddleEdges(right, left.next.edge, countLeft, countRight, middleSides);
                }
                else
                {
                    canExpand = CanGetMiddleEdges(left, right.next.edge, countRight, countLeft);

                    if (canExpand)
                        middleEdges = GetMiddleEdges(left, right.next.edge, countRight, countLeft, middleSides);
                }

                if (canExpand)
                {
                    middleDir = countLeft < countRight;

                    if (!left.complete)
                        leftEdges.Add(left.next.edge);

                    if (left.TryGetCompliment())
                        countLeft++;

                    if (!right.complete)
                        rightEdges.Add(right.next.edge);

                    if (right.TryGetCompliment())
                        countRight++;
                }
            }

            foreach (Side s in middleSides)
                MarkSurfaceUsed(s);

            foreach (Edge e in middleEdges[1])
                e.used = true;

            GetOrderedEdges(leftEdges, middleEdges[0], rightEdges, edges, middleDir);
        }

        /// <summary>
        /// Determines whether there is a viable path between the starting side and the
        /// end point.
        /// </summary>
        private static bool CanGetMiddleEdges(Side side, Edge end, int length, int width) 
        {
            int max = length - 1, count = 0, dirChanges = 0;

            side.GetNextSide();
            length--;

            while (side.next.edge != end && count <= max && dirChanges < 3)
            {
                if (count != max && side.TryGetCompliment())
                    count++;
                else
                {
                    if (count == max && (max < length + width)) max += width;

                    side.GetNextSide();
                    dirChanges++;
                }
            }

            return side.next.edge == end;
        }

        /// <summary>
        /// Compiles a list of intervening edges between the starting side and end point.
        /// </summary>
        private static List<Edge>[] GetMiddleEdges(Side side, Edge end, int length, int width, List<Side> sides)
        {
            int max = length - 1, count = 0, dirChanges = 0;
            List<Edge>[] edges = new List<Edge>[] { new List<Edge>(6), new List<Edge>() };

            edges[1].Add(side.next.edge);
            side.GetNextSide();
            length--;

            while (side.next.edge != end && count <= max && dirChanges < 3)
            {
                edges[0].Add(side.next.edge);
                sides.Add(side);

                if (count != max && side.TryGetCompliment())
                    count++;
                else
                {
                    if (count == max && (max < length + width)) max += width;
                    dirChanges++;

                    edges[1].Add(side.next.edge);
                    side.GetNextSide();
                }
            }

            sides.Add(side);
            return edges;
        }

        /// <summary>
        /// Marks the segments of a given surface as used in order to prevent reuse.
        /// </summary>
        private static void MarkSurfaceUsed(Side side)
        {
            if (!side.seg.IsUsed(side.dir))
            {
                Edge start = side.next.edge;
                int count = 0;

                while (side.TryGetNextSide() && start != side.next.edge)
                    count++;

                if (side.next == DirEdge.zero || start != side.next.edge || count < 2)
                    throw new Exception("[Cube Error] Could not form an enclosed surface. (" + count + ")");
            }
            
        }

        /// <summary>
        /// Arranges the edges of the surface so that they're in clockwise/counte-clockwise
        /// order starting from the first or leftmost edges and ending with the rightmost edges.
        /// </summary>
        private static void GetOrderedEdges(List<Edge> first, List<Edge> middle, List<Edge> last, List<Edge> edges, bool reverse)
        {
            edges.AddRange(first);

            if (reverse)
                for (int n = middle.Count - 1; n >= 0; n--)
                    edges.Add(middle[n]);
            else
                edges.AddRange(middle);

            for (int n = last.Count - 1; n >= 0; n--)
                edges.Add(last[n]);
        }
    }
}