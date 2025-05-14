using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class JimothyNPC : NetworkBehaviour
{
    [Header("References")]
    [SerializeField]
    private InteractTrigger _interactTrigger = null!;
    [SerializeField]
    private Animator _jimothyAnimator = null!;
    [SerializeField]
    private NetworkAnimator _jimothyNetworkAnimator = null!;
    [Header("Timers")]
    [SerializeField]
    private float _minIdleTime = 10f;
    [SerializeField]
    private float _maxIdleTime = 20f;

    private float _idleTimer = 0f;
    private bool pickedUp = false;
    private bool playersNearby = false;
    private bool playersNearbyLastFrame = false;

    private static readonly int ThumbsUpAnimation = Animator.StringToHash("thumbsUp"); // Trigger
    private static readonly int PickUpJimothyAnimation = Animator.StringToHash("pickUp"); // Trigger
    private static readonly int GreetPlayersAnimation = Animator.StringToHash("greetPlayers"); // Trigger
    private static readonly int RandomIdleAnimation = Animator.StringToHash("randomIdle"); // Trigger
    private static readonly int RandomTypeAnimation = Animator.StringToHash("randomType"); // Trigger

    public void Update()
    {
        if (!IsServer)
            return;

        if (!pickedUp)
            return;

        HandleCheckingNearbyPlayers();
        HandleIdleAnimation();
    }

    private void HandleCheckingNearbyPlayers()
    {
        playersNearby = false;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == null || player.isPlayerDead || !player.isPlayerControlled)
                continue;
            
            if (Vector3.Distance(transform.position, player.transform.position) > 10)
            {
                continue;
            }

            playersNearby = true;
            break;
        }

        if (playersNearby != playersNearbyLastFrame && playersNearby)
        {
            _jimothyNetworkAnimator.SetTrigger(GreetPlayersAnimation);
        }
        playersNearbyLastFrame = playersNearby;
    }

    private void HandleIdleAnimation()
    {
        _idleTimer -= Time.deltaTime;
        if (_idleTimer > 0)
            return;

        int animationToDo = Random.Range(0, 2);
        switch (animationToDo)
        {
            case 0:
                _jimothyNetworkAnimator.SetTrigger(RandomTypeAnimation);
                break;
            case 1:
                _jimothyNetworkAnimator.SetTrigger(RandomIdleAnimation);
                break;
        }
        _idleTimer = Random.Range(_minIdleTime, _maxIdleTime);
    }

    public void PickupFallenJimothy(PlayerControllerB player)
    {
        if (player != GameNetworkManager.Instance.localPlayerController)
            return;

        PickupFallenJimothyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PickupFallenJimothyServerRpc()
    {
        _jimothyNetworkAnimator.SetTrigger(PickUpJimothyAnimation);
        pickedUp = true;
        DisableInteractTriggerClientRpc();
        _idleTimer = Random.Range(_minIdleTime, _maxIdleTime);
    }

    [ClientRpc]
    private void DisableInteractTriggerClientRpc()
    {
        _interactTrigger.interactable = false;
    }

    public void PlayerUsedSallyButton()
    {
        _jimothyNetworkAnimator.SetTrigger(ThumbsUpAnimation);
    }
}