using CodeRebirth.Misc;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.ScrapStuff;

namespace CodeRebirth.ItemStuff;
public class Wallet : GrabbableObject {
    private RaycastHit hit;
    private ScanNodeProperties scanNode;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    public override void Start() {
        base.Start();
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();

        scanNode = GetComponentInChildren<ScanNodeProperties>();
        Plugin.Logger.LogInfo(RoundManager.Instance.currentLevel.PlanetName);
    }

    public override void ItemActivate(bool used, bool buttonDown = true) {
        if (!playerHeldBy) return;
        var interactRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
        if (Physics.Raycast(interactRay, out hit, playerHeldBy.grabDistance, playerHeldBy.interactableObjectsMask) && hit.collider.gameObject.layer != 8)
        {
            if (hit.collider.transform.gameObject.GetComponent<Money>())
            {
                Money coin = hit.collider.transform.gameObject.GetComponent<Money>();
                UpdateScrapValueServerRpc(coin.scrapValue);
                NetworkObject obj = coin.gameObject.GetComponent<NetworkObject>();
                Plugin.Logger.LogInfo($"Scrap: {scrapValue}");
                float newblendShapeWeight = Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0)+10f, 0, 300);
                if (playerHeldBy) {
                    IncreaseBlendShapeWeightClientRpc(newblendShapeWeight);
                }
                DestroyObjectServerRpc(obj);
            }
        }
    }

    [ClientRpc]
    public void IncreaseBlendShapeWeightClientRpc(float newblendShapeWeight) {
        skinnedMeshRenderer.SetBlendShapeWeight(0, newblendShapeWeight);
    }
    [ServerRpc(RequireOwnership = false)]
    public void UpdateScrapValueServerRpc(int valueToAdd) {
        this.scrapValue += valueToAdd;
        UpdateScrapValueClientRpc(scrapValue);
    }

    [ClientRpc]
    private void UpdateScrapValueClientRpc(int newScrapValue) {
        this.scrapValue = newScrapValue;
        scanNode.scrapValue = scrapValue;
        scanNode.subText = $"Value: {scrapValue}";
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyObjectServerRpc(NetworkObjectReference obj) {
        DestroyObjectClientRpc(obj);
    }

    [ClientRpc]
    public void DestroyObjectClientRpc(NetworkObjectReference obj) {
        if (obj.TryGet(out NetworkObject netObj)) {
            DestroyObject(netObj);
        } else {
            // COULDNT FIND THE OBJECT TO DESTROY //
        }
    }

    public void DestroyObject(NetworkObject netObj) {
        if(netObj.IsOwnedByServer && netObj.IsSpawned && netObj.IsOwner) netObj.Despawn();
    }
}