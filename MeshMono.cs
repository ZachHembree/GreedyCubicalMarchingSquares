using UnityEngine;
using UnityEditor;
using HelmetVolumes;
using System.Diagnostics;
using System.Collections.Generic;

public class MeshMono : MonoBehaviour
{
    public Mesh inputMesh;
    public float resolution;

    private MeshFilter meshFilter;
    public string display { get; private set; }

    public void ImportMesh()
    {
        Stopwatch timer = new Stopwatch();
        meshFilter = GetComponent<MeshFilter>();

        timer.Start();
        meshFilter.sharedMesh = CMS.ContourMesh(inputMesh, resolution);
        timer.Stop();

        meshFilter.sharedMesh.RecalculateNormals();

        display =
        (
            CMS.dimensions +
            CMS.octantSize + "\n" +
            CMS.importVoxTime + 
            "Total Time: " + timer.ElapsedMilliseconds + "ms\n\n" +
            CMS.startVertices +
            "Verticies: " + meshFilter.sharedMesh.vertexCount + "\n" +
            "Triangles: " + (meshFilter.sharedMesh.triangles.Length / 3) + "\n" +
            "Triangles/ms: " + (meshFilter.sharedMesh.triangles.Length / 3) / timer.ElapsedMilliseconds + " tris/ms\n" +
            "Vertices/ms: " + meshFilter.sharedMesh.vertexCount / timer.ElapsedMilliseconds + " verts/ms\n"
        );
    }
}

[CustomEditor(typeof(MeshMono))]
public class ImporterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        MeshMono importer = (MeshMono)target;
        base.OnInspectorGUI();

        if (GUILayout.Button("Import Mesh"))
            importer.ImportMesh();

        EditorGUILayout.TextArea(importer.display, GUILayout.MaxHeight(180));
    }    
}
