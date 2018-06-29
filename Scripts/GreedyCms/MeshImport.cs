using System;
using System.Collections.Generic;
using UnityEngine;

namespace GreedyCms
{
    public class MeshVolume : Volume
    {
        public MeshVolume(Mesh inputMesh, Vector3 res)
        {
            System.Diagnostics.Stopwatch importTimer = new System.Diagnostics.Stopwatch();
            importTimer.Start();

            int[] inTris = inputMesh.triangles;
            Vector3[] inVerts = inputMesh.vertices;

            if (res.x <= 0f || res.y <= 0f || res.z <= 0f) res = Vector3.one;
            Scale = GetScale(inVerts, inTris, res);
            Step = new Vector3(1f / Scale.x, 1f / Scale.y, 1f / Scale.z);
            Delta = new Vector3(Scale.x / 2f, Scale.y / 2f, Scale.z / 2f);
            Dimensions = GetDimensions(inVerts);

            List<Vector3> surface = GetMeshSurface(inTris, inVerts);
            List<Octant>[][] startingOctants = GetStartingOctants(surface, Dimensions);
            base.GetFinalOctants(startingOctants);

            importTimer.Stop();
            LastImportTime = importTimer.ElapsedMilliseconds;
        }

        private static Vector3 GetScale(Vector3[] v, int[] t, Vector3 res)
        {
            Vector3 avg = Vector3.zero;

            for (int n = 0; n < t.Length; n += 3)
            {
                avg.x += GetMax(v[t[n]].x, v[t[n + 1]].x, v[t[n + 2]].x) - GetMin(v[t[n]].x, v[t[n + 1]].x, v[t[n + 2]].x);
                avg.y += GetMax(v[t[n]].y, v[t[n + 1]].y, v[t[n + 2]].y) - GetMin(v[t[n]].y, v[t[n + 1]].y, v[t[n + 2]].y);
                avg.z += GetMax(v[t[n]].z, v[t[n + 1]].z, v[t[n + 2]].z) - GetMin(v[t[n]].z, v[t[n + 1]].z, v[t[n + 2]].z);
            }

            avg /= (t.Length / 3);
            avg.x *= res.x;
            avg.y *= res.y;
            avg.z *= res.z;

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

        private Vector3Int GetDimensions(Vector3[] vertices)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
                max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (Vector3 v in vertices)
            {
                if (v.x < min.x) min.x = v.x;
                if (v.y < min.y) min.y = v.y;
                if (v.z < min.z) min.z = v.z;
                if (v.x > max.x) max.x = v.x;
                if (v.y > max.y) max.y = v.y;
                if (v.z > max.z) max.z = v.z;
            }

            for (int n = 0; n < vertices.Length; n++)
                vertices[n] -= min;

            max -= min;
            return new Vector3Int((int)(max.x * Step.x), (int)(max.y * Step.y), (int)(max.z * Step.z)) + Vector3Int.one;
        }

        private List<Octant>[][] GetStartingOctants(List<Vector3> intersections, Vector3Int dimensions)
        {
            List<Octant>[][] startingOctants = new List<Octant>[dimensions.x][]; ;

            for (int x = 0; x < startingOctants.Length; x++)
            {
                startingOctants[x] = new List<Octant>[dimensions.y];

                for (int y = 0; y < startingOctants[x].Length; y++)
                    startingOctants[x][y] = new List<Octant>(6);
            }

            foreach (Vector3 v in intersections)
                startingOctants[(int)(v.x * Step.x)][(int)(v.y * Step.y)]
                    .Add(new Octant(v.x, v.y, v.z, (int)(v.z * Step.z)));

            for (int x = 0; x < startingOctants.Length; x++)
                for (int y = 0; y < startingOctants[x].Length; y++)
                    startingOctants[x][y].Sort((a, b) => a.range.CompareTo(b.range));

            return startingOctants;
        }

        private List<Vector3> GetMeshSurface(int[] triangles, Vector3[] vertices)
        {
            Triangle t;
            List<Vector3> intersections = new List<Vector3>(vertices.Length);

            for (int n = 0; n < triangles.Length; n += 3)
            {
                t = new Triangle(triangles, vertices, n, Step);
                t.GetIntersections(intersections);
            }

            return intersections;
        }

