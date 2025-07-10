using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using System.Linq;
using CodeRebirth.src.Util;
using System.Collections.Generic;
using CodeRebirthLib.ContentManagement.Enemies;
using CodeRebirthLib.ContentManagement.Items;
using CodeRebirth.src.Util.Extensions;

namespace CodeRebirth.src.Content.Items;
public class Hoverboard : GrabbableObject, IHittable
{
    public Rigidbody hb = null!;
    public InteractTrigger trigger = null!;
    private bool turnedOn = false;
    public GameObject hoverboardSeat = null!;
    public Transform[] anchors = new Transform[4];
    [NonSerialized]
    public PlayerControllerB? playerControlling;
    private bool _isHoverForwardHeld = false;
    private bool _isHoverBackwardHeld = false;
    private bool _isHoverLeftHeld = false;
    private bool _isHoverRightHeld = false;
    private bool _isSprintHeld = false;
    private bool jumpCooldown = true;
    public Transform hoverboardChild = null!;
    private HoverboardTypes hoverboardType = HoverboardTypes.Regular;
    private bool weightApplied = false;
    private float _speedMultiplier = 1f;
    private float _chargeIncreaseMultiplier = 1f;
    public enum HoverboardMode
    {
        None,
        Held,
        Mounted,
    }
    public enum HoverboardTypes
    {
        Regular,
        // Eventually wanna make other types of hoverboards
    }
    private HoverboardMode hoverboardMode;
    private Quaternion resetChildRotation;
    private bool isAdjusting = false;
    // Variables to store initial anchor positions and rotations
    private readonly Vector3[] initialAnchorPositions = new Vector3[4];
    private readonly Quaternion[] initialAnchorRotations = new Quaternion[4];

    public override void Start()
    {
        StartBaseImportant();
        ConfigureHoverboard();
        this.EnablePhysics(false);
        this.insertedBattery = new Battery(false, 1f);
        this.ChargeBatteries();
        switch (hoverboardType)
        {
            case HoverboardTypes.Regular:
                break;
            default:
                break;
        }
        SwitchModeExtension(true);
        resetChildRotation = hoverboardChild.rotation;
        if (IsServer)
        {
            PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
            if (StartOfRound.Instance.shipBounds.bounds.Contains(this.hoverboardChild.position) && !isInShipRoom)
            {
                this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
                isInShipRoom = true;
                isInElevator = true;
            }
            else if (!StartOfRound.Instance.shipBounds.bounds.Contains(this.hoverboardChild.position) && isInShipRoom)
            {
                this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
                isInShipRoom = false;
                isInElevator = false;
            }
        }
        // Save initial positions and rotations of the anchors
        for (int i = 0; i < anchors.Length; i++)
        {
            initialAnchorPositions[i] = anchors[i].localPosition;
            initialAnchorRotations[i] = anchors[i].localRotation;
        }
        SetHoverboardState(0);
    }

    private void ConfigureHoverboard()
    {
        if (Plugin.Mod.ItemRegistry().TryGetFromItemName("Hoverboard", out CRItemDefinition? hoverboardItemDefinition))
        {
            _speedMultiplier = hoverboardItemDefinition.GetGeneralConfig<float>("Hoverboard | Speed Multiplier").Value;
            _chargeIncreaseMultiplier = hoverboardItemDefinition.GetGeneralConfig<float>("Hoverboard | Charge Increase Multiplier").Value;
        }
    }

    public void OnInteract(PlayerControllerB player)
    {
        if (GameNetworkManager.Instance.localPlayerController != player) return;
        if (hoverboardMode == HoverboardMode.None)
        {
            StartCoroutine(OnInteractCoroutine(player));
        }
    }

    public IEnumerator OnInteractCoroutine(PlayerControllerB player)
    {
        SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        yield return new WaitUntil(() => playerControlling == player);
        SetHoverboardStateServerRpc(2);
    }

