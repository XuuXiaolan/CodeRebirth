using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirthLib;
using CodeRebirthLib.CRMod;
using CodeRebirthLib.Utils;


using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Maps;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkTransform))]
public class KamikazeJimothy : NetworkBehaviour
{
    [SerializeField]
    private UnityEvent _onJimFix = new();

    [SerializeField]
    private Animator _animator = null!;
    [SerializeField]
    private NetworkAnimator _networkAnimator = null!;

    [SerializeField]
    private AnimationClip _jimFixAnimation = null!;

    [SerializeField]
    private NavMeshAgent _agent = null!;

    [SerializeField]
    private Renderer[] _renderers = [];

    [SerializeField]
    private InteractTrigger _headTrigger = null!;

    private Coroutine? _feedingRoutine = null;
    private List<int> _grabbablesValues = new();
    private static readonly int AssembleHeadAnimationHash = Animator.StringToHash("assembleHead"); // Trigger

    private void Start()
    {
        _headTrigger.onInteract.AddListener(PlaceHeadTrigger);
    }

    private void PlaceHeadTrigger(PlayerControllerB playerControllerB)
    {
        if (!playerControllerB.IsLocalPlayer() || playerControllerB.currentlyHeldObjectServer == null || playerControllerB.currentlyHeldObjectServer is not JimBall)
            return;

        playerControllerB.DespawnHeldObject();
        PlaceHeadOnJimothyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceHeadOnJimothyServerRpc()
    {
        _networkAnimator.SetTrigger(AssembleHeadAnimationHash);
        PlaceHeadOnJimothyClientRpc();
        StartCoroutine(AnimationDelay());
    }

    private IEnumerator AnimationDelay()
    {
        foreach (var grabbableObject in transform.GetComponentsInChildren<GrabbableObject>().ToArray())
        {
            _grabbablesValues.Add(grabbableObject.scrapValue);
            grabbableObject.NetworkObject.Despawn(true);
        }
        yield return new WaitForSeconds(_jimFixAnimation.length + 1);
        _agent.enabled = true;
    }

    [ClientRpc]
    private void PlaceHeadOnJimothyClientRpc()
    {
        _onJimFix.Invoke();
        _headTrigger.enabled = false;
    }

    public void Update()
    {
        if (!_agent.enabled || ShreddingSarah.Instance == null || _feedingRoutine != null)
            return;

        _agent.SetDestination(ShreddingSarah.Instance.shreddingPoint.position);
        if (Vector3.Distance(transform.position, ShreddingSarah.Instance.shreddingPoint.transform.position) < 1f + _agent.stoppingDistance)
        {
            _feedingRoutine = StartCoroutine(FeedTheShredder());
        }
    }

    public IEnumerator FeedTheShredder()
    {
        foreach (var renderer in _renderers)
        {
            renderer.enabled = false;
        }

        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, this.transform.position) <= 30f)
        {
            CRModContent.Achievements.TryTriggerAchievement(NamespacedKey<CRMAchievementDefinition>.From("code_rebirth", "banzai"));
        }

        if (!IsServer)
            yield break;

        foreach (var grabbableObjectValue in _grabbablesValues)
        {
            ShreddingSarah.Instance!.TryFeedItemServerRpc(false, grabbableObjectValue);
            yield return new WaitForSeconds(0.2f);
        }

        NetworkObject.Despawn(true);
    }
}