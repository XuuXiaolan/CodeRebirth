using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeRebirth.src.Content.Items;
public class TomaHop : GrabbableObject
{
    public Transform holdTransform = null!;

    [SerializeField] private Rigidbody _rb = null!;
    [SerializeField] private Transform _pivot = null!;
    
    [Header("Handling")]
    [SerializeField] private float _minChargeTimer = 0.05f; // this value is just so that if a player accidentally hits space for a frame they don't get launched
    [SerializeField] private float _rotateSpeed = 20f;
    [SerializeField] private AnimationCurve _chargeTimeToForce = AnimationCurve.Linear(0, 0, 3, 10);

    private PlayerControllerB previousPlayerHeldBy = null!;
    private float _xAngle, _zAngle;
    private bool isOnGround = true;
    private float _pogoChargeTimer;

    public void FixedUpdate()
    {
        if (isOnGround) return;
        _rb.AddForce(Vector3.up * -9.8f);
    }

    public override void EquipItem()
    {
        base.EquipItem();
        previousPlayerHeldBy = playerHeldBy;
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
        if (playerHeldBy == null || !isHeld || !playerHeldBy.IsOwner) return;
        playerHeldBy.transform.SetPositionAndRotation(holdTransform.position, holdTransform.rotation);
        playerHeldBy.disableMoveInput = true;

        // detect player pressing space to hop up
        if (!isOnGround)
        {
            RaycastHit hit;
            bool hitPlayerOrEnemy = Physics.Raycast(transform.position, Vector3.down, out hit, 1f, CodeRebirthUtils.Instance.playersAndEnemiesMask, QueryTriggerInteraction.Collide);
            if (hitPlayerOrEnemy)
            {
                // damage enemy
                if (hit.transform.gameObject.layer == 3 && hit.transform.TryGetComponent(out PlayerControllerB player) && player != playerHeldBy)
                {
                    player.DamagePlayer(1, true, true, CauseOfDeath.Gravity, 0, false, default);
                }
                else if (hit.transform.gameObject.layer == 19 && hit.transform.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect))
                {
                    enemyAICollisionDetect.mainScript.HitEnemyOnLocalClient(1, Vector3.up, playerHeldBy, true, 1);
                }
                _pogoChargeTimer += 0.5f;
                ApplyForceToRigidBody();
                return;
            }
            bool hitGround = Physics.Raycast(transform.position, Vector3.down, out hit, 1f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
            if (hitGround)
            {
                SetRigidBodyToGround();
                Plugin.ExtendedLogging($"Raycast hit: {hit.collider.name}");
                return;
            }
            return;
        }
        DetectPlayerPressingSpaceToHopUp();
    }

    public override void LateUpdate()
    {
        if (playerHeldBy != null && isHeld)
        {
            RotateAroundPoint(_xAngle, _zAngle);
            return;
        }
        base.LateUpdate();
    }
    
    private void RotateAroundPoint(float xAngle, float zAngle)
    {
        Quaternion targetRotation = Quaternion.Euler(xAngle, 0f, zAngle);

        Vector3 pivotWorldPos = _pivot.position;

        transform.rotation = targetRotation;

        Vector3 positionOffset = pivotWorldPos - _pivot.position;
        transform.position += positionOffset;
    }
    
    public void DetectPlayerPressingSpaceToHopUp()
    {
        if (Keyboard.current.spaceKey.isPressed)
        { // temporary
            _pogoChargeTimer += Time.deltaTime;
            
            // handle rotation (THIS SHOULD BE USING A VECTOR 2 COMPOSITE INPUT ACTION!!)
            float horizontal = 0, vertical = 0;
            if (Keyboard.current.aKey.isPressed)
            {
                horizontal += _rotateSpeed * Time.deltaTime;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                horizontal -= _rotateSpeed * Time.deltaTime;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                vertical += _rotateSpeed * Time.deltaTime;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                vertical -= _rotateSpeed * Time.deltaTime;
            }

            _xAngle += vertical;
            _zAngle += horizontal;
            return;
        }
        
        if (_pogoChargeTimer < _minChargeTimer)
        {
            _pogoChargeTimer = 0;
            return;
        }
        
        // player has released space and now calculate launch velocity
        Plugin.ExtendedLogging($"pogo charge timer: {_pogoChargeTimer}");
        ApplyForceToRigidBody();
    }

    public void ApplyForceToRigidBody()
    {
        float force = _chargeTimeToForce.Evaluate(_pogoChargeTimer);
        Vector3 launchVector = transform.up * force * 5f; // 5f is a temporay value and should be removed, i just don't want to keep rebuilding the bundle.
        Plugin.ExtendedLogging($"launching player with vector: {launchVector}");

        isOnGround = false;
        _rb.isKinematic = false;
        _rb.velocity = launchVector; // maybe _rb.AddVelocity would be better
        // remember to have continuous detection for enemies and players and ground while in this state.
        
        // reset state
        _pogoChargeTimer = 0;
    }

    public void SetRigidBodyToGround()
    {
        isOnGround = true;
        if (!_rb.isKinematic) _rb.velocity = Vector3.zero;
        _rb.isKinematic = true;
    }
}