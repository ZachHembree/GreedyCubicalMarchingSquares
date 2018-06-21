using UnityEngine;
using CmsNext;
using System.Diagnostics;
using System.Collections.Generic;

public class Importer : MonoBehaviour
{
    public Mesh[] meshes;
    public Mesh inputMesh;
    public Vector3 resolution;
    public bool simplifyMesh;
    public string display { get; private set; }

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    public void Start()
    {
        resolution = Vector3.one;
        simplifyMesh = false;
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        meshes = new Mesh[]
        {
            GetPrimitiveMesh(PrimitiveType.Sphere),
            GetPrimitiveMesh(PrimitiveType.Cylinder),
            Resources.Load<Mesh>("Scope"),
            Resources.Load<Mesh>("Cabinet")
        };
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
        Volume volume = new Volume();
        MeshData meshData = volume.VoxelizeMesh(inputMesh, resolution, simplifyMesh);
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

    private Mesh GetPrimitiveMesh(PrimitiveType primitive)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitive);
        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        Destroy(gameObject);

        return mesh;
    }
}
