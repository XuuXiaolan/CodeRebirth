using Unity.Netcode;
using UnityEngine;
using CodeRebirth.ScrapStuff;

namespace CodeRebirth.ItemStuff;
public class WalletOld : GrabbableObject {
    private RaycastHit hit;
    private ScanNodeProperties scanNode = null!;
    private SkinnedMeshRenderer skinnedMeshRenderer = null!;
    public override void Start() {
        base.Start();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        scanNode = GetComponentInChildren<ScanNodeProperties>();
    }

    public override void ItemActivate(bool used, bool buttonDown = true) {
        if (!playerHeldBy) return;
        var interactRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
        if (Physics.Raycast(interactRay, out hit, playerHeldBy.grabDistance, playerHeldBy.interactableObjectsMask) && hit.collider.gameObject.layer != 8)
        {
            Money coin = hit.collider.transform.gameObject.GetComponent<Money>();
            if (coin == null) return;
            GetComponent<AudioSource>().Play();
            UpdateScrapValueServerRpc(coin.scrapValue);
            NetworkObject obj = coin.gameObject.GetComponent<NetworkObject>();
            float newblendShapeWeight = Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0)+20f, 0, 300);
            if (playerHeldBy) {
                IncreaseBlendShapeWeightClientRpc(newblendShapeWeight);
            }
            DestroyObjectServerRpc(obj);
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