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
using Unity.Mathematics;

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
    private Transform oldParent;
    private Transform hoverboardChild;
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
        SwitchModeExtension(true);
        hoverboardChild = transform.Find("HoverboardChild");
        if (!IsHost) return;
        SetHoverboardStateClientRpc(0);
    }
    public void OnInteract(PlayerControllerB player) {
        if (GameNetworkManager.Instance.localPlayerController != player) return;
        if (hoverboardMode == HoverboardMode.None) {            
            if (IsHost) {
                SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                SetHoverboardStateClientRpc(2);
            } else {
                SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                SetHoverboardStateServerRpc(2);
            }
        }
    }
    public void ModeHandler(InputAction.CallbackContext context) {
        if (hoverboardMode == HoverboardMode.None || playerControlling == null || playerControlling != GameNetworkManager.Instance.localPlayerController) return;
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame)
        {
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
        if (btn.wasPressedThisFrame)
        {
            HandleMovement();
        }
    }

    public override void Update()
    {
        base.Update();
        if (HandleDropping()) return;
        if (playerControlling == null || !IsHost) return;
        if (hoverboardMode == HoverboardMode.Mounted && playerControlling.transform.GetParent() != hoverboardChild.Find("Seat")) {
            playerControlling.transform.SetParent(hoverboardChild.Find("Seat"), true);
            Plugin.Logger.LogInfo($"Setting parent of {playerControlling} to {playerControlling.transform.GetParent().name}");
        }
        if (Vector3.Distance(hoverboardChild.position, playerControlling.transform.position) > 5f || playerControlling.inAnimationWithEnemy || playerControlling.inSpecialInteractAnimation || (hoverboardMode == HoverboardMode.Mounted && playerControlling.isClimbingLadder)) {
            DropHoverboard();
            return;
        }
        #region stuff gets weird
        // maybe i should have this be in fixed update
        if (hoverboardMode == HoverboardMode.Held) {
            // Position the hoverboard to the right side of the player
            hoverboardChild.position = playerControlling.transform.position + playerControlling.transform.right * 0.7f + playerControlling.transform.up * 1f;
            // Make the hoverboard face upwards with its up vector pointing to the player's right hand
            Quaternion rotationOffset = Quaternion.Euler(180, 180, -90); // Adjust to match correct facing direction
            // hoverboardChild.rotation = playerControlling.transform.rotation * rotationOffset;
            }
            if (hoverboardMode == HoverboardMode.Mounted) {
                if (turnedOn) {
                    for (int i = 0; i < 4; i++) {
                        ApplyForce(anchors[i], hits[i]);
                    }
                }
                if (_isHoverForwardHeld) {
                    hb.AddForce(Vector3.zero + hoverboardChild.right * 15f, ForceMode.Acceleration);
                }

                // Get the player's head rotation (assuming the player's head is represented by a transform)
                Quaternion playerHeadRotation = playerControlling.transform.rotation;

                // Calculate the desired rotation for the hoverboard child based on the player's head rotation
                // You might want to smooth this rotation to avoid sudden changes
                Quaternion desiredHoverboardRotation = Quaternion.Euler(0, playerHeadRotation.eulerAngles.y, 0);

                // Apply the rotation to the hoverboard child
                hoverboardChild.rotation = desiredHoverboardRotation;

                // Adjust the player's rotation to counter the hoverboard child's rotation
                Quaternion counterRotation = Quaternion.Inverse(desiredHoverboardRotation);
                playerControlling.transform.rotation = counterRotation * playerHeadRotation;

                // Ensure the hoverboard seat follows the hoverboard child's rotation
                hoverboardSeat.transform.rotation = hoverboardChild.rotation;
                Plugin.Logger.LogInfo($"{hoverboardChild.rotation.x} + {hoverboardChild.rotation.y} + {hoverboardChild.rotation.z} + {hoverboardChild.rotation.w}");

                // Reset the player's fall gravity (if necessary)
                playerControlling.ResetFallGravity();
            }

        //UpdatePositionsForClientsClientRpc(this.transform.position, this.transform.rotation, playerControlling.transform.position, playerControlling.transform.rotation);
        #endregion
    }
    [ClientRpc]
    public void UpdatePositionsForClientsClientRpc(Vector3 position, Quaternion rotation, Vector3 playerPosition, Quaternion playerRotation) {
        if (IsHost) return;
        this.transform.position = position;
        this.transform.rotation = rotation;
        playerControlling.transform.position = playerPosition;
        playerControlling.transform.rotation = playerRotation;
    }
    public override void LateUpdate() {
        base.LateUpdate();
        if (!IsHost) return;
        PlayerControllerB realPlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault();
        if (Vector3.Distance(transform.position, StartOfRound.Instance.shipBounds.transform.position) < 15 && !isInShipRoom) {
            this.transform.SetParent(realPlayer.playersManager.elevatorTransform, true);
            isInShipRoom = true;
            isInElevator = true;
        } else if (Vector3.Distance(transform.position, StartOfRound.Instance.shipBounds.transform.position) >= 15 && isInShipRoom) {
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

    private void HandleMovement() // the reason these transform.forward and transform.right seemingly don't match with the buttons is because the exported hoverboard is kinda fucked... oh well.
    {
        if (playerControlling == null) return;
        Vector3 forceDirection = Vector3.zero;
        float moveForce = 50f;
        
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
            moveForce = 500f;
            StartCoroutine(JumpTimerStart());
        }

        if (forceDirection == Vector3.zero) return;
        if (IsHost) {
            HbAddForceClientRpc(forceDirection, moveForce, false);
        } else {
            HbAddForceServerRpc(forceDirection, moveForce, false);
        }
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
            HbAddForceClientRpc(hitDirection.normalized, force * 100, true);
        } else {
            HbAddForceServerRpc(hitDirection.normalized, force * 100, true);
        }

		return true; // this bool literally doesn't get used. i have no idea.
	}

    [ServerRpc(RequireOwnership = false)]
    internal void HbAddForceServerRpc(Vector3 forceDirection, float moveForce, bool impulse) {
        HbAddForceClientRpc(forceDirection, moveForce, impulse);
    }

    [ClientRpc]
    internal void HbAddForceClientRpc(Vector3 forceDirection, float moveForce, bool impulse) {
        if (!impulse) hb.AddForce(forceDirection * moveForce, ForceMode.Acceleration);
        else hb.AddForce(forceDirection * moveForce, ForceMode.Impulse);
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
            // playerControlling.transform.rotation = Quaternion.identity;
            playerControlling.playerActions.Movement.Jump.Enable();
            playerControlling.playerActions.Movement.Move.Enable();
            playerControlling.transform.SetParent(null, true);
            playerControlling = null;
            // playerRidingCollider.enabled = false;
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
        playerControlling = StartOfRound.Instance.allPlayerScripts[PlayerID];
        if (IsHost) {
            //this.transform.Find("PlayerOffsetGameObject").transform.position = Vector3.zero;
            oldParent = playerControlling.transform.GetParent();
            Transform parentToUse = hoverboardSeat.transform.GetParent();
            playerControlling.transform.SetParent(parentToUse, true);
        }
        playerControlling.playerActions.Movement.Jump.Disable();
        playerControlling.playerActions.Movement.Move.Disable();
        playerControlling.transform.position = hoverboardSeat.transform.position;
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
        if (playerControlling == null) {
            Plugin.Logger.LogInfo($"Player controlling is null for me...");
        }
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
