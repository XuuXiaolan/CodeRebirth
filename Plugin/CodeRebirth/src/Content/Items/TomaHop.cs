using System;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeRebirth.src.Content.Items;
public class TomaHop : GrabbableObject
{
    // todo: maybe a small debounce on the end of the player's land?
    // todo: ground and ceiling detection probably break due to player rotating on their own so do something about that, maybe a sphere instead of a raycast??

    [SerializeField]
    private InteractTrigger trigger = null!;

    [SerializeField]
    private Transform realPogo = null!;

    [SerializeField]
    private Transform holdTransform = null!;

    [SerializeField]
    private Transform topTransform = null!;

    [SerializeField]
    private Transform bottomTransform = null!;

    [SerializeField]
    private SkinnedMeshRenderer _skinnedMeshRenderer = null!;

    [SerializeField]
    private int _blendShapeIndex = 0;

    [Header("Handling")]
    [SerializeField]
    private float _minChargeTimer = 0.05f; // this value is just so that if a player accidentally hits space for a frame they don't get launched

    [SerializeField]
    private float _jumpTimerMax = 0.1f;

    [SerializeField]
    private float _rotateSpeed = 20f;

    [SerializeField]
    private AnimationCurve _chargeTimeToForce = AnimationCurve.Linear(0, 0, 3, 10);

    private PlayerControllerB previousPlayerHeldBy = null!;
    private float _xAngle, _yAngle = 270, _zAngle;
    private bool _isOnGround = true;
    private float _jumpTimer = 0.1f;
    private float _pogoChargeTimer;
    private Vector3 _velocity;
    private Collider[] _cachedColliders = new Collider[4];

    public override void Start()
    {
        base.Start();
        trigger.onInteract.AddListener(OnInteract);
    }

    private void OnInteract(PlayerControllerB player)
    {
        if (!player.IsOwner) return;
        // player.carryWeight += Mathf.Clamp(this.itemProperties.weight - 1f, 0f, 10f);
        player.GrabObjectServerRpc(this.NetworkObject);
        parentObject = player.localItemHolder;
        GrabItemOnClient();
    }

    public override void EquipItem()
    {
        base.EquipItem();
        previousPlayerHeldBy = playerHeldBy;
        realPogo.transform.position = playerHeldBy.transform.position;
        realPogo.transform.rotation = playerHeldBy.transform.rotation;
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        OnPogoHitGround();
        Plugin.ExtendedLogging($"Toma Hop Discarded with position: {realPogo.transform.position} and rotation: {realPogo.transform.rotation}");
        _pogoChargeTimer = 0f;
        Launch();
        previousPlayerHeldBy.disableMoveInput = false;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        OnPogoHitGround();
        previousPlayerHeldBy.disableMoveInput = false;
    }

    public override void Update()
    {
        parentObject = null;
        base.Update();
        trigger.interactable = !isHeld;
        if (playerHeldBy == null || isPocketed)
            return;

        if (_pogoChargeTimer > 0)
        {
            _skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.Clamp(100 - _pogoChargeTimer * 33.3f, 0, 100));
        }
        else
        {
            _skinnedMeshRenderer.SetBlendShapeWeight(0, 100);
        }

        if (!playerHeldBy.IsOwner)
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
            // When on the ground and held, check for jump input.
            DetectPlayerPressingSpaceToHopUp();

            if (_pogoChargeTimer > 0) return;
            _jumpTimer -= Time.deltaTime;
            if (_jumpTimer > 0) return;
            _jumpTimer = _jumpTimerMax / 2;
            _pogoChargeTimer = _minChargeTimer;
            Launch();
        }
    }

    private bool DoRaycast(Vector3 distance, Transform raycastTransform, out RaycastHit hit)
    {
        return Physics.Raycast(
            raycastTransform.position,
            distance.normalized,
            out hit,
            distance.magnitude,
            StartOfRound.Instance.collidersAndRoomMaskAndDefault,
            QueryTriggerInteraction.Ignore
        );
    }
    
    private void FixedUpdate()
    {
        if (_isOnGround) return;

        // this technically isn't good but oh well :3
        _velocity -= new Vector3(0, 9.8f, 0) * Time.fixedDeltaTime * 2.5f;
        Vector3 distanceThisFrame = _velocity * Time.fixedDeltaTime;

        Transform raycastTransform;
        if (distanceThisFrame.y > 0)
        {
            raycastTransform = topTransform;
        }
        else
        {
            raycastTransform = bottomTransform;
        }
        if (DoRaycast(distanceThisFrame, raycastTransform, out RaycastHit hitInfo))
        {
            Vector3 offset = hitInfo.point - raycastTransform.position;
            realPogo.position += offset;

            if (Mathf.Abs(hitInfo.normal.y) < 0.05f)
            { // value here is how vertical the surface needs in order to be considered a wall
                Plugin.ExtendedLogging("Hit wall");
                _velocity = Vector3.Reflect(_velocity, hitInfo.normal);
                // hit wall!
            }
            else if (distanceThisFrame.y > 0)
            {
                Plugin.ExtendedLogging("Hit ceiling");
                _velocity = Vector3.Reflect(_velocity, hitInfo.normal) / 2f;
                // hit ceiling!
            }
            else
            {
                OnPogoHitGround();
            }
            return;
        }
        else
        {
            realPogo.position += distanceThisFrame;
        }
        
        int playerOrEnemyCollidersHits = Physics.OverlapSphereNonAlloc(bottomTransform.position, 0.1f, _cachedColliders, CodeRebirthUtils.Instance.playersAndEnemiesAndHazardMask, QueryTriggerInteraction.Collide);
        if (playerOrEnemyCollidersHits <= 0) return;
        // Damage enemy.
        for (int i = 0; i < playerOrEnemyCollidersHits; i++)
        {
            if (!_cachedColliders[i].TryGetComponent(out IHittable hit) || _cachedColliders[i].gameObject == previousPlayerHeldBy.gameObject)
                continue;
            hit.Hit(1, Vector3.up, previousPlayerHeldBy, true, 1);
        }
        //Launch();
    }

    public override void FallWithCurve()
    {
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        if (playerHeldBy == null || isPocketed || !playerHeldBy.IsOwner) return;
        RotateAroundPoint(_xAngle, _yAngle, _zAngle);
    }

    private void RotateAroundPoint(float xAngle, float yAngle, float zAngle)
    {
        Quaternion targetRotation = Quaternion.Euler(xAngle, yAngle, zAngle);
        realPogo.rotation = targetRotation;
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

        // Plugin.ExtendedLogging($"_xAngle: {_xAngle}, _yAngle: {_yAngle}, _zAngle: {_zAngle}");
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
        Launch();
    }

    public void Launch()
    {
        float force = _chargeTimeToForce.Evaluate(_pogoChargeTimer);
        Vector3 launchVector = realPogo.up * force * 5f; // 5f is a temporay value and should be removed, i just don't want to keep rebuilding the bundle.
        Plugin.ExtendedLogging($"launching player with vector: {launchVector}");

        _isOnGround = false;
        _velocity = launchVector;
        _pogoChargeTimer = 0;
    }

    public void OnPogoHitGround()
    {
        Plugin.ExtendedLogging("Hit ground");
        _isOnGround = true;
        _velocity = Vector3.zero;
    }
}