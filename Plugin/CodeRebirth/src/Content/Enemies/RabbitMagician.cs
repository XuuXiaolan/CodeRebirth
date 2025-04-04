using System;
using System.Collections;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

public class RabbitMagician : CodeRebirthEnemyAI
{
    [SerializeField]
    private readonly AnimationClip _spawnAnimation = null!;
    [SerializeField]
    private readonly AnimationClip _spottedAnimation = null!;

    private Coroutine? attachRoutine = null;
    public enum RabbitMagicianState
    {
        Spawn,
        Idle,
        Attached,
        SwitchingTarget
    }
    // todo: spawn animation, wanders around slowly.
    // todo: when spotted, stops moving, does spot animation, attaches to player back once player looks away.
    // todo: sits on the players back until someone spots the rabbit.
    // todo: if seen, kills current player and transfers to next player.
    // todo: if player dies, goes back to spawning phase.
    public override void Start()
    {
        base.Start();

        SwitchToBehaviourStateOnLocalClient((int)RabbitMagicianState.Spawn);
        if (!IsServer) return;
        SwitchToIdle();
    }

    #region State Machines
    public override void DoAIInterval()
    {
        base.DoAIInterval();

        switch (currentBehaviourStateIndex)
        {
            case (int)RabbitMagicianState.Spawn:
                DoSpawn();
                break;
            case (int)RabbitMagicianState.Idle:
                DoIdle();
                break;
            case (int)RabbitMagicianState.Attached:
                DoAttached();
                break;
            case (int)RabbitMagicianState.SwitchingTarget:
                DoSwitchingTarget();
                break;
        }
    }

    public void DoSpawn()
    {
        // Do Nothing
    }

    public void DoIdle()
    {
        if (attachRoutine != null)
            return;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled) continue;
            if (Vector3.Distance(transform.position, player.transform.position) > 40) continue;
            Vector3 directionToRabbit = (transform.position - player.gameObject.transform.position).normalized;
            if (Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, directionToRabbit) < 0.4f) continue;
            if (!Physics.Linecast(transform.position, player.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) continue;
            smartAgentNavigator.StopSearchRoutine();
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            SwitchToBehaviourServerRpc((int)RabbitMagicianState.Attached);
            smartAgentNavigator.enabled = false;
            agent.enabled = false;
            // do animation
            // attach to a player bone
            return;
        }
    }

    public void DoAttached()
    {

    }

    public void DoSwitchingTarget()
    {

    }
    #endregion

    #region Misc Functions
    private IEnumerator SwitchToIdle()
    {
        yield return new WaitForSeconds(_spawnAnimation.length);
        SwitchToBehaviourServerRpc((int)RabbitMagicianState.Idle);
        smartAgentNavigator.StartSearchRoutine(transform.position, 20);
    }

    private IEnumerator SwitchToAttached()
    {
        yield return new WaitForSeconds(_spottedAnimation.length);
        bool stoppedLooking = false;
        while (!stoppedLooking)
        {
            yield return new WaitForSeconds(1f);
            stoppedLooking = Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, (transform.position - targetPlayer.transform.position).normalized) < 0.4f;
        }
        // attach to a player bone
        SwitchToBehaviourServerRpc((int)RabbitMagicianState.Attached);
    }
    #endregion
}