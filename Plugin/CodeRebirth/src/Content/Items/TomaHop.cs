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

    [SerializeField] private Rigidbody _rb = null!;

    [Header("Handling")]
    [SerializeField] private float _minChargeTimer = 0.05f; // this value is just so that if a player accidentally hits space for a frame they don't get launched
    [SerializeField] private float _rotateSpeed = 20f;
    [SerializeField] private AnimationCurve _chargeTimeToForce = AnimationCurve.Linear(0, 0, 3, 10);

    private PlayerControllerB previousPlayerHeldBy = null!;
    private float _xAngle, _yAngle, _zAngle;
    private bool isOnGround = true;
    private float _pogoChargeTimer;
    private float _triggerTimer;

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
        SetRigidBodyToGround();
        previousPlayerHeldBy.disableMoveInput = false;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        SetRigidBodyToGround();
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
        if (isOnGround)
        {
            if (_skinnedMeshRenderer != null)
            {
                // change the blendshape number based on the charge timer
                _skinnedMeshRenderer.SetBlendShapeWeight(_blendShapeIndex, 0);
            }
        }

        // If the item is airborne, check for collisions overhead or below.
        if (!isOnGround)
        {
            _triggerTimer -= Time.deltaTime;
            if (_triggerTimer > 0)
                return;

            bool hitCeiling = Physics.SphereCast(topTransform.position, 0.2f, transform.up, out _, 0.3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
            if (hitCeiling)
            {
                _rb.velocity = new Vector3(_rb.velocity.x/2, -2f, _rb.velocity.z/2);
                return;
            }

            Collider[] playerOrEnemyColliders = Physics.OverlapSphere(bottomTransform.position, 0.1f, CodeRebirthUtils.Instance.playersAndEnemiesAndHazardMask, QueryTriggerInteraction.Collide);
            if (playerOrEnemyColliders.Length > 0)
            {
                // Damage enemy.
                foreach (Collider collider in playerOrEnemyColliders)
                {
                    if (!collider.TryGetComponent(out IHittable hit))
                        continue;
                    hit.Hit(1, Vector3.up, playerHeldBy, true, 1);
                }
                _pogoChargeTimer += 0.5f;
                ApplyForceToRigidBody();
                return;
            }
            bool hitGround = Physics.CheckSphere(bottomTransform.position, 0.2f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
            if (!hitGround) return;
            SetRigidBodyToGround();
            Plugin.ExtendedLogging("Hit ground");
            return;
        }
        // When on the ground and held, check for jump input.
        DetectPlayerPressingSpaceToHopUp();
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

        Plugin.ExtendedLogging($"_xAngle: {_xAngle}, _yAngle: {_yAngle}, _zAngle: {_zAngle}");
        Vector2 mouseDelta = Plugin.InputActionsInstance.MouseDelta.ReadValue<Vector2>();
        _yAngle += mouseDelta.x * _rotateSpeed * 0.25f * Time.deltaTime;
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
        ApplyForceToRigidBody();
    }

    public void ApplyForceToRigidBody()
    {
        float force = _chargeTimeToForce.Evaluate(_pogoChargeTimer);
        _triggerTimer = 0.25f;
        Vector3 launchVector = transform.up * force * 5f; // 5f is a temporay value and should be removed, i just don't want to keep rebuilding the bundle.
        Plugin.ExtendedLogging($"launching player with vector: {launchVector}");

        isOnGround = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.AddForce(launchVector, ForceMode.Impulse);
        _pogoChargeTimer = 0;
    }

    public void SetRigidBodyToGround()
    {
        isOnGround = true;
        if (!_rb.isKinematic)
            _rb.velocity = Vector3.zero;
        _rb.isKinematic = true;
        _rb.useGravity = false;
    }
}