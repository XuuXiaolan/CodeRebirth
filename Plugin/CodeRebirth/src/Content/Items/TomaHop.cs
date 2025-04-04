using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CodeRebirth.src.Content.Items;
public class TomaHop : GrabbableObject
{
    public Transform holdTransform = null!;

    private bool _isPogoForwardHeld = false;
    private bool _isPogoBackwardHeld = false;
    private bool _isPogoRightHeld = false;
    private bool _isPogoLeftHeld = false;
    private bool isOnGround = true;

    float _pogoChargeTimer;

    [SerializeField]
    Rigidbody _rb;

    [SerializeField]
    Transform _pivot;
    
    [Header("Handling")]
    [SerializeField]
    float _minChargeTimer = 0.05f; // this value is just so that if a player accidentally hits space for a frame they don't get launched

    [SerializeField]
    float _rotateSpeed = 20f;
    
    [SerializeField]
    AnimationCurve _chargeTimeToForce = AnimationCurve.Linear(0, 0, 3, 10);

    float _xAngle, _zAngle;
    
    void Awake() {
        if(!_rb) _rb = GetComponent<Rigidbody>();
    }
    
    public override void Update() {
        base.Update();
        if (playerHeldBy == null || !isHeld) return;
        playerHeldBy.transform.SetPositionAndRotation(holdTransform.position, holdTransform.rotation);
        playerHeldBy.disableMoveInput = true;

        // detect player pressing space to hop up
        if (!isOnGround) return;
        DetectPlayerPressingSpaceToHopUp();
    }

    public override void LateUpdate() {
        if (playerHeldBy != null && isHeld) {
            RotateAroundPoint(_xAngle, _zAngle);
            return;
        }
        base.LateUpdate();
    }
    
    void RotateAroundPoint(float xAngle, float zAngle) {
        Quaternion targetRotation = Quaternion.Euler(xAngle, 0f, zAngle);

        Vector3 pivotWorldPos = _pivot.position;

        transform.rotation = targetRotation;

        Vector3 positionOffset = pivotWorldPos - _pivot.position;
        transform.position += positionOffset;
    }
    
    public void DetectPlayerPressingSpaceToHopUp() {
        if(Keyboard.current.spaceKey.isPressed) { // temporary
            _pogoChargeTimer += Time.deltaTime;
            
            // handle rotation (THIS SHOULD BE USING A VECTOR 2 COMPOSITE INPUT ACTION!!)
            float horizontal = 0, vertical = 0;
            if(Keyboard.current.aKey.isPressed) {
                horizontal += _rotateSpeed * Time.deltaTime;
            }

            if(Keyboard.current.dKey.isPressed) {
                horizontal -= _rotateSpeed * Time.deltaTime;
            }

            if(Keyboard.current.wKey.isPressed) {
                vertical += _rotateSpeed * Time.deltaTime;
            }

            if(Keyboard.current.sKey.isPressed) {
                vertical -= _rotateSpeed * Time.deltaTime;
            }

            _xAngle += vertical;
            _zAngle += horizontal;
            
            return;
        }
        
        if(_pogoChargeTimer < _minChargeTimer) {
            _pogoChargeTimer = 0;
            return;
        }
        
        // player has released space and now calculate launch velocity
        float force = _chargeTimeToForce.Evaluate(_pogoChargeTimer);
        Vector3 launchVector = transform.up * force * 5f; // 5f is a temporay value and should be removed, i just don't want to keep rebuilding the bundle.
        Plugin.ExtendedLogging($"launching player with vector: {launchVector}");
        
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.velocity = launchVector; // maybe _rb.AddVelocity would be better
        
        
        // remember to have continuous detection for enemies and players and ground while in this state.
        
        // reset state
        _pogoChargeTimer = 0;
    }

