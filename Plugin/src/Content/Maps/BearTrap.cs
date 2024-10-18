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

    private PlayerControllerB? playerCaught = null;
    private float enemyCaughtTimer = 0f;
    private EnemyAI? enemyCaught = null;
    private readonly static int triggerTrapAnimation = Animator.StringToHash("triggerTrap");
    private readonly static int resetTrapAnimation = Animator.StringToHash("resetTrap");
    private bool isTriggered = false;
    private bool canTrigger = true;
    private float retriggerTimer = 0f;

    private void Start()
    {
        // todo: make an interact trigger in unity that subscribes to ReleaseTrap()
    }

    private void Update()
    {
        if (!IsServer) return;

        if (isTriggered)
        {
            retriggerTimer += Time.deltaTime;
            if (retriggerTimer >= 5)
            {

                playerCaught?.DamagePlayer(20, true, true, CauseOfDeath.Crushing, 0, false, default);
                enemyCaught?.HitEnemyClientRpc(1, -1, true, -1);
                retriggerTimer = 0;
                ReleaseTrap();
                if (playerCaught != null) TriggerPlayerTrapClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerCaught));
                if (enemyCaught != null) TriggerEnemyTrapClientRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemyCaught));
            }
        }
        if (enemyCaught != null)
        {
            enemyCaught.agent.speed = 0f;
            enemyCaughtTimer += Time.deltaTime;
            if (enemyCaughtTimer >= 10f)
            {
                enemyCaughtTimer = 0f;
                ReleaseTrap();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || isTriggered || !canTrigger) return;

        if (other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player) && player != null)
        {
            TriggerPlayerTrapServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
        }

        Transform? parent = CRUtilities.TryFindRoot(other.gameObject.transform);
        if (other.CompareTag("Enemy") && parent != null && parent.gameObject.TryGetComponent<EnemyAI>(out EnemyAI enemyAI) && enemyAI != null)
        {
            TriggerEnemyTrapServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemyAI));
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerEnemyTrapServerRpc(int enemyIndex)
    {
        isTriggered = true;
        TriggerEnemyTrapClientRpc(enemyIndex);
    }

    [ClientRpc]
    private void TriggerEnemyTrapClientRpc(int enemyIndex)
    {
        trapAnimator.SetTrigger(triggerTrapAnimation);
        trapCollider.enabled = false;
        enemyCaught = RoundManager.Instance.SpawnedEnemies[enemyIndex];
        enemyCaught.HitEnemy(1, null, false, -1);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerPlayerTrapServerRpc(int playerIndex)
    {
        isTriggered = true;
        TriggerPlayerTrapClientRpc(playerIndex);
    }

    [ClientRpc]
    private void TriggerPlayerTrapClientRpc(int playerIndex)
    {
        trapAnimator.SetTrigger(triggerTrapAnimation);
        trapCollider.enabled = false;
        playerCaught = StartOfRound.Instance.allPlayerScripts[playerIndex];
        playerCaught.DamagePlayer(25, true, true, CauseOfDeath.Crushing, 0, false, default);
        playerCaught.disableMoveInput = true;
    }

    public void ReleaseTrap()
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
            if (playerCaught.disableMoveInput) playerCaught.disableMoveInput = false;
            playerCaught = null;
        }
        if (enemyCaught != null)
        {
            enemyCaught = null;
        }
    }

    private IEnumerator DelayForReuse()
    {
        canTrigger = false;
        yield return new WaitForSeconds(delayBeforeReset);
        canTrigger = true;
    }
}
