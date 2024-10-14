using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;

namespace CodeRebirth.src.Content.Items;
public class Wallet : GrabbableObject {  // todo: fix only host being able to pick it up
    public InteractTrigger trigger = null!;
    private RaycastHit hit;
    private ScanNodeProperties scanNode = null!;
    private SkinnedMeshRenderer skinnedMeshRenderer = null!;
    private NetworkVariable<bool> isInteractable = new NetworkVariable<bool>(true);
    public enum WalletModes {
        Held,
        None,
        Sold,
    }
    private WalletModes walletMode = WalletModes.None;
    private PlayerControllerB? walletHeldBy;

    public override void Start() {
        StartBaseImportant();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        scanNode = GetComponentInChildren<ScanNodeProperties>();
        this.grabbable = false;
        this.grabbableToEnemies = false;
        trigger.onInteract.AddListener(OnInteract);
        // Set initial state of the trigger interactable
        trigger.interactable = isInteractable.Value;

        // Add a listener to react to changes in the NetworkVariable
        isInteractable.OnValueChanged += OnInteractableChanged;
    }

    private void OnInteractableChanged(bool oldValue, bool newValue)
    {
        trigger.interactable = newValue;
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
    }

    public void OnInteract(PlayerControllerB player) {
        if (GameNetworkManager.Instance.localPlayerController != player) return;
        if (player.GetCRPlayerData().holdingWallet) return;
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
        var interactRay = new Ray(walletHeldBy.gameplayCamera.transform.position, walletHeldBy.gameplayCamera.transform.forward);
        if (Physics.Raycast(interactRay, out hit, walletHeldBy.grabDistance, walletHeldBy.interactableObjectsMask) && hit.collider.gameObject.layer != 8) {
            Money coin = hit.collider.transform.gameObject.GetComponent<Money>();
            if (coin == null) return;
            coin.customGrabTooltip = $"{Plugin.InputActionsInstance.WalletActivate.GetBindingDisplayString().Split(' ')[0]} to Collect!";
        }
        HandleItemActivate(walletHeldBy);
        HandleItemDrop(walletHeldBy);
        HandleItemSell();
        var playerPocketPosition = GameNetworkManager.Instance.localPlayerController.transform.position + GameNetworkManager.Instance.localPlayerController.transform.up * 1f + GameNetworkManager.Instance.localPlayerController.transform.right * 0.25f + GameNetworkManager.Instance.localPlayerController.transform.forward * 0.05f;
        if (Vector3.Distance(playerPocketPosition, this.transform.position) > 0.5f) {
            // Calculate the interpolation factor, ensuring it remains between 0 and 1
            float step = Mathf.Clamp01(Time.deltaTime * 10f);

            // Update this.transform.position to move towards playerPocketPosition.position
            this.transform.position = Vector3.Lerp(this.transform.position, playerPocketPosition, step);
        }
        // Target rotation is the local player's rotation modified by the specified rotations
        Quaternion rotationLeft = Quaternion.Euler(0, 90, 0);
        Quaternion rotationForward = Quaternion.Euler(15, 0, 0);
        Quaternion combinedRotation = rotationLeft * rotationForward;
        Quaternion targetRotation = GameNetworkManager.Instance.localPlayerController.transform.rotation * combinedRotation;

        // Current rotation of 'this.transform'
        Quaternion currentRotation = this.transform.rotation;

        // Calculate the angular difference
        float angleDifference = Quaternion.Angle(currentRotation, targetRotation);

        // Check if the angular difference is greater than a certain threshold, e.g., 30 degrees
        if (angleDifference > 30f) {
            // Apply the new rotation if the condition is met
            this.transform.rotation = targetRotation;
        }
    }

