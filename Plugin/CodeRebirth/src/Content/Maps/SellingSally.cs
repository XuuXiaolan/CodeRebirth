using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class SellingSally : NetworkBehaviour
{
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;
    public Transform endOfBarrelTransform = null!;
    public Transform sallyLoaderTransform = null!;

    private List<SallyCubes> sallyCubes = new();
    private static readonly int OpenedAnimation = Animator.StringToHash("open"); // Bool
    private static readonly int ShootAnimation = Animator.StringToHash("shoot"); // Trigger // Won't shoot unless the scraps are only cubes inside sally
    [HideInInspector] public static SellingSally? Instance = null;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instance = null;
    }

    public void OnButtonInteract(PlayerControllerB player)
    {
        if (!player.IsOwner) return;
        PressButtonServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PressButtonServerRpc()
    {
        Plugin.ExtendedLogging($"Can press button {CanCurrentlyShoot()}");
        if (!CanCurrentlyShoot()) return;
        networkAnimator.SetTrigger(ShootAnimation);
    }

    public void OnBellInteract(PlayerControllerB player)
    {
        if (!player.IsOwner) return;
        RingBellServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RingBellServerRpc()
    {
        animator.SetBool(OpenedAnimation, !animator.GetBool(OpenedAnimation));
    }

    private bool CanCurrentlyShoot()
    {
        foreach (var grabbableObject in transform.GetComponentsInChildren<GrabbableObject>())
        {
            if (grabbableObject.transform.parent != sallyLoaderTransform) continue;
            if (grabbableObject is SallyCubes sallyCube)
            {
                sallyCubes.Add(sallyCube);
                continue;
            }
            sallyCubes.Clear();
            return false;
        }
        return true;
    }

    public void DoShootScrapAnimEvent()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (!player.IsOwner || player.transform.parent != endOfBarrelTransform.parent) continue; 
            player.transform.position = endOfBarrelTransform.position;
            player.DamagePlayer(9999, true, true, CauseOfDeath.Blast, 0, false, endOfBarrelTransform.forward * 100f);
        }
        int scrapValueToMake = 0;
        foreach (var sallyCube in sallyCubes)
        {
            scrapValueToMake += sallyCube.scrapValue; 
        }

        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        SellAndDisplayItemProfits(scrapValueToMake, Object.FindFirstObjectByType<Terminal>());
    }

    private void SellAndDisplayItemProfits(int profit, Terminal terminal)
	{
		terminal.groupCredits += profit;
		StartOfRound.Instance.gameStats.scrapValueCollected += profit;
		TimeOfDay.Instance.quotaFulfilled += profit;
        HUDManager.Instance.DisplayCreditsEarning(profit, sallyCubes.ToArray(), terminal.groupCredits);

        foreach (var sallyCube in sallyCubes)
        {
            if (IsServer) sallyCube.NetworkObject.Despawn();
        }
        sallyCubes.Clear();
        TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
    }
}