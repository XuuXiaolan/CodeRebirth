using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Content.Items;

namespace CodeRebirth.src.Content.Items;
public class WalletOld : GrabbableObject {
    private RaycastHit hit;
    private ScanNodeProperties scanNode = null!;
    private int ownScrapValue = 0;
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
            NetworkObject obj = coin.NetworkObject;
            float newblendShapeWeight = Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0)+20f, 0, 300);
            if (playerHeldBy) {
                IncreaseBlendShapeWeightClientRpc(newblendShapeWeight);
            }
            DestroyObjectServerRpc(obj);
        }
    }

    public override void Update()
    {
        base.Update();
        if (playerHeldBy == null) return;
        if (scrapValue < ownScrapValue)
        {
            // there was a decrease in scrap value.
            float amountToDecrease = (ownScrapValue - scrapValue)/ownScrapValue;
            float decreaseAmount = skinnedMeshRenderer.GetBlendShapeWeight(0)*amountToDecrease;
            IncreaseBlendShapeWeightClientRpc(skinnedMeshRenderer.GetBlendShapeWeight(0) - decreaseAmount);
        }
        ownScrapValue = scrapValue;
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
        Destroy(netObj.gameObject.GetComponent<Money>().radarIcon.gameObject);
        if(netObj.IsOwnedByServer && netObj.IsSpawned && netObj.IsOwner) netObj.Despawn();
    }
}