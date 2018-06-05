using UnityEngine;
using UnityEditor;
using CmsMain;
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
            meshFilter.sharedMesh = Volume.ContourMesh(map, octantSize, simplifyMesh);
            timer.Stop();

            meshFilter.sharedMesh.RecalculateNormals();

            string vertRate = timer.ElapsedMilliseconds > 0f ? ("" + (meshFilter.sharedMesh.vertexCount / timer.ElapsedMilliseconds)) : "inf", 
                triRate = timer.ElapsedMilliseconds > 0f ? ("" + (meshFilter.sharedMesh.triangles.Length / 3 / timer.ElapsedMilliseconds)) : "inf";

            display =
            (
                "Total Time: " + timer.ElapsedMilliseconds + "ms\n\n" +
                Volume.startVertices +
                "Verticies: " + meshFilter.sharedMesh.vertexCount + " (" + vertRate + " verts/ms)\n" +
                "Triangles: " + (meshFilter.sharedMesh.triangles.Length / 3) + " (" + triRate + " tris/ms)\n"
            );
        }
    }
}
