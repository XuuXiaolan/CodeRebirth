using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Maps;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkTransform))]
public class KamikazeJimothy : NetworkBehaviour
{
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
    private List<GrabbableObject> _grabbables = new();
    private static readonly int AssembleHeadAnimationHash = Animator.StringToHash("assembleHead"); // Trigger

    private void Start()
    {
        _headTrigger.onInteract.AddListener(PlaceHeadTrigger);
    }

    private void PlaceHeadTrigger(PlayerControllerB playerControllerB)
    {
        if (!playerControllerB.IsLocalPlayer() || playerControllerB.currentlyHeldObjectServer == null || playerControllerB.currentlyHeldObjectServer is not JimBall)
            return;

        PlaceHeadOnJimothyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceHeadOnJimothyServerRpc()
    {
        _networkAnimator.SetTrigger(AssembleHeadAnimationHash);
        StartCoroutine(AnimationDelay());
    }

    private IEnumerator AnimationDelay()
    {
        PlaceHeadOnJimothyClientRpc();
        yield return new WaitForSeconds(_jimFixAnimation.length);
        _agent.enabled = true;
    }

    [ClientRpc]
    private void PlaceHeadOnJimothyClientRpc()
    {
        foreach (var grabbableObject in transform.GetComponentsInChildren<GrabbableObject>())
        {
            grabbableObject.grabbable = false;
            grabbableObject.grabbableToEnemies = false;
            _grabbables.Add(grabbableObject);
        }
        _headTrigger.enabled = false;
    }

    public void Update()
    {
        if (!_agent.enabled || ShreddingSarah.Instance == null || _feedingRoutine != null)
            return;

        _agent.SetDestination(ShreddingSarah.Instance.shreddingPoint.position);
        if (Vector3.Distance(transform.position, ShreddingSarah.Instance.shreddingPoint.transform.position) < 0.5f)
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
        foreach (var grabbableObject in _grabbables)
        {
            if (grabbableObject.mainObjectRenderer != null)
                grabbableObject.mainObjectRenderer.enabled = false;
        }

        foreach (var grabbableObject in _grabbables)
        {
            ShreddingSarah.Instance!.TryFeedItemServerRpc(false, grabbableObject.scrapValue);
            yield return new WaitForSeconds(0.1f);
        }

        NetworkObject.Despawn(true);
    }
}