    public void HandleItemSell() {
        if (!Plugin.InputActionsInstance.WalletSell.triggered) return;
        if (FindObjectOfType<DepositItemsDesk>() != null && walletHeldBy != null)
        {
            DepositItemsDesk depositItemsDesk = FindObjectOfType<DepositItemsDesk>();
            if (Vector3.Distance(FindObjectOfType<DepositItemsDesk>().triggerCollider.transform.position, walletHeldBy.transform.position) < 8f || depositItemsDesk.triggerCollider.bounds.Contains(walletHeldBy.transform.position))
            {
                if (depositItemsDesk.deskObjectsContainer.GetComponentsInChildren<GrabbableObject>().Length >= 12 || depositItemsDesk.inGrabbingObjectsAnimation)
                {
                    return;
                }
                if (GameNetworkManager.Instance != null && walletHeldBy == GameNetworkManager.Instance.localPlayerController)
                {
                    depositItemsDesk.AddObjectToDeskServerRpc(GetComponent<NetworkObject>());
                    walletMode = WalletModes.Sold;
                    DropWallet();
                    this.transform.position = new Vector3(-29.3048f, -1.2182f, -31.4077f);
                }
                return;
            }
        }
    }

    public void HandleItemDrop(PlayerControllerB walletHeldBy)
    {
        if (!Plugin.InputActionsInstance.WalletDrop.triggered || walletHeldBy.inTerminalMenu) return;
        DropWallet();
    }

    public void DropWallet()
    {
        if (IsHost)
        {
            SetTargetClientRpc(-1);
        }
        else
        {
            SetTargetServerRpc(-1);
        }
    }

