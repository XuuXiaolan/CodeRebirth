using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using CodeRebirth.Keybinds;
using Unity.Netcode;
using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.AI;
using CodeRebirth.Patches;

namespace CodeRebirth.ScrapStuff;
public class Hoverboard : GrabbableObject, IHittable {
    public Rigidbody hb;
    public InteractTrigger trigger;
    private bool turnedOn = false;
    public float mult;
    public GameObject hoverboardSeat;
    public Transform[] anchors = new Transform[4];
    private PlayerControllerB playerControlling;
    private RaycastHit[] hits = new RaycastHit[4];
    private bool _isHoverForwardHeld = false;
    private bool jumpCooldown = true;
    public Transform hoverboardChild;
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
            if (Vector3.Distance(hoverboardChild.position, StartOfRound.Instance.shipBounds.transform.position) < 12 && !isInShipRoom) {
                this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
                isInShipRoom = true;
                isInElevator = true;
            } else if (Vector3.Distance(hoverboardChild.position, StartOfRound.Instance.shipBounds.transform.position) >= 12 && isInShipRoom) {
                this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
                isInShipRoom = false;
                isInElevator = false;
            }
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
        if (IsServer) {
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
            CalculateVerticalLookingInput(inputVector);

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
        if (!IsServer || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving) return;
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (Vector3.Distance(hoverboardChild.position, StartOfRound.Instance.shipBounds.transform.position) < 12 && !isInShipRoom) {
            this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            isInShipRoom = true;
            isInElevator = true;
        } else if (Vector3.Distance(hoverboardChild.position, StartOfRound.Instance.shipBounds.transform.position) >= 12 && isInShipRoom) {
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

    public void ApplyForce(Transform anchor, RaycastHit hit) {
        LayerMask mask = LayerMask.GetMask("Room");
        if (Physics.Raycast(anchor.position, -anchor.up, out hit, this.isInShipRoom ? 2.5f : 20f, mask)) {
            if (IsPointOnNavMesh(hit.point, 6.0f)) // Adjust maxDistance as needed
            {
                float force = Mathf.Clamp(Mathf.Abs(1 / (hit.point.y - anchor.position.y)), 0, this.isInShipRoom ? 2.5f : 20f);
                hb.AddForceAtPosition(transform.up * force * mult, anchor.position, ForceMode.Acceleration);
            }
        }
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        if (IsHost) {
            hb.AddForce(hitDirection.normalized * force * 100, ForceMode.Impulse);
        } else {
            HbAddForceServerRpc(hitDirection.normalized, force * 100, true);
        }
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void HbAddForceServerRpc(Vector3 forceDirection, float moveForce, bool impulse) {
        if (!impulse) hb.AddForce(forceDirection * moveForce, ForceMode.Acceleration);
        else hb.AddForce(forceDirection * moveForce, ForceMode.Impulse);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID) {
        SetTargetClientRpc(PlayerID);
    }

    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID) {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (PlayerID == -1) {
            playerControlling.playerActions.Movement.Jump.Enable();
            playerControlling.playerActions.Movement.Look.Enable();
            playerControlling.movementSpeed = playerMovementSpeed;
            if (playerControlling.isInHangarShipRoom || Vector3.Distance(playerControlling.transform.position, StartOfRound.Instance.shipBounds.transform.position) > 12f) {
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
                SwitchToHeld();
                break;
            case 2:
                SwitchToMounted();
                break;
            default:
                Plugin.Logger.LogFatal("Invalid Hoverboard state!");
                break;
        }
        HandleToolTips();
    }
    public void SwitchToMounted() {
        if (hoverboardMode == HoverboardMode.Held) {
            hoverboardChild.position = playerControlling.transform.position;
        }
        hoverboardChild.rotation = resetChildRotation;
        hoverboardChild.position += Vector3.up * 0.3f;
        playerControlling.transform.SetParent(hoverboardSeat.transform, true);
        if (GameNetworkManager.Instance.localPlayerController == playerControlling) {
            PlayerControllerBPatch.mountedPlayer = true;
        }
        hoverboardMode = HoverboardMode.Mounted;
        playerControlling.playerActions.Movement.Look.Disable();
        playerControlling.playerActions.Movement.Jump.Disable();
        playerMovementSpeed = playerControlling.movementSpeed;
        playerControlling.movementSpeed = 0f;
        if (playerControlling == GameNetworkManager.Instance.localPlayerController) StartCoroutine(TurnOnHoverboard());
        hb.useGravity = true;
        hb.isKinematic = false;
        trigger.interactable = false;
        SwitchModeExtension(false);
    }
    public void SwitchToHeld() {
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (IsHost) {
            if (playerControlling.isInHangarShipRoom) {
                playerControlling.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            } else {
                playerControlling.transform.SetParent(realPlayer.playersManager.playersContainer, true);
            }
        }
        if (IsServer) {
            this.transform.SetParent(playerControlling.transform, true);
        }
        PlayerControllerBPatch.mountedPlayer = false;
        hoverboardMode = HoverboardMode.Held;
        playerControlling.playerActions.Movement.Look.Enable();
        playerControlling.playerActions.Movement.Jump.Enable();
        playerControlling.movementSpeed = playerMovementSpeed;
        hoverboardChild.position = playerControlling.transform.position + playerControlling.transform.right * 0.7f + playerControlling.transform.up * 1f;
        Quaternion rotationOffset = Quaternion.Euler(180, 180, -90); // Adjust to match correct facing direction
        hoverboardChild.rotation = playerControlling.transform.rotation * rotationOffset;
        _isHoverForwardHeld = false;
        turnedOn = false;
        hb.useGravity = false;
        hb.isKinematic = true;
    }
    public void SwitchToNothing() {
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        hoverboardMode = HoverboardMode.None;
        realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (IsServer) {
            if (isInShipRoom) {
                this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            } else {
                this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
            }
        }
        PlayerControllerBPatch.mountedPlayer = false;
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

    private void CalculateVerticalLookingInput(Vector2 inputVector) {
        if (!playerControlling.smoothLookEnabledLastFrame) {
            playerControlling.smoothLookEnabledLastFrame = true;
            playerControlling.smoothLookTurnCompass.rotation = playerControlling.gameplayCamera.transform.rotation;
            playerControlling.smoothLookTurnCompass.SetParent(null);
        }

        playerControlling.cameraUp -= inputVector.y;
        playerControlling.cameraUp = Mathf.Clamp(playerControlling.cameraUp, -80f, 60f);
        playerControlling.smoothLookTurnCompass.localEulerAngles = new Vector3(playerControlling.cameraUp, playerControlling.smoothLookTurnCompass.localEulerAngles.y, playerControlling.smoothLookTurnCompass.localEulerAngles.z);
        playerControlling.gameplayCamera.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(playerControlling.gameplayCamera.transform.localEulerAngles.x, playerControlling.cameraUp, playerControlling.smoothLookMultiplier * Time.deltaTime), playerControlling.gameplayCamera.transform.localEulerAngles.y, playerControlling.gameplayCamera.transform.localEulerAngles.z);
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
}