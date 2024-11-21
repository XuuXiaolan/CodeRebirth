using System;
using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class Charger : NetworkBehaviour
{
    public InteractTrigger ActivateOrDeactivateTrigger = null!;
    public Transform ChargeTransform = null!;
    public bool canActivateOnCompanyMoon;
    [NonSerialized] public GalAI GalAI = null!;

    public IEnumerator ActivateGalAfterLand()
    {
        while (true)
        {
            yield return new WaitUntil(() => TimeOfDay.Instance.normalizedTimeOfDay <= 0.12f && StartOfRound.Instance.shipHasLanded && !GalAI.Animator.GetBool("activated") && !StartOfRound.Instance.shipIsLeaving && !StartOfRound.Instance.inShipPhase && RoundManager.Instance.currentLevel.levelID != 3);
            Plugin.Logger.LogInfo("Activating  Gal" + TimeOfDay.Instance.normalizedTimeOfDay);
            if (!GalAI.Animator.GetBool("activated"))
            {
                PlayerControllerB closestPlayer = StartOfRound.Instance.allPlayerScripts.Where(p => p.isPlayerControlled).OrderBy(p => Vector3.Distance(transform.position, p.transform.position)).First();
                GalAI.ActivateGal(closestPlayer);
            }
        }
    }

    public void OnActivateGal(PlayerControllerB playerInteracting)
    {
        if (!NetworkObject.IsSpawned) return;
        if (playerInteracting == null || playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving || (canActivateOnCompanyMoon && RoundManager.Instance.currentLevel.levelID == 3 && TimeOfDay.Instance.quotaFulfilled >= TimeOfDay.Instance.profitQuota && !Plugin.ModConfig.ConfigGalBypassQuota.Value)) return;
        if (!GalAI.Animator.GetBool("activated"))
        {
            ActivateGirlServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
        }
        else
        {
            if (Plugin.ModConfig.ConfigOnlyOwnerDisablesGal.Value && playerInteracting != GalAI.ownerPlayer) return;
            ActivateGirlServerRpc(-1);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateGirlServerRpc(int index)
    {
        GalAI.Animator.SetBool("activated", index != -1);
        ActivateGirlClientRpc(index);
    }

    [ClientRpc]
    private void ActivateGirlClientRpc(int index)
    {
        if (index != -1) GalAI.ActivateGal(StartOfRound.Instance.allPlayerScripts[index]);
        else GalAI.DeactivateGal();
    }
}