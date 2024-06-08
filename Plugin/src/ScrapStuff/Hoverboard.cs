using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using CodeRebirth.Keybinds;
using Unity.Netcode;
using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using System.Linq;
using System.Diagnostics.Tracing;
using Unity.Netcode.Samples;

namespace CodeRebirth.ScrapStuff;
public class Hoverboard : GrabbableObject, IHittable
{
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
    public enum HoverboardMode {
        None,
        Held,
        Mounted,
    }
    public enum HoverboardTypes {
        Regular
        // Eventually wanna make other types of hoverboards
    }
    private HoverboardMode hoverboardMode;
    public override void Start()
    {
        StartBaseImportant();
        if (IsHost) {
            SetHoverboardStateClientRpc(0);
        } else {
            SetHoverboardStateServerRpc(0);
        }
        SwitchModeExtension(true);
    }
    public void ModeHandler(InputAction.CallbackContext context) {
        if (hoverboardMode == HoverboardMode.None) return;
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame)
        {
            if (hoverboardMode == HoverboardMode.Mounted) {
                if (IsHost) {
                    SetHoverboardStateClientRpc(1);
                } else {
                    SetHoverboardStateServerRpc(1);
                }
                SwitchModeExtension(true);
            } else if (hoverboardMode == HoverboardMode.Held) {
                if (IsHost) {
                    SetHoverboardStateClientRpc(2);
                } else {
                    SetHoverboardStateServerRpc(2);
                }
                SwitchModeExtension(false);
            }
            Plugin.InputActionsInstance.SwitchMode.performed += ModeHandler;
        }
    }
    public void OnInteract(PlayerControllerB player) {
        if (hoverboardMode == HoverboardMode.None) {            
            if (IsHost) {
                SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                SetHoverboardStateClientRpc(2);
            } else {
                SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                SetHoverboardStateServerRpc(2);
            }
            SwitchModeExtension(false);
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
        if (btn.wasPressedThisFrame)
        {
            HandleMovement();
        }
    }

    public override void Update()
    {
        base.Update();
        HandleDropping();
        if (playerControlling == null) return;
        if (turnedOn) {
            for (int i = 0; i < 4; i++) {
                ApplyForce(anchors[i], hits[i]);
            }
        }
        if (Vector3.Distance(transform.position, playerControlling.transform.position) > 5f) {
            DropHoverboard(true);
            return;
        }
        if (playerControlling.inAnimationWithEnemy || playerControlling.inSpecialInteractAnimation || (hoverboardMode == HoverboardMode.Mounted && playerControlling.isClimbingLadder)) {
            DropHoverboard(true);
            return;
        }
        Quaternion playerRotation = playerControlling.transform.rotation;
        Quaternion rotationOffset;
        if (hoverboardMode == HoverboardMode.Held) {
            // Position the hoverboard to the right side of the player
            this.transform.position = playerControlling.transform.position + playerControlling.transform.right * 0.7f + playerControlling.transform.up * 1f;

            // Make the hoverboard face upwards with its up vector pointing to the player's right hand
            rotationOffset = Quaternion.Euler(180, 180, -90); // Adjust to match correct facing direction
            this.transform.rotation = playerRotation * rotationOffset;
            return;
        }
        Plugin.Logger.LogInfo("Hoverboard Mode: " + hoverboardMode);
        Plugin.Logger.LogInfo("Hover Forward: " + _isHoverForwardHeld.ToString());
        Plugin.Logger.LogInfo("Turned On: " + turnedOn.ToString());
        if (_isHoverForwardHeld) {
            hb.AddForce(Vector3.zero + transform.right * 15f, ForceMode.Acceleration);
        }
        if (hoverboardMode == HoverboardMode.Mounted) {
            playerControlling.transform.position = hoverboardSeat.transform.position;
            rotationOffset = Quaternion.Euler(0, -90, 0); // 90 degrees to the left around the y-axis
            this.transform.rotation = playerRotation * rotationOffset;
            playerControlling.ResetFallGravity();
        }
    }
    public override void LateUpdate() {
        base.LateUpdate();
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (Vector3.Distance(transform.position, StartOfRound.Instance.shipBounds.transform.position) < 15 && !isInShipRoom && IsHost) {
            this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            isInShipRoom = true;
            isInElevator = true;
        } else if (Vector3.Distance(transform.position, StartOfRound.Instance.shipBounds.transform.position) >= 15 && isInShipRoom && IsHost) {
            this.transform.SetParent(realPlayer.playersManager.propsContainer, true);
            isInShipRoom = false;
            isInElevator = false;
        }
    }
    public void HandleDropping() {
        if (!Plugin.InputActionsInstance.DropHoverboard.triggered) return;
        DropHoverboard(true);
    }
    public void DropHoverboard(bool stillWorks = true) {
        if (IsHost) {
            SetHoverboardStateClientRpc(0);
            SetTargetClientRpc(-1);
        } else {
            SetHoverboardStateServerRpc(0);
            SetTargetServerRpc(-1);
        }
        SwitchModeExtension(true);
        trigger.interactable = stillWorks;
    }

    private void HandleMovement() // the reason these transform.forward and transform.right seemingly don't match with the buttons is because the exported hoverboard is kinda fucked... oh well.
    {
        if (playerControlling == null) return;
        Vector3 forceDirection = Vector3.zero;
        float moveForce = 50f;
        
        if (Plugin.InputActionsInstance.HoverLeft.WasPressedThisFrame())
            forceDirection += transform.forward;

        if (Plugin.InputActionsInstance.HoverRight.WasPressedThisFrame())
            forceDirection -= transform.forward;

        if (Plugin.InputActionsInstance.HoverBackward.WasPressedThisFrame())
            forceDirection -= transform.right;

        if (Plugin.InputActionsInstance.HoverForward.WasPressedThisFrame())
            forceDirection += transform.right;

        if (Plugin.InputActionsInstance.HoverUp.WasPressedThisFrame() && jumpCooldown) {
            jumpCooldown = false;
            forceDirection += transform.up;
            moveForce = 500f;
            StartCoroutine(JumpTimerStart());
        }

        if (forceDirection == Vector3.zero) return;
        if (IsHost) {
            hbAddForceClientRpc(forceDirection, moveForce, false);
        } else {
            hbAddForceServerRpc(forceDirection, moveForce, false);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    internal void hbAddForceServerRpc(Vector3 forceDirection, float moveForce, bool impulse) {
        hbAddForceClientRpc(forceDirection, moveForce, impulse);
    }

    [ClientRpc]
    internal void hbAddForceClientRpc(Vector3 forceDirection, float moveForce, bool impulse) {
        if (!impulse) hb.AddForce(forceDirection * moveForce, ForceMode.Acceleration);
        else hb.AddForce(forceDirection * moveForce, ForceMode.Impulse);
    }
    public IEnumerator JumpTimerStart() {
        yield return new WaitForSeconds(2f);
        jumpCooldown = true;
    }
    
    public void ApplyForce(Transform anchor, RaycastHit hit)
    {
        LayerMask mask = LayerMask.GetMask("Room");
        if (Physics.Raycast(anchor.position, -anchor.up, out hit, 1000, mask))
        {
            float force = Mathf.Abs(1 / (hit.point.y - anchor.position.y));
            hb.AddForceAtPosition(transform.up * force * mult, anchor.position, ForceMode.Acceleration);
        }
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        // Move the hoverboard when hit.
        if (IsHost) {
            hbAddForceClientRpc(hitDirection.normalized, force * 100, true);
        } else {
            hbAddForceServerRpc(hitDirection.normalized, force * 100, true);
        }

		return true; // this bool literally doesn't get used. i have no idea.
	}
    [ServerRpc(RequireOwnership = false)]
    internal void SetTargetServerRpc(int PlayerID) {
        SetTargetClientRpc(PlayerID);
    }
    /*[ClientRpc]
    internal void ApplyForcesForHoverboardClientRpc() {
        if (Vector3.Distance(transform.position, playerControlling.transform.position) > 5f) {
            DropHoverboard(true);
            return;
        }
        if (_isHoverForwardHeld && playerControlling == GameNetworkManager.Instance.localPlayerController) {
            hb.AddForce(Vector3.zero + transform.right * 40f, ForceMode.Acceleration);
        }
        if (turnedOn) {
            for (int i = 0; i < 4; i++) {
                ApplyForce(anchors[i], hits[i]);
            }
        }
    }*/
    [ClientRpc]
    internal void SetTargetClientRpc(int PlayerID) {
        if (PlayerID == -1) {
            playerControlling = null;
            // playerRidingCollider.enabled = false;
            Plugin.Logger.LogInfo($"Clearing target on {this}");
            return;
        }
        if (StartOfRound.Instance.allPlayerScripts[PlayerID] == null) {
            Plugin.Logger.LogInfo($"Index invalid! {this}");
            return;
        }
        playerControlling = StartOfRound.Instance.allPlayerScripts[PlayerID];
        // playerRidingCollider.enabled = true;
        Plugin.Logger.LogInfo($"{this} setting target to: {playerControlling.playerUsername}");
    }
    /*[ClientRpc]
    internal void SetHoverboardPositionClientRpc() {
        Quaternion playerRotation = playerControlling.transform.rotation;
        Quaternion rotationOffset;
        if (hoverboardMode == HoverboardMode.Held) {
            // Position the hoverboard to the right side of the player
            this.transform.position = playerControlling.transform.position + playerControlling.transform.right * 0.7f + playerControlling.transform.up * 1f;

            // Make the hoverboard face upwards with its up vector pointing to the player's right hand
            rotationOffset = Quaternion.Euler(180, 180, -90); // Adjust to match correct facing direction
            this.transform.rotation = playerRotation * rotationOffset;
            return;
        }

        if (hoverboardMode == HoverboardMode.Mounted) {
            playerControlling.transform.position = hoverboardSeat.transform.position;
            rotationOffset = Quaternion.Euler(0, -90, 0); // 90 degrees to the left around the y-axis
            this.transform.rotation = playerRotation * rotationOffset;
            playerControlling.ResetFallGravity();
        }
    }*/
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
        switch(state) {
            case 0:
                hoverboardMode = HoverboardMode.None;
                turnedOn = false;
                _isHoverForwardHeld = false;
                hb.useGravity = true;
                hb.isKinematic = false;
                trigger.interactable = true;
                break;
            case 1:
                hoverboardMode = HoverboardMode.Held;
                _isHoverForwardHeld = false;
                turnedOn = false;
                hb.useGravity = false;
                hb.isKinematic = true;
                break;
            case 2:
                hoverboardMode = HoverboardMode.Mounted;
                turnedOn = true;
                hb.useGravity = true;
                hb.isKinematic = false;
                trigger.interactable = false;
                SwitchModeExtension(false);
                break;
            default:
                hoverboardMode = HoverboardMode.None;
                _isHoverForwardHeld = false;
                turnedOn = false;
                hb.useGravity = true;
                hb.isKinematic = false;
                trigger.interactable = true;
                SwitchModeExtension(true);
                Plugin.Logger.LogInfo("Invalid state!");
                break;
        }
        HandleToolTips();
    }
    public override void FallWithCurve() {
        return;
    }
    public void StartBaseImportant() {
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
            HUDManager.Instance.ChangeControlTipMultiple([
                $"Dismount Hoverboard : [{Plugin.InputActionsInstance.DropHoverboard.GetBindingDisplayString().Split(" ")[0]}]",
                $"Move Hoverboard : [{Plugin.InputActionsInstance.HoverForward.GetBindingDisplayString().Split(" ")[0]}][{Plugin.InputActionsInstance.HoverLeft.GetBindingDisplayString().Split(" ")[0]}][{Plugin.InputActionsInstance.HoverBackward.GetBindingDisplayString().Split(" ")[0]}][{Plugin.InputActionsInstance.HoverRight.GetBindingDisplayString().Split(" ")[0]}]",
                $"Up Boost : [{Plugin.InputActionsInstance.HoverUp.GetBindingDisplayString().Split(" ")[0]}]",
                $"Switch Mode (Mounted) : [{Plugin.InputActionsInstance.SwitchMode.GetBindingDisplayString().Split(" ")[0]}]",
            ]);
        } else if (hoverboardMode == HoverboardMode.Held) {
            HUDManager.Instance.ChangeControlTipMultiple([
                $"Drop Hoverboard : [{Plugin.InputActionsInstance.DropHoverboard.GetBindingDisplayString().Split(" ")[0]}]",
                $"Switch Mode (Held) : [{Plugin.InputActionsInstance.SwitchMode.GetBindingDisplayString().Split(" ")[0]}]",
            ]);
        }
    }
}
