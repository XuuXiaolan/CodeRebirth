using System.Linq;
using CodeRebirth.src.Util;
using CodeRebirthLib.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeRebirth.src.Content.Items;
public enum HoverboardTypes
{
    Regular,
    // Eventually wanna make other types of hoverboards
}

public class FuturisticHoverboard : NetworkBehaviour
{
    [SerializeField]
    private Rigidbody _rigidBody = null!;
    [SerializeField]
    private InteractTrigger _sitTrigger = null!;
    [SerializeField]
    private Transform[] _anchors = new Transform[4];
    [SerializeField]
    private float _jumpTimer = 5f;

    private bool _turnedOn = false;
    private float _speedMultiplier = 1f;
    private float _chargeIncreaseMultiplier = 1f;
    private InputAction _moveAction = null!;
    private InputAction _sprintAction = null!;
    private InputAction _jumpAction = null!;
    private InputAction _crouchAction = null!;
    private PlayerControllerB? _playerRiding = null;
    private HoverboardTypes _hoverboardType = HoverboardTypes.Regular;

    #region Unity Methods
    private void Start()
    {
        _moveAction = GameNetworkManager.Instance.localPlayerController.playerActions.m_Movement.FindAction("Move");

        /*Vector2 move = _moveAction.ReadValue<Vector2>();
        float forward  = Mathf.Max(0, move.y);
        float backward = Mathf.Max(0, -move.y);
        float right    = Mathf.Max(0, move.x);
        float left     = Mathf.Max(0, -move.x);*/

        _sprintAction = GameNetworkManager.Instance.localPlayerController.playerActions.m_Movement.FindAction("Sprint");
        _jumpAction = GameNetworkManager.Instance.localPlayerController.playerActions.m_Movement.FindAction("Jump");
        _crouchAction = GameNetworkManager.Instance.localPlayerController.playerActions.m_Movement.FindAction("Crouch");
        _sitTrigger.onInteract.AddListener(OnInteract);
        // ConfigureHoverboard();
        // this.insertedBattery = new Battery(false, 1f);
        // this.ChargeBatteries();
        switch (_hoverboardType)
        {
            case HoverboardTypes.Regular:
                break;
            default:
                break;
        }
    }

    private void FixedUpdate()
    {
        if (_playerRiding != GameNetworkManager.Instance.localPlayerController)
            return;

        if (_turnedOn)
        {
            for (int i = 0; i < _anchors.Length; i++)
            {
                ApplyForce(_anchors[i]);
            }
        }
    }
    #endregion

    private void ApplyForce(Transform anchor)
    {
        if (Physics.Raycast(anchor.position, -anchor.up, out RaycastHit hit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            float force = Mathf.Clamp(Mathf.Abs(1 / (hit.point.y - anchor.position.y)), 0, _playerRiding.isInHangarShipRoom ? 3f : 100f);
            Plugin.ExtendedLogging($"Force: {force}");
            _rigidBody.AddForceAtPosition(_playerRiding.transform.up * force * 8f, anchor.position, ForceMode.Acceleration);
        }
    }

    private void OnInteract(PlayerControllerB player)
    {
        if (!player.IsOwner)
            return;

        if (_playerRiding != null)
            return;

        MountPlayerServerRpc(player);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MountPlayerServerRpc(PlayerControllerReference playerReference)
    {
        MountPlayerClientRpc(playerReference);
    }

    [ClientRpc]
    private void MountPlayerClientRpc(PlayerControllerReference playerReference)
    {
        PlayerControllerB player = playerReference;
        MountPlayer(player);
    }

    private void MountPlayer(PlayerControllerB player)
    {
        _playerRiding = player;
    }
}