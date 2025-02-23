using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class CruiserCharger : Charger
{
    public Animator animator = null!;
    [HideInInspector] public static readonly int isActivatedAnimation = Animator.StringToHash("IsActivated"); // bool
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        // Instantiate the CruiserGalAI prefab
        GalAI = Instantiate(UnlockableHandler.Instance.CruiserGal.CruiserGalPrefab, ChargeTransform.position, ChargeTransform.rotation).GetComponent<CruiserGalAI>();
        NetworkObject netObj = GalAI.GetComponent<NetworkObject>();
        GalAI.GalCharger = this;
        netObj.Spawn();
    }
}