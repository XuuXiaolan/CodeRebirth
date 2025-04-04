using System;
using CodeRebirth.src;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

public class PeaceKeeper : CodeRebirthEnemyAI
{

    private float backOffTimer = 0f;
    public enum PeaceKeeperState
    {
        Idle,
        FollowPlayer,
        AttackingPlayer
    }

    public override void Start()
    {
        base.Start();

        if (!IsServer) return;

        HandleSwitchingToIdle();
    }

    #region State Machines
    public override void DoAIInterval()
    {
        base.DoAIInterval();

        switch (currentBehaviourStateIndex)
        {
            case (int)PeaceKeeperState.Idle:
                DoIdle();
                break;
            case (int)PeaceKeeperState.FollowPlayer:
                DoFollowPlayer();
                break;
            case (int)PeaceKeeperState.AttackingPlayer:
                DoAttackingPlayer();
                break;
        }
    }

    public void DoIdle()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled) continue;
            if (player.currentlyHeldObjectServer == null || !player.currentlyHeldObjectServer.itemProperties.isDefensiveWeapon) continue;
            if (Vector3.Distance(transform.position, player.transform.position) > 40) continue;
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            if (Vector3.Dot(transform.forward, directionToPlayer) < 0.1f) continue;
            if (!Physics.Linecast(transform.position, player.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) continue;
            smartAgentNavigator.StopSearchRoutine();
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            SwitchToBehaviourServerRpc((int)PeaceKeeperState.FollowPlayer);
            smartAgentNavigator.DoPathingToDestination(player.transform.position);
            Plugin.ExtendedLogging($"Player spotting holding weapon: {player.name}");
            return;
        }
    }
    
    public void DoFollowPlayer()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            HandleSwitchingToIdle();
            return;
        }

        if (targetPlayer.currentlyHeldObjectServer == null || targetPlayer.currentlyHeldObjectServer.itemProperties.isDefensiveWeapon)
        {
            backOffTimer += Time.deltaTime;
            if (backOffTimer > 10f)
            {
                HandleSwitchingToIdle();
                return;
            }
        }
        backOffTimer = 0f;

        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
    } // do a patch to attacking, if there's any callbacks to players

    public void DoAttackingPlayer()
    {

    }
    #endregion

    #region Misc Functions

    public void HandleSwitchingToIdle()
    {
        SetTargetServerRpc(-1);
        SwitchToBehaviourServerRpc((int)PeaceKeeperState.Idle);
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 40);
    }

    #endregion
    // wanders around normally.
    // but if it sees a player, it will follow them if they have a weapon.
    // will leave a player alone if they pocket/dont have a weapon.
    // if you attack something, it will kill you.
    // it keeps the peace.
    // it has a ranged minigun attack and a melee attack.
    // melee attack has big knockback to go back to shooting minigun.
}