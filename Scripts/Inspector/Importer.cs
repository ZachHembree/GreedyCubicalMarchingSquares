using UnityEngine;
using GreedyCms;
using System.Diagnostics;
using System.Collections.Generic;

public class Importer : MonoBehaviour
{
    public Mesh inputMesh;
    public Vector3 resolution;
    public bool simplifyMesh;
    public string Display { get; private set; }
    public Mesh[] Meshes { get; private set; }

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    public void Start()
    {
        resolution = Vector3.one;
        simplifyMesh = false;
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        Meshes = new Mesh[]
        {
            GetPrimitiveMesh(PrimitiveType.Sphere),
            GetPrimitiveMesh(PrimitiveType.Cylinder),
            Resources.Load<Mesh>("Scope"),
            Resources.Load<Mesh>("Cabinet")
        };
    }

    public void ToggleReduction() =>
        simplifyMesh = !simplifyMesh;

    public void GetMesh()
    {
        Stopwatch timer = new Stopwatch();

        if (meshRenderer == null || meshFilter == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }

        timer.Start();
        MeshVolume volume = new MeshVolume(inputMesh, resolution);
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

    private Mesh GetPrimitiveMesh(PrimitiveType primitive)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitive);
        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        Destroy(gameObject);

        return mesh;
    }
}
