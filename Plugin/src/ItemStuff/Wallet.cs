using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class Wallet : GrabbableObject {
    private RaycastHit hit;
    public AudioSource WalletPlayer;
    public ScanNodeProperties scanNode;
    public override void Start() {
        base.Start();
        scanNode = GetComponentInChildren<ScanNodeProperties>();
    }
    public override void Update() {
        base.Update();
        if (!playerHeldBy) return;
        var interactRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
        if (Physics.Raycast(interactRay, out hit, playerHeldBy.grabDistance, playerHeldBy.interactableObjectsMask) && hit.collider.gameObject.layer != 8) {
            if (hit.collider.transform.gameObject.GetComponent<Money>()) {
                Money coin = hit.collider.transform.gameObject.GetComponent<Money>();
                DetectUseKey(coin);
            }
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true) {
        base.ItemActivate(used, buttonDown);
    }
    
    public void DetectUseKey(Money coin) {
        if (Plugin.InputActionsInstance.UseWallet.triggered) { // Keybind is in CodeRebirthInputs.cs
            UpdateScrapValueServerRpc(coin.scrapValue);
            NetworkObject obj = coin.gameObject.GetComponent<NetworkObject>();
            Plugin.Logger.LogInfo($"Scrap: {scrapValue}" );
            DestroyObjectServerRpc(obj);
        }
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