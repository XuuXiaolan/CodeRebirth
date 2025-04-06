using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

public class PeaceKeeper : CodeRebirthEnemyAI
{
    [SerializeField]
    private Transform _gunStartTransform = null!;

    [SerializeField]
    private Transform _gunEndTransform = null!;

    [SerializeField]
    private float _walkingSpeed = 2f;

    [SerializeField]
    private float _chasingSpeed = 5f;

    [SerializeField]
    private float _shootingSpeed = 0.1f;

    private List<Material> _materials = new();
    private float _backOffTimer = 0f;
    private bool _isShooting = false;
    private Coroutine? _bitchSlappingRoutine = null;
    private Collider[] _cachedColliders = new Collider[24];
    private static readonly int ShootingAnimation = Animator.StringToHash("shooting"); // Bool
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int BitchSlapAnimation = Animator.StringToHash("bitchSlap"); // Trigger

    private static int ScrollSpeedID = Shader.PropertyToID("_ScrollSpeed"); // Vector3
    public enum PeaceKeeperState
    {
        Idle,
        FollowPlayer,
        AttackingPlayer
    }

    public override void Start()
    {
        base.Start();

        _materials.Add(skinnedMeshRenderers[0].materials[2]);
        _materials.Add(skinnedMeshRenderers[0].materials[3]);
        if (!IsServer) return;
        HandleSwitchingToIdle();
    }

    #region State Machines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        float velocity = agent.velocity.magnitude / 3;
        creatureAnimator.SetFloat(RunSpeedFloat, velocity);
        _materials[0].SetVector(ScrollSpeedID, new Vector3(0, -velocity, 0)); // Left Tread
        _materials[1].SetVector(ScrollSpeedID, new Vector3(0, velocity, 0)); // Right Tread
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
        Plugin.ExtendedLogging($"Checking idle state.");
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled) continue;
            Plugin.ExtendedLogging($"Checking player {player.name} for weapon.");
            if (player.currentlyHeldObjectServer == null || !player.currentlyHeldObjectServer.itemProperties.isDefensiveWeapon) continue;
            Vector3 directionToPlayer = (player.transform.position - eye.position).normalized;
            Plugin.ExtendedLogging($"Dot Product to player: {Vector3.Dot(transform.forward, directionToPlayer)}.");
            if (Vector3.Dot(transform.forward, directionToPlayer) < 0f) continue;
            float distanceToPlayer = Vector3.Distance(eye.position, player.transform.position);
            if (Physics.Raycast(eye.position, directionToPlayer, distanceToPlayer, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore)) continue;
            smartAgentNavigator.StopSearchRoutine();
            SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            SwitchToBehaviourServerRpc((int)PeaceKeeperState.FollowPlayer);
            smartAgentNavigator.DoPathingToDestination(player.transform.position);
            Plugin.ExtendedLogging($"Player spotted holding weapon: {player.name}");
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

        Plugin.ExtendedLogging($"following player with backofftimer: {_backOffTimer}");
        if (targetPlayer.currentlyHeldObjectServer == null || !targetPlayer.currentlyHeldObjectServer.itemProperties.isDefensiveWeapon)
        {
            _backOffTimer += AIIntervalTime;
            if (_backOffTimer > 10f)
            {
                HandleSwitchingToIdle();
                return;
            }
        }
        else
        {
            _backOffTimer = 0f;
        }

        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
    } // do a patch to attacking, if there's any callbacks to players

    public void DoAttackingPlayer()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            HandleSwitchingToIdle();
            return;
        }

        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
        float distanceToTargetPlayer = Vector3.Distance(transform.position, targetPlayer.transform.position);
        if (distanceToTargetPlayer > 3) // todo: add more detection because player in a different height just wont get hit lol.
        {
            if (Vector3.Dot(eye.forward, (targetPlayer.transform.position - eye.position).normalized) < 0.4f || Physics.Raycast(eye.position, eye.forward, distanceToTargetPlayer, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                if (_isShooting)
                {
                    _isShooting = false;
                    agent.speed = _chasingSpeed;
                    creatureAnimator.SetBool(ShootingAnimation, false);
                }
                return;
            }
            if (_isShooting) return;
            agent.speed = _shootingSpeed;
            _isShooting = true;
            creatureAnimator.SetBool(ShootingAnimation, true);
            return;
        }

        if (_isShooting)
        {
            _isShooting = false;
            agent.speed = _chasingSpeed;
            creatureAnimator.SetBool(ShootingAnimation, false);
        }

        if (_bitchSlappingRoutine != null) return;
        _bitchSlappingRoutine = StartCoroutine(DoBitchSlapping());
    }
    #endregion

    #region Misc Functions

    public void HandleSwitchingToIdle()
    {
        _backOffTimer = 0f;
        agent.speed = _walkingSpeed;
        SetTargetServerRpc(-1);
        SwitchToBehaviourServerRpc((int)PeaceKeeperState.Idle);
        smartAgentNavigator.StartSearchRoutine(this.transform.position, 40);
    }

    public IEnumerator DoBitchSlapping()
    {
        creatureAnimator.SetTrigger(BitchSlapAnimation);
        yield return new WaitForSeconds(2f);
        _bitchSlappingRoutine = null;
    }
    #endregion

    #region Callback

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead) return;

        if (playerWhoHit != null)
        {
            targetPlayer = playerWhoHit;
            smartAgentNavigator.StopSearchRoutine();
            agent.speed = _chasingSpeed;
            SwitchToBehaviourStateOnLocalClient((int)PeaceKeeperState.AttackingPlayer);
        }
        enemyHP -= force;

        if (IsOwner && enemyHP <= 0)
        {
            KillEnemyOnOwnerClient();
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);

        creatureVoice.PlayOneShot(dieSFX);
        if (!IsServer) return;
        creatureAnimator.SetBool(IsDeadAnimation, true);
    }
    #endregion

    #region Animation Events

    public void BitchSlapAnimationEvent()
    {
        if (!IsServer) return;
        int numHits = Physics.OverlapCapsuleNonAlloc(_gunStartTransform.position, _gunEndTransform.position, 2f, _cachedColliders, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            Collider collider = _cachedColliders[i];
            Plugin.ExtendedLogging($"Bitch Slap hit {collider.name}");
            if (!collider.TryGetComponent(out IHittable iHittable))
                continue;
            if (iHittable is EnemyAICollisionDetect enemyAICollisionDetect && enemyAICollisionDetect.mainScript.gameObject == gameObject)
                continue;
            if (iHittable is PlayerControllerB player)
            {
                Vector3 directionVector = (player.transform.position - this.transform.position).normalized * 100f;
                player.DamagePlayer(40, true, true, CauseOfDeath.Bludgeoning, 0, false, directionVector);
                player.externalForceAutoFade += directionVector;
            }
            else if (iHittable is EnemyAICollisionDetect enemyAICollisionDetect1)
            {
                enemyAICollisionDetect1.mainScript.HitEnemyOnLocalClient(2, this.transform.position, null, true, -1);
            }
            else
            {
                iHittable.Hit(2, this.transform.position, null, true, -1);
            }
        }
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