    public void HandleItemActivate(PlayerControllerB walletHeldBy)
    {
        if (!Plugin.InputActionsInstance.WalletActivate.triggered) return;
        var interactRay = new Ray(walletHeldBy.gameplayCamera.transform.position, walletHeldBy.gameplayCamera.transform.forward);
        if (Physics.Raycast(interactRay, out hit, walletHeldBy.grabDistance, walletHeldBy.interactableObjectsMask) && hit.collider.gameObject.layer != 8)
        {
            Money coin = hit.collider.transform.gameObject.GetComponent<Money>();
            if (coin == null) return;
            GetComponentInChildren<AudioSource>().Play();
            UpdateScrapValueServerRpc(coin.scrapValue);
            NetworkObject obj = coin.gameObject.GetComponent<NetworkObject>();
            float newblendShapeWeight = Mathf.Clamp(skinnedMeshRenderer.GetBlendShapeWeight(0) + 20f, 0, 300);
            IncreaseBlendShapeWeightServerRpc(newblendShapeWeight);
            UpdateToolTips();
            DestroyObjectServerRpc(obj);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseBlendShapeWeightServerRpc(float newblendShapeWeight)
    {
        IncreaseBlendShapeWeightClientRpc(newblendShapeWeight);
    }
    [ClientRpc]
    public void IncreaseBlendShapeWeightClientRpc(float newblendShapeWeight)
    {
        this.itemProperties.weight += 0.01f;
        skinnedMeshRenderer.SetBlendShapeWeight(0, newblendShapeWeight);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateScrapValueServerRpc(int valueToAdd)
    {
        this.scrapValue += valueToAdd;
        UpdateScrapValueClientRpc(scrapValue);
    }

    [ClientRpc]
    private void UpdateScrapValueClientRpc(int newScrapValue)
    {
        this.scrapValue = newScrapValue;
        scanNode.scrapValue = scrapValue;
        scanNode.subText = $"Value: {scrapValue}";
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyObjectServerRpc(NetworkObjectReference obj)
    {
        DestroyObjectClientRpc(obj);
    }

    [ClientRpc]
    public void DestroyObjectClientRpc(NetworkObjectReference obj)
    {
        if (obj.TryGet(out NetworkObject netObj))
        {
            DestroyObject(netObj);
        }
        else
        {
            Plugin.Logger.LogDebug("COULDNT FIND THE OBJECT TO DESTROY");
        }
    }

    public void DestroyObject(NetworkObject netObj)
    {
        Destroy(netObj.gameObject.GetComponent<Money>().radarIcon.gameObject);
        if (netObj.IsOwnedByServer && netObj.IsSpawned && netObj.IsOwner) netObj.Despawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID)
    {
        SetTargetClientRpc(PlayerID);
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        if (!IsServer || walletMode == WalletModes.Held) return;
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (StartOfRound.Instance.shipBounds.bounds.Contains(this.transform.position) && !isInShipRoom)
        {
            this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            isInShipRoom = true;
            isInElevator = true;
        }
        else if (!StartOfRound.Instance.shipBounds.bounds.Contains(this.transform.position) && isInShipRoom)
        {
            this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
            isInShipRoom = false;
            isInElevator = false;
        }
    }



    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID)
    {
        if (PlayerID == -1)
        {
            PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
            if (IsServer)
            {
                if (isInShipRoom)
                {
                    this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
                }
                else
                {
                    this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
                }
            }
            Plugin.ExtendedLogging($"Clearing target on {this}");
            
            isHeld = false;
            if (walletMode == WalletModes.Sold)
            {
                SetInteractable(false);
            }
            if (walletHeldBy != null)
            {
                // Perform a raycast from the camera to find the drop position
                var interactRay = new Ray(walletHeldBy.gameplayCamera.transform.position, walletHeldBy.gameplayCamera.transform.forward);
                if (Physics.Raycast(interactRay, out RaycastHit hit, walletHeldBy.grabDistance + 3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
                {
                    // Set the wallet's position to the hit point with an offset away from the surface
                    this.transform.position = hit.point + hit.normal * 0.1f;
                    this.transform.position += Vector3.down * 0.05f;
                }
                else
                {
                    this.transform.position = walletHeldBy.transform.position + walletHeldBy.transform.forward * 0.3f;
                }
                SetInteractable(true);
                walletHeldBy.carryWeight -= (this.itemProperties.weight - 1);
                walletHeldBy.GetCRPlayerData().holdingWallet = false;
                walletMode = WalletModes.None;
                walletHeldBy = null;
            }
            return;
        }

        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[PlayerID];
        if (player == null)
        {
            Plugin.Logger.LogWarning($"Invalid player index: {PlayerID}");
            return;
        }
        walletMode = WalletModes.Held;
        SetInteractable(false);
        walletHeldBy = player;
        this.transform.position = player.transform.position + player.transform.up * 1f + player.transform.right * 0.25f + player.transform.forward * 0.05f;
        walletHeldBy.carryWeight += (this.itemProperties.weight - 1f);
        if (walletHeldBy == GameNetworkManager.Instance.localPlayerController && !walletHeldBy.GetCRPlayerData().holdingWallet)
        {
            DialogueSegment dialogue = new DialogueSegment
            {
                    speakerText = "Wallet Tooltips",
                    bodyText = "L to Drop, E to Grab, LMB to Grab coins, O at company Counter to sell.",
                    waitTime = 7f
            };
            HUDManager.Instance.ReadDialogue([dialogue]);
            walletHeldBy.GetCRPlayerData().holdingWallet = true;
        }
        walletHeldBy.GetCRPlayerData().holdingWallet = true;
        // Apply the rotations
        Quaternion rotationLeft = Quaternion.Euler(0, 180, 0);
        Quaternion rotationForward = Quaternion.Euler(15, 0, 0);

        // Combine the rotations: rotate 180 degrees left first, then rotate forward by 15 degrees
        this.transform.rotation = rotationLeft * rotationForward;

        isHeld = true;
        this.transform.SetParent(walletHeldBy.transform, true);
        if (IsServer)
        {
            GetComponent<NetworkObject>().ChangeOwnership(player.actualClientId);
        }
        UpdateToolTips();
    }

    private void SetInteractable(bool value)
    {
        if (!IsHost)
        {
            isInteractable.Value = value;
        }
        else
        {
            SetInteractableServerRpc(value);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetInteractableServerRpc(bool value)
    {
        isInteractable.Value = value;
    }
    
    public void UpdateToolTips()
    {
        if (walletHeldBy == null || GameNetworkManager.Instance.localPlayerController != walletHeldBy) return;
        HUDManager.Instance.ClearControlTips();
        if (walletMode == WalletModes.Held)
        {
            HUDManager.Instance.ChangeControlTipMultiple(new string[]
            {
                $"Use Wallet : [{Plugin.InputActionsInstance.WalletActivate.GetBindingDisplayString().Split(' ')[0]}]",
                $"Drop Wallet : [{Plugin.InputActionsInstance.WalletDrop.GetBindingDisplayString().Split(' ')[0]}]",
                $"Sell Wallet : [{Plugin.InputActionsInstance.WalletSell.GetBindingDisplayString().Split(' ')[0]}]"
            });
        }
    }
    public void OnDisable()
    {
        if (isHeld)
        {
            if (!IsHost) return;
            SetTargetClientRpc(-1);
        }
    }
}