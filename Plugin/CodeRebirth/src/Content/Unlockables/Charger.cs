using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.ModCompats;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class Charger : NetworkBehaviour
{
    public InteractTrigger ActivateOrDeactivateTrigger = null!;
    public Transform ChargeTransform = null!;
    [NonSerialized] public GalAI GalAI = null!;
    [NonSerialized] public static List<Charger> Instances = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instances.Add(this);
    }

    public IEnumerator ActivateGalAfterLand()
    {
        if (!IsServer) yield break;
        while (true)
        {
            yield return new WaitUntil(() => TimeOfDay.Instance.normalizedTimeOfDay <= 0.12f && StartOfRound.Instance.shipHasLanded && !GalAI.Animator.GetBool("activated") && !StartOfRound.Instance.shipIsLeaving && !StartOfRound.Instance.inShipPhase && RoundManager.Instance.currentLevel.levelID != 3);
            Plugin.ExtendedLogging("Activating  Gal" + TimeOfDay.Instance.normalizedTimeOfDay);
            if (!GalAI.Animator.GetBool("activated"))
            {
                PlayerControllerB closestPlayer = StartOfRound.Instance.allPlayerScripts.Where(p => p.isPlayerControlled && !p.isPlayerDead).OrderBy(p => Vector3.Distance(transform.position, p.transform.position)).First();
                int playerIndex = Array.IndexOf(StartOfRound.Instance.allPlayerScripts, closestPlayer);
                if (!NetworkObject.IsSpawned) yield break;
                ActivateGirlServerRpc(playerIndex);
            }
        }
    }

    public void OnActivateGal(PlayerControllerB playerInteracting)
    {
        if (!NetworkObject.IsSpawned) return;
        if (playerInteracting == null || playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving || !(RoundManager.Instance.currentLevel.levelID == 3 && NavmeshInCompanyCompat.Enabled)) return;
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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instances.Remove(this);
    }
}