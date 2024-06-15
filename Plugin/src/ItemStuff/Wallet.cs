using CodeRebirth.Misc;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.ScrapStuff;

namespace CodeRebirth.ItemStuff;
public class Wallet : GrabbableObject { // default value: takes a lot money, takes up a slot
    public InteractTrigger trigger;
    private RaycastHit hit;
    private ScanNodeProperties scanNode;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    public override void Start() { // you'll need to do stuff like giving the wallet a client network transform if the position isn't sync'ing properly, then you'd do stuff with changing ownership, see hoverboard for how i do it, its simple
        StartBaseImportant();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        scanNode = GetComponentInChildren<ScanNodeProperties>();
    }
    public void StartBaseImportant() {
        this.propColliders = base.gameObject.GetComponentsInChildren<Collider>();
        this.originalScale = base.transform.localScale;
        if (this.itemProperties.itemSpawnsOnGround) {
            this.startFallingPosition = base.transform.position;
            if (base.transform.parent != null) {
                this.startFallingPosition = base.transform.parent.InverseTransformPoint(this.startFallingPosition);
            }
            this.FallToGround(false);
        } else {
            this.hasHitGround = true;
            this.reachedFloorTarget = true;
            this.targetFloorPosition = base.transform.localPosition;
        }
        if (this.itemProperties.isScrap) {
            this.hasHitGround = true;
        }
        if (this.itemProperties.isScrap && RoundManager.Instance.mapPropsContainer != null) {
            this.radarIcon = Instantiate<GameObject>(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
        }
        if (!this.itemProperties.isScrap) {
            HoarderBugAI.grabbableObjectsInMap.Add(base.gameObject);
        }
        MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < componentsInChildren.Length; i++) {
            componentsInChildren[i].renderingLayerMask = 1U;
        }
        SkinnedMeshRenderer[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int j = 0; j < componentsInChildren2.Length; j++) {
            componentsInChildren2[j].renderingLayerMask = 1U;
        }
        trigger.onInteract.AddListener(OnInteract);
    }
    public void OnInteract(PlayerControllerB player) {
        playerHeldBy = player; // rpc this probably
        // parent wallet to player and position it to their right pocket or smthn
    }
    public override void Update() {
        base.Update();
        if (playerHeldBy == null || GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        HandleItemActivate();
        HandleItemDrop();
        HandleItemSell();
    }
    public void HandleItemSell() {
        if (!Plugin.InputActionsInstance.WalletSell.triggered) return;
        // basically check if the player's screen's tooltips thing says smthn about selling to company moon when holding over the counter or some raycast of some sort
    }
    public void HandleItemDrop() {
        if (!Plugin.InputActionsInstance.WalletDrop.triggered) return;
        // make a couple rpc's, switch a few booleans
        // drop the wallet infront of the player onto the ground or smthn
    }
    public void HandleItemActivate() {
        if (!Plugin.InputActionsInstance.WalletActivate.triggered) return;
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
            Plugin.Logger.LogDebug("COULDNT FIND THE OBJECT TO DESTROY");
            // COULDNT FIND THE OBJECT TO DESTROY //
        }
    }

    public void DestroyObject(NetworkObject netObj) {
        if(netObj.IsOwnedByServer && netObj.IsSpawned && netObj.IsOwner) netObj.Despawn();
    }
}