    public void ModeHandler(InputAction.CallbackContext context)
    {
        if (hoverboardMode == HoverboardMode.None || playerControlling == null || !playerControlling.IsLocalPlayer()) return;
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame)
        {
            if (hoverboardMode == HoverboardMode.Mounted)
            {
                SetHoverboardStateServerRpc(1);
            }
            else if (hoverboardMode == HoverboardMode.Held)
            {
                SetHoverboardStateServerRpc(2);
            }
            Plugin.InputActionsInstance.SwitchMode.performed += ModeHandler;
        }
    }

    public void OnKeyHeld(InputAction.CallbackContext context)
    {
        if (GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        var btn = (ButtonControl)context.control;
        InputAction action = context.action;
        bool forward = false, backward = false, left = false, right = false, sprint = false;
        if (btn.wasPressedThisFrame)
        {
            switch (action.name)
            {
                case "HoverForward":
                    forward = true;
                    break;
                case "HoverBackward":
                    backward = true;
                    break;
                case "HoverLeft":
                    left = true;
                    break;
                case "HoverRight":
                    right = true;
                    break;
                case "SprintForward":
                    sprint = true;
                    break;
            }
            SetHoverboardHeldServerRpc(forward, backward, right, left, sprint);
        }
    }

    public void OnKeyReleased(InputAction.CallbackContext context)
    {
        if (GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        var btn = (ButtonControl)context.control;
        InputAction action = context.action;
        bool forward = true, backward = true, left = true, right = true, sprint = true;
        if (btn.wasReleasedThisFrame)
        {
            switch (action.name)
            {
                case "HoverForward":
                    forward = false;
                    break;
                case "HoverBackward":
                    backward = false;
                    break;
                case "HoverLeft":
                    left = false;
                    break;
                case "HoverRight":
                    right = false;
                    break;
                case "SprintForward":
                    sprint = false;
                    break;
            }
            SetHoverboardReleasedServerRpc(forward, backward, right, left, sprint);
        }
    }

    public void MovementHandler(InputAction.CallbackContext context)
    {
        if (GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame && context.action == Plugin.InputActionsInstance.HoverUp)
        {
            HandleMovement();
        }
    }

    public void FixedUpdate()
    {
        if (playerControlling == null || !playerControlling.IsLocalPlayer()) return;
        if (turnedOn && hoverboardMode != HoverboardMode.Held)
        {
            for (int i = 0; i < 4; i++)
            {
                ApplyForce(anchors[i]);
            }
        }
    }

    public override void Update()
    {
        base.Update();
        if (HandleDropping()) return;
        if (_isSprintHeld && !this.insertedBattery.empty && _isHoverForwardHeld && hoverboardMode == HoverboardMode.Mounted)
        {
            this.isBeingUsed = true;
            this.insertedBattery.charge = Mathf.Clamp(this.insertedBattery.charge - (Time.deltaTime / this.itemProperties.batteryUsage), 0f, 1f);
            if (this.insertedBattery.charge <= 0f && !this.insertedBattery.empty)
            {
                this.insertedBattery.empty = true;
            }
        }
        else
        {
            this.isBeingUsed = false;
            this.insertedBattery.charge = Mathf.Clamp(this.insertedBattery.charge + (Time.deltaTime + _chargeIncreaseMultiplier / this.itemProperties.batteryUsage), 0f, 1f);
            if (this.insertedBattery.charge >= 0.15f && this.insertedBattery.empty)
            {
                this.insertedBattery.empty = false;
            }
        }
        if ((hoverboardMode == HoverboardMode.Held || hoverboardMode == HoverboardMode.Mounted) && playerControlling == null)
        {
            DropHoverboard();
            return;
        }
        if (playerControlling == null) return;
        if (playerControlling.IsLocalPlayer() && Vector3.Distance(hoverboardChild.position, playerControlling.transform.position) > 5)
        {
            SetHoverboardStateServerRpc(1);
            return;
        }
        if (playerControlling.IsLocalPlayer() && hoverboardMode == HoverboardMode.Mounted)
        {
            Vector2 currentMouseDelta = Plugin.InputActionsInstance.MouseDelta.ReadValue<Vector2>();

            float turnSpeed = 0.1f; // Adjust the turn speed as needed
            float turnAmount = currentMouseDelta.x * turnSpeed;

            float turnAmountY = currentMouseDelta.y * turnSpeed * 0.8f;
            Vector2 inputVector = new Vector2(0, turnAmountY);

            // Call CalculateSmoothLookingInput to handle the camera rotation
            CalculateVerticalLookingInput(inputVector, playerControlling);

            hoverboardChild.Rotate(Vector3.up, turnAmount);
        }
        if (hoverboardMode == HoverboardMode.Mounted && playerControlling.transform.GetParent() != hoverboardSeat.transform)
        {
            playerControlling.transform.SetParent(hoverboardSeat.transform, true);
            Plugin.ExtendedLogging($"Setting parent of {playerControlling} to {playerControlling.transform.GetParent()}");
        }
        if (playerControlling.inAnimationWithEnemy || playerControlling.isClimbingLadder || (hoverboardMode == HoverboardMode.Mounted && playerControlling.isClimbingLadder))
        {
            DropHoverboard();
            return;
        }
        if (hoverboardMode == HoverboardMode.Mounted && turnedOn && playerControlling.IsLocalPlayer())
        {
            playerControlling.transform.position = hoverboardSeat.transform.position;
            if (_isHoverForwardHeld)
            {
                hb.AddForce(hoverboardChild.right * _speedMultiplier * 25f * ((_isSprintHeld && !this.insertedBattery.empty) ? 2f : 1f), ForceMode.Acceleration);
            }
            if (_isHoverBackwardHeld)
            {
                hb.AddForce(-hoverboardChild.right * _speedMultiplier * 5f * ((_isSprintHeld && !this.insertedBattery.empty) ? 2f : 1f), ForceMode.Acceleration);
            }
            if (_isHoverLeftHeld)
            {
                hb.AddForce(hoverboardChild.forward * _speedMultiplier * 5f * ((_isSprintHeld && !this.insertedBattery.empty) ? 2f : 1f), ForceMode.Acceleration);
            }
            if (_isHoverRightHeld)
            {
                hb.AddForce(-hoverboardChild.forward * _speedMultiplier * 5f * ((_isSprintHeld && !this.insertedBattery.empty) ? 2f : 1f), ForceMode.Acceleration);
            }

            if (!isAdjusting)
                CheckIfUpsideDown();
        }
        if (hoverboardMode == HoverboardMode.Mounted) playerControlling.ResetFallGravity();
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        if (!IsServer || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving && hoverboardMode != HoverboardMode.Held) return;
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (StartOfRound.Instance.shipBounds.bounds.Contains(this.hoverboardChild.position) && !isInShipRoom)
        {
            this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            isInShipRoom = true;
            isInElevator = true;
        }
        else if (!StartOfRound.Instance.shipBounds.bounds.Contains(this.hoverboardChild.position) && isInShipRoom)
        {
            this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
            isInShipRoom = false;
            isInElevator = false;
        }
    }

    public bool HandleDropping()
    {
        if (playerControlling == null || !playerControlling.IsLocalPlayer() || !Plugin.InputActionsInstance.DropHoverboard.triggered) return false;
        DropHoverboard();
        return true;
    }

    public void DropHoverboard()
    {
        SetHoverboardStateServerRpc(0);
        SetTargetServerRpc(-1);
        SwitchModeExtension(true);
    }

    private void HandleMovement()
    {
        if (playerControlling == null || !playerControlling.IsLocalPlayer()) return;
        Vector3 forceDirection = Vector3.zero;
        float moveForce = 0f;

        if (jumpCooldown)
        {
            jumpCooldown = false;
            forceDirection += hoverboardChild.up;
            moveForce = 1500f;
            StartCoroutine(JumpTimerStart());
        }

        if (forceDirection == Vector3.zero) return;
        hb.AddForce(forceDirection * moveForce, ForceMode.Acceleration);
    }

    public IEnumerator JumpTimerStart()
    {
        yield return new WaitForSeconds(2f);
        jumpCooldown = true;
    }

    public void ApplyForce(Transform anchor)
    {
        if (Physics.Raycast(anchor.position, -anchor.up, out RaycastHit hit, 1000f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
        {
            float force = Mathf.Clamp(Mathf.Abs(1 / (hit.point.y - anchor.position.y)), 0, this.isInShipRoom ? 3f : 100f);
            // Debug log for force and anchor positions
            hb.AddForceAtPosition(hoverboardChild.up * force * 8f, anchor.position, ForceMode.Acceleration);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetServerRpc(int PlayerID)
    {
        SetTargetClientRpc(PlayerID);
    }

    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID)
    {
        if (PlayerID == -1 && playerControlling != null)
        {
            playerControlling.disableMoveInput = false;
            playerControlling.disableLookInput = false;
            if (playerControlling.isInHangarShipRoom || StartOfRound.Instance.shipBounds.bounds.Contains(this.playerControlling.transform.position))
            {
                playerControlling.transform.SetParent(playerControlling.playersManager.elevatorTransform, true);
            }
            else
            {
                playerControlling.transform.SetParent(playerControlling.playersManager.playersContainer, true);
            }
            playerControlling = null;
            Plugin.ExtendedLogging($"Clearing target on {this}");
            return;
        }
        if (StartOfRound.Instance.allPlayerScripts[PlayerID] == null)
        {
            Plugin.Logger.LogWarning($"Index invalid! {PlayerID}");
            return;
        }
        if (playerControlling == StartOfRound.Instance.allPlayerScripts[PlayerID])
        {
            Plugin.ExtendedLogging($"{this} already targeting: {playerControlling.playerUsername}");
            return;
        }
        hoverboardChild.rotation = resetChildRotation;
        playerControlling = StartOfRound.Instance.allPlayerScripts[PlayerID];
        if (IsServer) this.NetworkObject.ChangeOwnership(playerControlling.actualClientId);
        if (playerControlling.IsLocalPlayer() && !playerControlling.GetCRPlayerData().ridingHoverboard)
        {
            DialogueSegment dialogue = new DialogueSegment
            {
                speakerText = "Hoverboard Tooltips",
                bodyText = "C to Drop, E to Mount, F to Switch between Held and Mounted mode, Space to Jump, Shift to activate Boost.",
                waitTime = 7f
            };
            HUDManager.Instance.ReadDialogue([dialogue]);
        }
        playerControlling.GetCRPlayerData().ridingHoverboard = true;
        playerControlling.GetCRPlayerData().hoverboardRiding = this;
        playerControlling.transform.SetPositionAndRotation(hoverboardSeat.transform.position, hoverboardSeat.transform.rotation * Quaternion.Euler(0, 90, 0));
        Plugin.ExtendedLogging($"{this} setting target to: {playerControlling.playerUsername}");
    }

    [ServerRpc(RequireOwnership = false)]
    internal void SetHoverboardHeldServerRpc(bool forwardHeld, bool backwardHeld, bool rightHeld, bool leftHeld, bool sprintHeld)
    {
        SetHoverboardHeldClientRpc(forwardHeld, backwardHeld, rightHeld, leftHeld, sprintHeld);
    }

    [ClientRpc]
    internal void SetHoverboardHeldClientRpc(bool forwardHeld, bool backwardHeld, bool rightHeld, bool leftHeld, bool sprintHeld)
    {
        if (forwardHeld) _isHoverForwardHeld = true;
        if (backwardHeld) _isHoverBackwardHeld = true;
        if (rightHeld) _isHoverRightHeld = true;
        if (leftHeld) _isHoverLeftHeld = true;
        if (sprintHeld) _isSprintHeld = true;
    }
    [ServerRpc(RequireOwnership = false)]
    internal void SetHoverboardReleasedServerRpc(bool forwardHeld, bool backwardHeld, bool rightHeld, bool leftHeld, bool sprintHeld)
    {
        SetHoverboardReleasedClientRpc(forwardHeld, backwardHeld, rightHeld, leftHeld, sprintHeld);
    }

    [ClientRpc]
    internal void SetHoverboardReleasedClientRpc(bool forwardHeld, bool backwardHeld, bool rightHeld, bool leftHeld, bool sprintHeld)
    {
        if (!forwardHeld) _isHoverForwardHeld = false;
        if (!backwardHeld) _isHoverBackwardHeld = false;
        if (!rightHeld) _isHoverRightHeld = false;
        if (!leftHeld) _isHoverLeftHeld = false;
        if (!sprintHeld) _isSprintHeld = false;
    }

    [ServerRpc(RequireOwnership = false)]
    internal void SetHoverboardStateServerRpc(int state)
    {
        SetHoverboardStateClientRpc(state);
    }

    [ClientRpc]
    internal void SetHoverboardStateClientRpc(int state)
    {
        SetHoverboardState(state);
    }

    private void SetHoverboardState(int state)
    {
        if (playerControlling == null && hoverboardMode != HoverboardMode.None)
        {
            Plugin.Logger.LogWarning($"Player controlling is null for me...");
        }
        switch (state)
        {
            case 0:
                if (playerControlling != null)
                {
                    SwitchToNothing(playerControlling);
                }
                else
                {
                    Plugin.Logger.LogError($"Player controlling is null when switching to nothing...");
                }
                break;
            case 1:
                if (playerControlling != null)
                {
                    SwitchToHeld(playerControlling);
                }
                else
                {
                    Plugin.Logger.LogError($"Player controlling is null when switching to held...");
                }
                break;
            case 2:
                if (playerControlling != null)
                {
                    SwitchToMounted(playerControlling);
                }
                else
                {
                    Plugin.Logger.LogError($"Player controlling is null when switching to mounted...");
                }
                break;
        }
        HandleToolTips();
    }

    public void SwitchToMounted(PlayerControllerB playerCurrentlyControlling)
    {
        if (hoverboardMode == HoverboardMode.Held)
        {
            hoverboardChild.position = playerCurrentlyControlling.transform.position;
            if (IsServer)
            {
                PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
                if (isInShipRoom)
                {
                    this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
                }
                else
                {
                    this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
                }
            }
        }
        // Reset hoverboardChild's rotation and position
        hoverboardChild.rotation = resetChildRotation;
        hoverboardChild.position += Vector3.up * 0.3f;
        for (int i = 0; i < anchors.Length; i++)
        {
            anchors[i].localPosition = initialAnchorPositions[i];
            anchors[i].localRotation = initialAnchorRotations[i];
        }
        if (IsServer) playerCurrentlyControlling.transform.SetParent(hoverboardSeat.transform, true);
        playerCurrentlyControlling.GetCRPlayerData().ridingHoverboard = true;
        playerCurrentlyControlling.GetCRPlayerData().hoverboardRiding = this;
        hoverboardMode = HoverboardMode.Mounted;
        if (weightApplied) playerCurrentlyControlling.carryWeight = Mathf.Clamp(playerCurrentlyControlling.carryWeight - 0.24f, 1, 1000);
        weightApplied = false;
        SetupCollidersIgnoringOrIncluding(true);
        playerCurrentlyControlling.disableLookInput = true;
        playerCurrentlyControlling.disableMoveInput = true;
        if (playerCurrentlyControlling.IsLocalPlayer())
        {
            StartCoroutine(TurnOnHoverboard());
        }
        hb.useGravity = true;
        hb.isKinematic = false;
        trigger.interactable = false;
        SwitchModeExtension(false);
    }
    public void SwitchToHeld(PlayerControllerB playerCurrentlyControlling)
    {
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (playerCurrentlyControlling.isInHangarShipRoom)
        {
            playerCurrentlyControlling.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
        }
        else
        {
            playerCurrentlyControlling.transform.SetParent(realPlayer.playersManager.playersContainer, true);
        }
        if (IsServer)
        {
            this.transform.SetParent(playerCurrentlyControlling.transform, true);
        }
        playerCurrentlyControlling.GetCRPlayerData().ridingHoverboard = false;
        playerCurrentlyControlling.GetCRPlayerData().hoverboardRiding = null;
        playerCurrentlyControlling.disableMoveInput = false;
        playerCurrentlyControlling.disableLookInput = false;
        hoverboardMode = HoverboardMode.Held;
        playerCurrentlyControlling.disableLookInput = false;
        playerCurrentlyControlling.disableMoveInput = false;
        if (!weightApplied) playerCurrentlyControlling.carryWeight = Mathf.Clamp(playerCurrentlyControlling.carryWeight + 0.24f, 1, 1000);
        weightApplied = true;
        hoverboardChild.position = playerCurrentlyControlling.transform.position + playerCurrentlyControlling.transform.right * 0.7f + playerCurrentlyControlling.transform.up * 1f;
        Quaternion rotationOffset = Quaternion.Euler(180, 180, -90); // Adjust to match correct facing direction
        hoverboardChild.rotation = playerCurrentlyControlling.transform.rotation * rotationOffset;
        _isHoverForwardHeld = false;
        _isSprintHeld = false;
        _isHoverRightHeld = false;
        _isHoverLeftHeld = false;
        _isHoverBackwardHeld = false;
        isBeingUsed = false;
        turnedOn = false;
        hb.useGravity = false;
        hb.isKinematic = true;
    }
    public void SwitchToNothing(PlayerControllerB playerCurrentlyControlling)
    {
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        hoverboardMode = HoverboardMode.None;
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
        if (weightApplied) playerCurrentlyControlling.carryWeight = Mathf.Clamp(playerCurrentlyControlling.carryWeight - 0.24f, 1, 1000);
        weightApplied = false;
        SetupCollidersIgnoringOrIncluding(false);
        turnedOn = false;
        _isHoverForwardHeld = false;
        _isSprintHeld = false;
        _isHoverRightHeld = false;
        _isHoverLeftHeld = false;
        _isHoverBackwardHeld = false;
        isBeingUsed = false;
        hb.useGravity = true;
        hb.isKinematic = false;
        trigger.interactable = true;
    }
    public IEnumerator TurnOnHoverboard()
    {
        yield return new WaitForSeconds(1f);
        if (hoverboardMode != HoverboardMode.Mounted) yield break;
        TurnOnHoverboardServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TurnOnHoverboardServerRpc()
    {
        TurnOnHoverboardClientRpc();
    }

    [ClientRpc]
    public void TurnOnHoverboardClientRpc()
    {
        turnedOn = true;
    }

    public void StartBaseImportant()
    {
        this.propColliders = base.gameObject.GetComponentsInChildren<Collider>();
        this.originalScale = base.transform.localScale;
        if (this.itemProperties.itemSpawnsOnGround)
        {
            this.startFallingPosition = base.transform.position;
            if (base.transform.parent != null)
            {
                this.startFallingPosition = base.transform.parent.InverseTransformPoint(this.startFallingPosition);
            }
            this.FallToGround(false);
        }
        else
        {
            this.hasHitGround = true;
            this.reachedFloorTarget = true;
            this.targetFloorPosition = base.transform.localPosition;
        }
        if (this.itemProperties.isScrap)
        {
            this.hasHitGround = true;
        }
        if (this.itemProperties.isScrap && RoundManager.Instance.mapPropsContainer != null)
        {
            this.radarIcon = Instantiate<GameObject>(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
        }
        if (!this.itemProperties.isScrap)
        {
            HoarderBugAI.grabbableObjectsInMap.Add(base.gameObject);
        }
        MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < componentsInChildren.Length; i++)
        {
            componentsInChildren[i].renderingLayerMask = 1U;
        }
        SkinnedMeshRenderer[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int j = 0; j < componentsInChildren2.Length; j++)
        {
            componentsInChildren2[j].renderingLayerMask = 1U;
        }

        trigger.onInteract.AddListener(OnInteract);
    }

    public void SwitchModeExtension(bool SwitchingOff)
    {
        if (SwitchingOff)
        {
            Plugin.InputActionsInstance.HoverForward.performed -= OnKeyHeld;
            Plugin.InputActionsInstance.HoverBackward.performed -= OnKeyHeld;
            Plugin.InputActionsInstance.HoverLeft.performed -= OnKeyHeld;
            Plugin.InputActionsInstance.HoverRight.performed -= OnKeyHeld;
            Plugin.InputActionsInstance.SprintForward.performed -= OnKeyHeld;
            Plugin.InputActionsInstance.HoverForward.canceled -= OnKeyReleased;
            Plugin.InputActionsInstance.HoverBackward.canceled -= OnKeyReleased;
            Plugin.InputActionsInstance.HoverLeft.canceled -= OnKeyReleased;
            Plugin.InputActionsInstance.HoverRight.canceled -= OnKeyReleased;
            Plugin.InputActionsInstance.SprintForward.canceled -= OnKeyReleased;
            Plugin.InputActionsInstance.HoverUp.performed -= MovementHandler;
            Plugin.InputActionsInstance.SwitchMode.performed -= ModeHandler;
        }
        else
        {
            Plugin.InputActionsInstance.HoverForward.performed += OnKeyHeld;
            Plugin.InputActionsInstance.HoverBackward.performed += OnKeyHeld;
            Plugin.InputActionsInstance.HoverLeft.performed += OnKeyHeld;
            Plugin.InputActionsInstance.HoverRight.performed += OnKeyHeld;
            Plugin.InputActionsInstance.SprintForward.performed += OnKeyHeld;
            Plugin.InputActionsInstance.HoverForward.canceled += OnKeyReleased;
            Plugin.InputActionsInstance.HoverBackward.canceled += OnKeyReleased;
            Plugin.InputActionsInstance.HoverLeft.canceled += OnKeyReleased;
            Plugin.InputActionsInstance.HoverRight.canceled += OnKeyReleased;
            Plugin.InputActionsInstance.SprintForward.canceled += OnKeyReleased;
            Plugin.InputActionsInstance.HoverUp.performed += MovementHandler;
            Plugin.InputActionsInstance.SwitchMode.performed += ModeHandler;
        }
    }

    public void HandleToolTips()
    {
        if (playerControlling == null || GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        HUDManager.Instance.ClearControlTips();
        if (hoverboardMode == HoverboardMode.Mounted)
        {
            HUDManager.Instance.ChangeControlTipMultiple([
                $"Dismount Hoverboard : [{Plugin.InputActionsInstance.DropHoverboard.GetBindingDisplayString().Split(' ')[0]}]",
                $"Move Hoverboard : [{Plugin.InputActionsInstance.HoverForward.GetBindingDisplayString().Split(' ')[0]}][{Plugin.InputActionsInstance.HoverLeft.GetBindingDisplayString().Split(' ')[0]}][{Plugin.InputActionsInstance.HoverBackward.GetBindingDisplayString().Split(' ')[0]}][{Plugin.InputActionsInstance.HoverRight.GetBindingDisplayString().Split(' ')[0]}]",
                $"Up Boost : [{Plugin.InputActionsInstance.HoverUp.GetBindingDisplayString().Split(' ')[0]}]",
                $"Switch Mode (Mounted) : [{Plugin.InputActionsInstance.SwitchMode.GetBindingDisplayString().Split(' ')[0]}]"
            ]);
        }
        else if (hoverboardMode == HoverboardMode.Held)
        {
            HUDManager.Instance.ChangeControlTipMultiple([
                $"Drop Hoverboard : [{Plugin.InputActionsInstance.DropHoverboard.GetBindingDisplayString().Split(' ')[0]}]",
                $"Switch Mode (Held) : [{Plugin.InputActionsInstance.SwitchMode.GetBindingDisplayString().Split(' ')[0]}]"
            ]);
        }
    }

    public override void FallWithCurve()
    {
    }

    private void CalculateVerticalLookingInput(Vector2 inputVector, PlayerControllerB playerCurrentlyControlling)
    {
        if (!playerCurrentlyControlling.smoothLookEnabledLastFrame)
        {
            playerCurrentlyControlling.smoothLookEnabledLastFrame = true;
            playerCurrentlyControlling.smoothLookTurnCompass.rotation = playerCurrentlyControlling.gameplayCamera.transform.rotation;
            playerCurrentlyControlling.smoothLookTurnCompass.SetParent(null);
        }

        playerCurrentlyControlling.cameraUp -= inputVector.y;
        playerCurrentlyControlling.cameraUp = Mathf.Clamp(playerCurrentlyControlling.cameraUp, -80f, 60f);
        playerCurrentlyControlling.smoothLookTurnCompass.localEulerAngles = new Vector3(playerCurrentlyControlling.cameraUp, playerCurrentlyControlling.smoothLookTurnCompass.localEulerAngles.y, playerCurrentlyControlling.smoothLookTurnCompass.localEulerAngles.z);
        playerCurrentlyControlling.gameplayCamera.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(playerCurrentlyControlling.gameplayCamera.transform.localEulerAngles.x, playerCurrentlyControlling.cameraUp, playerCurrentlyControlling.smoothLookMultiplier * Time.deltaTime), playerCurrentlyControlling.gameplayCamera.transform.localEulerAngles.y, playerCurrentlyControlling.gameplayCamera.transform.localEulerAngles.z);
    }

    private void CheckIfUpsideDown()
    {
        // Calculate the angle between the hoverboard's up vector and the world's up vector
        float angle = Vector3.Angle(hoverboardChild.up, Vector3.up);

        // If the angle is greater than 90 degrees, the hoverboard is considered upside down
        if (angle > 60f)
        {
            // Start the adjustment process
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(hoverboardChild.forward, Vector3.up), Vector3.up);
            StartCoroutine(AdjustOrientation(targetRotation));
        }
    }

    private IEnumerator AdjustOrientation(Quaternion targetRotation)
    {
        isAdjusting = true;
        float duration = 0.5f; // Duration of the rotation
        float elapsed = 0f;
        Quaternion initialRotation = hoverboardChild.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hoverboardChild.rotation = Quaternion.Slerp(initialRotation, targetRotation, elapsed / duration);
            yield return null;
        }

        hoverboardChild.rotation = targetRotation;
        isAdjusting = false;
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        // Move the hoverboard when hit.
        hb.AddForce(hitDirection * force, ForceMode.Impulse);
        return true; // this bool literally doesn't get used. i have no idea.
    }

    public void SetupCollidersIgnoringOrIncluding(bool ignore)
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            foreach (var collider in this.GetComponentsInChildren<Collider>())
            {
                SimplifyCollidersIgnore(collider, player, ignore);
            }
        }
    }

    public void SimplifyCollidersIgnore(Collider hoverboardCollider, PlayerControllerB player, bool ignore)
    {
        Physics.IgnoreCollision(hoverboardCollider, player.playerCollider, ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.playerRigidbody.GetComponent<Collider>(), true);
        Physics.IgnoreCollision(hoverboardCollider, player.GetComponent<CharacterController>().GetComponent<Collider>(), true);
        Physics.IgnoreCollision(hoverboardCollider, player.transform.Find("PlayerPhysicsBox").GetComponent<BoxCollider>(), ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.transform.Find("PlayerPhysicsBox").GetComponent<Rigidbody>().GetComponent<Collider>(), ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.transform.Find("Misc").Find("Cube").GetComponent<BoxCollider>(), ignore);
        Physics.IgnoreCollision(hoverboardCollider, player.transform.Find("Misc").Find("Cube").GetComponent<Rigidbody>().GetComponent<BoxCollider>(), ignore);
    }
}