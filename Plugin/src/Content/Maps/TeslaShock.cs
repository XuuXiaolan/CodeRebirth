using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class TeslaShock : NetworkBehaviour
{
    public float distanceFromPlayer = 5f;
    public int playerDamageAmount = 40;
    public float delayBeforeExplodePlayer = 3f;
    public float delayBetweenZaps = 2f;

    private PlayerControllerB? targetPlayer;

    private void Update()
    {
        if (targetPlayer != null) return;
        if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer == null) return;
        if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.isConductiveMetal) return;
        if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, transform.position) > distanceFromPlayer) return;

        targetPlayer = GameNetworkManager.Instance.localPlayerController;
        SetTargetPlayerClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            player.DamagePlayer(playerDamageAmount, true, false, CauseOfDeath.Blast, 0, false, default);
        }
    }

    [ClientRpc]
    private void SetTargetPlayerClientRpc(int playerIndex)
    {
        targetPlayer = StartOfRound.Instance.allPlayerScripts[playerIndex];
        StartCoroutine(ExplodePlayerAfterDelay(delayBeforeExplodePlayer, targetPlayer));
    }

    private IEnumerator ExplodePlayerAfterDelay(float delay, PlayerControllerB affectedPlayer)
    {
        yield return new WaitForSeconds(delay);
        affectedPlayer.DamagePlayer(playerDamageAmount, true, false, CauseOfDeath.Blast, 0, false, default);
        yield return new WaitForSeconds(delayBetweenZaps);
        targetPlayer = null;
    }
}