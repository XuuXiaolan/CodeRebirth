
using System;
using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class Pandora : CodeRebirthEnemyAI
{

    private float currentTimerCooldown => GetTimerCooldown();
    private float currentTeleportTimer = 0f;
    public enum State
    {
        LookingForPlayer,
        ShowdownWithPlayer,
        Death,
    }

    public float GetTimerCooldown()
    {
        float timerMultiplier = Mathf.Clamp01(1 - TimeOfDay.Instance.normalizedTimeOfDay);
        return timerMultiplier * 55f + 5f;
    }

    public override void Start()
    {
        base.Start();
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (StartOfRound.Instance.allPlayersDead || isEnemyDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.LookingForPlayer:
                DoLookingForPlayer();
                break;
            case (int)State.ShowdownWithPlayer:
                DoShowdownWithPlayer();
                break;
            case (int)State.Death:
                DoDeath();
                break;
        }
    }

    #region State Machines
    private void DoLookingForPlayer()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled || !player.isInsideFactory) continue;
            if (Vector3.Distance(transform.position, player.transform.position) > 15) continue;
            currentTeleportTimer = currentTimerCooldown;
            if (Physics.Raycast(player.gameplayCamera.transform.position, (transform.position - player.gameplayCamera.transform.forward).normalized, out _, 20, StartOfRound.Instance.collidersAndRoomMaskAndDefault | LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore)) continue;
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            smartAgentNavigator.StopSearchRoutine();
            agent.velocity = Vector3.zero;
            SwitchToBehaviourServerRpc((int)State.ShowdownWithPlayer);
            break;
        }
        currentTeleportTimer -= AIIntervalTime;
        if (currentTeleportTimer <= 0)
        {
            currentTeleportTimer = currentTimerCooldown;
            StartCoroutine(TeleportAndResetSearchRoutine());
            return;
        }
    }

    private void DoShowdownWithPlayer()
    {
        // Cease Player and Pandora Movement
        // Stare at the player and make em stare at you.
    }

    private void DoDeath()
    {
        // Probably nothing
    }

    #endregion

    private IEnumerator TeleportAndResetSearchRoutine()
    {
        smartAgentNavigator.StopSearchRoutine();
        Vector3 randomPosition = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
        bool foundSuitablePosition = false;
        while (!foundSuitablePosition)
        {
            bool suitable = true;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerDead || !player.isPlayerControlled || !player.isInsideFactory) continue;
                if (Vector3.Distance(randomPosition, player.transform.position) < 5)
                {
                    suitable = false;
                    break;
                }
            }
            if (suitable)
            {
                foundSuitablePosition = true;
            }
            else
            {
                randomPosition = RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
                yield return null;
            }
        }
        agent.Warp(randomPosition);
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 20);
    }
}