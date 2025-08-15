using System;
using CodeRebirthLib.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CodeRebirth.src.MiscScripts;
public class InteractSeatable : InteractTrigger // I stole this from paco
{
    public PlayerControllerB? SittingPlayer { get; private set; }
    public bool LocalPlayerSeated { get; private set; }
    public Vector3 PlayerExitPoint { get; private set; } = Vector3.zero;

    [Header("Interact Seatable")]
    [SerializeField]
    private string _actionToExit = "";
    [SerializeField]
    private AudioSource? _seatableSource = null;
    [SerializeField]
    private UnityEvent _onPlayerSit = new();
    [SerializeField]
    private UnityEvent _onPlayerStand = new();
    private InputAction? _playerAction = null;
    private void Reset()
    {
        hoverTip = "Sit down : [LMB]";
        oneHandedItemAllowed = true;
        holdInteraction = true;
        timeToHold = 0.2f;
        cooldownTime = 0.3f;
        specialCharacterAnimation = true;
        stopAnimationManually = true;
        stopAnimationString = "SA_stopAnimation";
        hidePlayerItem = true;
        animationWaitTime = 2f;
        animationString = "SA_Truck";
        lockPlayerPosition = true;
        clampLooking = true;
        setVehicleAnimation = true;
        minVerticalClamp = 50f;
        maxVerticalClamp = -70f;
        horizontalClamp = 120;
    }

    public void Awake()
    {
        onInteractEarlyOtherClients.AddListener(player =>
        {
            if (player.IsLocalPlayer())
            {
                SetPlayerOnSeatServerRpc(player);
            }
        });

        _playerAction = GameNetworkManager.Instance.localPlayerController.playerActions.m_Movement.FindAction(_actionToExit);
    }

    public new void Update()
    {
        base.Update();

        if (!LocalPlayerSeated || SittingPlayer == null || _playerAction == null)
        {
            return;
        }

        if (_playerAction.WasPressedThisFrame())
        {
            ExitChairLocal(true);
            ExitChairServerRpc();
        }
    }

    public void SetPlayerOnSeatLocal(PlayerControllerB playerSitting)
    {
        if (playerSitting.IsLocalPlayer())
        {
            PlayerExitPoint = playerSitting.visorCamera.transform.position;
            LocalPlayerSeated = true;
        }

        SittingPlayer = playerSitting;
        interactable = false;

        _onPlayerSit.Invoke();
    }

    public void ExitChairLocal(bool teleport = false)
    {
        if (SittingPlayer == null)
        {
            return;
        }

        if (teleport)
        {
            SittingPlayer.TeleportPlayer(PlayerExitPoint);
        }

        _onPlayerStand.Invoke();

        PlayerExitPoint = Vector3.zero;
        LocalPlayerSeated = false;
        SittingPlayer = null;

        interactable = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerOnSeatServerRpc(NetworkBehaviourReference playerReference)
    {
        if (LocalPlayerSeated || SittingPlayer != null)
        {
            return;
        }

        SetPlayerOnSeatClientRpc(playerReference);
    }

    [ClientRpc]
    public void SetPlayerOnSeatClientRpc(NetworkBehaviourReference playerReference)
    {
        if (playerReference.TryGet(out PlayerControllerB player))
        {
            SetPlayerOnSeatLocal(player);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ExitChairServerRpc()
    {
        ExitChairClientRpc();
    }

    [ClientRpc]
    public void ExitChairClientRpc()
    {
        ExitChairLocal();
    }
}