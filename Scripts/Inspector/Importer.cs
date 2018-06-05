using UnityEngine;
using CmsMain;
using System.Diagnostics;
using System.Collections.Generic;

public class Importer : MonoBehaviour
{
    public Mesh inputMesh;
    public Vector3 resolution;
    public bool simplifyMesh;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    public string display { get; private set; }

    public void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
    }

    public void ToggleReduction() =>
        simplifyMesh = !simplifyMesh;

    public void ImportMesh()
    {
        Stopwatch timer = new Stopwatch();

        if (meshRenderer == null || meshFilter == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }

        timer.Start();
        meshFilter.sharedMesh = Volume.ContourMesh(inputMesh, resolution, simplifyMesh);
        timer.Stop();

        meshFilter.sharedMesh.RecalculateNormals();

        string vtxSpeed = timer.ElapsedMilliseconds > 0f ? "" + (meshFilter.sharedMesh.vertexCount / timer.ElapsedMilliseconds) : "inf",
            triSpeed = timer.ElapsedMilliseconds > 0f ? "" + (meshFilter.sharedMesh.triangles.Length / 3 / timer.ElapsedMilliseconds) : "inf";

        display =
        (
            Volume.dimensions +
            Volume.octantSize + "\n" +
            Volume.importVoxTime + 
            "Total Time: " + timer.ElapsedMilliseconds + "ms\n\n" +
            Volume.startVertices +
            "Verticies: " + meshFilter.sharedMesh.vertexCount + " (" + vtxSpeed + " verts/ms)\n" +
            "Triangles: " + (meshFilter.sharedMesh.triangles.Length / 3) + " (" + triSpeed + " tris/ms)\n"
        );
    }
}
