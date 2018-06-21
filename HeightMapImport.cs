using System.Collections.Generic;
using UnityEngine;

namespace HelmetVolumes
{
    static partial class CMS
    {
        public static Mesh ContourMesh(IList<float>[][] heightMap, Vector3 _scale)
        {
            scale = _scale;
            step = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            delta = new Vector3(scale.x / 2f, scale.y / 2f, scale.z / 2f);

            List<Vector3> vertices;
            List<Segment>[][][] segments;
            GetSegments(GetOctants(heightMap), out vertices, out segments);

            List<Square>[][][] squares = GetSquares(segments);
            List<int> triangles = GetCubes(vertices, squares);

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();

            return mesh;
        }

        private static Octant[][][] GetOctants(IList<float>[][] heightMap)
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
                        octants[x + 1][y + 1] = new Octant[heightMap[x][y].Count];

                        for (int z = 0; z < heightMap[x][y].Count; z++)
                            octants[x + 1][y + 1][z] = new Octant(x, y, heightMap[x][y][z]);
                    }
                    else
                        octants[x + 1][y + 1] = new Octant[0];

            return octants;
        }
    }
}