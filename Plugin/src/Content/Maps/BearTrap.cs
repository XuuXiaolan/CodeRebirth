using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class BearTrap : NetworkBehaviour
{
    public Animator trapAnimator = null!;
    public Collider trapCollider = null!;
    public float delayBeforeReset = 2.0f;
    public float hinderedMultiplier = 1.5f;

    private PlayerControllerB? playerCaught = null;
    private readonly static int triggerTrapAnimation = Animator.StringToHash("triggerTrap");
    private readonly static int resetTrapAnimation = Animator.StringToHash("resetTrap");
    private bool isTriggered = false;
    private bool canTrigger = true;

    private void Start()
    {
        // todo: make an interact trigger in unity that subscribes to ReleaseTrap()
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || isTriggered || !canTrigger) return;

        if (other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player) && player != null)
        {
            TriggerTrapServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerTrapServerRpc(int playerIndex)
    {
        isTriggered = true;
        TriggerTrapClientRpc(playerIndex);
    }

    [ClientRpc]
    private void TriggerTrapClientRpc(int playerIndex)
    {
        trapAnimator.SetTrigger(triggerTrapAnimation);
        trapCollider.enabled = false;
        playerCaught = StartOfRound.Instance.allPlayerScripts[playerIndex];
        playerCaught.DamagePlayer(25, true, true, CauseOfDeath.Crushing, 0, false, default);
        playerCaught.hinderedMultiplier *= hinderedMultiplier;
    }

    public void ReleaseTrap(PlayerControllerB player)
    {
        if (!IsServer) return;

        ReleaseTrapServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleaseTrapServerRpc()
    {
        isTriggered = false;
        StartCoroutine(DelayForReuse());
        ReleaseTrapClientRpc();
    }

    [ClientRpc]
    private void ReleaseTrapClientRpc()
    {
        trapAnimator.SetTrigger(resetTrapAnimation);
        trapCollider.enabled = true;
        if (playerCaught != null)
        {
            playerCaught.hinderedMultiplier /= hinderedMultiplier;
            playerCaught = null;
        }
    }

    private IEnumerator DelayForReuse()
    {
        canTrigger = false;
        yield return new WaitForSeconds(delayBeforeReset);
        canTrigger = true;
    }
}
