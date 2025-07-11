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
    private NavMeshAgent _agent = null!;

    [SerializeField]
    private InteractTrigger _headTrigger = null!;

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
        _agent.enabled = true;
        PlaceHeadOnJimothyClientRpc();
    }

    [ClientRpc]
    private void PlaceHeadOnJimothyClientRpc()
    {
        _headTrigger.enabled = false;
    }

    public void Update()
    {
        if (!_agent.enabled || ShreddingSarah.Instance == null)
            return;

        _agent.SetDestination(ShreddingSarah.Instance.transform.position);
        if (Vector3.Distance(transform.position, ShreddingSarah.Instance.transform.position) < 0.5f)
        {
            // death
        }
    }
}