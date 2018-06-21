using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace CmsMain
{
    public partial class Volume
    {
        /// <summary>
        /// Creates a finished and simplified mesh from the provided segments and vertices.
        /// </summary>
        private static List<int> GetReducedMesh(List<Vector3> vertices, List<Edge>[][][] startEdges, List<Segment> segments, out List<Vector3> redVertices) 
        {
            List<Edge> corners = new List<Edge>(12), edges = new List<Edge>(vertices.Count * 2);

            foreach (Segment seg in segments)
            {
                if (!seg.IsUsed(0))
                    TryGetReducedSurface(new Side(seg, seg.start, 0), corners, edges);

                if (!seg.IsUsed(1))
                    TryGetReducedSurface(new Side(seg, seg.end, 1), corners, edges);
            }

            foreach (Segment seg in segments)
            {
                if (!seg.IsUsed(0))
                    GetSurface(new Side(seg, seg.start, 0), corners, edges);

                if (!seg.IsUsed(1))
                    GetSurface(new Side(seg, seg.end, 1), corners, edges);
            }   

            return GetTriangles(edges, startEdges, vertices, out redVertices);
        }

        /// <summary>
        /// Creates a new list of vertices containing only vertices used and uses those vertices in conjunction
        /// with the supplied edges to create the final list of triangles.
        /// </summary>
        private static List<int> GetTriangles(List<Edge> edges, List<Edge>[][][] startEdges, List<Vector3> vertices, out List<Vector3> redVertices)
        {
            List<int> indices = new List<int>(12), triangles = new List<int>(vertices.Count * 9);
            redVertices = new List<Vector3>(vertices.Count / 2);

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
        /// Finds the edges necessary to polygonize a surface without extending into neighboring voxels.
        /// </summary>
        private static void GetSurface(Side side, List<Edge> corners, List<Edge> edges)
        {
            Edge end = side.Next.edge;

            corners.Clear();
            corners.Add(side.Next.edge);
            side.GetNextFace();

            while (side.Next != DirEdge.zero && side.Next.edge != end)
            {
                corners.Add(side.Next.edge);
                side.GetNextFace();
            }

            if (side.Next == DirEdge.zero || side.Next.edge != end || corners.Count < 3)
                throw new Exception("[Reduction Error] Could not start surface. (" + corners.Count + ")");

            foreach (Edge e in corners)
                e.used = true;

            MarkSurfaceUsed(side);
            edges.AddRange(corners);
            edges.Add(Edge.zero);
        }

        /// <summary>
        /// Tries to find pair of linearly independent segments to form the basis of the outermost edges of a planar
        /// surface comprised of an arbitrary number of coplanar and interconnected segments. 
        /// </summary>
        private static void TryGetReducedSurface(Side side, List<Edge> corners, List<Edge> edges)
        {
            int sidesFound = 0;
            Edge end = side.Next.edge;
            Side left = new Side(), right = new Side();

            corners.Clear();
            corners.Add(side.Next.edge);
            side.GetNextFace();

            while (side.Next != DirEdge.zero && side.Next.edge != end)
            {
                corners.Add(side.Next.edge);

                if (sidesFound < 2)
                    sidesFound = side.TryGetStart(ref left, ref right);

                side.GetNextFace();
            }

            if (side.Next == DirEdge.zero || side.Next.edge != end || corners.Count < 3)
                throw new Exception("[Reduction Error] Could not start surface. (" + corners.Count + ")");

            if (corners.Count <= 4 && sidesFound > 0) 
            {
                Edge opp = left.Seg.GetOppositeEdge(left.Next.edge).edge;
                edges.Add(opp);
                opp.used = true;

                GetSurfaceEdges(left, right, edges);
                edges.Add(Edge.zero);
            }
        }

        /// <summary>
        /// Finds the edges of a given planar surface of arbitrary size given a starting basis (the left and right sides).
        /// </summary>
        private static void GetSurfaceEdges(Side left, Side right, List<Edge> edges)
        {
            int countLeft = -1, countRight = -1;
            bool skip = true, diagonal = (left.Seg.diagonal && right.Seg.diagonal);
            Side lastLeft = left, lastRight = right;
            List<Side> middleSides = new List<Side>(6);
            List<Edge>[] middleEdges = null;
            List<Edge> leftEdges = new List<Edge>(), rightEdges = new List<Edge>();

            while (CanExpand(left, right, leftEdges.Count, rightEdges.Count)) 
            {
                skip = !skip;

                if (!left.EndFound)
                    leftEdges.Add(left.Next.edge);                    

                if (!right.EndFound)
                    rightEdges.Add(right.Next.edge);

                if (diagonal && skip)
                {
                    if (countLeft != -1)
                        GetMiddleEdges(lastLeft, lastRight, countLeft, countRight, middleSides);

                    countLeft = leftEdges.Count; countRight = rightEdges.Count;
                    lastLeft = left; lastRight = right;
                }
                else
                    middleEdges = GetMiddleEdges(left, right, leftEdges.Count, rightEdges.Count, middleSides);

                left.TryGetEnd();
                right.TryGetEnd();
            }

            if (diagonal && skip)
                middleEdges = GetMiddleEdges(lastLeft, lastRight, countLeft, countRight, middleSides);

            for (int n = middleSides.Count - 1; n >= 0; n--)
                MarkSurfaceUsed(middleSides[n]);

            for (int n = middleEdges[1].Count - 1; n >= 0; n--)
                middleEdges[1][n].used = true;

            GetOrderedEdges(leftEdges, middleEdges[0], rightEdges, edges);
        }

        /// <summary>
        /// Determines whether there is a viable path between the starting side and the
        /// end point.
        /// </summary>
        private static bool CanExpand(Side start, Side end, int length, int width) 
        {
            if (!start.EndFound || !end.EndFound)
            {
                if (!start.EndFound)
                    length++;
                if (!end.EndFound)
                    width++;

                if (length < width)
                    SwapValues(ref start, ref end);
                else
                    SwapValues(ref length, ref width);

                int max, count = 0, dirChanges = 0, maxChanges;

                start.GetNextFace();
                length = (start.Seg.diagonal == end.Seg.diagonal && length > 2) ? 2 : length - 1;
                max = length;
                maxChanges = end.Seg.comp == start.Seg.comp ? 2 : 1;

                while (start.Next.edge != end.Next.edge && count <= max && dirChanges < maxChanges)
                {
                    if (count != max && start.TryGetEnd())
                        count++;
                    else
                    {
                        if (count == max && (max < length + width))
                            max += width;

                        start.GetNextFace();
                        dirChanges++;
                    }
                }

                return start.Next.edge == end.Next.edge;
            }
            else
                return false;
        }

        /// <summary>
        /// Compiles a list of intervening edges between the starting side and end side.
        /// </summary>
        private static List<Edge>[] GetMiddleEdges(Side start, Side end, int length, int width, List<Side> sides)
        {
            if (length < width)
                SwapValues(ref start, ref end);
            else
                SwapValues(ref length, ref width);

            int max, count = 0;
            List<Edge>[] edges = new List<Edge>[] { new List<Edge>(6), new List<Edge>() };

            edges[1].Add(start.Next.edge);
            start.GetNextFace();
            length = (start.Seg.diagonal == end.Seg.diagonal && length > 2) ? 2 : length - 1;
            max = length;
            sides.Add(start);

            while (start.Next.edge != end.Next.edge && count <= max)
            {
                edges[0].Add(start.Next.edge);

                if (count != max && start.TryGetEnd())
                {
                    sides.Add(start);
                    count++;
                }
                else
                {
                    if (count == max && (max < length + width)) max += width;

                    edges[1].Add(start.Next.edge);
                    start.GetNextFace();
                }
            }

            edges[1].Add(start.Next.edge);
            return edges;
        }

        /// <summary>
        /// Transposes the references of two supplied variables with one another.
        /// </summary>
        private static void SwapValues<T>(ref T a, ref T b)
        {
            T c = a;
            a = b;
            b = c;
        }

        /// <summary>
        /// Marks the segments of a given surface as used in order to prevent reuse.
        /// </summary>
        private static void MarkSurfaceUsed(Side side)
        {
            if (!side.Seg.IsUsed(side.Dir))
            {
                Edge start = side.Next.edge;
                int count = 0;

                while (side.TryGetNextFace() && start != side.Next.edge)
                    count++;

                if (side.Next == DirEdge.zero || start != side.Next.edge || count < 2)
                    throw new Exception("[Cube Error] Could not form an enclosed surface. (" + count + ")");
            }
            
        }

        /// <summary>
        /// Arranges the edges of the surface so that they're in clockwise/counter-clockwise
        /// order starting from the first or leftmost edges and ending with the rightmost edges.
        /// </summary>
        private static void GetOrderedEdges(List<Edge> left, List<Edge> middle, List<Edge> right, List<Edge> edges)
        {
            edges.AddRange(left);

            if (left.Count < right.Count)
                for (int n = middle.Count - 1; n >= 0; n--)
                    edges.Add(middle[n]);
            else
                edges.AddRange(middle);

            for (int n = right.Count - 1; n >= 0; n--)
                edges.Add(right[n]);
        }
    }
}