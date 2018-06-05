using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapGenUI : MonoBehaviour
{
    public GameObject volumeGenerator;
    public Dropdown mapSizeSelector;
    public Text display;
    public Button meshGenButton;

    private MapGen mapGen;
    private MeshRenderer meshRenderer;
    private Transform meshTransform;

    public void Start ()
    {
        mapGen = volumeGenerator.GetComponent<MapGen>();
        meshRenderer = volumeGenerator.GetComponent<MeshRenderer>();
        meshTransform = volumeGenerator.GetComponent<Transform>();

        meshRenderer.enabled = false;
        mapGen.octantSize = new Vector3(.5f, .5f, .5f);

        meshGenButton.onClick.AddListener(delegate { GenerateMesh(); });
    }

    private void GenerateMesh()
    {
        meshRenderer.enabled = true;

        GenerateMap();
        UpdateTransform();
        mapGen.GetMesh();

        display.text = mapGen.display;
    }

    private void GenerateMap()
    {
        if (mapSizeSelector.value == 0)
        { mapGen.dimensions.x = 16; mapGen.dimensions.y = 16; mapGen.dimensions.z = 16; }
        else if (mapSizeSelector.value == 1)
        { mapGen.dimensions.x = 32; mapGen.dimensions.y = 32; mapGen.dimensions.z = 32; }
        else if (mapSizeSelector.value == 2)
        { mapGen.dimensions.x = 64; mapGen.dimensions.y = 64; mapGen.dimensions.z = 64; }
        else if (mapSizeSelector.value == 3)
        { mapGen.dimensions.x = 96; mapGen.dimensions.y = 96; mapGen.dimensions.z = 96; }

        meshTransform.eulerAngles = Vector3.zero;
        mapGen.cohesion = true;
        mapGen.useRandom = true;
        mapGen.GetMap();
    }

    private void UpdateTransform()
    {
        if (mapSizeSelector.value == 0)
            meshTransform.position = new Vector3(3.11f, 3.51f, -11.21f);
        else if (mapSizeSelector.value == 1)
            meshTransform.position = new Vector3(2.78f, -4.57f, -10.39f);
        else if (mapSizeSelector.value == 2)
            meshTransform.position = new Vector3(1.68f, -19.01f, -15.73f);
        else if (mapSizeSelector.value == 3)
            meshTransform.position = new Vector3(0f, -32.91f, -22.24f);
    }
}
