using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class PeaceKeeper : CodeRebirthEnemyAI, IVisibleThreat
{
    [SerializeField]
    private Transform _gunStartTransform = null!;

    [SerializeField]
    private Transform _gunEndTransform = null!;

    [Header("Movement")]
    [SerializeField]
    private float _walkingSpeed = 2f;
    [SerializeField]
    private float _chasingSpeed = 5f;
    [SerializeField]
    private float _shootingSpeed = 0.1f;

    [Header("Visuals")]
    [SerializeField]
    private GameObject _gunParticleSystemGO = null!;
    [SerializeField]
    private GameObject _fakeGunGO = null!;

    [Header("Audio")]
    [SerializeField]
    private AudioSource _treadsSource = null!;
    [SerializeField]
    private AudioSource _aggroSFX = null!;
    [SerializeField]
    private AudioClip _revUpSound = null!;
    [SerializeField]
    private AudioClip _revDownSound = null!;
    [SerializeField]
    private AudioClip _bitchSlapSound = null!;
    [SerializeField]
    private AudioClip _bitchSlapStartSound = null!;

    private List<Material> _materials = new();
    private float _backOffTimer = 0f;
    private bool _isShooting = false;
    private bool _killedByPlayer = false;
    private float _damageInterval = 0f;
    private Coroutine? _bitchSlappingRoutine = null;
    private Collider[] _cachedColliders = new Collider[30];
    private static readonly int ShootingAnimation = Animator.StringToHash("shooting"); // Bool
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int BitchSlapAnimation = Animator.StringToHash("bitchSlap"); // Trigger

    private static readonly int ScrollSpeedID = Shader.PropertyToID("_ScrollSpeed"); // Vector3

    #region IVisibleThreat
    public ThreatType type => ThreatType.RadMech;

    int IVisibleThreat.SendSpecialBehaviour(int id)
    {
        return 0;
    }

    int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
    {
        return 18;
    }

    int IVisibleThreat.GetInterestLevel()
    {
        return 0;
    }

    Transform IVisibleThreat.GetThreatLookTransform()
    {
        return base.transform;
    }

    Transform IVisibleThreat.GetThreatTransform()
    {
        return base.transform;
    }

    Vector3 IVisibleThreat.GetThreatVelocity()
    {
        if (base.IsOwner)
        {
            return agent.velocity;
        }
        return Vector3.zero;
    }

    float IVisibleThreat.GetVisibility()
    {
        if (isEnemyDead)
        {
            return 0f;
        }
        if (agent.velocity.sqrMagnitude > 0f)
        {
            return 1f;
        }
        return 0.75f;
    }

	bool IVisibleThreat.IsThreatDead()
	{
		return isEnemyDead;
	}

	GrabbableObject? IVisibleThreat.GetHeldObject()
	{
		return null;
	}
    #endregion
    public static List<PeaceKeeper> Instances = new();
    public enum PeaceKeeperState
    {
        Idle,
        FollowPlayer,
        AttackingPlayer
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instances.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instances.Remove(this);
    }

    public override void Start()
    {
        base.Start();
        _materials.Add(skinnedMeshRenderers[0].materials[2]);
        _materials.Add(skinnedMeshRenderers[0].materials[3]);
        if (!IsServer) return;
        HandleSwitchingToIdle();
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead)
            return;

        _idleTimer -= Time.deltaTime;
        if (_idleTimer <= 0)
        {
            _idleTimer = enemyRandom.NextFloat(_idleAudioClips.minTime, _idleAudioClips.maxTime);
            creatureVoice.PlayOneShot(_idleAudioClips.audioClips[enemyRandom.Next(0, _idleAudioClips.audioClips.Length)]);
        }

        if (!_aggroSFX.isPlaying && currentBehaviourStateIndex == (int)PeaceKeeperState.AttackingPlayer)
        {
            _aggroSFX.Play();
        }

        if (!IsServer)
            return;

        if (_bitchSlappingRoutine != null)
        {
            RotateTowardsNearestPlayer();
            return;
        }

        if (!_isShooting)
            return;

        if (!_gunParticleSystemGO.activeSelf)
            return;

        DoGatlingGunDamage();
    }

    #region State Machines
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            return;

        float velocity = agent.velocity.magnitude / 3;
        creatureAnimator.SetFloat(RunSpeedFloat, velocity);
        _materials[0].SetVector(ScrollSpeedID, new Vector3(0, -velocity, 0)); // Left Tread
        _materials[1].SetVector(ScrollSpeedID, new Vector3(0, velocity, 0)); // Right Tread
        if (velocity > 0)
        {
            _treadsSource.volume = 1f;
        }
        else
        {
            _treadsSource.volume = 0f;
        }
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
            if (player.isPlayerDead || !player.isPlayerControlled)
                continue;

            if (player.currentlyHeldObjectServer == null || !player.currentlyHeldObjectServer.itemProperties.isDefensiveWeapon)
                continue;

            if (!EnemySeesPlayer(player, 0f))
                continue;

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
    }

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
            if (!EnemySeesPlayer(targetPlayer, 0.6f))
            {
                if (_isShooting)
                {
                    _isShooting = false;
                    agent.speed = _chasingSpeed;
                    creatureAnimator.SetBool(ShootingAnimation, false);
                }
                return;
            }
            if (_isShooting)
                return;
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

        if (_bitchSlappingRoutine != null)
            return;

        _bitchSlappingRoutine = StartCoroutine(DoBitchSlapping());
    }
    #endregion

    #region Misc Functions

    public void RotateTowardsNearestPlayer()
    {
        PlayerControllerB? nearestPlayer = null;
        float closestDistance = float.MaxValue;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled)
                continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > closestDistance)
                continue;

            nearestPlayer = player;
        }

        if (nearestPlayer == null)
            return;

        Vector3 direction = nearestPlayer.transform.position - transform.position;
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5 * Time.deltaTime);
    }

    public void HandleSwitchingToIdle()
    {
        _backOffTimer = 0f;
        agent.speed = _walkingSpeed;
        SetTargetServerRpc(-1);
        SwitchToBehaviourServerRpc((int)PeaceKeeperState.Idle);
        smartAgentNavigator.StartSearchRoutine(40);
    }

    public IEnumerator DoBitchSlapping()
    {
        creatureAnimator.SetTrigger(BitchSlapAnimation);
        yield return new WaitForSeconds(2f);
        _bitchSlappingRoutine = null;
    }

    [Header("Gatling Gun")]
    [SerializeField]
    private float _gatlingDamageInterval = 0.21f;
    [SerializeField]
    private float _minigunRange = 30f;
    [SerializeField]
    private float _minigunWidth = 1f;
    [SerializeField]
    private int _minigunDamage = 5;
    [SerializeField]
    private Transform _leftGunStartTransform = null!;
    [SerializeField]
    private Transform _rightGunStartTransform = null!;

    public void DoGatlingGunDamage()
    {
        if (_damageInterval < _gatlingDamageInterval)
        {
            _damageInterval += Time.deltaTime;
            return;
        }
        _damageInterval = 0f;

        if (!IsServer) return;
        // Use a HashSet to avoid applying damage twice to the same target
        HashSet<IHittable> damagedTargets = new HashSet<IHittable>();

        // Process both minigun heads
        Transform[] gunTransforms = new Transform[] { _leftGunStartTransform, _rightGunStartTransform };

        foreach (var gunTransform in gunTransforms)
        {
            // Define the capsule area starting at the gun head and extending forward by _minigunRange.
            Vector3 capsuleStart = gunTransform.position;
            Vector3 capsuleEnd = gunTransform.position + gunTransform.forward * _minigunRange;

            int numHits = Physics.OverlapCapsuleNonAlloc(capsuleStart, capsuleEnd, _minigunWidth, _cachedColliders, CodeRebirthUtils.Instance.playersAndInteractableAndEnemiesAndPropsHazardMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < numHits; i++)
            {
                Collider collider = _cachedColliders[i];
                if (!collider.TryGetComponent(out IHittable hittable))
                    continue;

                // Skip if already processed this target this tick.
                if (damagedTargets.Contains(hittable))
                    continue;

                damagedTargets.Add(hittable);

                if (hittable is EnemyAICollisionDetect enemyAICollision && enemyAICollision.mainScript.gameObject == gameObject)
                {
                    continue;
                }

                // Apply damage based on the target type.
                if (hittable is PlayerControllerB player)
                {
                    // Calculate a direction vector (for potential knockback effects).
                    Vector3 damageDirection = (player.transform.position - gunTransform.position).normalized;
                    player.DamagePlayer(_minigunDamage, true, true, CauseOfDeath.Gunshots, 0, false, damageDirection * 10f);
                    player.externalForceAutoFade += damageDirection * 2f;
                }
                else if (hittable is EnemyAICollisionDetect enemy)
                {
                    enemy.mainScript.HitEnemyOnLocalClient(_minigunDamage, gunTransform.position, null, true, -1);
                }
                else
                {
                    hittable.Hit(_minigunDamage, gunTransform.position, null, true, -1);
                }
            }
        }
    }

    public void AlertPeaceKeeperToLocalPlayer(PlayerControllerB playerWhoHit)
    {
        if (isEnemyDead)
            return;

        if (playerWhoHit.isPlayerDead)
            return;

        if (currentBehaviourStateIndex == (int)PeaceKeeperState.AttackingPlayer)
            return;

        if (currentBehaviourStateIndex == (int)PeaceKeeperState.FollowPlayer && targetPlayer == playerWhoHit)
        {
            AlertPeaceKeeperToPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoHit));
            return;
        }

        if (EnemySeesPlayer(playerWhoHit, 0f))
        {
            AlertPeaceKeeperToPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoHit));
            return;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AlertPeaceKeeperToPlayerServerRpc(int playerIndex)
    {
        targetPlayer = StartOfRound.Instance.allPlayerScripts[playerIndex];
        smartAgentNavigator.StopSearchRoutine();
        agent.speed = _chasingSpeed;
        SwitchToBehaviourClientRpc((int)PeaceKeeperState.AttackingPlayer);
    }
    #endregion

    #region Callback

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead)
            return;

        if (playerWhoHit != null)
        {
            targetPlayer = playerWhoHit;
            smartAgentNavigator.StopSearchRoutine();
            agent.speed = _chasingSpeed;
            SwitchToBehaviourStateOnLocalClient((int)PeaceKeeperState.AttackingPlayer);
        }
        else if (IsServer)
        {
            creatureNetworkAnimator.SetTrigger(BitchSlapAnimation);
        }
        enemyHP -= force;

        if (enemyHP <= 0)
        {
            if (playerWhoHit != null)
            {
                Plugin.ExtendedLogging($"PeaceKeeper killed by player: {playerWhoHit.name} who dealt {force} damage with hit ID: {hitID}.");
                _killedByPlayer = true;
            }
            if (IsOwner)
                KillEnemyOnOwnerClient();
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        smartAgentNavigator.StopSearchRoutine();
        _aggroSFX.Stop();
        creatureVoice.PlayOneShot(dieSFX);
        if (_killedByPlayer)
        {
            _fakeGunGO.transform.parent.gameObject.SetActive(false);
        }

        if (!IsServer)
            return;

        if (_killedByPlayer)
        {
            CodeRebirthUtils.Instance.SpawnScrap(EnemyHandler.Instance.PeaceKeeper!.ItemDefinitions.GetCRItemDefinitionWithItemName("Ceasefire")?.item, _fakeGunGO.transform.position, false, false, 0, _fakeGunGO.transform.parent.rotation);
        }
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

            if (iHittable is PlayerControllerB player)
            {
                Vector3 directionVector = (player.transform.position - this.transform.position).normalized * 100f;
                player.DamagePlayer(40, true, true, CauseOfDeath.Bludgeoning, 0, false, directionVector);
                player.externalForceAutoFade += directionVector;
            }
            else if (iHittable is EnemyAICollisionDetect enemyAICollisionDetect)
            {
                if (enemyAICollisionDetect.mainScript.gameObject == gameObject)
                    continue;

                enemyAICollisionDetect.mainScript.HitEnemyOnLocalClient(2, this.transform.position, null, true, -1);
            }
            else
            {
                iHittable.Hit(2, this.transform.position, null, true, -1);
            }
        }
    }

    public void PlayMiscSoundsAnimationEvent(int soundID)
    {
        switch (soundID)
        {
            case 0:
                creatureSFX.PlayOneShot(_revUpSound);
                break;
            case 1:
                creatureSFX.PlayOneShot(_revDownSound);
                break;
            case 2:
                creatureSFX.PlayOneShot(_bitchSlapSound);
                break;
            case 3:
                creatureSFX.PlayOneShot(_bitchSlapStartSound);
                break;
        }
    }
    #endregion
}