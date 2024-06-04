using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using CodeRebirth.Keybinds;
using Unity.Netcode;
using System;

namespace CodeRebirth.ScrapStuff;
public class Hoverboard : GrabbableObject, IHittable
{
    Rigidbody hb;
    public InteractTrigger trigger;
    private bool turnedOn = false;
    public float mult;
    public float turnTorque;
    public GameObject hoverboardSeat;
    public PlayerControllerB playerControlling;
    public enum HoverboardTypes {
        Regular
    }
    public override void Start()
    {
        hb = GetComponent<Rigidbody>();
        trigger = GetComponent<InteractTrigger>();
        trigger.onInteract.AddListener(OnInteract);
    }
    public Transform[] anchors = new Transform[4];
    public RaycastHit[] hits = new RaycastHit[4];
    public void OnInteract(PlayerControllerB player) {
        if (!IsHost) SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        else SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        trigger.interactable = false;
    }

    public void FixedUpdate()
    {
        if (!turnedOn) return;
        for (int i = 0; i < 4; i++)
            ApplyForce(anchors[i], hits[i]);
        HandleMovement();
        if (playerControlling == null) return;    
        playerControlling.transform.position = transform.position + (transform.up * 1.5f);
        Quaternion playerRotation = playerControlling.transform.rotation;
        Quaternion rotationOffset = Quaternion.Euler(0, -90, 0); // 90 degrees to the left around the y-axis
        this.transform.rotation = playerRotation * rotationOffset;
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
        if (playerControlling == null) return;
        playerControlling.transform.position = hoverboardSeat.transform.position;
        playerControlling.ResetFallGravity();
    }
    private void HandleMovement() // the reason these transform.forward and transform.right seemingly don't match with the buttons is because the exported hoverboard is kinda fucked... oh well.
    {
        if (GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        Vector3 forceDirection = Vector3.zero;
        float moveForce = 200f;

        if (Plugin.InputActionsInstance.HoverLeft.triggered)
            forceDirection += transform.forward;

        if (Plugin.InputActionsInstance.HoverRight.triggered)
            forceDirection -= transform.forward;

        if (Plugin.InputActionsInstance.HoverForward.triggered)
            forceDirection += transform.right;

        if (Plugin.InputActionsInstance.HoverBackward.triggered)
            forceDirection -= transform.right;

        if (Plugin.InputActionsInstance.HoverUp.triggered) {
            forceDirection += transform.up;
            moveForce = 1000f;
        }

        if (forceDirection == Vector3.zero) return;
        Plugin.Logger.LogInfo("moveForce: " + moveForce);
        Plugin.Logger.LogInfo("forceDirection: " + forceDirection);
        hb.AddForce(forceDirection * moveForce, ForceMode.Acceleration);
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
        hb.AddForce(hitDirection.normalized * force, ForceMode.Impulse);

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
            Plugin.Logger.LogInfo($"Clearing target on {this}");
            return;
        }
        if (StartOfRound.Instance.allPlayerScripts[PlayerID] == null) {
            Plugin.Logger.LogInfo($"Index invalid! {this}");
            return;
        }
        playerControlling = StartOfRound.Instance.allPlayerScripts[PlayerID];
        Plugin.Logger.LogInfo($"{this} setting target to: {playerControlling.playerUsername}");
    }
}
