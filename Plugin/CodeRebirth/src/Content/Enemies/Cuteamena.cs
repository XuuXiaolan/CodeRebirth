using System;
using CodeRebirth.src.MiscScripts.PathFinding;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class YandereCuteamena : CodeRebirthEnemyAI
{
    [Header("Misc")]
    [SerializeField]
    private InteractTrigger _headPatTrigger = null!;

    [SerializeField]
    private GameObject _cleaverGameObject = null!;

    [Header("Stats")]
    [SerializeField]
    private float _wanderSpeed = 2f;

    [SerializeField]
    private float _followSpeed = 4f;

    [SerializeField]
    private float _chasingSpeed = 6f;

    [SerializeField]
    private int _healAmount = 10;

    [SerializeField]
    private float _healCooldown = 5f;

    [SerializeField]
    private float _attentionDistanceThreshold = 8f;

    [SerializeField]
    private float _jealousAttackRange = 5f;

    [SerializeField]
    private float _detectionRange = 20f;

    [SerializeField]
    private float _doorLockpickInterval = 200f;

    [SerializeField]
    private float _attackInterval = 5f;

    [SerializeField]
    private float _threatFindInterval = 2f;

    [Header("Audio")]
    [SerializeField]
    private AudioSource _griefingSource = null!;

    [SerializeField]
    private AudioClip _spawnSound = null!;

    [SerializeField]
    private AudioClip _cheerUpSound = null!;

    [SerializeField]
    private AudioClip _yandereLaughSound = null!;

    private enum CuteamenaState
    {
        Searching,
        Passive,
        Jealous,
        Yandere,
        Grief
    }

    private enum AttackType
    {
        Cleaver,
        Headbutt
    }

    private PlayerControllerB? _chasingPlayer = null;
    private DoorLock? _targetDoor;
    private bool _isCleaverDrawn = false;
    private float _healTimer = 0f;
    private float _doorLockpickTimer = 0f;
    private float _attackTimer = 0f;
    private float _threatFindTimer = 0f;

    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // Float
    private static readonly int IsDeadAnimation = Animator.StringToHash("isDead"); // Bool
    private static readonly int CryingAnimation = Animator.StringToHash("crying"); // Bool
    private static readonly int HitAnimation = Animator.StringToHash("hit"); // Trigger
    private static readonly int HeadbuttAnimation = Animator.StringToHash("headbutt"); // Trigger
    private static readonly int PetAnimation = Animator.StringToHash("pat"); // Trigger
    private static readonly int PullOutKnifeAnimation = Animator.StringToHash("pullOutKnife"); // Trigger
    private static readonly int SlashAnimation = Animator.StringToHash("slashAttack"); // Trigger
    
    public override void Start()
    {
        smartAgentNavigator = GetComponent<SmartAgentNavigator>(); // todo: remove this
        base.Start();
        _headPatTrigger.onInteract.AddListener(OnHeadPatInteract);
        smartAgentNavigator.StartSearchRoutine(40f);
        agent.speed = _wanderSpeed;
        // creatureSFX.PlayOneShot(_spawnSound);
        _healTimer = _healCooldown;
        _doorLockpickTimer = _doorLockpickInterval;
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead)
            return;

        if (!IsServer) return;

        _healTimer -= Time.deltaTime;
        _doorLockpickTimer -= Time.deltaTime;
        _attackTimer -= Time.deltaTime;
    }

    #region StateMachine
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
            return;

        creatureAnimator.SetFloat(RunSpeedFloat, agent.velocity.magnitude / 3f);
        switch (currentBehaviourStateIndex)
        {
            case (int)CuteamenaState.Searching:
                LookForSenpai();
                break;
            case (int)CuteamenaState.Passive:
                DoPassiveBehavior();
                break;
            case (int)CuteamenaState.Jealous:
                DoJealousBehavior();
                break;
            case (int)CuteamenaState.Yandere:
                DoYandereBehavior();
                break;
            case (int)CuteamenaState.Grief:
                DoGriefBehavior();
                break;
        }
    }

    private void LookForSenpai()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead || !player.isPlayerControlled)
                continue;

            if (Vector3.Distance(transform.position, player.transform.position) < _detectionRange)
            {
                agent.speed = _followSpeed;
                SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                SwitchToBehaviourServerRpc((int)CuteamenaState.Passive);
                Plugin.ExtendedLogging($"Yandere Cuteamena has claimed {player.name} as her Senpai!");
                return;
            }
        }
    }

    private void DoPassiveBehavior()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            agent.speed = 0f;
            creatureAnimator.SetBool(CryingAnimation, true);
            SwitchToBehaviourServerRpc((int)CuteamenaState.Grief);
            return;
        }

        if (targetEnemy != null)
        {
            if (targetEnemy.isEnemyDead)
            {
                SetEnemyTargetServerRpc(-1);
                return;
            }
            smartAgentNavigator.DoPathingToDestination(targetEnemy.transform.position);

            if (_attackTimer > 0)
                return;

            if (Vector3.Distance(transform.position, targetEnemy.transform.position) < 2f)
            {
                _attackTimer = _attackInterval;
                creatureNetworkAnimator.SetTrigger(HeadbuttAnimation);
            }
            return;
        }

        if (_targetDoor)
        {
            smartAgentNavigator.DoPathingToDestination(_targetDoor!.transform.position);

            if (Vector3.Distance(transform.position, _targetDoor.transform.position) < 2f)
            {
                creatureNetworkAnimator.SetTrigger(HeadbuttAnimation);
            }
            
            return;
        }
        
        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);

        if (_healTimer <= 0f && targetPlayer.health < 100)
        {
            HealSenpai();
            _healTimer = _healCooldown;
            return;
        }

        if (_threatFindTimer <= 0f)
        {
            FindThreatsNearbySenpai();
            _threatFindTimer = _threatFindInterval;
            return;
        }
        
        if (_doorLockpickTimer <= 0f)
        {
            ChooseDoorToLockpick();
            _doorLockpickTimer = _doorLockpickInterval;
            return;
        }

        if (IsSenpaiIgnoringMe() || IsSenpaiWithOtherPlayers())
        {
            SwitchToBehaviourServerRpc((int)CuteamenaState.Jealous);
            return;
        }
    }

    private void DoJealousBehavior()
    {
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            agent.speed = 0f;
            creatureAnimator.SetBool(CryingAnimation, true);
            SwitchToBehaviourServerRpc((int)CuteamenaState.Grief);
            return;
        }

        FollowSenpaiForwardly();

        PlayerControllerB? rival = LookForOtherPlayers(out float distanceToRival);
        if (rival != null)
        {
            AttackPlayer(rival, distanceToRival);
            return;
        }

        if (HasBeenFurtherIgnored())
        {
            Plugin.ExtendedLogging("transition to yandere by pure luck");
            creatureNetworkAnimator.SetTrigger(PullOutKnifeAnimation);
            agent.speed = _chasingSpeed;
            SwitchToBehaviourServerRpc((int)CuteamenaState.Yandere);
        }
    }

    private void DoYandereBehavior()
    {
        if (!_isCleaverDrawn)
        {
            return;
        }

        if (_chasingPlayer == null)
        {
            float closestDistance = float.MaxValue;
            PlayerControllerB? closestPlayer = null;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!player.isPlayerControlled || player.isPlayerDead)
                    continue;

                closestPlayer = player;
                closestDistance = Mathf.Min(closestDistance, Vector3.Distance(transform.position, player.transform.position));
            }

            if (closestDistance <= _detectionRange)
            {
                _chasingPlayer = closestPlayer;
            }
        }
        else if (_chasingPlayer.isPlayerDead)
        {
            _chasingPlayer = null;
        }

        ChaseAndAttackPlayer(_chasingPlayer);

        // Check for nearby breaker box; with some chance, shut off the power.
        // AttemptShutOffPower();
    }

    private void DoGriefBehavior()
    {
        if (HasSenpaiBodyBeenMoved())
        {
            Plugin.ExtendedLogging("body moved");
            creatureNetworkAnimator.SetTrigger(PullOutKnifeAnimation);
            agent.speed = _chasingSpeed;
            StartOrStopGriefingSoundServerRpc(false);
            creatureAnimator.SetBool(CryingAnimation, false);
            SwitchToBehaviourServerRpc((int)CuteamenaState.Yandere);
        }
    }
    #endregion

    #region Misc Functions
    private void OnHeadPatInteract(PlayerControllerB player)
    {
        if (player == null || player != targetPlayer)
            return;

        HeadPatServerRpc(player);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HeadPatServerRpc(PlayerControllerReference playerControllerReference)
    {
        PlayerControllerB player = playerControllerReference;
        creatureNetworkAnimator.SetTrigger(PetAnimation);
        // creatureVoice.PlayOneShot(patSounds[enemyRandom.Next(patSounds.Length)]);    

        if (currentBehaviourStateIndex != (int)CuteamenaState.Jealous)
            return;

        if (player != targetPlayer)
            return;

        SwitchToBehaviourClientRpc((int)CuteamenaState.Passive);
    }

    private void ChaseAndAttackPlayer(PlayerControllerB? player)
    {
        if (player == null)
            return;

        smartAgentNavigator.DoPathingToDestination(player.transform.position);

        if (Vector3.Distance(transform.position, player.transform.position) < 2f)
        {
            creatureNetworkAnimator.SetTrigger(SlashAnimation);
            Plugin.ExtendedLogging("Yandere Cuteamena attacked a player with her cleaver!");
        }
    }

    private void FollowSenpaiForwardly()
    {
        // move slightly infront of the player to be kind of more annoying
        Vector3 target = targetPlayer.transform.position + targetPlayer.transform.forward * 2f;
        smartAgentNavigator.DoPathingToDestination(target);
    }

    private void HealSenpai()
    {
        targetPlayer.DamagePlayerFromOtherClientServerRpc(-_healAmount, this.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer));
        Plugin.ExtendedLogging("Cuteamena healed her Senpai!");
        // creatureSFX.PlayOneShot(_cheerUpSound);
    }

    private void FindThreatsNearbySenpai()
    {
        Collider[] nearbyEntities = Physics.OverlapSphere(targetPlayer.transform.position, 10, CodeRebirthUtils.Instance.enemiesMask, QueryTriggerInteraction.Collide);
        foreach (var collider in nearbyEntities)
        {
            EnemyAICollisionDetect enemyAICollisionDetect = collider.GetComponent<EnemyAICollisionDetect>();
            if (enemyAICollisionDetect == null)
                continue;

            if (!enemyAICollisionDetect.mainScript.enemyType.canDie)
                continue;

            SetEnemyTargetServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemyAICollisionDetect.mainScript));
            Plugin.ExtendedLogging("Cuteamena started targetting a nearby monster to protect her Senpai!");
            break;
        }
    }

    private void ChooseDoorToLockpick()
    {
        Collider[] doors = Physics.OverlapSphere(transform.position, 5f, CodeRebirthUtils.Instance.interactableMask, QueryTriggerInteraction.Collide);
        foreach (var doorCollider in doors)
        {
            DoorLock door = doorCollider.GetComponent<DoorLock>();
            if (door != null && door.isLocked)
            {
                _targetDoor = door;
                // todo: probably headbutt the door, maybe set a target door for it to go towards first lol
                Plugin.ExtendedLogging("Cuteamena chose a door to lockpick.");
                break;
            }
        }
    }

    private bool IsSenpaiIgnoringMe()
    {
        return Vector3.Distance(transform.position, targetPlayer.transform.position) > _attentionDistanceThreshold && !PlayerLookingAtEnemy(targetPlayer, 0.2f);
    }

    private bool IsSenpaiWithOtherPlayers()
    {
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == targetPlayer || player.isPlayerDead)
                continue;
            if (Vector3.Distance(targetPlayer.transform.position, player.transform.position) > 5f)
                continue;

            return true;
        }
        return false;
    }

    private PlayerControllerB? LookForOtherPlayers(out float distanceToRival)
    {
        distanceToRival = 0f;
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == targetPlayer || player.isPlayerDead || !player.isPlayerControlled)
                continue;

            distanceToRival = Vector3.Distance(transform.position, player.transform.position);

            if (distanceToRival > _jealousAttackRange + 5)
                continue;

            if (IsSenpaiLookingAt(player))
                continue;

            return player;
        }
        return null;
    }

    private bool IsSenpaiLookingAt(PlayerControllerB player)
    {
        Vector3 toPlayer = (player.transform.position - targetPlayer.gameplayCamera.transform.position).normalized;
        float dot = Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, toPlayer);
        return dot > 0.7f;
    }

    private bool IsSenpaiCheeringUp()
    {
        return UnityEngine.Random.Range(0, 1000) < 5;
    }

    private bool HasBeenFurtherIgnored()
    {
        return UnityEngine.Random.Range(0, 1000) < 2;
    }

    private void AttackPlayer(PlayerControllerB player, float distanceToRival)
    {
        Vector3 knockback = (player.transform.position - transform.position).normalized * 5f;
        smartAgentNavigator.DoPathingToDestination(player.transform.position);

        if (distanceToRival < agent.stoppingDistance + _jealousAttackRange)
            return;

        creatureNetworkAnimator.SetTrigger(HeadbuttAnimation);
        
        Plugin.ExtendedLogging("Cuteamena attacked a rival player out of jealousy!");
    }

    [ClientRpc]
    private void AttackPlayerClientRPC(PlayerControllerReference reference, AttackType attackType)
    {
        PlayerControllerB player = reference;
        if (player != GameNetworkManager.Instance.localPlayerController) return;

        switch (attackType)
        {
            case AttackType.Cleaver:
                player.DamagePlayer(20, true, false, CauseOfDeath.Bludgeoning, 0, false, transform.forward * 10f);
                break;
            case AttackType.Headbutt:
                player.DamagePlayer(10, true, false, CauseOfDeath.Bludgeoning, 0, false, transform.forward * 10f);
                break;
        }
    }

    private bool HasSenpaiBodyBeenMoved()
    {
        if (targetPlayer.deadBody == null)
            return false;
        
        DeadBodyInfo body = targetPlayer.deadBody;
        if (body.grabBodyObject.isHeld)
            return true;

        return body.bodyMovedThisFrame;
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartOrStopGriefingSoundServerRpc(bool start)
    {
        StartOrStopGriefingSoundClientRpc(start);
    }

    [ClientRpc]
    public void StartOrStopGriefingSoundClientRpc(bool start)
    {
        if (start)
        {
            _griefingSource.Play();
        }
        else
        {
            _griefingSource.Stop();
        }
    }
    #endregion

    #region Animation Events
    public void DrawMeatCleaverAnimEvent()
    {
        _isCleaverDrawn = true;
        _cleaverGameObject.SetActive(true);
        Plugin.ExtendedLogging("Cuteamena has drawn her meat cleaver! Yandere mode engaged!");
        // creatureSFX.PlayOneShot(_yandereLaughSound);
    }

    public void DropCleaverAnimEvent()
    {
        // todo: Instantiate or spawn the meat cleaver as scrap
        Plugin.ExtendedLogging("Meat cleaver dropped as scrap.");
    }
    
    public void OnSwingCleaverAnimEvent() {
        if(!IsServer && !IsHost) return;
        if(!_chasingPlayer) return;
        
        AttackPlayerClientRPC(_chasingPlayer!, AttackType.Cleaver);
    }

    public void HeadbuttAnimEvent()
    {
        if(!IsServer && !IsHost) return;
        if (_targetDoor)
        {
            _targetDoor.UnlockDoorClientRpc();
            _targetDoor = null;
        } else if (_chasingPlayer)
        {
            AttackPlayerClientRPC(_chasingPlayer!, AttackType.Headbutt);
        }
        else
        {
            Plugin.ExtendedLogging("reason to headbutt is unknown?");
        }
    }
    
    #endregion

    /*private void AttemptShutOffPower()
    {
        // Look for a breaker box within an 8-unit radius.
        Collider[] boxes = Physics.OverlapSphere(transform.position, 8f, LayerMask.GetMask("BreakerBoxes"));
        if (boxes.Length > 0)
        {
            if (UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10% chance to act when near a breaker box.
            {
                BreakerBox box = boxes[0].GetComponent<BreakerBox>();
                if (box != null)
                {
                    box.ShutOffPower();
                    Plugin.ExtendedLogging("Cuteamena flicked the breaker and shut off the power!");
                }
            }
        }
    }*/

    #region Call Backs
    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead)
            return;

        enemyHP -= force;
        if (enemyHP < 0)
        {
            if (IsOwner)
            {
                KillEnemyOnOwnerClient();
            }
            return;
        }

        if (IsServer)
            creatureNetworkAnimator.SetTrigger(HitAnimation);

        if (playerWhoHit == null || currentBehaviourStateIndex == (int)CuteamenaState.Yandere)
            return;

        _griefingSource.Stop();
        if (currentBehaviourStateIndex == (int)CuteamenaState.Jealous)
        {
            creatureNetworkAnimator.SetTrigger(PullOutKnifeAnimation);
            agent.speed = _chasingSpeed;
            SwitchToBehaviourStateOnLocalClient((int)CuteamenaState.Yandere);
            return;
        }
        SwitchToBehaviourStateOnLocalClient((int)CuteamenaState.Jealous);
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);
        _griefingSource.Stop();
        _cleaverGameObject.SetActive(false);
        if (!IsServer)
            return;

        creatureAnimator.SetBool(IsDeadAnimation, true);
        Plugin.ExtendedLogging("Cuteamena has been defeated.");
    }
    #endregion
}
