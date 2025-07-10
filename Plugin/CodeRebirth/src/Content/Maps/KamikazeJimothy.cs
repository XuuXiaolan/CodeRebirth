using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Maps;

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
        
    }
}