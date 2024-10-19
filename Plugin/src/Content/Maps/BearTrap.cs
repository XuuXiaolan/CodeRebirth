using System;
using System.Collections;
using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class BearTrap : NetworkBehaviour
{
    public Animator trapAnimator = null!;
    public Collider trapCollider = null!;
    public float delayBeforeReset = 3.0f;
    public InteractTrigger trapTrigger = null!;

    private PlayerControllerB? playerCaught = null;
    private float enemyCaughtTimer = 0f;
    private EnemyAI? enemyCaught = null;
    private readonly static int retriggerTrapAnimation = Animator.StringToHash("retriggerTrap");
    private readonly static int triggerTrapAnimation = Animator.StringToHash("triggerTrap");
    private readonly static int resetTrapAnimation = Animator.StringToHash("resetTrap");
    private bool isTriggered = false;
    private bool canTrigger = true;
    private float retriggerTimer = 0f;

    private void Update()
    {
        trapTrigger.interactable = isTriggered;
        if (!IsServer) return;

        if (isTriggered)
        {
            retriggerTimer += Time.deltaTime;
            if (retriggerTimer >= 10f)
            {
                playerCaught?.DamagePlayer(20, true, true, CauseOfDeath.Crushing, 0, false, default);
                enemyCaught?.HitEnemyClientRpc(1, -1, true, -1);
                retriggerTimer = 0f;
                RetriggerForEnemyAndPlayer();
            }
        }
        if (enemyCaught != null)
        {
            enemyCaught.agent.speed = 0f;
            enemyCaughtTimer += Time.deltaTime;
            if (enemyCaughtTimer >= 10f)
            {
                enemyCaughtTimer = 0f;
                StartCoroutine(DelayReleasingEnemy());
            }
        }
    }

    private IEnumerator DelayReleasingEnemy()
    {
        TrapReleaseAnimationClientRpc();
        yield return new WaitForSeconds(3f);
        if (enemyCaught != null) ReleaseTrap();
    }

    private void RetriggerForEnemyAndPlayer()
    {
        if (playerCaught != null) TriggerPlayerTrapClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerCaught), true);
        if (enemyCaught != null) TriggerEnemyTrapClientRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemyCaught), true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || isTriggered || !canTrigger) return;

        if (other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player) && player != null)
        {
            TriggerPlayerTrapServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player), false);
        }

        Transform? parent = CRUtilities.TryFindRoot(other.gameObject.transform);
        if (other.CompareTag("Enemy") && parent != null && parent.gameObject.TryGetComponent<EnemyAI>(out EnemyAI enemyAI) && enemyAI != null)
        {
            TriggerEnemyTrapServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemyAI), false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerEnemyTrapServerRpc(int enemyIndex, bool retrigger)
    {
        isTriggered = true;
        TriggerEnemyTrapClientRpc(enemyIndex, retrigger);
    }

    [ClientRpc]
    private void TriggerEnemyTrapClientRpc(int enemyIndex, bool retrigger)
    {
        if (!retrigger) trapAnimator.SetTrigger(triggerTrapAnimation);
        else trapAnimator.SetTrigger(retriggerTrapAnimation);
        trapCollider.enabled = false;
        enemyCaught = RoundManager.Instance.SpawnedEnemies[enemyIndex];
        enemyCaught.HitEnemy(1, null, false, -1);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerPlayerTrapServerRpc(int playerIndex, bool retrigger)
    {
        isTriggered = true;
        TriggerPlayerTrapClientRpc(playerIndex, retrigger);
    }

    [ClientRpc]
    private void TriggerPlayerTrapClientRpc(int playerIndex, bool retrigger)
    {
        if (!retrigger) trapAnimator.SetTrigger(triggerTrapAnimation);
        else trapAnimator.SetTrigger(retriggerTrapAnimation);
        trapCollider.enabled = false;
        playerCaught = StartOfRound.Instance.allPlayerScripts[playerIndex];
        playerCaught.DamagePlayer(25, true, true, CauseOfDeath.Crushing, 0, false, default);
        playerCaught.disableMoveInput = true;
    }

    public void ReleaseTrapEarly()
    {
        trapAnimator.SetTrigger(resetTrapAnimation);
    }

    [ClientRpc]
    private void TrapReleaseAnimationClientRpc()
    {
        trapAnimator.SetTrigger(resetTrapAnimation);
    }

    public void OnCancelReleaseTrap()
    {
        if (!IsServer) return;
        if (playerCaught != null) TriggerPlayerTrapServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerCaught), false);
        if (enemyCaught != null) TriggerEnemyTrapServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemyCaught), false);
    }

    public void ReleaseTrap()
    {
        trapAnimator.SetTrigger(resetTrapAnimation);
        trapCollider.enabled = true;
        retriggerTimer = 0f;
        enemyCaughtTimer = 0f;
        if (playerCaught != null)
        {
            if (playerCaught.disableMoveInput) playerCaught.disableMoveInput = false;
            playerCaught = null;
        }
        if (enemyCaught != null)
        {
            enemyCaught = null;
        }
        if (!IsServer) return;
        isTriggered = false;
        StartCoroutine(DelayForReuse());
    }

    private IEnumerator DelayForReuse()
    {
        canTrigger = false;
        yield return new WaitForSeconds(delayBeforeReset);
        canTrigger = true;
    }
}
