using UnityEngine;
using UnityEditor;
using HelmetVolumes;
using System.Diagnostics;
using System.Collections.Generic;

public class MapGen : MonoBehaviour
{
    public Vector3 octantSize;
    public float[][][] map;
	
    private MeshFilter meshFilter;
    public string display { get; private set; }

    public void GetMesh()
    {
        if (map != null)
        {
            Stopwatch timer = new Stopwatch();
            meshFilter = GetComponent<MeshFilter>();

            timer.Start();
            meshFilter.sharedMesh = CMS.ContourMesh(map, octantSize);
            timer.Stop();

            meshFilter.sharedMesh.RecalculateNormals();

            display =
            (
                "Total Time: " + timer.ElapsedMilliseconds + "ms\n\n" +
                CMS.startVertices +
                "Verticies: " + meshFilter.sharedMesh.vertexCount + "\n" +
                "Triangles: " + (meshFilter.sharedMesh.triangles.Length / 3) + "\n" +
                "Triangles/ms: " + (meshFilter.sharedMesh.triangles.Length / 3) / timer.ElapsedMilliseconds + " tris/ms\n" +
                "Vertices/ms: " + meshFilter.sharedMesh.vertexCount / timer.ElapsedMilliseconds + " verts/ms\n"
            );
        }
    }
}

[CustomEditor(typeof(MapGen))]
public class MapGenInspector : Editor
{
    public override void OnInspectorGUI()
    {
        MapGen cGen = (MapGen)target;
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate Mesh"))
            cGen.GetMesh();

        EditorGUILayout.TextArea(cGen.display, GUILayout.MaxHeight(100));
    }
}
