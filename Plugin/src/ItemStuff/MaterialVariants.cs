using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace CodeRebirth.ItemStuff;

[AddComponentMenu("TestAccount666/Code Rebirth/Material Variants")]
public class MaterialVariants : NetworkBehaviour {
    [Tooltip("The item data of the scrap.")]
    public Item itemData;

    [Space(5f)]
    [Tooltip("The mesh renderers to change the material of. This will use the first material in the array.")]
    public Renderer[] renderers;

    [FormerlySerializedAs("ChangeScanNodeText")]
    [Space(5f)]
    public bool changeScanNodeText;

    [Tooltip("The text to change to when the material is changed.")]
    public string[] scanNodeText;

    [Space(5f)]
    [Tooltip("The scan node properties to change the text of.")]
    public ScanNodeProperties scanNodeProperties;

    [Space(5f)]
    [Tooltip("The currently saved material variant.")]
    public int savedMaterialVariant = -1;

    public override void OnNetworkSpawn() =>
        SetRendererServerRpc();

    [ServerRpc(RequireOwnership = false)]
    private void SetRendererServerRpc() {
        savedMaterialVariant = savedMaterialVariant is not -1
            ? Math.Clamp(savedMaterialVariant, 0, itemData.materialVariants.Length - 1)
            : Random.Range(0, itemData.materialVariants.Length);

        SetRendererClientRpc(savedMaterialVariant);
    }

    [ClientRpc]
    private void SetRendererClientRpc(int materialVariant) {
        foreach (var renderer in renderers) {
            renderer.material = itemData.materialVariants[materialVariant];

            if (!changeScanNodeText)
                continue;

            scanNodeProperties.headerText = scanNodeText[materialVariant];
        }
    }
}