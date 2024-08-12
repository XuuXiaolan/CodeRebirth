using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace CodeRebirth.src.Content.Items;

[AddComponentMenu("TestAccount666/Code Rebirth/Material Variants")]
public class MaterialVariants : NetworkBehaviour {
    [SerializeField]
    [Tooltip("The item data of the scrap.")]
    private Item itemData = null!;

    [Space(5f)]
    [SerializeField]
    [Tooltip("The mesh renderers to change the material of. This will use the first material in the array.")]
    private Renderer[] renderers = null!;

    [FormerlySerializedAs("ChangeScanNodeText")]
    [Space(5f)]
    private bool changeScanNodeText = false;
    
    [SerializeField]
    [Tooltip("The text to change to when the material is changed.")]
    private string[] scanNodeText = null!;

    [Space(5f)]
    [SerializeField]
    [Tooltip("The scan node properties to change the text of.")]
    private ScanNodeProperties scanNodeProperties = null!;

    [Space(5f)]
    [SerializeField]
    [Tooltip("The currently saved material variant.")]
    private int savedMaterialVariant = -1;

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