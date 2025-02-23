using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class TerminalCharger : Charger
{
    public Animator animator = null!;
    [HideInInspector] public static readonly int isOpenedAnimation = Animator.StringToHash("isOpen"); // bool
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        // Instantiate the TerminalGalAI prefab
        GalAI = Instantiate(UnlockableHandler.Instance.TerminalBot.TerminalGalPrefab, ChargeTransform.position, ChargeTransform.rotation).GetComponent<TerminalGalAI>();
        NetworkObject netObj = GalAI.GetComponent<NetworkObject>();
        GalAI.GalCharger = this;
        netObj.Spawn();
    }
}