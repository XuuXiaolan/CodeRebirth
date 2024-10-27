using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Maps;
public class BearTrap : NetworkBehaviour
{
    public Animator trapAnimator = null!;
    public Collider trapCollider = null!;
    public float delayBeforeReset = 3.0f;
    public InteractTrigger trapTrigger = null!;

    private Vector3 caughtPosition = Vector3.zero;
    private PlayerControllerB? playerCaught = null;
    private EnemyAI? enemyCaught = null;
    private bool isTriggered = false;
    private bool canTrigger = true;
    [NonSerialized] public bool byProduct = false;
    private Coroutine? releaseCoroutine = null;
    private static readonly int IsTrapTriggered = Animator.StringToHash("isTrapTriggered");
    private static readonly int IsTrapResetting = Animator.StringToHash("isTrapResetting");

    private void Start()
    {
        if (!IsServer || byProduct) return;
        NavMeshHit hit = default;
        List<Vector3> usedPositions = new List<Vector3> { transform.position };
        for (int i = 0; i <= 4; i++)
        {
            Vector3 newPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(this.transform.position, 6, hit);
            for (int j = 0; j < usedPositions.Count; j++)
            {
                if (Vector3.Distance(usedPositions[j], newPosition) < 1f)
                {
                    newPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(this.transform.position, 9, hit);
                }
            }
            GameObject newBearTrap = Instantiate(this.gameObject, newPosition, default, this.transform.parent);
            newBearTrap.transform.up = hit.normal;
            newBearTrap.gameObject.GetComponent<NetworkObject>().Spawn();
            newBearTrap.GetComponent<BearTrap>().byProduct = true;
            usedPositions.Add(newPosition);
        }
    }

    private void Update()
    {
        trapTrigger.interactable = isTriggered;
        if (playerCaught != null) playerCaught.transform.position = Vector3.Lerp(playerCaught.transform.position, caughtPosition, 5f * Time.deltaTime);
        if (enemyCaught == null) return;

        enemyCaught.agent.speed = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered || !canTrigger) return;

        if (other.gameObject.layer == 3 && other.TryGetComponent(out PlayerControllerB player))
        {
            TriggerTrap(player);
        }
        else if (other.gameObject.layer == 19 && other.TryGetComponent(out EnemyAI enemy))
        {
            TriggerTrap(enemy);
        }
    }

    private void TriggerTrap(PlayerControllerB player)
    {
        isTriggered = true;
        playerCaught = player;
        playerCaught.disableMoveInput = true;
        playerCaught.DamagePlayer(25, true, false, CauseOfDeath.Crushing, 0, false, default);
        caughtPosition = playerCaught.transform.position;
        trapAnimator.SetBool(IsTrapTriggered, true);
        StartCoroutine(ResetBooleanAfterDelay(IsTrapTriggered, 0.5f));
        trapCollider.enabled = false;
    }

    private void TriggerTrap(EnemyAI enemy)
    {
        isTriggered = true;
        enemyCaught = enemy;
        enemyCaught.HitEnemy(1, null, false, -1);
        trapAnimator.SetBool(IsTrapTriggered, true);
        StartCoroutine(ResetBooleanAfterDelay(IsTrapTriggered, 0.5f));
        trapCollider.enabled = false;

        if (releaseCoroutine != null)
        {
            StopCoroutine(releaseCoroutine);
        }

        releaseCoroutine = StartCoroutine(DelayReleasingEnemy());
    }

    private IEnumerator DelayReleasingEnemy()
    {
        yield return new WaitForSeconds(12f);
        ReleaseTrap();
        releaseCoroutine = null;
    }

    public void ReleaseTrapEarly()
    {
        trapAnimator.SetBool(IsTrapResetting, true);
        StartCoroutine(ResetBooleanAfterDelay(IsTrapResetting, 0.5f));
    }

    public void OnCancelReleaseTrap()
    {
        if (!isTriggered || playerCaught == null) return;

        TriggerTrap(playerCaught);
    }

    public void ReleaseTrap()
    {
        trapAnimator.SetBool(IsTrapResetting, true);
        trapCollider.enabled = true;

        if (playerCaught != null)
        {
            playerCaught.disableMoveInput = false;
            playerCaught = null;
        }

        if (enemyCaught != null)
        {
            enemyCaught = null;
        }

        isTriggered = false;
        StartCoroutine(DelayForReuse());

        // Reset `isTrapTriggered` if it hasn't been reset properly before
        if (trapAnimator.GetBool(IsTrapTriggered))
        {
            trapAnimator.SetBool(IsTrapTriggered, false);
        }

        StartCoroutine(ResetBooleanAfterDelay(IsTrapResetting, 0.5f));
    }

    private IEnumerator DelayForReuse()
    {
        canTrigger = false;
        yield return new WaitForSeconds(delayBeforeReset);
        canTrigger = true;
    }

    private IEnumerator ResetBooleanAfterDelay(int parameterHash, float delay)
    {
        yield return new WaitForSeconds(delay);
        trapAnimator.SetBool(parameterHash, false);
    }
}