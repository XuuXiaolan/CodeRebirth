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
    public float rightMult;
    public float upMult;
    private ScanNodeProperties scanNode;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    public enum WalletModes {
        Held,
        None,
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
            SetTargetClientRpc(System.Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player), true);
        } else {
            SetTargetServerRpc(System.Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player), true);
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
                    DropWallet(true);
                    this.transform.position = new Vector3(-29.3048f, -0.7182f, -31.4077f);
                }
                return;
            }
        }
    }

    public void HandleItemDrop() {
        if (!Plugin.InputActionsInstance.WalletDrop.triggered) return;
        DropWallet(false);
    }

    public void DropWallet(bool sold) {
        if (IsHost) {
            SetTargetClientRpc(-1, sold);
        } else {
            SetTargetServerRpc(-1, sold);
        }
    }

    public void HandleItemActivate() {
        if (!Plugin.InputActionsInstance.WalletActivate.triggered) return;
        UpdateToolTips();
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
            DestroyObjectServerRpc(obj);
        }
        Plugin.Logger.LogInfo($"Scrap Value: {scrapValue}");
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
        }
    }

    public void DestroyObject(NetworkObject netObj) {
        if (netObj.IsOwnedByServer && netObj.IsSpawned && netObj.IsOwner) netObj.Despawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID, bool sold) {
        SetTargetClientRpc(PlayerID, sold);
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
    public void SetTargetClientRpc(int PlayerID, bool sold) {
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
            isHeld = false;
            if (sold) {
                trigger.interactable = false;
            } else {
                // Perform a raycast from the camera to find the drop position
                var interactRay = new Ray(walletHeldBy.gameplayCamera.transform.position, walletHeldBy.gameplayCamera.transform.forward);
                if (Physics.Raycast(interactRay, out RaycastHit hit, walletHeldBy.grabDistance + 2f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
                    // Set the wallet's position to the hit point with an offset away from the surface
                    this.transform.position = hit.point + hit.normal * 0.1f;

                    // Check if there is anything below the hit point within 4f
                    if (!CheckBelowWallet()) {
                        // If nothing is below, simulate a fall
                        StartCoroutine(SimulateFall());
                    }
                } else {
                    // If no valid drop position is found, drop wallet from under player
                    this.transform.position = walletHeldBy.transform.position + walletHeldBy.transform.forward * 0.1f;

                    // Check if there is anything below the player's position within 4f
                    if (!CheckBelowWallet()) {
                        // If nothing is below, simulate a fall
                        StartCoroutine(SimulateFall());
                    }
                }
                trigger.interactable = true;
            }
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
        this.transform.position = player.transform.position + player.transform.up * upMult + player.transform.right * rightMult;
        this.transform.rotation = Quaternion.identity;
        isHeld = true;
        this.transform.SetParent(walletHeldBy.transform, true);
        this.transform.position = walletHeldBy.transform.position;
        if (IsServer) {
            GetComponent<NetworkObject>().ChangeOwnership(player.actualClientId);
        }
        UpdateToolTips();
    }

    private bool CheckBelowWallet() {
        BoxCollider boxCollider = this.transform.Find("WalletChild").GetComponentInChildren<BoxCollider>();
        Vector3[] corners = GetBoundingBoxCorners(boxCollider);

        foreach (Vector3 corner in corners) {
            if (Physics.Raycast(corner, Vector3.down, out RaycastHit downHit, 4f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
                return true;
            }
        }
        return false;
    }

    private IEnumerator SimulateFall() {
        float fallSpeed = 2f;
        float maxFallDistance = 4f;
        float distanceFallen = 0f;

        BoxCollider boxCollider = this.transform.Find("WalletChild").GetComponentInChildren<BoxCollider>();
        Vector3[] corners = GetBoundingBoxCorners(boxCollider);

        while (distanceFallen < maxFallDistance) {
            bool hitDetected = false;

            foreach (Vector3 corner in corners) {
                if (Physics.Raycast(corner, Vector3.down, out RaycastHit downHit, 0.1f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
                    // If we hit something below, adjust the position and stop falling
                    this.transform.position = downHit.point + Vector3.up * 0.05f;
                    hitDetected = true;
                    break;
                }
            }

            if (hitDetected) {
                yield break;
            }

            // Move the wallet down
            this.transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            distanceFallen += fallSpeed * Time.deltaTime;

            // Update the corners
            corners = GetBoundingBoxCorners(boxCollider);

            yield return null;
        }

        // If we've fallen the maximum distance and still not hit anything, leave the wallet at its last position
        if (distanceFallen >= maxFallDistance) {
            Plugin.Logger.LogInfo($"Wallet has fallen the maximum distance without hitting anything.");
        }
    }

    private Vector3[] GetBoundingBoxCorners(BoxCollider boxCollider) {
        Vector3 center = boxCollider.center;
        Vector3 extents = boxCollider.size / 2;

        Vector3[] corners = new Vector3[8];
        corners[0] = this.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z));
        corners[1] = this.transform.TransformPoint(center + new Vector3(extents.x, -extents.y, -extents.z));
        corners[2] = this.transform.TransformPoint(center + new Vector3(extents.x, -extents.y, extents.z));
        corners[3] = this.transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, extents.z));
        corners[4] = this.transform.TransformPoint(center + new Vector3(-extents.x, extents.y, -extents.z));
        corners[5] = this.transform.TransformPoint(center + new Vector3(extents.x, extents.y, -extents.z));
        corners[6] = this.transform.TransformPoint(center + new Vector3(extents.x, extents.y, extents.z));
        corners[7] = this.transform.TransformPoint(center + new Vector3(-extents.x, extents.y, extents.z));

        return corners;
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
}