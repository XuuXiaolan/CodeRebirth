using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.AI;
using CodeRebirth.Util.PlayerManager;

namespace CodeRebirth.ScrapStuff;
public class Hoverboard : GrabbableObject, IHittable {
    public Rigidbody hb = null!;
    public InteractTrigger trigger = null!;
    private bool turnedOn = false;
    public float mult;
    public GameObject hoverboardSeat = null!;
    public Transform[] anchors = new Transform[4];
    private PlayerControllerB? playerControlling;
    private RaycastHit[] hits = new RaycastHit[4];
    private bool _isHoverForwardHeld = false;
    private bool jumpCooldown = true;
    public Transform hoverboardChild = null!;
    private HoverboardTypes hoverboardType = HoverboardTypes.Regular;
    private float playerMovementSpeed = 0f;
    public enum HoverboardMode {
        None,
        Held,
        Mounted,
    }
    public enum HoverboardTypes {
        Regular,
        // Eventually wanna make other types of hoverboards
    }
    private HoverboardMode hoverboardMode;
    private Quaternion resetChildRotation;
    private bool isAdjusting = false;
    private Quaternion targetRotation;
    // Variables to store initial anchor positions and rotations
    private Vector3[] initialAnchorPositions = new Vector3[4];
    private Quaternion[] initialAnchorRotations = new Quaternion[4];
    private bool collidersIgnored = false;

    public override void Start() {
        StartBaseImportant();

        System.Random random = new System.Random();
        int hoverboardTypeIndex = random.Next(0, 3);
        switch (hoverboardTypeIndex) {
            case 0:
                hoverboardType = HoverboardTypes.Regular;
                break;
            default:
                break;
        }
        switch (hoverboardType) {
            case HoverboardTypes.Regular:
                break;
            default:
                break;
        }
        SwitchModeExtension(true);
        resetChildRotation = hoverboardChild.rotation;
        if (IsServer) {
            PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
            if (StartOfRound.Instance.shipBounds.bounds.Contains(this.hoverboardChild.position) && !isInShipRoom) {
                this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
                isInShipRoom = true;
                isInElevator = true;
            } else if (!StartOfRound.Instance.shipBounds.bounds.Contains(this.hoverboardChild.position) && isInShipRoom) {
                this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
                isInShipRoom = false;
                isInElevator = false;
            }
        }
        // Save initial positions and rotations of the anchors
        for (int i = 0; i < anchors.Length; i++) {
            initialAnchorPositions[i] = anchors[i].localPosition;
            initialAnchorRotations[i] = anchors[i].localRotation;
        }
        if (!IsHost) return;
        SetHoverboardStateClientRpc(0);
    }

    public void OnInteract(PlayerControllerB player) {
        if (GameNetworkManager.Instance.localPlayerController != player) return;
        if (hoverboardMode == HoverboardMode.None) {
            StartCoroutine(OnInteractCoroutine(player));
        }
    }

    public IEnumerator OnInteractCoroutine(PlayerControllerB player) {
        if (IsHost) {
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            yield return new WaitUntil(() => playerControlling == player);
            SetHoverboardStateClientRpc(2);
        } else {
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            yield return new WaitUntil(() => playerControlling == player);
            SetHoverboardStateServerRpc(2);
        }
    }

