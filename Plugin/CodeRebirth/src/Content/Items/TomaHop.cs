using System;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeRebirth.src.Content.Items;
public class TomaHop : GrabbableObject
{
    // todo: implement the blendshape and making the item go up and down with the blendshape.
    // todo: implement an idle small bounce that basically does the same as the player holding space but tiny.
    // todo: maybe a small debounce on the end of the player's land?
    // todo: not allow a jump to occur if the model is partly through the ground already to prevent clipping through the ground.
    // todo: improve detection of ceilings and things above so that it doesnt go through walls.
    // todo: improve ground detection because it clips through the ground at high speeds for some reason.

    public Transform holdTransform = null!;
    public Transform topTransform = null!;
    public Transform bottomTransform = null!;

    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer = null!;
    [SerializeField] private int _blendShapeIndex = 0;

    [Header("Handling")]
    [SerializeField] private float _minChargeTimer = 0.05f; // this value is just so that if a player accidentally hits space for a frame they don't get launched
    [SerializeField] private float _rotateSpeed = 20f;
    [SerializeField] private AnimationCurve _chargeTimeToForce = AnimationCurve.Linear(0, 0, 3, 10);

    private PlayerControllerB previousPlayerHeldBy = null!;
    private float _xAngle, _yAngle = 270, _zAngle;
    private bool _isOnGround = true;
    private float _pogoChargeTimer;

    Vector3 _velocity;
    
    public override void EquipItem()
    {
        base.EquipItem();
        previousPlayerHeldBy = playerHeldBy;
        transform.position = playerHeldBy.transform.position;
        transform.rotation = playerHeldBy.transform.rotation;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        OnHitGround();
        previousPlayerHeldBy.disableMoveInput = false;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        OnHitGround();
        previousPlayerHeldBy.disableMoveInput = false;
    }

    public override void Update()
    {
        base.Update();
        if (playerHeldBy == null || !isHeld || !playerHeldBy.IsOwner)
            return;

        // Attach player to the hold transform.
        playerHeldBy.transform.SetPositionAndRotation(holdTransform.position, holdTransform.rotation);
        playerHeldBy.ResetFallGravity();
        playerHeldBy.disableMoveInput = true;

        HandleRotating();

        // Apply idle bounce & blendshape animation when on ground
        if (_isOnGround)
        {
            if (_skinnedMeshRenderer != null)
            {
                // change the blendshape number based on the charge timer
                _skinnedMeshRenderer.SetBlendShapeWeight(_blendShapeIndex, 0);
            }
        }
        
        // When on the ground and held, check for jump input.
        DetectPlayerPressingSpaceToHopUp();
    }

    bool DoRaycast(Vector3 distance, out RaycastHit hit) => Physics.Raycast(
        bottomTransform.position,
        distance.normalized,
        out hit,
        distance.magnitude,
        StartOfRound.Instance.collidersAndRoomMaskAndDefault,
        QueryTriggerInteraction.Ignore
    );
    
    void FixedUpdate() {
        if(_isOnGround) return;
        
        // this technically isn't good but oh well :3
        _velocity -= new Vector3(0, 9.8f, 0) * Time.fixedDeltaTime;
        Vector3 distanceThisFrame = _velocity * Time.fixedDeltaTime;
        
        if(DoRaycast(distanceThisFrame, out RaycastHit hitInfo)) {
            Vector3 offset = hitInfo.point - bottomTransform.position;
            transform.position += offset;

            if(Mathf.Abs(hitInfo.normal.y) < 0.05f) { // value here is how vertical the surface needs in order to be considered a wall
                Plugin.ExtendedLogging("Hit wall");
                // hit wall!
            } else if(distanceThisFrame.y > 0) {
                Plugin.ExtendedLogging("Hit ceiling");
                // hit ceiling!
            } else {
                OnHitGround();
            }
        } else {
            transform.position += distanceThisFrame;
        }
        
        Collider[] playerOrEnemyColliders = Physics.OverlapSphere(bottomTransform.position, 0.1f, CodeRebirthUtils.Instance.playersAndEnemiesAndHazardMask, QueryTriggerInteraction.Collide);
        if(playerOrEnemyColliders.Length <= 0) return;
        // Damage enemy.
        foreach (Collider collider in playerOrEnemyColliders)
        {
            if (!collider.TryGetComponent(out IHittable hit))
                continue;
            hit.Hit(1, Vector3.up, playerHeldBy, true, 1);
        }
        _pogoChargeTimer += 0.5f;
        //Launch();
    }

    public override void LateUpdate()
    {
        if (playerHeldBy != null && isHeld)
        {
            RotateAroundPoint(_xAngle, _yAngle, _zAngle);
            return;
        }
        base.LateUpdate();
    }

    private void RotateAroundPoint(float xAngle, float yAngle, float zAngle)
    {
        Quaternion targetRotation = Quaternion.Euler(xAngle, yAngle, zAngle);
        transform.rotation = targetRotation;
    }

    public void HandleRotating()
    {
        // handle rotation (THIS SHOULD BE USING A VECTOR 2 COMPOSITE INPUT ACTION!!)
        float horizontal = 0, vertical = 0;
        if (Keyboard.current.aKey.isPressed)
            horizontal += _rotateSpeed * Time.deltaTime;
        if (Keyboard.current.dKey.isPressed)
            horizontal -= _rotateSpeed * Time.deltaTime;
        if (Keyboard.current.wKey.isPressed)
            vertical += _rotateSpeed * Time.deltaTime;
        if (Keyboard.current.sKey.isPressed)
            vertical -= _rotateSpeed * Time.deltaTime;

        _xAngle = Mathf.Clamp(_xAngle + vertical, -30, 30);
        _zAngle = Mathf.Clamp(_zAngle + horizontal, -30, 30);
    }

    public void DetectPlayerPressingSpaceToHopUp()
    {
        if (Keyboard.current.spaceKey.isPressed)
        {
            _pogoChargeTimer += Time.deltaTime;
            return;
        }

        if (_pogoChargeTimer < _minChargeTimer)
        {
            _pogoChargeTimer = 0;
            return;
        }

        Plugin.ExtendedLogging($"pogo charge timer: {_pogoChargeTimer}");
        Launch(_xAngle, _zAngle);
    }

    public void Launch(float xAngle, float zAngle)
    {
        Plugin.ExtendedLogging($"x: {xAngle}, z: {zAngle}");
        float force = _chargeTimeToForce.Evaluate(_pogoChargeTimer);

        Quaternion xRotation = Quaternion.AngleAxis(xAngle, Vector3.forward);
        Quaternion zRotation = Quaternion.AngleAxis(-zAngle, Vector3.right);
        Vector3 direction = zRotation * xRotation * Vector3.up;
        
        Vector3 launchVector = direction * force * 5f; // 5f is a temporay value and should be removed, i just don't want to keep rebuilding the bundle.
        Plugin.ExtendedLogging($"launching player with vector: {launchVector}");

        _isOnGround = false;
        _velocity = launchVector;
        _pogoChargeTimer = 0;
    }

    public void OnHitGround()
    {
        Plugin.ExtendedLogging("Hit ground");
        
        _isOnGround = true;
        _velocity = Vector3.zero;
    }
}