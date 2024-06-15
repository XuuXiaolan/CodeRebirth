using CodeRebirth.Misc;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.ScrapStuff;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.InputSystem;

namespace CodeRebirth.ItemStuff;
public class Wallet : GrabbableObject { // default value: takes a lot money, takes up a slot
    public InteractTrigger trigger;
    private RaycastHit hit;
    private ScanNodeProperties scanNode;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    public Transform WalletChild;
    public enum WalletModes {
        Held,
        None,
    }
    private WalletModes walletMode = WalletModes.None;
    public override void Start() {
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
        if (GameNetworkManager.Instance.localPlayerController != player) return;
        StartCoroutine(OnInteractCoroutine(player));
    }
    public IEnumerator OnInteractCoroutine(PlayerControllerB player) {
        if (IsHost) {
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            yield return new WaitUntil(() => playerHeldBy == player);
        } else {
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            yield return new WaitUntil(() => playerHeldBy == player);
        }
    }
    public override void Update() {
        base.Update();
        if (playerHeldBy == null || GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        Plugin.Logger.LogDebug($"playerHeldBy: {playerHeldBy}");
        HandleItemActivate();
        HandleItemDrop();
        HandleItemSell();
    }
    public void HandleItemSell() {
        if (!Plugin.InputActionsInstance.WalletSell.triggered) return;
        // place wallet in company counter
    }
    public void HandleItemDrop() {
        if (!Plugin.InputActionsInstance.WalletDrop.triggered) return;
        DropWallet();
    }
    public void DropWallet() {
        if (IsHost) {
            SetTargetClientRpc(-1);
        } else {
            SetTargetServerRpc(-1);
        }
    }
    public void HandleItemActivate() {
        if (!Plugin.InputActionsInstance.WalletActivate.triggered) return;
        UpdateToolTips();
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

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID) {
        SetTargetClientRpc(PlayerID);
    }

    public override void LateUpdate() {
        base.LateUpdate();
        if (!IsServer || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving) return;
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (Vector3.Distance(WalletChild.position, StartOfRound.Instance.shipBounds.transform.position) < 12 && !isInShipRoom) {
            this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            isInShipRoom = true;
            isInElevator = true;
        } else if (Vector3.Distance(WalletChild.position, StartOfRound.Instance.shipBounds.transform.position) >= 12 && isInShipRoom) {
            this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
            isInShipRoom = false;
            isInElevator = false;
        }
    }
    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID) {
        if (PlayerID == -1) {
            PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
            walletMode = WalletModes.None;
            if (IsServer) {
                if (isInShipRoom) {
                    this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
                } else {
                    this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
                }
            }
            Plugin.Logger.LogInfo($"Clearing target on {this}");
            playerHeldBy = null;
            isHeld = false;
            trigger.interactable = true;
            return;
        }

        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[PlayerID];
        if (player == null) {
            Plugin.Logger.LogInfo($"Invalid player index: {PlayerID}");
            return;
        }
        walletMode = WalletModes.Held;
        trigger.interactable = false;
        playerHeldBy = player;
        isHeld = true;
        this.transform.SetParent(playerHeldBy.transform, true);
        this.WalletChild.position = playerHeldBy.transform.position;
        if (IsServer) {
            GetComponent<NetworkObject>().ChangeOwnership(player.actualClientId);
        }
        UpdateToolTips();
    }
    public void UpdateToolTips() {
        if (playerHeldBy == null || GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        HUDManager.Instance.ClearControlTips();
        if (walletMode == WalletModes.Held) {
            HUDManager.Instance.ChangeControlTipMultiple(new string[] {
                $"Use Wallet : [{Plugin.InputActionsInstance.WalletActivate.GetBindingDisplayString().Split(' ')[0]}]",
                $"Drop Wallet : [{Plugin.InputActionsInstance.WalletDrop.GetBindingDisplayString().Split(' ')[0]}]",
                $"Sell Wallet : [{Plugin.InputActionsInstance.WalletSell.GetBindingDisplayString().Split(' ')[0]}]"
            });
        }
    }
}