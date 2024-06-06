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
    private PlayerControllerB playerControlling;
    public Transform[] anchors = new Transform[4];
    public RaycastHit[] hits = new RaycastHit[4];
    private PlayerControllerB previousPlayerHeldBy;
    private bool _isHoverForwardHeld = false;
    private bool jumpCooldown = true;
    private Quaternion targetRotation;
    private bool isAdjusting = false;
    public enum HoverboardMode {
        None,
        Held,
        Mounted,
    }
    public enum HoverboardTypes {
        Regular
        // Eventually wanna make other types of hoverboards
    }
    private HoverboardMode hoverboardMode = HoverboardMode.None;
    public void Awake() {
        if (isInShipRoom) {
            this.transform.position = StartOfRound.Instance.shipBounds.transform.position;
        }
    }
    public override void Start()
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
        
        trigger = GetComponent<InteractTrigger>();
        hb = GetComponent<Rigidbody>();
        trigger.onInteract.AddListener(OnInteract);
    }
    public void ModeHandler(InputAction.CallbackContext context) {
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame)
        {
            if (hoverboardMode == HoverboardMode.Mounted) {
                hoverboardMode = HoverboardMode.Held;
                Plugin.InputActionsInstance.IncreaseHover.performed -= MovementHandler;
                Plugin.InputActionsInstance.DecreaseHover.performed -= MovementHandler;
                Plugin.InputActionsInstance.HoverForward.performed -= OnHoverForward;
                Plugin.InputActionsInstance.HoverForward.canceled -= OnHoverForward;
                Plugin.InputActionsInstance.HoverLeft.performed -= MovementHandler;
                Plugin.InputActionsInstance.HoverRight.performed -= MovementHandler;
                Plugin.InputActionsInstance.HoverBackward.performed -= MovementHandler;
                Plugin.InputActionsInstance.HoverForward.performed -= MovementHandler;
                Plugin.InputActionsInstance.HoverUp.performed -= MovementHandler;
                Plugin.InputActionsInstance.SwitchMode.performed += ModeHandler;
                turnedOn = false;
                hb.useGravity = false;
                hb.isKinematic = true;
            } else if (hoverboardMode == HoverboardMode.Held) {
                hoverboardMode = HoverboardMode.Mounted;
                Plugin.InputActionsInstance.IncreaseHover.performed += MovementHandler;
                Plugin.InputActionsInstance.DecreaseHover.performed += MovementHandler;
                Plugin.InputActionsInstance.HoverForward.performed += OnHoverForward;
                Plugin.InputActionsInstance.HoverForward.canceled += OnHoverForward;
                Plugin.InputActionsInstance.HoverLeft.performed += MovementHandler;
                Plugin.InputActionsInstance.HoverRight.performed += MovementHandler;
                Plugin.InputActionsInstance.HoverBackward.performed += MovementHandler;
                Plugin.InputActionsInstance.HoverForward.performed += MovementHandler;
                Plugin.InputActionsInstance.HoverUp.performed += MovementHandler;
                Plugin.InputActionsInstance.SwitchMode.performed += ModeHandler;
                hb.useGravity = true;
                hb.isKinematic = false;
            }
        }
    }
    public void OnInteract(PlayerControllerB player) {
        if (hoverboardMode == HoverboardMode.None) {
            Plugin.InputActionsInstance.HoverForward.performed += OnHoverForward;
            Plugin.InputActionsInstance.HoverForward.canceled += OnHoverForward;
            Plugin.InputActionsInstance.HoverLeft.performed += MovementHandler;
            Plugin.InputActionsInstance.HoverRight.performed += MovementHandler;
            Plugin.InputActionsInstance.HoverBackward.performed += MovementHandler;
            Plugin.InputActionsInstance.HoverForward.performed += MovementHandler;
            Plugin.InputActionsInstance.HoverUp.performed += MovementHandler;
            Plugin.InputActionsInstance.SwitchMode.performed += ModeHandler;
            if (!IsHost) SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            else SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            trigger.interactable = false;
            hoverboardMode = HoverboardMode.Mounted;
        }
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

    }
    public void MovementHandler(InputAction.CallbackContext context) {
        var btn = (ButtonControl)context.control;
        if (btn.wasPressedThisFrame)
        {
            HandleMovement();
        }
    }
    public void FixedUpdate()
    {
        if (playerControlling != null && hoverboardMode == HoverboardMode.Mounted && Vector3.Distance(transform.position, playerControlling.transform.position) > 5f) {
            DropHoverboard(false, true);
            return;
        }
        if (hoverboardMode == HoverboardMode.Held) {
            // Position the hoverboard to the right side of the player
            this.transform.position = playerControlling.transform.position + playerControlling.transform.right * 0.7f + playerControlling.transform.up * 1f;

            // Make the hoverboard face upwards with its up vector pointing to the player's right hand
            Quaternion playerRotation = playerControlling.transform.rotation;
            Quaternion rotationOffset = Quaternion.Euler(180, 180, -90); // Adjust to match correct facing direction
            this.transform.rotation = playerRotation * rotationOffset;

            return;
        }
        if (playerControlling != null && hoverboardMode == HoverboardMode.Mounted) {
            playerControlling.transform.position = hoverboardSeat.transform.position;
            Quaternion playerRotation = playerControlling.transform.rotation;
            Quaternion rotationOffset = Quaternion.Euler(0, -90, 0); // 90 degrees to the left around the y-axis
            this.transform.rotation = playerRotation * rotationOffset;
            playerControlling.ResetFallGravity();
        }
        if (_isHoverForwardHeld && playerControlling == GameNetworkManager.Instance.localPlayerController)
            hb.AddForce(Vector3.zero + transform.right * 40f * (turnedOn ? 1f : 0.05f), ForceMode.Acceleration);
        if (!turnedOn) return;
        for (int i = 0; i < 4; i++)
            ApplyForce(anchors[i], hits[i]);
        if (!isAdjusting)
            CheckIfUpsideDown();
    }

    public override void Update()
    {
        base.Update();
        PlayerControllerB livePlayer = StartOfRound.Instance.allPlayerScripts.FirstOrDefault(x => x.isPlayerControlled);
        if (Vector3.Distance(transform.position, StartOfRound.Instance.shipBounds.transform.position) < 15 && !isInShipRoom) {
            this.transform.SetParent(livePlayer.playersManager.elevatorTransform, true);
            isInShipRoom = true;
            isInElevator = true;
        } else if (Vector3.Distance(transform.position, StartOfRound.Instance.shipBounds.transform.position) >= 15 && isInShipRoom) {
            this.transform.SetParent(livePlayer.playersManager.propsContainer, true);
            isInShipRoom = false;
            isInElevator = false;
        }
        if (playerControlling == null) return;
        if (playerControlling.inAnimationWithEnemy || playerControlling.inSpecialInteractAnimation || (hoverboardMode == HoverboardMode.Mounted && playerControlling.isClimbingLadder)) {
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
        hb.useGravity = true;
        hb.isKinematic = false;
        hoverboardMode = HoverboardMode.None;
        Plugin.InputActionsInstance.IncreaseHover.performed -= MovementHandler;
        Plugin.InputActionsInstance.DecreaseHover.performed -= MovementHandler;
        Plugin.InputActionsInstance.HoverForward.performed -= OnHoverForward;
        Plugin.InputActionsInstance.HoverForward.canceled -= OnHoverForward;
        Plugin.InputActionsInstance.HoverLeft.performed -= MovementHandler;
        Plugin.InputActionsInstance.HoverRight.performed -= MovementHandler;
        Plugin.InputActionsInstance.HoverBackward.performed -= MovementHandler;
        Plugin.InputActionsInstance.HoverForward.performed -= MovementHandler;
        Plugin.InputActionsInstance.HoverUp.performed -= MovementHandler;
        Plugin.InputActionsInstance.SwitchMode.performed -= ModeHandler;
        turnedOn = keepOn;
        trigger.interactable = stillWorks;
    }

    private void HandleMovement() // the reason these transform.forward and transform.right seemingly don't match with the buttons is because the exported hoverboard is kinda fucked... oh well.
    {
        if (playerControlling == null) return;
        if (GameNetworkManager.Instance.localPlayerController != playerControlling) return;
        Vector3 forceDirection = Vector3.zero;
        float moveForce = 100f;
        if (Plugin.InputActionsInstance.IncreaseHover.WasPressedThisFrame())
            mult += 0.2f;
        if (Plugin.InputActionsInstance.DecreaseHover.WasPressedThisFrame())
            mult -= 0.2f;
        Mathf.Clamp(mult, 0f, 999f);
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
    // New Methods
    private void CheckIfUpsideDown()
    {
        // Check if the hoverboard's up vector is pointing down
        if (Vector3.Dot(transform.up, Vector3.down) > 0)
        {
            // If upside down, start the adjustment process
            targetRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
            StartCoroutine(AdjustOrientation());
        }
    }

    private IEnumerator AdjustOrientation()
    {
        isAdjusting = true;
        float duration = 1f;
        float elapsed = 0f;
        Quaternion initialRotation = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, elapsed / duration);
            yield return null;
        }

        transform.rotation = targetRotation;
        isAdjusting = false;
    }
}
