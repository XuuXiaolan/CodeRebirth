using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

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

    private RaycastHit[] _hits = new RaycastHit[4];
    private bool _isHoverForwardHeld = false;
    private bool _isHoverBackwardHeld = false;
    private bool _isHoverLeftHeld = false;
    private bool _isHoverRightHeld = false;
    private bool _isSprintHeld = false;
    private bool jumpCooldown = true;
    private bool _turnedOn = false;
    private float _speedMultiplier = 1f;
    private float _chargeIncreaseMultiplier = 1f;
    private PlayerControllerB? _playerRiding = null;
    private HoverboardTypes _hoverboardType = HoverboardTypes.Regular;

    #region Unity Methods
    private void Start()
    {
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
            // Debug log for force and anchor positions
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