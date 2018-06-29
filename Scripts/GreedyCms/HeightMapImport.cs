using System.Collections.Generic;
using UnityEngine;

namespace GreedyCms
{
    public class HeightMapVolume : Volume
    {
        public HeightMapVolume(IList<float>[][] heightMap, Vector3 _scale)
        {
            System.Diagnostics.Stopwatch importTimer = new System.Diagnostics.Stopwatch();
            importTimer.Start();

            Scale = (_scale.x > 0f && _scale.y > 0f && _scale.z > 0f) ? _scale : Vector3.one;
            Step = new Vector3(1f / Scale.x, 1f / Scale.y, 1f / Scale.z);
            Delta = new Vector3(Scale.x / 2f, Scale.y / 2f, Scale.z / 2f);
            Dimensions = new Vector3Int(heightMap.Length, heightMap[0].Length, -1);

            List<Octant>[][] startingOctants = GetStartingOctants(heightMap);
            base.GetFinalOctants(startingOctants);

            importTimer.Stop();
            LastImportTime = importTimer.ElapsedMilliseconds;
        }

        private List<Octant>[][] GetStartingOctants(IList<float>[][] heightMap)
        {
            int length = heightMap.Length, width = heightMap[0].Length;
            List<Octant>[][] startingOctants = new List<Octant>[length][];

            for (int x = 0; x < length; x++)
            {
                startingOctants[x] = new List<Octant>[width];

                for (int y = 0; y < width; y++)
                    if (heightMap[x][y] != null)
                    {
                        startingOctants[x][y] = new List<Octant>(heightMap[x][y].Count);

                        for (int z = 0; z < heightMap[x][y].Count; z++)
                            startingOctants[x][y].Add(new Octant(x * Scale.x, y * Scale.y, heightMap[x][y][z], (int)(heightMap[x][y][z] * Step.z)));
                    }
            }

            return startingOctants;
        }
    }
}