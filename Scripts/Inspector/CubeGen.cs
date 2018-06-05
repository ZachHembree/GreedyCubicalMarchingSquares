using System.Collections.Generic;
using System;
using UnityEngine;
using CmsMain;

[ExecuteInEditMode]
public class CubeGen : MonoBehaviour
{
    public float offset = 0.0f;
    public bool simplifyMesh, a, b, c, d, e, f, g, h;
    public Vector3 octantSize;

    public string display { get; private set; }
    private MeshFilter meshFilter;
    private float lastOffset = 0.0f;
    private int count = 0;

    public void Update()
    {
        if (Math.Abs(lastOffset - offset) > 0.01f)
        {
            GetMesh();
            lastOffset = offset;
        }
    }

    public void GetMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = Volume.ContourMesh(GetHeightMap(), octantSize, simplifyMesh);
        meshFilter.sharedMesh.RecalculateNormals();

        display =
            Volume.dimensions +
            Volume.startVertices +
            "Verticies: " + meshFilter.sharedMesh.vertexCount + "\n" +
            "Triangles: " + (meshFilter.sharedMesh.triangles.Length / 3) + "\n";
    }

    private float[][][] GetHeightMap()
    {
        float[][][] heightMap;
        List<float> 
            l1 = new List<float>(), l2 = new List<float>(), 
            l3 = new List<float>(), l4 = new List<float>();

        if (d) l1.Add(0.25f + offset);
        if (a) l1.Add(0.75f + offset);

        if (c) l2.Add(0.25f + offset);
        if (b) l2.Add(0.75f + offset);

        if (h) l3.Add(0.25f + offset);
        if (e) l3.Add(0.75f + offset);

        if (g) l4.Add(0.25f + offset);
        if (f) l4.Add(0.75f + offset);

        heightMap = new float[2][][]
        {
            new float[2][] { l1.ToArray(), l2.ToArray() },
            new float[2][] { l3.ToArray(), l4.ToArray() }
        };

        return heightMap;
    }

    public void TestAll()
    {
        count = 0;

        for (int n = 0; n < 256; n++)
            IterateCube();
    }

    public void ResetCount() =>
        count = 0;

    public void IterateCube()
    {
        if (count >= 256) count = 0;
        int n = count;

        if (n >= 128)
        { h = true; n -= 128; }
        else
            h = false;

        if (n >= 64)
        { g = true; n -= 64; }
        else
            g = false;

        if (n >= 32)
        { f = true; n -= 32; }
        else
            f = false;

        if (n >= 16)
        { e = true; n -= 16; }
        else
            e = false;

        if (n >= 8)
        { d = true; n -= 8; }
        else
            d = false;

        if (n >= 4)
        { c = true; n -= 4; }
        else
            c = false;

        if (n >= 2)
        { b = true; n -= 2; }
        else
            b = false;

        if (n >= 1)
        { a = true; n -= 1; }
        else
            a = false;

        GetMesh();
        count++;
    }
}
