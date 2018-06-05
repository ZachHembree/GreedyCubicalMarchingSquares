using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenMatToggle : MonoBehaviour
{
    public Material wire, normal;

	private MeshRenderer meshRenderer;

	public void Start ()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }
	
	public void ToggleWireMesh()
    {
        if (meshRenderer != null)
        {
            if (meshRenderer.sharedMaterial == wire)
                meshRenderer.sharedMaterial = normal;
            else
                meshRenderer.sharedMaterial = wire;
        }
    }
}
