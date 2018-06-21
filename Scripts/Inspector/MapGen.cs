using UnityEngine;
using UnityEditor;
using CmsNext;
using System.Diagnostics;
using System.Collections.Generic;

public class MapGen : MonoBehaviour
{
    public Vector3 octantSize;
    public Vector3Int dimensions;
    public string seed;
    public bool simplifyMesh, useRandom, cohesion;

    private MeshFilter meshFilter;
    private IList<float>[][] map;
    public string display { get; private set; }

    public void Start()
    {
        simplifyMesh = false;
    }

    public void GetMap()
    {
        MapGenerator mapGenerator = new MapGenerator(dimensions.x, dimensions.y, dimensions.z, useRandom, cohesion, seed);
        map = mapGenerator.GenerateMap();
    }

    public void ToggleReduction() =>
        simplifyMesh = !simplifyMesh;

    public void GetMesh()
    {
        if (map != null)
        {
            Stopwatch timer = new Stopwatch();
            meshFilter = GetComponent<MeshFilter>();

            timer.Start();
            Volume volume = new Volume();
            MeshData meshData = volume.VoxelizeHeightmap(map, octantSize, simplifyMesh);
            meshFilter.sharedMesh = meshData.GetMesh();
            timer.Stop();

            meshFilter.sharedMesh.RecalculateNormals();

            string vtxSpeed = timer.ElapsedMilliseconds > 0f ? "" + (meshFilter.sharedMesh.vertexCount / timer.ElapsedMilliseconds) : "inf",
                triSpeed = timer.ElapsedMilliseconds > 0f ? "" + (meshFilter.sharedMesh.triangles.Length / 3 / timer.ElapsedMilliseconds) : "inf";

            display =
            (
                "Dimensions: (" + volume.dimensions.x + ", " + volume.dimensions.y + ", " + volume.dimensions.z + ")\n" +
                "Scale: (" + volume.scale.x + ", " + volume.scale.y + ", " + volume.scale.z + ")\n" +
                "Verticies: " + meshFilter.sharedMesh.vertexCount + " (" + vtxSpeed + " verts/ms)\n" +
                "Triangles: " + (meshFilter.sharedMesh.triangles.Length / 3) + " (" + triSpeed + " tris/ms)\n\n" +
                "Import Time: " + volume.lastImportTime + "ms\n" +
                "Voxel Time: " + volume.lastVoxelTime + "ms\n" +
                "Total Time: " + timer.ElapsedMilliseconds + "ms\n"
            );
        }
    }
}
