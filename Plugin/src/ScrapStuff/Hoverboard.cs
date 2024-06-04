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

namespace CodeRebirth.ScrapStuff;
public class Hoverboard : GrabbableObject, IHittable
{
    Rigidbody hb;
    public InteractTrigger trigger;
    private bool turnedOn = false;
    public float mult;
    public float turnTorque;
    public GameObject hoverboardSeat;
    public BoxCollider playerRidingCollider;
    public PlayerControllerB playerControlling;
    public Transform[] anchors = new Transform[4];
    public RaycastHit[] hits = new RaycastHit[4];
    private bool _isHoverForwardHeld = false;
    private bool jumpCooldown = true;

    public enum HoverboardTypes {
        Regular
    }
    public override void Start()
    {
        Plugin.InputActionsInstance.HoverForward.performed += OnHoverForward;
        Plugin.InputActionsInstance.HoverForward.canceled += OnHoverForward;
        Plugin.InputActionsInstance.HoverLeft.performed += MovementHandler;
        Plugin.InputActionsInstance.HoverRight.performed += MovementHandler;
        Plugin.InputActionsInstance.HoverBackward.performed += MovementHandler;
        Plugin.InputActionsInstance.HoverForward.performed += MovementHandler;
        Plugin.InputActionsInstance.HoverUp.performed += MovementHandler;
        Plugin.InputActionsInstance.SwitchMode.performed += ModeHandler;

        hb = GetComponent<Rigidbody>();
        trigger = GetComponent<InteractTrigger>();
        trigger.onInteract.AddListener(OnInteract);
    }
    public void ModeHandler(InputAction.CallbackContext context) {
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame)
        {
            trigger.interactable = !trigger.interactable;
            trigger.holdInteraction = trigger.interactable;
        }
    }
    public void OnInteract(PlayerControllerB player) {
        if (trigger.holdInteraction) {
            StartCoroutine(HandleGrabbing(this, player));
            return;
        }
        if (!IsHost) SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        else SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        trigger.interactable = false;
    }

    public void OnHoverForward(InputAction.CallbackContext context) {
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame)
        {
            // Button has started being held
            _isHoverForwardHeld = true;
        }

        if (btn.wasReleasedThisFrame)
        {
            // Button was released
            _isHoverForwardHeld = false;
        }
    }

    public IEnumerator HandleGrabbing(GrabbableObject _obj, PlayerControllerB player) {
        yield return new WaitForEndOfFrame();
        trigger.interactable = false;
        trigger.holdInteraction = false;
        trigger.enabled = false;
        _obj.InteractItem();
        if(GameNetworkManager.Instance.localPlayerController.FirstEmptyItemSlot() == -1){
            Plugin.Logger.LogInfo("GiveItemToPlayer: Could not grab item, inventory full!");
            yield break;
        }
        player.twoHanded = _obj.itemProperties.twoHanded;
        player.carryWeight += Mathf.Clamp(_obj.itemProperties.weight - 1f, 0f, 10f);
        player.grabbedObjectValidated = true;
        player.GrabObjectServerRpc(_obj.NetworkObject);
        _obj.GrabItemOnClient();
        _obj.parentObject = player.localItemHolder;
        player.isHoldingObject = true;
        _obj.hasBeenHeld = true;
        _obj.EnablePhysics(false);
    }
    public void MovementHandler(InputAction.CallbackContext context) {
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame)
        {
            HandleMovement();
        }
    }
    public override void DiscardItem() {
        base.DiscardItem();
        this.transform.position = playerHeldBy.transform.position + transform.forward * 1.5f;
        trigger.enabled = true;
        trigger.interactable = true;
    }
    public void FixedUpdate()
    {
        if (playerControlling != null) {
            playerControlling.transform.position = hoverboardSeat.transform.position;
            Quaternion playerRotation = playerControlling.transform.rotation;
            Quaternion rotationOffset = Quaternion.Euler(0, -90, 0); // 90 degrees to the left around the y-axis
            this.transform.rotation = playerRotation * rotationOffset;
            playerControlling.ResetFallGravity();
        }
        if (_isHoverForwardHeld && playerControlling == GameNetworkManager.Instance.localPlayerController)
            hb.AddForce(Vector3.zero + transform.right * 40f * (turnedOn ? 1f : 0.1f), ForceMode.Acceleration);
        if (!turnedOn) return;
        for (int i = 0; i < 4; i++)
            ApplyForce(anchors[i], hits[i]);
    }

    public override void Update()
    {
        base.Update();
        if (playerControlling == null) return;
        if (playerControlling.inAnimationWithEnemy || playerControlling.inSpecialInteractAnimation || playerControlling.isClimbingLadder) {
            DropHoverboard(false, true);
            return;
        }
        HandleDropping();
        HandleTurningOnOrOff();
    }

    public void HandleTurningOnOrOff() {
        if (!Plugin.InputActionsInstance.TurnOnHoverboard.triggered) return;
        turnedOn = !turnedOn;
    }
    public void HandleDropping() {
        if (!Plugin.InputActionsInstance.DropHoverboard.triggered) return;
        DropHoverboard(turnedOn, true);
    }
    public void DropHoverboard(bool keepOn = true, bool stillWorks = true) {
        if (!IsHost) SetTargetServerRpc(-1);
        else SetTargetClientRpc(-1);
        turnedOn = keepOn;
        trigger.interactable = stillWorks;
    }
    public override void LateUpdate() {
        base.LateUpdate();
    }
    private void HandleMovement() // the reason these transform.forward and transform.right seemingly don't match with the buttons is because the exported hoverboard is kinda fucked... oh well.
    {
        if (playerControlling == null) return;
        if (GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        Vector3 forceDirection = Vector3.zero;
        float moveForce = 100f;

        if (Plugin.InputActionsInstance.HoverLeft.WasPressedThisFrame())
            forceDirection += transform.forward;

        if (Plugin.InputActionsInstance.HoverRight.WasPressedThisFrame())
            forceDirection -= transform.forward;

        if (Plugin.InputActionsInstance.HoverBackward.WasPressedThisFrame())
            forceDirection -= transform.right;

        if (Plugin.InputActionsInstance.HoverForward.WasPressedThisFrame())
            forceDirection += transform.right;

        if (Plugin.InputActionsInstance.HoverUp.WasPressedThisFrame() && turnedOn && jumpCooldown) {
            jumpCooldown = false;
            forceDirection += transform.up;
            moveForce = 1000f;
            StartCoroutine(JumpTimerStart());
        }

        if (forceDirection == Vector3.zero) return;
        Plugin.Logger.LogInfo("moveForce: " + moveForce);
        Plugin.Logger.LogInfo("forceDirection: " + forceDirection);
        if (!turnedOn) 
            moveForce *= 0.1f;
        hb.AddForce(forceDirection * moveForce, ForceMode.Acceleration);
    }
    public IEnumerator JumpTimerStart() {
        yield return new WaitForSeconds(2f);
        jumpCooldown = true;
    }
    public override void FallWithCurve() {
        return;
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
        hb.AddForce(hitDirection.normalized * force * 100, ForceMode.Impulse);

		return true; // this bool literally doesn't get used. i have no idea.
	}
    [ServerRpc(RequireOwnership = false)]
    internal void SetTargetServerRpc(int PlayerID) {
        SetTargetClientRpc(PlayerID);
    }
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
}
