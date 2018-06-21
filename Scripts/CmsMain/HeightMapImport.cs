using System.Collections.Generic;
using System;
using UnityEngine;

namespace CmsMain
{
    public partial class Volume
    {
        public MeshData VoxelizeHeightmap(IList<float>[][] heightMap, Vector3 _scale, bool reduce = false)
        {
            System.Diagnostics.Stopwatch importTimer = new System.Diagnostics.Stopwatch();
            importTimer.Start();

            if (_scale.x <= 0 || _scale.y <= 0 || _scale.z <= 0)
                scale = Vector3.one;
            else
                scale = _scale;

            step = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            delta = new Vector3(scale.x / 2f, scale.y / 2f, scale.z / 2f);
            dimensions = new Vector3Int(heightMap.Length + 2, heightMap[0].Length + 2, -1);
            List<Octant>[][] octants = GetOctants(heightMap);

            importTimer.Stop();
            lastImportTime = importTimer.ElapsedMilliseconds;

            return ContourMesh(octants, reduce);
        }

        public List<Octant>[][] GetHeightmapOctants(IList<float>[][] heightMap, Vector3 _scale)
        {
            System.Diagnostics.Stopwatch importTimer = new System.Diagnostics.Stopwatch();
            importTimer.Start();

            if (_scale.x <= 0 || _scale.y <= 0 || _scale.z <= 0)
                scale = Vector3.one;
            else
                scale = _scale;

            step = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            delta = new Vector3(scale.x / 2f, scale.y / 2f, scale.z / 2f);
            dimensions = new Vector3Int(heightMap.Length + 2, heightMap[0].Length + 2, -1);
            List<Octant>[][] octants = GetOctants(heightMap);

            importTimer.Stop();
            lastImportTime = importTimer.ElapsedMilliseconds;

            return octants;
        }

        private List<Octant>[][] GetOctants(IList<float>[][] heightMap)
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

                                last = new Octant(x * scale.x, y * scale.y, heightMap[x][y][z], next);
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