    public void ModeHandler(InputAction.CallbackContext context) {
        if (hoverboardMode == HoverboardMode.None || playerControlling == null || playerControlling != GameNetworkManager.Instance.localPlayerController) return;
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame) {
            if (hoverboardMode == HoverboardMode.Mounted) {
                if (IsHost) {
                    SetHoverboardStateClientRpc(1);
                } else {
                    SetHoverboardStateServerRpc(1);
                }
            } else if (hoverboardMode == HoverboardMode.Held) {
                if (IsHost) {
                    SetHoverboardStateClientRpc(2);
                } else {
                    SetHoverboardStateServerRpc(2);
                }
            }
            Plugin.InputActionsInstance.SwitchMode.performed += ModeHandler;
        }
    }

    public void OnHoverForward(InputAction.CallbackContext context) {
        if (GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame) {
            if (IsHost) {
                SetHoverboardHeldClientRpc(true);
            } else {
                SetHoverboardHeldServerRpc(true);
            }
        } else if (btn.wasReleasedThisFrame) {
            if (IsHost) {
                SetHoverboardHeldClientRpc(false);
            } else {
                SetHoverboardHeldServerRpc(false);
            }
        }
    }

    public void MovementHandler(InputAction.CallbackContext context) {
        if (GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame) {
            HandleMovement();
        }
    }

    public void FixedUpdate() {
        if (playerControlling == null) return;
        if (GameNetworkManager.Instance.localPlayerController == playerControlling) {
            if (hoverboardMode != HoverboardMode.Held && turnedOn) {
                for (int i = 0; i < 4; i++) {
                    ApplyForce(anchors[i], hits[i]);
                }
            } // make the force only apply to raycasts that are 0.3 to 1 of the dot product of the up vector 
        }
    }

    public override void Update() {
        base.Update();
        if (HandleDropping()) return;
        if ((hoverboardMode == HoverboardMode.Held || hoverboardMode == HoverboardMode.Mounted) && playerControlling == null) {
            DropHoverboard();
            return;
        }
        if (playerControlling == null) return;
        if (playerControlling == GameNetworkManager.Instance.localPlayerController && Vector3.Distance(hoverboardChild.position, playerControlling.transform.position) > 5) {
            if (IsHost) {
                SetHoverboardStateClientRpc(1);
            } else {
                SetHoverboardStateServerRpc(1);
            }
            return;
        }
        if (playerControlling == GameNetworkManager.Instance.localPlayerController && hoverboardMode == HoverboardMode.Mounted) {
            Vector2 currentMouseDelta = Plugin.InputActionsInstance.MouseDelta.ReadValue<Vector2>();

            float turnSpeed = 0.1f; // Adjust the turn speed as needed
            float turnAmount = currentMouseDelta.x * turnSpeed;

            float turnAmountY = currentMouseDelta.y * turnSpeed * 0.8f;
            Vector2 inputVector = new Vector2(0, turnAmountY);

            // Call CalculateSmoothLookingInput to handle the camera rotation
            CalculateVerticalLookingInput(inputVector, playerControlling);

            hoverboardChild.Rotate(Vector3.up, turnAmount);
        }
        if (hoverboardMode == HoverboardMode.Mounted && playerControlling.transform.GetParent() != hoverboardSeat.transform) {
            playerControlling.transform.SetParent(hoverboardSeat.transform, true);
            Plugin.Logger.LogInfo($"Setting parent of {playerControlling} to {playerControlling.transform.GetParent()}");
        }
        if (playerControlling.inAnimationWithEnemy || playerControlling.isClimbingLadder || (hoverboardMode == HoverboardMode.Mounted && playerControlling.isClimbingLadder)) {
            DropHoverboard();
            return;
        }
        if (hoverboardMode == HoverboardMode.Mounted) {
            if (Vector3.Distance(hoverboardSeat.transform.position, playerControlling.transform.position) > 0.01f) {
                playerControlling.transform.position = Vector3.Lerp(playerControlling.transform.position, hoverboardSeat.transform.position, Time.deltaTime * 5f);
            }
            if (_isHoverForwardHeld) {
                hb.AddForce(Vector3.zero + hoverboardChild.right * 25f * (playerControlling.isSprinting ? 1.6f : 1f), ForceMode.Acceleration);
            }
            if (!isAdjusting)
                CheckIfUpsideDown();
            playerControlling.ResetFallGravity();
        }
    }

    public override void LateUpdate() {
        base.LateUpdate();
        if (!IsServer || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving && hoverboardMode != HoverboardMode.Held) return;
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (StartOfRound.Instance.shipBounds.bounds.Contains(this.hoverboardChild.position) && !isInShipRoom) {
            this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            isInShipRoom = true;
            isInElevator = true;
        } else if (!StartOfRound.Instance.shipBounds.bounds.Contains(this.hoverboardChild.position) && isInShipRoom) {
            this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
            isInShipRoom = false;
            isInElevator = false;
        }
    }

    public bool HandleDropping() {
        if (playerControlling == null || playerControlling != GameNetworkManager.Instance.localPlayerController || !Plugin.InputActionsInstance.DropHoverboard.triggered) return false;
        DropHoverboard();
        return true;
    }

    public void DropHoverboard() {
        if (IsHost) {
            SetHoverboardStateClientRpc(0);
            SetTargetClientRpc(-1);
        } else {
            SetHoverboardStateServerRpc(0);
            SetTargetServerRpc(-1);
        }
        SwitchModeExtension(true);
    }

    public bool IsPointOnNavMesh(Vector3 position, float maxDistance) {
        NavMeshHit hit;
        bool hasNavMesh = NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas);
        return hasNavMesh;
    }

    private void HandleMovement() {
        if (playerControlling == null) return;
        Vector3 forceDirection = Vector3.zero;
        float moveForce = 75f;

        if (Plugin.InputActionsInstance.HoverLeft.WasPressedThisFrame())
            forceDirection += hoverboardChild.forward;

        if (Plugin.InputActionsInstance.HoverRight.WasPressedThisFrame())
            forceDirection -= hoverboardChild.forward;

        if (Plugin.InputActionsInstance.HoverBackward.WasPressedThisFrame())
            forceDirection -= hoverboardChild.right;

        if (Plugin.InputActionsInstance.HoverForward.WasPressedThisFrame())
            forceDirection += hoverboardChild.right;

        if (Plugin.InputActionsInstance.HoverUp.WasPressedThisFrame() && jumpCooldown) {
            jumpCooldown = false;
            forceDirection += hoverboardChild.up;
            moveForce = 1000f;
            StartCoroutine(JumpTimerStart());
        }

        if (forceDirection == Vector3.zero) return;
        hb.AddForce(forceDirection * moveForce, ForceMode.Acceleration);
    }

    public IEnumerator JumpTimerStart() {
        yield return new WaitForSeconds(2f);
        jumpCooldown = true;
    }

    // ApplyForce method with added debug logs
    public void ApplyForce(Transform anchor, RaycastHit hit) {
        if (Physics.Raycast(anchor.position, -anchor.up, out hit, 1000f, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
            float force = Mathf.Clamp(Mathf.Abs(1 / (hit.point.y - anchor.position.y)), 0, this.isInShipRoom ? 3f : 100f);
            // Debug log for force and anchor positions
            hb.AddForceAtPosition(hoverboardChild.up * force * mult, anchor.position, ForceMode.Acceleration);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID) {
        SetTargetClientRpc(PlayerID);
    }

    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID) {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (PlayerID == -1 && playerControlling != null) {
            playerControlling.playerActions.Movement.Jump.Enable();
            playerControlling.playerActions.Movement.Look.Enable();
            playerControlling.movementSpeed = playerMovementSpeed;
            if (playerControlling.isInHangarShipRoom || StartOfRound.Instance.shipBounds.bounds.Contains(this.playerControlling.transform.position)) {
                playerControlling.transform.SetParent(playerControlling.playersManager.elevatorTransform, true);
            } else {
                playerControlling.transform.SetParent(playerControlling.playersManager.playersContainer, true);
            }
            playerControlling = null;
            Plugin.Logger.LogInfo($"Clearing target on {this}");
            return;
        }
        if (StartOfRound.Instance.allPlayerScripts[PlayerID] == null) {
            Plugin.Logger.LogInfo($"Index invalid! {PlayerID}");
            return;
        }
        if (playerControlling == StartOfRound.Instance.allPlayerScripts[PlayerID]) {
            Plugin.Logger.LogInfo($"{this} already targeting: {playerControlling.playerUsername}");
            return;
        }
        hoverboardChild.rotation = resetChildRotation;
        playerControlling = StartOfRound.Instance.allPlayerScripts[PlayerID];
        if (IsServer) {
            networkObject.ChangeOwnership(playerControlling.actualClientId);
        }
        CodeRebirthPlayerManager localPlayerManager = playerControlling.gameObject.GetComponent<CodeRebirthPlayerManager>();
        if (playerControlling == GameNetworkManager.Instance.localPlayerController && !localPlayerManager.ItemUsages[CodeRebirthItemUsages.Hoverboard]) {
            DialogueSegment dialogue = new DialogueSegment {
                    speakerText = "Hoverboard Tooltips",
                    bodyText = "C to Drop, E to Mount, F to Switch between Held and Mounted mode, Space to Jump, Shift to activate Boost.",
                    waitTime = 7f
            };
            HUDManager.Instance.ReadDialogue([dialogue]);
        }
        localPlayerManager.ItemUsages[CodeRebirthItemUsages.Hoverboard] = true;
        playerControlling.transform.position = hoverboardSeat.transform.position;
        playerControlling.transform.rotation = hoverboardSeat.transform.rotation * Quaternion.Euler(0, 90, 0);
        Plugin.Logger.LogInfo($"{this} setting target to: {playerControlling.playerUsername}");
    }

    [ServerRpc(RequireOwnership = false)]
    internal void SetHoverboardHeldServerRpc(bool held) {
        SetHoverboardHeldClientRpc(held);
    }

    [ClientRpc]
    internal void SetHoverboardHeldClientRpc(bool held) {
        _isHoverForwardHeld = held;
    }

    [ServerRpc(RequireOwnership = false)]
    internal void SetHoverboardStateServerRpc(int state) {
        SetHoverboardStateClientRpc(state);
    }

    [ClientRpc]
    internal void SetHoverboardStateClientRpc(int state) {
        if (playerControlling == null && hoverboardMode != HoverboardMode.None) {
            Plugin.Logger.LogInfo($"Player controlling is null for me...");
        }
        switch (state) {
            case 0:
                SwitchToNothing();
                break;
            case 1:
                if (playerControlling != null) {
                    SwitchToHeld(playerControlling);
                } else {
                    Plugin.Logger.LogError($"Player controlling is null when switching to held...");
                }
                break;
            case 2:
                if (playerControlling != null) {
                    SwitchToMounted(playerControlling);
                } else {
                    Plugin.Logger.LogError($"Player controlling is null when switching to mounted...");
                }
                break;
            default:
                Plugin.Logger.LogFatal("Invalid Hoverboard state!");
                break;
        }
        HandleToolTips();
    }
    public void SwitchToMounted(PlayerControllerB playerCurrentlyControlling) {
        if (hoverboardMode == HoverboardMode.Held) {
            hoverboardChild.position = playerCurrentlyControlling.transform.position;
            if (IsServer) {
                PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
                if (isInShipRoom) {
                    this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
                } else {
                    this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
                }
            }
        }
        // Reset hoverboardChild's rotation and position
        hoverboardChild.rotation = resetChildRotation;
        if (hoverboardMode == HoverboardMode.Held) {
            hoverboardChild.rotation = playerCurrentlyControlling.transform.rotation * Quaternion.Euler(0, -90, 0);
        }
        hoverboardChild.position += Vector3.up * 0.3f;
        for (int i = 0; i < anchors.Length; i++) {
            anchors[i].localPosition = initialAnchorPositions[i];
            anchors[i].localRotation = initialAnchorRotations[i];
        }
        // Debug log for position and rotation
        playerCurrentlyControlling.transform.SetParent(hoverboardSeat.transform, true);
        playerCurrentlyControlling.gameObject.GetComponent<CodeRebirthPlayerManager>().ridingHoverboard = true;
        hoverboardMode = HoverboardMode.Mounted;
        if (!collidersIgnored) SetupCollidersIgnoringOrIncluding(true);
        playerCurrentlyControlling.playerActions.Movement.Look.Disable();
        playerCurrentlyControlling.playerActions.Movement.Jump.Disable();
        playerMovementSpeed = playerCurrentlyControlling.movementSpeed;
        playerCurrentlyControlling.movementSpeed = 0f;
        if (playerCurrentlyControlling == GameNetworkManager.Instance.localPlayerController) {
            StartCoroutine(TurnOnHoverboard());
        }
        hb.useGravity = true;
        hb.isKinematic = false;
        trigger.interactable = false;
        SwitchModeExtension(false);
    }
    public void SwitchToHeld(PlayerControllerB playerCurrentlyControlling) {
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (playerCurrentlyControlling.isInHangarShipRoom) {
            playerCurrentlyControlling.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
        } else {
            playerCurrentlyControlling.transform.SetParent(realPlayer.playersManager.playersContainer, true);
        }
        if (IsServer) {
            this.transform.SetParent(playerCurrentlyControlling.transform, true);
        }
        playerCurrentlyControlling.gameObject.GetComponent<CodeRebirthPlayerManager>().ridingHoverboard = false;
        hoverboardMode = HoverboardMode.Held;
        playerCurrentlyControlling.playerActions.Movement.Look.Enable();
        playerCurrentlyControlling.playerActions.Movement.Jump.Enable();
        playerCurrentlyControlling.movementSpeed = playerMovementSpeed;
        hoverboardChild.position = playerCurrentlyControlling.transform.position + playerCurrentlyControlling.transform.right * 0.7f + playerCurrentlyControlling.transform.up * 1f;
        Quaternion rotationOffset = Quaternion.Euler(180, 180, -90); // Adjust to match correct facing direction
        hoverboardChild.rotation = playerCurrentlyControlling.transform.rotation * rotationOffset;
        _isHoverForwardHeld = false;
        turnedOn = false;
        hb.useGravity = false;
        hb.isKinematic = true;
    }
    public void SwitchToNothing() {
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        hoverboardMode = HoverboardMode.None;
        if (IsServer) {
            if (isInShipRoom) {
                this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            } else {
                this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
            }
        }
        if (collidersIgnored) SetupCollidersIgnoringOrIncluding(false);
        if (playerControlling != null) playerControlling.gameObject.GetComponent<CodeRebirthPlayerManager>().ridingHoverboard = false;
        turnedOn = false;
        _isHoverForwardHeld = false;
        hb.useGravity = true;
        hb.isKinematic = false;
        trigger.interactable = true;
    }
    public IEnumerator TurnOnHoverboard() {
        yield return new WaitForSeconds(1f);
        if (hoverboardMode != HoverboardMode.Mounted) yield break;
        if (IsHost) {
            TurnOnHoverboardClientRpc();
        } else {
            TurnOnHoverboardServerRpc();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void TurnOnHoverboardServerRpc() {
        TurnOnHoverboardClientRpc();
    }

    [ClientRpc]
    public void TurnOnHoverboardClientRpc() {
        turnedOn = true;
    }

    public override void FallWithCurve() {
        return;
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

    public void SwitchModeExtension(bool SwitchingOff) {
        if (SwitchingOff) {
            Plugin.InputActionsInstance.HoverForward.performed -= OnHoverForward;
            Plugin.InputActionsInstance.HoverForward.canceled -= OnHoverForward;
            Plugin.InputActionsInstance.HoverLeft.performed -= MovementHandler;
            Plugin.InputActionsInstance.HoverRight.performed -= MovementHandler;
            Plugin.InputActionsInstance.HoverBackward.performed -= MovementHandler;
            Plugin.InputActionsInstance.HoverForward.performed -= MovementHandler;
            Plugin.InputActionsInstance.HoverUp.performed -= MovementHandler;
            Plugin.InputActionsInstance.SwitchMode.performed -= ModeHandler;
        } else {
            Plugin.InputActionsInstance.HoverForward.performed += OnHoverForward;
            Plugin.InputActionsInstance.HoverForward.canceled += OnHoverForward;
            Plugin.InputActionsInstance.HoverLeft.performed += MovementHandler;
            Plugin.InputActionsInstance.HoverRight.performed += MovementHandler;
            Plugin.InputActionsInstance.HoverBackward.performed += MovementHandler;
            Plugin.InputActionsInstance.HoverForward.performed += MovementHandler;
            Plugin.InputActionsInstance.HoverUp.performed += MovementHandler;
            Plugin.InputActionsInstance.SwitchMode.performed += ModeHandler;
        }
    }

    public void HandleToolTips() {
        if (playerControlling == null || GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        HUDManager.Instance.ClearControlTips();
        if (hoverboardMode == HoverboardMode.Mounted) {
            HUDManager.Instance.ChangeControlTipMultiple(new string[] {
                $"Dismount Hoverboard : [{Plugin.InputActionsInstance.DropHoverboard.GetBindingDisplayString().Split(' ')[0]}]",
                $"Move Hoverboard : [{Plugin.InputActionsInstance.HoverForward.GetBindingDisplayString().Split(' ')[0]}][{Plugin.InputActionsInstance.HoverLeft.GetBindingDisplayString().Split(' ')[0]}][{Plugin.InputActionsInstance.HoverBackward.GetBindingDisplayString().Split(' ')[0]}][{Plugin.InputActionsInstance.HoverRight.GetBindingDisplayString().Split(' ')[0]}]",
                $"Up Boost : [{Plugin.InputActionsInstance.HoverUp.GetBindingDisplayString().Split(' ')[0]}]",
                $"Switch Mode (Mounted) : [{Plugin.InputActionsInstance.SwitchMode.GetBindingDisplayString().Split(' ')[0]}]"
            });
        } else if (hoverboardMode == HoverboardMode.Held) {
            HUDManager.Instance.ChangeControlTipMultiple(new string[] {
                $"Drop Hoverboard : [{Plugin.InputActionsInstance.DropHoverboard.GetBindingDisplayString().Split(' ')[0]}]",
                $"Switch Mode (Held) : [{Plugin.InputActionsInstance.SwitchMode.GetBindingDisplayString().Split(' ')[0]}]"
            });
        }
    }

    private void CalculateVerticalLookingInput(Vector2 inputVector, PlayerControllerB playerCurrentlyControlling) {
        if (!playerCurrentlyControlling.smoothLookEnabledLastFrame) {
            playerCurrentlyControlling.smoothLookEnabledLastFrame = true;
            playerCurrentlyControlling.smoothLookTurnCompass.rotation = playerCurrentlyControlling.gameplayCamera.transform.rotation;
            playerCurrentlyControlling.smoothLookTurnCompass.SetParent(null);
        }

        playerCurrentlyControlling.cameraUp -= inputVector.y;
        playerCurrentlyControlling.cameraUp = Mathf.Clamp(playerCurrentlyControlling.cameraUp, -80f, 60f);
        playerCurrentlyControlling.smoothLookTurnCompass.localEulerAngles = new Vector3(playerCurrentlyControlling.cameraUp, playerCurrentlyControlling.smoothLookTurnCompass.localEulerAngles.y, playerCurrentlyControlling.smoothLookTurnCompass.localEulerAngles.z);
        playerCurrentlyControlling.gameplayCamera.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(playerCurrentlyControlling.gameplayCamera.transform.localEulerAngles.x, playerCurrentlyControlling.cameraUp, playerCurrentlyControlling.smoothLookMultiplier * Time.deltaTime), playerCurrentlyControlling.gameplayCamera.transform.localEulerAngles.y, playerCurrentlyControlling.gameplayCamera.transform.localEulerAngles.z);
    }

    private void CheckIfUpsideDown() {
        // Check if the hoverboard's up vector is pointing down
        if (Vector3.Dot(hoverboardChild.up, Vector3.down) > 0) {
            // If upside down, start the adjustment process
            targetRotation = Quaternion.LookRotation(hoverboardChild.forward, Vector3.up);
            StartCoroutine(AdjustOrientation());
        }
    }

    private IEnumerator AdjustOrientation() {
        isAdjusting = true;
        float duration = 1f;
        float elapsed = 0f;
        Quaternion initialRotation = hoverboardChild.rotation;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            hoverboardChild.rotation = Quaternion.Lerp(initialRotation, targetRotation, elapsed / duration);
            yield return null;
        }

        hoverboardChild.rotation = targetRotation;
        isAdjusting = false;
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        // Move the hoverboard when hit.
        hb.AddForce(hitDirection * force, ForceMode.Impulse);
		return true; // this bool literally doesn't get used. i have no idea.
	}
    
    public void SetupCollidersIgnoringOrIncluding(bool ignore) {
        collidersIgnored = ignore;
        Collider hbCollider = hb.GetComponent<Collider>();
        Collider hoverboardChildCollider = hoverboardChild.GetComponent<Collider>();
        Collider hoverboardChildChildrenCollider = hoverboardChild.GetComponentInChildren<Collider>();
        foreach (var player in StartOfRound.Instance.allPlayerScripts) {
            SimplifyCollidersIgnore(hbCollider, player, ignore);
            SimplifyCollidersIgnore(hoverboardChildCollider, player, ignore);
            SimplifyCollidersIgnore(hoverboardChildChildrenCollider, player, ignore);
        }
    }
    public void SimplifyCollidersIgnore(Collider hoverboardCollider, PlayerControllerB player, bool ignore) {
        Physics.IgnoreCollision(hoverboardCollider, player.playerCollider, ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.playerRigidbody.GetComponent<Collider>(), ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.GetComponent<CharacterController>().GetComponent<Collider>(), ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.transform.Find("PlayerPhysicsBox").GetComponent<BoxCollider>(), ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.transform.Find("PlayerPhysicsBox").GetComponent<Rigidbody>().GetComponent<Collider>(), ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.transform.Find("Misc").Find("Cube").GetComponent<BoxCollider>(), ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.transform.Find("Misc").Find("Cube").GetComponent<Rigidbody>().GetComponent<BoxCollider>(), ignore);
    }
}