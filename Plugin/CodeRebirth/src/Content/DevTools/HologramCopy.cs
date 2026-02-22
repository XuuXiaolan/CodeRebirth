using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace CodeRebirth.src.Content.DevTools;

public class HologramCopy
{
    public GameObject HologramObject { get; private set; }

    public void SetUpHologram(GameObject gameObject, Material TransparentMaterial)
    {
        // create a hologram copy of the map object and set it up to be displayed as a hologram
        HologramObject = GameObject.Instantiate(gameObject, Vector3.zero, Quaternion.identity);
        List<Component> components = [.. HologramObject.GetComponentsInChildren<Component>()];
        HashSet<Renderer> renderers = new();
        foreach (Component component in components)
        {
            if (component is Transform or MeshFilter)
            {
                continue;
            }

            if (component is Renderer renderer)
            {
                renderers.Add(renderer);
                continue;
            }

            if (component is ProBuilderMesh proBuilderMesh)
            {
                renderers.Add(proBuilderMesh.renderer);
                continue;
            }

            Component.Destroy(component);
        }

        foreach (Renderer renderer in renderers)
        {
            int materialsCount = renderer.materials.Length;
            for (int i = 0; i < materialsCount; i++)
            {
                renderer.materials[i] = TransparentMaterial;
            }
        }
        HologramObject.SetActive(false);
    }

    public void UpdateTick(UnityEngine.RaycastHit raycastHit)
    {
        HologramObject.transform.position = raycastHit.point;
        HologramObject.SetActive(true);
    }
}