        private class Triangle
        {
            private static readonly float res = 2f;
            private Vector3 x, y, z, step;

            public Triangle(int[] triangles, Vector3[] vertices, int n, Vector3 _step)
            {
                x = vertices[triangles[n]];
                y = vertices[triangles[n + 1]];
                z = vertices[triangles[n + 2]];
                step = _step;
            }

            public void GetIntersections(List<Vector3> intersections)
            {
                intersections.Add(x);
                intersections.Add(y);
                intersections.Add(z);

                Vector3 facingX = new Vector3(
                    Math.Abs(x.x - y.x) * step.x,
                    Math.Abs(x.x - z.x) * step.x,
                    Math.Abs(y.x - z.x) * step.x) * res,
                facingY = new Vector3(
                    Math.Abs(x.y - y.y) * step.y,
                    Math.Abs(x.y - z.y) * step.y,
                    Math.Abs(y.y - z.y) * step.y) * res,
                facingZ = new Vector3(
                    Math.Abs(x.z - y.z) * step.z,
                    Math.Abs(x.z - z.z) * step.z,
                    Math.Abs(y.z - z.z) * step.z) * res,
                startX = new Vector3(
                    1f - (int)(facingX.x) / facingX.x,
                    1f - (int)(facingX.y) / facingX.y,
                    1f - (int)(facingX.z) / facingX.z),
                startY = new Vector3(
                    1f - (int)(facingY.x) / facingY.x,
                    1f - (int)(facingY.y) / facingY.y,
                    1f - (int)(facingY.z) / facingY.z),
                startZ = new Vector3(
                    1f - (int)(facingZ.x) / facingZ.x,
                    1f - (int)(facingZ.y) / facingZ.y,
                    1f - (int)(facingZ.z) / facingZ.z);

                GetPerspectives(intersections, facingX, startX);
                GetPerspectives(intersections, facingY, startY);
                GetPerspectives(intersections, facingZ, startZ);
            }

            private void GetPerspectives(List<Vector3> intersections, Vector3 facing, Vector3 start)
            {
                GetEdgeIntersections(intersections, x, y, z, facing.x, facing.y, start.x, start.y);
                GetEdgeIntersections(intersections, y, x, z, facing.x, facing.z, start.x, start.z);
                GetEdgeIntersections(intersections, z, x, y, facing.y, facing.z, start.y, start.z);
            }

            private void GetEdgeIntersections(List<Vector3> intersections, Vector3 x, Vector3 y, Vector3 z, float end1, float end2, float start1, float start2)
            {
                float left = start1, right = start2;
                Vector3 edge1 = x, edge2 = x, middleEdge;

                for (float n = 0; (left < 1f && right < 1f); n++)
                {
                    edge1 = x + ((y - x) * left);
                    intersections.Add(edge1);
                    left = start1 + n / end1;

                    edge2 = x + ((z - x) * right);
                    intersections.Add(edge2);
                    right = start2 + n / end2;

                    middleEdge = new Vector3(
                        Math.Abs(edge1.x - edge2.x) * step.x,
                        Math.Abs(edge1.y - edge2.y) * step.y,
                        Math.Abs(edge1.z - edge2.z) * step.z) * res;

                    GetMidIntersections(intersections, edge1, edge2, middleEdge.x, 1f - (int)(middleEdge.x) / middleEdge.x);
                    GetMidIntersections(intersections, edge1, edge2, middleEdge.y, 1f - (int)(middleEdge.y) / middleEdge.y);
                    GetMidIntersections(intersections, edge1, edge2, middleEdge.z, 1f - (int)(middleEdge.z) / middleEdge.z);
                }
            }

            private static void GetMidIntersections(List<Vector3> intersections, Vector3 x, Vector3 y, float end, float start)
            {
                float t = start;
                Vector3 midpoint = new Vector3();

                for (float n = 0; t < 1f; n++)
                {
                    midpoint = x + ((y - x) * t);
                    intersections.Add(midpoint);
                    t = start + n / end;
                }
            }
        }
    }
}