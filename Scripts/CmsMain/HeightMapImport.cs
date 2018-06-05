using System.Collections.Generic;
using System;
using UnityEngine;

namespace CmsMain
{
    public static partial class Volume
    {
        public static Mesh ContourMesh(IList<float>[][] heightMap, Vector3 _scale, bool reduce = false, bool expand = false)
        {
            if (_scale.x <= 0 || _scale.y <= 0 || _scale.z <= 0)
                scale = Vector3.one;
            else
                scale = _scale;

            step = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            delta = new Vector3(scale.x / 2f, scale.y / 2f, scale.z / 2f);

            List<Vector3> vertices, redVertices;
            List<Octant>[][] octants = GetOctants(heightMap);
            List<Edge>[][][] edges = GetEdges(octants, out vertices); // make these 2d/3d at some point; fuck CLR
            List<Segment> segments = GetSegments(edges);
            List<int> triangles;

            if (reduce)
            {
                triangles = GetReducedMesh(vertices, edges, segments, out redVertices);
                vertices = redVertices;
            }
            else
                triangles = GetMesh(vertices, segments);

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            return mesh;
        }

        private static List<Octant>[][] GetOctants(IList<float>[][] heightMap)
        {
            int length = heightMap.Length, width = heightMap[0].Length, next, n;
            Octant last;
            List<Octant>[][] octants = new List<Octant>[length + 2][];

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
                    if (heightMap[x][y] != null)
                    {
                        last = Octant.zero; n = 1;
                        octants[x + 1][y + 1] = new List<Octant>(heightMap[x][y].Count);

                        for (int z = 0; z < heightMap[x][y].Count; z++)
                        {
                            next = (int)(heightMap[x][y][z] * step.z);

                            if (next > last.range)
                            {
                                if (n > 1)
                                {
                                    last.z /= n;
                                    n = 1;
                                }

                                last = new Octant(x, y, heightMap[x][y][z], next);
                                octants[x + 1][y + 1].Add(last);
                            }
                            else
                            {
                                last.z += heightMap[x][y][z];
                                n++;
                            }
                        }
                    }
                    else
                        octants[x + 1][y + 1] = new List<Octant>(0);

            return octants;
        }
    }
}