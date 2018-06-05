using System;
using UnityEngine;
using UnityEngine.UI;

public class ImporterUI : MonoBehaviour
{
    public GameObject volumeGenerator;
    public Dropdown meshSelector;
    public Text display;
    public InputField xField, yField, zField;
    public Button button;

    private MeshRenderer meshRenderer;
    private Transform meshTransform;
    private Importer importer;
    private Mesh[] meshes;

	public void Start ()
    {
        importer = volumeGenerator.GetComponent<Importer>();
        meshRenderer = volumeGenerator.GetComponent<MeshRenderer>();
        meshTransform = volumeGenerator.transform;

        meshRenderer.enabled = false;
        button.onClick.AddListener(delegate { UpdateImporter(); });

        meshes = new Mesh[] 
        {
            GetPrimitiveMesh(PrimitiveType.Sphere),
            GetPrimitiveMesh(PrimitiveType.Cylinder),
            Resources.Load<Mesh>("Scope"),
            Resources.Load<Mesh>("Cabinet")
        };
    }

    private Mesh GetPrimitiveMesh(PrimitiveType primitive)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitive);
        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        Destroy(gameObject);

        return mesh;
    }

    private void UpdateRes()
    {
        float x = 1f, y = 1f, z = 1f;

        if (float.TryParse(xField.text, out x))
            importer.resolution.x = x;

        if (float.TryParse(yField.text, out y))
            importer.resolution.y = y;

        if (float.TryParse(zField.text, out z))
            importer.resolution.z = z;
    }

    private void UpdateMesh()
    {
        if (meshSelector.value == 0)
        {
            importer.inputMesh = meshes[0];
            meshTransform.position = new Vector3(3.58f, 11.44f, -11.17f);
            meshTransform.eulerAngles = Vector3.zero;
        }
        else if (meshSelector.value == 1)
        {
            importer.inputMesh = meshes[1];
            meshTransform.position = new Vector3(3.66f, 10.29f, -10.79f);
            meshTransform.eulerAngles = Vector3.zero;
        }
        else if (meshSelector.value == 2)
        {
            importer.inputMesh = meshes[2];
            meshTransform.position = new Vector3(3.94f, 4.88f, -3.07f);
            meshTransform.eulerAngles = new Vector3(0f, 120f, 0f);
        }
        else if (meshSelector.value == 3)
        {
            importer.inputMesh = meshes[3];
            meshTransform.position = new Vector3(3.3f, -165.69f, 129f);
            meshTransform.eulerAngles = Vector3.zero;
        }
    }

    public void UpdateImporter()
    {
        UpdateRes();
        UpdateMesh();

        meshRenderer.enabled = true;
        importer.ImportMesh();
        display.text = importer.display;
    }
}