    public void SwitchModeExtension(bool SwitchingOff)
    {
        /*if (SwitchingOff)
        {
            Plugin.InputActionsInstance.PogoForward.performed -= OnKeyHeld;
            Plugin.InputActionsInstance.PogoBackward.performed -= OnKeyHeld;
            Plugin.InputActionsInstance.PogoLeft.performed -= OnKeyHeld;
            Plugin.InputActionsInstance.PogoRight.performed -= OnKeyHeld;
            Plugin.InputActionsInstance.PogoForward.canceled -= OnKeyReleased;
            Plugin.InputActionsInstance.PogoBackward.canceled -= OnKeyReleased;
            Plugin.InputActionsInstance.PogoLeft.canceled -= OnKeyReleased;
            Plugin.InputActionsInstance.PogoRight.canceled -= OnKeyReleased;
        }
        else
        {
            Plugin.InputActionsInstance.PogoForward.performed += OnKeyHeld;
            Plugin.InputActionsInstance.PogoBackward.performed += OnKeyHeld;
            Plugin.InputActionsInstance.PogoLeft.performed += OnKeyHeld;
            Plugin.InputActionsInstance.PogoRight.performed += OnKeyHeld;
            Plugin.InputActionsInstance.PogoForward.canceled += OnKeyReleased;
            Plugin.InputActionsInstance.PogoBackward.canceled += OnKeyReleased;
            Plugin.InputActionsInstance.PogoLeft.canceled += OnKeyReleased;
            Plugin.InputActionsInstance.PogoRight.canceled += OnKeyReleased;
        }*/
    }

    public void OnKeyHeld(InputAction.CallbackContext context)
    {
        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        var btn = (ButtonControl)context.control;
        InputAction action = context.action;
        bool forward = false, backward = false, left = false, right = false;
        if (btn.wasPressedThisFrame)
        {
            switch (action.name)
            {
                case "PogoForward":
                    forward = true;
                    break;
                case "PogoBackward":
                    backward = true;
                    break;
                case "PogoLeft":
                    left = true;
                    break;
                case "PogoRight":
                    right = true;
                    break;
            }
            SetMovementHeldServerRpc(forward, backward, right, left);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMovementHeldServerRpc(bool forwardHeld, bool backwardHeld, bool rightHeld, bool leftHeld)
    {
        SetMovementHeldClientRpc(forwardHeld, backwardHeld, rightHeld, leftHeld);
    }

    [ClientRpc]
    public void SetMovementHeldClientRpc(bool forwardHeld, bool backwardHeld, bool rightHeld, bool leftHeld)
    {
        if (forwardHeld) _isPogoForwardHeld = true;
        if (backwardHeld) _isPogoBackwardHeld = true;
        if (rightHeld) _isPogoRightHeld = true;
        if (leftHeld) _isPogoLeftHeld = true;
    }

    public void OnKeyReleased(InputAction.CallbackContext context)
    {
        if (GameNetworkManager.Instance.localPlayerController != playerHeldBy) return;
        var btn = (ButtonControl)context.control;
        InputAction action = context.action;
        bool forward = true, backward = true, left = true, right = true;
        if (btn.wasReleasedThisFrame)
        {
            switch (action.name)
            {
                case "HoverForward":
                    forward = false;
                    break;
                case "HoverBackward":
                    backward = false;
                    break;
                case "HoverLeft":
                    left = false;
                    break;
                case "HoverRight":
                    right = false;
                    break;
            }
            SetMovementReleasedServerRpc(forward, backward, right, left);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMovementReleasedServerRpc(bool forwardReleased, bool backwardReleased, bool rightReleased, bool leftReleased)
    {
        SetMovementReleasedClientRpc(forwardReleased, backwardReleased, rightReleased, leftReleased);
    }

    [ClientRpc]
    public void SetMovementReleasedClientRpc(bool forwardReleased, bool backwardReleased, bool rightReleased, bool leftReleased)
    {
        if (!forwardReleased) _isPogoForwardHeld = false;
        if (!backwardReleased) _isPogoBackwardHeld = false;
        if (!rightReleased) _isPogoRightHeld = false;
        if (!leftReleased) _isPogoLeftHeld = false;
    }

    /*public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) isOnGround = true;
    }*/ // Todo: the script with on collision enter wouldn't be here, but a sub script that links to this one.
}