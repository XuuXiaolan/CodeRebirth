using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using CodeRebirth.Keybinds;

namespace CodeRebirth.ScrapStuff;
public class Hoverboard : GrabbableObject
{
    Rigidbody hb;
    private bool turnedOn = false;
    public float mult;
    public float moveForce;
    public float turnTorque;
    public PlayerControllerB playerControlling;
    
    public override void Start()
    {
        hb = GetComponent<Rigidbody>();
    }

    public Transform[] anchors = new Transform[4];
    public RaycastHit[] hits = new RaycastHit[4];

    public void FixedUpdate()
    {
        if (!turnedOn) return; // need to set up an interact trigger where the player can mount the hoverboard, turn it on, handle movements etc
        for (int i = 0; i < 4; i++)
            ApplyForce(anchors[i], hits[i]);
        HandleMovement();
    }

    public override void Update()
    {
        base.Update();
        if (playerHeldBy.inAnimationWithEnemy || playerHeldBy.inSpecialInteractAnimation) {
            playerHeldBy.DiscardHeldObject();
            return;
        } // Change a lot of this to where it uses an interact trigger instead of grabbable object's grab function
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
}
