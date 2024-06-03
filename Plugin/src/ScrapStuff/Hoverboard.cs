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
    public float moveForce = 10f;
    public float turnTorque;
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
        if (playerControlling != null)
            playerControlling.transform.position = transform.position + (transform.up * 1.5f);
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
        HandleTurningOn();
    }
    public void HandleTurningOn() {
        if (!Plugin.InputActionsInstance.TurnOnHoverboard.triggered) return;
        turnedOn = true;
    }
    public void HandleDropping() {
        if (!Plugin.InputActionsInstance.DropHoverboard.triggered) return;
        DropHoverboard(true, true);
    }
    public void DropHoverboard(bool keepOn = true, bool stillWorks = true) {
        if (!IsHost) SetTargetServerRpc(-1);
        else SetTargetClientRpc(-1);
        turnedOn = keepOn;
        trigger.interactable = stillWorks;
    }
    public override void LateUpdate() {
        return;
    }
    private void HandleMovement()
    {
        Vector3 forceDirection = Vector3.zero;

        if (Plugin.InputActionsInstance.HoverForward.triggered)
            forceDirection += transform.forward;

        if (Plugin.InputActionsInstance.HoverBackward.triggered)
            forceDirection -= transform.forward;

        if (Plugin.InputActionsInstance.HoverRight.triggered)
            forceDirection += transform.right;

        if (Plugin.InputActionsInstance.HoverLeft.triggered)
            forceDirection -= transform.right;

        if (Plugin.InputActionsInstance.HoverUp.triggered)
            forceDirection += transform.up;
        hb.AddForce(forceDirection.normalized * moveForce, ForceMode.Acceleration);
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
            Plugin.Logger.LogInfo("hit room");
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
