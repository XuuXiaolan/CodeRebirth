using System;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class YandereCuteamena : CodeRebirthEnemyAI
{
    [SerializeField]
    private float _wanderSpeed = 2f;

    [SerializeField]
    private float _followSpeed = 4f;

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
    private float _doorLockpickInterval = 200f; // once per Moon

    [SerializeField]
    private float _attackEnemyInterval = 5f;

    [SerializeField]
    private AudioSource _cuteSFX;

    [SerializeField]
    private AudioClip _spawnSound;

    [SerializeField]
    private AudioClip _cheerUpSound;

    [SerializeField]
    private AudioClip _yandereLaughSound;

    [SerializeField]
    private AudioClip _griefSound;

    public static List<YandereCuteamena> Instances = new List<YandereCuteamena>();

    private enum CuteamenaState
    {
        Searching,
        Passive,
        Jealous,
        Yandere,
        Grief
    }

    private float _healTimer = 0f;
    private float _doorLockpickTimer = 0f;
    private float _attackEnemyTimer = 0f;

    private bool _isCleaverDrawn = false;

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
        agent.speed = _wanderSpeed;
        _cuteSFX.PlayOneShot(_spawnSound);
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
        _attackEnemyTimer -= Time.deltaTime;
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

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
        // If Senpai is lost or dead, transition to Grief
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
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

            if (Vector3.Distance(transform.position, targetEnemy.transform.position) < 2f)
            {
                // do attack animation
            }
            return;
        }
        // Follow Senpai closely
        FollowSenpai();

        // Heal Senpai if injured and if cooldown has elapsed.
        if (_healTimer <= 0f && targetPlayer.health < 100)
        {
            HealSenpai();
            _healTimer = _healCooldown;
            return;
        }

        if (_attackEnemyTimer <= 0f)
        {
            AttackThreatsNearSenpai();
            _attackEnemyTimer = _attackEnemyInterval;
            return;
        }

        if (_doorLockpickTimer <= 0f)
        {
            AttemptLockpickDoor();
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
        // If Senpai dies, shift to Grief state.
        if (targetPlayer == null || targetPlayer.isPlayerDead)
        {
            SwitchToBehaviourServerRpc((int)CuteamenaState.Grief);
            return;
        }

        // Continue following Senpai
        FollowSenpai();

        // Look for any other player nearby who might be a rival and, if Senpai isn’t looking, attack them.
        PlayerControllerB? rival = LookForOtherPlayers();
        if (rival != null)
        {
            AttackPlayer(rival);
        }

        // Check if Senpai shows affection (e.g. petting Cuteamena's head);
        // if so, consider reverting back to Passive state.
        if (IsSenpaiCheeringUp())
        {
            SwitchToBehaviourServerRpc((int)CuteamenaState.Passive);
        }

        // If she is further neglected or harmed, escalate to Yandere
        if (HasBeenHarmedOrFurtherIgnored())
        {
            SwitchToBehaviourServerRpc((int)CuteamenaState.Yandere);
        }
    }

    private void DoYandereBehavior()
    {
        // In Yandere mode, ensure the meat cleaver is drawn.
        if (!_isCleaverDrawn)
        {
            DrawMeatCleaver();
        }

        // Chase and attack all players.
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerDead)
                continue;

            ChaseAndAttackPlayer(player);
        }

        // Check for nearby breaker box; with some chance, shut off the power.
        // AttemptShutOffPower();
    }

    private void DoGriefBehavior()
    {
        // Stay near Senpai's body and play grief animations/sounds.
        SitAndCry();

        // If Senpai's body is disturbed (e.g. picked up or teleported), immediately switch to Yandere.
        if (HasSenpaiBodyBeenMoved())
        {
            SwitchToBehaviourServerRpc((int)CuteamenaState.Yandere);
        }
    }

    private void FollowSenpai()
    {
        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position);
    }

    private void HealSenpai()
    {
        targetPlayer.DamagePlayerFromOtherClientServerRpc(-_healAmount, this.transform.position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, targetPlayer));
        Plugin.ExtendedLogging("Cuteamena healed her Senpai!");
        _cuteSFX.PlayOneShot(_cheerUpSound);
    }

    private void AttackThreatsNearSenpai()
    {
        // Find nearby enemy colliders within a radius (e.g., 10 units)
        Collider[] nearbyEntities = Physics.OverlapSphere(targetPlayer.transform.position, 10f, CodeRebirthUtils.Instance.enemiesMask);
        foreach (var collider in nearbyEntities)
        {
            EnemyAICollisionDetect monster = collider.GetComponent<EnemyAICollisionDetect>();
            if (monster == null)
                continue;
            // Skip excluded types
            if (!monster.mainScript.enemyType.canDie)
                continue;

            AttackMonster(monster);
        }
    }

    private void AttemptLockpickDoor()
    {
        // Look for doors within a small radius (e.g., 5 units)
        Collider[] doors = Physics.OverlapSphere(transform.position, 5f, CodeRebirthUtils.Instance.interactableMask, QueryTriggerInteraction.Collide);
        foreach (var doorCollider in doors)
        {
            DoorLock door = doorCollider.GetComponent<DoorLock>();
            if (door != null && door.isLocked)
            {
                door.UnlockDoorClientRpc();
                Plugin.ExtendedLogging("Cuteamena attempted to lockpick a door.");
                break;
            }
        }
    }

    private bool IsSenpaiIgnoringMe()
    {
        return Vector3.Distance(transform.position, targetPlayer.transform.position) > _attentionDistanceThreshold;
    }

    private bool IsSenpaiWithOtherPlayers()
    {
        // If another player is very near the Senpai, return true.
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == targetPlayer || player.isPlayerDead)
                continue;
            if (Vector3.Distance(targetPlayer.transform.position, player.transform.position) < 5f)
            {
                return true;
            }
        }
        return false;
    }

    private PlayerControllerB? LookForOtherPlayers()
    {
        // Look for a rival player (other than Senpai) within the jealous attack range.
        foreach (var player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player == targetPlayer || player.isPlayerDead)
                continue;
            if (Vector3.Distance(transform.position, player.transform.position) < _jealousAttackRange)
            {
                if (!IsSenpaiLookingAt(player))
                {
                    return player;
                }
            }
        }
        return null;
    }

    private bool IsSenpaiLookingAt(PlayerControllerB player)
    {
        Vector3 toPlayer = (player.transform.position - targetPlayer.transform.position).normalized;
        float dot = Vector3.Dot(targetPlayer.transform.forward, toPlayer);
        return dot > 0.7f;
    }

    private bool IsSenpaiCheeringUp()
    {
        return UnityEngine.Random.Range(0, 1000) < 5;
    }

    private bool HasBeenHarmedOrFurtherIgnored()
    {
        return UnityEngine.Random.Range(0, 1000) < 2;
    }

    private void AttackPlayer(PlayerControllerB player)
    {
        Vector3 knockback = (player.transform.position - transform.position).normalized * 5f;
        player.DamagePlayer(10, true, false, CauseOfDeath.Bludgeoning, 0, false, knockback);
        Plugin.ExtendedLogging("Cuteamena attacked a rival player out of jealousy!");
    }

    private void AttackMonster(EnemyAICollisionDetect monster)
    {
        SetEnemyTargetServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(monster.mainScript));
        // monster.mainScript.HitEnemyOnLocalClient(1, transform.position, null, true, -1);
        Plugin.ExtendedLogging("Cuteamena attacked a monster to protect her Senpai!");
    }

    private void DrawMeatCleaver()
    {
        _isCleaverDrawn = true;
        Plugin.ExtendedLogging("Cuteamena has drawn her meat cleaver! Yandere mode engaged!");
        _cuteSFX.PlayOneShot(_yandereLaughSound);
        // trigger an animation
    }

    private void ChaseAndAttackPlayer(PlayerControllerB player)
    {
        smartAgentNavigator.DoPathingToDestination(player.transform.position);

        // If close enough, do animation event that deals damage.
        if (Vector3.Distance(transform.position, player.transform.position) < 2f)
        {
            player.DamagePlayer(20, true, false, CauseOfDeath.Bludgeoning, 0, false, transform.forward * 10f);
            Plugin.ExtendedLogging("Yandere Cuteamena attacked a player with her cleaver!");
        }
    }

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

    private void SitAndCry()
    {
        Plugin.ExtendedLogging("Cuteamena is grieving over her lost Senpai...");
        _cuteSFX.PlayOneShot(_griefSound);
        // trigger a grief animation and halt movement.
    }

    private bool HasSenpaiBodyBeenMoved()
    {
        // In a full implementation, this would detect if the Senpai’s body object has moved.
        // For now, simulate with a small random chance.
        return UnityEngine.Random.Range(0, 1000) < 3; // ~0.3% chance per frame.
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

        enemyHP -= force;
        if (enemyHP < 0)
        {
            if (!IsOwner) return;
            KillEnemyOnOwnerClient();
            return;
        }
        // If Cuteamena is hit while in Passive or Jealous modes, she becomes Yandere.
        if (currentBehaviourStateIndex == (int)CuteamenaState.Passive || currentBehaviourStateIndex == (int)CuteamenaState.Jealous)
        {
            SwitchToBehaviourStateOnLocalClient((int)CuteamenaState.Yandere);
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        base.KillEnemy(destroy);

        if (!IsServer) return;
        DropCleaver();
        Plugin.ExtendedLogging("Cuteamena has been defeated.");
    }

    private void DropCleaver()
    {
        // Instantiate or spawn the meat cleaver as scrap
        Plugin.ExtendedLogging("Meat cleaver dropped as scrap.");
    }
}
