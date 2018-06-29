using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CubeGen))]
public class MeshInspector : Editor
{
    public override void OnInspectorGUI()
    {
        GUIStyle style = new GUIStyle();
        CubeGen gen = (CubeGen)target;

        //gen.mat = (Material)EditorGUILayout.ObjectField("Mat", gen.mat, typeof(Material), true);
        gen.octantSize = EditorGUILayout.Vector3Field("Octant Size", gen.octantSize);

        EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Offset", style);
            gen.offset = GUILayout.HorizontalSlider(gen.offset, 0f, 6f);
        EditorGUILayout.EndHorizontal();

        gen.simplifyMesh = GUILayout.Toggle(gen.simplifyMesh, new GUIContent(" Simplify Mesh"));

        EditorGUILayout.BeginHorizontal();
            gen.a = GUILayout.Toggle(gen.a, new GUIContent(" a"));
            gen.b = GUILayout.Toggle(gen.b, new GUIContent(" b"));
            gen.c = GUILayout.Toggle(gen.c, new GUIContent(" c"));
            gen.d = GUILayout.Toggle(gen.d, new GUIContent(" d"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
            gen.e = GUILayout.Toggle(gen.e, new GUIContent(" e"));
            gen.f = GUILayout.Toggle(gen.f, new GUIContent(" f"));
            gen.g = GUILayout.Toggle(gen.g, new GUIContent(" g"));
            gen.h = GUILayout.Toggle(gen.h, new GUIContent(" h"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test All"))
                gen.TestAll();

            if (GUILayout.Button("Iterate Cube"))
                gen.IterateCube();

            if (GUILayout.Button("Reset Count"))
                gen.ResetCount();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Generate Cube"))
            gen.GetMesh();

        EditorGUILayout.SelectableLabel(gen.Display, EditorStyles.textField, GUILayout.Height(58));
    }
}

[CustomEditor(typeof(MapGen))]
public class MapGenInspector : Editor
{
    public override void OnInspectorGUI()
    {
        MapGen mGen = (MapGen)target;
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Map"))
            mGen.GetMap();

        if (GUILayout.Button("Generate Mesh"))
            mGen.GetMesh();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.SelectableLabel(mGen.Display, EditorStyles.textField, GUILayout.Height(120));
    }
}

[CustomEditor(typeof(Importer))]
public class ImporterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        Importer importer = (Importer)target;
        base.OnInspectorGUI();

        if (GUILayout.Button("Import Mesh"))
            importer.GetMesh();

        EditorGUILayout.SelectableLabel(importer.Display, EditorStyles.textField, GUILayout.Height(120));
    }
}
