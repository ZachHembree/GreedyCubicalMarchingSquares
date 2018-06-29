using UnityEngine;
using GreedyCms;
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
    public string Display { get; private set; }

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
            HeightMapVolume volume = new HeightMapVolume(map, octantSize);
            Surface surface = new Surface(volume, simplifyMesh);
            surface.GetMeshData();

            meshFilter.sharedMesh = surface.MeshData.GetMesh();
            meshFilter.sharedMesh.RecalculateNormals();
            timer.Stop();

            string vtxSpeed = timer.ElapsedMilliseconds > 0f ? "" + (meshFilter.sharedMesh.vertexCount / timer.ElapsedMilliseconds) : "inf",
                triSpeed = timer.ElapsedMilliseconds > 0f ? "" + (meshFilter.sharedMesh.triangles.Length / 3 / timer.ElapsedMilliseconds) : "inf";

            Display =
            (
                "Dimensions: (" + volume.Dimensions.x + ", " + volume.Dimensions.y + ", " + volume.Dimensions.z + ")\n" +
                "Scale: (" + volume.Scale.x + ", " + volume.Scale.y + ", " + volume.Scale.z + ")\n" +
                "Verticies: " + meshFilter.sharedMesh.vertexCount + " (" + vtxSpeed + " verts/ms)\n" +
                "Triangles: " + (meshFilter.sharedMesh.triangles.Length / 3) + " (" + triSpeed + " tris/ms)\n\n" +
                "Import Time: " + volume.LastImportTime + "ms\n" +
                "Voxel Time: " + surface.LastVoxelTime + "ms\n" +
                "Total Time: " + timer.ElapsedMilliseconds + "ms\n"
            );
        }
    }
}
