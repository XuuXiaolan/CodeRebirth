using System.Collections;
using System.Linq;
using CodeRebirth.Misc;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using CodeRebirth.ScrapStuff;

namespace CodeRebirth.ItemStuff;
public class Wallet : GrabbableObject { 
    public InteractTrigger trigger;
    private RaycastHit hit;
    private ScanNodeProperties scanNode;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    public enum WalletModes {
        Held,
        None,
        Sold,
    }
    private WalletModes walletMode = WalletModes.None;
    private PlayerControllerB walletHeldBy;

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
        if (IsHost) {
            SetTargetClientRpc(System.Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        } else {
            SetTargetServerRpc(System.Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        }
    }

    public override void FallWithCurve() {
        return;
    }


    public override void Update() {
        base.Update();
        if (walletHeldBy == null) {
            this.transform.rotation = Quaternion.Lerp(
                                            this.transform.rotation,
                                            Quaternion.Euler(this.itemProperties.restingRotation.x, (float)(this.floorYRot + this.itemProperties.floorYOffset) + 90f, this.itemProperties.restingRotation.z),
                                            Mathf.Clamp(14f * Time.deltaTime / 2, 0f, 1f)
                                        );
        }
        if (walletHeldBy == null || GameNetworkManager.Instance.localPlayerController != walletHeldBy) return;
        HandleItemActivate();
        HandleItemDrop();
        HandleItemSell();
    }

    public void HandleItemSell() {
        if (!Plugin.InputActionsInstance.WalletSell.triggered) return;
        if (Object.FindObjectOfType<DepositItemsDesk>() != null && walletHeldBy != null)
        {
            DepositItemsDesk depositItemsDesk = Object.FindObjectOfType<DepositItemsDesk>();
            if (Vector3.Distance(Object.FindObjectOfType<DepositItemsDesk>().triggerCollider.transform.position, walletHeldBy.transform.position) < 8f || depositItemsDesk.triggerCollider.bounds.Contains(walletHeldBy.transform.position))
            {
                if (depositItemsDesk.deskObjectsContainer.GetComponentsInChildren<GrabbableObject>().Length >= 12 || depositItemsDesk.inGrabbingObjectsAnimation)
                {
                    return;
                }
                if (GameNetworkManager.Instance != null && walletHeldBy == GameNetworkManager.Instance.localPlayerController)
                {
                    depositItemsDesk.AddObjectToDeskServerRpc(this.GetComponent<NetworkObject>());
                    walletMode = WalletModes.Sold;
                    DropWallet();
                    this.transform.position = new Vector3(-29.3048f, -1.2182f, -31.4077f);
                }
                return;
            }
        }
    }

    public void HandleItemDrop() {
        if (!Plugin.InputActionsInstance.WalletDrop.triggered || walletHeldBy == null || walletHeldBy.inTerminalMenu) return;
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
        var interactRay = new Ray(walletHeldBy.gameplayCamera.transform.position, walletHeldBy.gameplayCamera.transform.forward);
        if (Physics.Raycast(interactRay, out hit, walletHeldBy.grabDistance, walletHeldBy.interactableObjectsMask) && hit.collider.gameObject.layer != 8) {
            Money coin = hit.collider.transform.gameObject.GetComponent<Money>();
            if (coin == null) return;
            GetComponentInChildren<AudioSource>().Play();
            UpdateScrapValueServerRpc(coin.scrapValue);
            NetworkObject obj = coin.gameObject.GetComponent<NetworkObject>();
            float newblendShapeWeight = Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0) + 20f, 0, 300);
            if (walletHeldBy) {
                IncreaseBlendShapeWeightClientRpc(newblendShapeWeight);
            }
            UpdateToolTips();
            DestroyObjectServerRpc(obj);
        }
    }

    [ClientRpc]
    public void IncreaseBlendShapeWeightClientRpc(float newblendShapeWeight) {
        this.itemProperties.weight += 0.01f;
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
        }
    }

    public void DestroyObject(NetworkObject netObj) {
        if (netObj.IsOwnedByServer && netObj.IsSpawned && netObj.IsOwner) netObj.Despawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID) {
        SetTargetClientRpc(PlayerID);
    }

    public override void LateUpdate() {
        base.LateUpdate();
        if (!IsServer || walletMode == WalletModes.Held) return;
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (StartOfRound.Instance.shipBounds.bounds.Contains(this.transform.position) && !isInShipRoom) {
            this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            isInShipRoom = true;
            isInElevator = true;
        } else if (!StartOfRound.Instance.shipBounds.bounds.Contains(this.transform.position) && isInShipRoom) {
            this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
            isInShipRoom = false;
            isInElevator = false;
        }
    }



    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID) {
        if (PlayerID == -1) {
            PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
            if (IsServer) {
                if (isInShipRoom) {
                    this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
                } else {
                    this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
                }
            }
            Plugin.Logger.LogInfo($"Clearing target on {this}");
            isHeld = false;
            if (walletMode == WalletModes.Sold) {
                trigger.interactable = false;
            } else {
                // Perform a raycast from the camera to find the drop position
                var interactRay = new Ray(walletHeldBy.gameplayCamera.transform.position, walletHeldBy.gameplayCamera.transform.forward);
                if (Physics.Raycast(interactRay, out RaycastHit hit, walletHeldBy.grabDistance + 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
                    // Set the wallet's position to the hit point with an offset away from the surface
                    this.transform.position = hit.point + hit.normal * 0.1f;
                    this.transform.position += Vector3.down * 0.05f;
                } else {
                    this.transform.position = walletHeldBy.transform.position + walletHeldBy.transform.forward * 0.3f;
                }
                trigger.interactable = true;
            }
            walletHeldBy.carryWeight -= (this.itemProperties.weight - 1);
            walletMode = WalletModes.None;
            walletHeldBy = null;
            return;
        }

        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[PlayerID];
        if (player == null) {
            Plugin.Logger.LogInfo($"Invalid player index: {PlayerID}");
            return;
        }
        walletMode = WalletModes.Held;
        trigger.interactable = false;
        walletHeldBy = player;
        this.transform.position = player.transform.position + player.transform.up * 1f + player.transform.right * 0.25f + player.transform.forward * 0.05f;
        walletHeldBy.carryWeight += (this.itemProperties.weight - 1f);
        // Apply the rotations
        Quaternion rotationLeft = Quaternion.Euler(0, 180, 0);
        Quaternion rotationForward = Quaternion.Euler(15, 0, 0);

        // Combine the rotations: rotate 180 degrees left first, then rotate forward by 15 degrees
        this.transform.rotation = rotationLeft * rotationForward;

        isHeld = true;
        this.transform.SetParent(walletHeldBy.transform, true);
        if (IsServer) {
            GetComponent<NetworkObject>().ChangeOwnership(player.actualClientId);
        }
        UpdateToolTips();
    }

    public void UpdateToolTips() {
        if (walletHeldBy == null || GameNetworkManager.Instance.localPlayerController != walletHeldBy) return;
        HUDManager.Instance.ClearControlTips();
        if (walletMode == WalletModes.Held) {
            HUDManager.Instance.ChangeControlTipMultiple(new string[] {
                $"Use Wallet : [{Plugin.InputActionsInstance.WalletActivate.GetBindingDisplayString().Split(' ')[0]}]",
                $"Drop Wallet : [{Plugin.InputActionsInstance.WalletDrop.GetBindingDisplayString().Split(' ')[0]}]",
                $"Sell Wallet : [{Plugin.InputActionsInstance.WalletSell.GetBindingDisplayString().Split(' ')[0]}]"
            });
        }
    }
    public void OnDisable() {
        if (isHeld) {
            if (!IsHost) return;
            SetTargetClientRpc(-1);
        }
    }
}