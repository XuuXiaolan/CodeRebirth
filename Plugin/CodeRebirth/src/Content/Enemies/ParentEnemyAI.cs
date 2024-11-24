using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class ParentEnemyAI : CodeRebirthEnemyAI
{
    public float Acceleration = 0f;
    public float WalkingSpeed = 0f;
    public float SprintingSpeed = 0f;
    public int MeleeDamage = 0;
    public int SpecialMeleeDamage = 0;
    public int SpecialAOEDamage = 0;
    public float SpecialAttackPushForce = 0f;
    public List<Material> ShinyMaterials = new();
    public AudioClip NormalAttackSound = null!;
    public AudioClip SpecialAttackSound = null!;
    public AudioClip SpawnSound = null!;
    public AudioClip EnrageSound = null!;
    public AudioClip[] IdleSounds = null!;
    public AudioClip[] FootstepSounds = null!;
    public AudioClip[] FastFootstepSounds = null!;
    public AudioClip[] hitSounds = null!;
    public AudioClip[] deathSounds = null!;
    public Transform MouthTransform = null!;

    private float specialAttackTimer = 15f;
    private bool CloseToEevee => Vector3.Distance(childEevee.transform.position, this.transform.position) <= 10;
    [NonSerialized] public Transform spawnTransform = null!;
    private ChildEnemyAI childEevee = null!;
    private float timeSinceHittingLocalPlayer = 0f;
    private bool holdingChild = false;
    [NonSerialized] public bool isSpawnInside = true;
    private System.Random enemyRandom = null!;
    private bool childCreated = false;
    private static readonly int GuardingAnimation = Animator.StringToHash("isGuarding"); // bool
    private static readonly int DeadAnimation = Animator.StringToHash("isDead"); // bool
    private static readonly int RunSpeedFloat = Animator.StringToHash("RunSpeed"); // float
    private static readonly int RunningAnimation = Animator.StringToHash("isRunning"); // bool
    private static readonly int WalkingAnimation = Animator.StringToHash("isWalking"); // bool
    private static readonly int OnHitAnimation = Animator.StringToHash("onHit"); // trigger
    private static readonly int MeleeAnimation = Animator.StringToHash("doMelee"); // trigger
    private static readonly int SpecialMeleeAnimation = Animator.StringToHash("doSpecialMelee"); // trigger
    private static readonly int StareAnimation = Animator.StringToHash("doStare"); // trigger

    public enum State
    {
        Spawning,
        Wandering,
        Guarding,
        ChasingPlayer,
        Death,
    }

    public void BaseOutsideOrInsideStart()
    {
        var outsideNodePositions = new Vector3[RoundManager.Instance.outsideAINodes.Length];
        for (int i = 0; i < RoundManager.Instance.outsideAINodes.Length; i++)
        {
            outsideNodePositions[i] = RoundManager.Instance.outsideAINodes[i].transform.position;
        }

        var insideNodePositions = new Vector3[RoundManager.Instance.insideAINodes.Length];
        for (int i = 0; i < RoundManager.Instance.insideAINodes.Length; i++)
        {
            insideNodePositions[i] = RoundManager.Instance.insideAINodes[i].transform.position;
        }

        Vector3 enemyPos = this.transform.position;
        Vector3 closestOutsideNode = Vector3.positiveInfinity;
        Vector3 closestInsideNode = Vector3.positiveInfinity;

        for (int i = 0; i < outsideNodePositions.Length; i++)
        {
            if ((outsideNodePositions[i] - enemyPos).sqrMagnitude < (closestOutsideNode - enemyPos).sqrMagnitude)
            {
                closestOutsideNode = outsideNodePositions[i];
            }
        }
        for (int i = 0; i < insideNodePositions.Length; i++)
        {
            if ((insideNodePositions[i] - enemyPos).sqrMagnitude < (closestInsideNode - enemyPos).sqrMagnitude)
            {
                closestInsideNode = insideNodePositions[i];
            }
        }

        if (!this.isOutside && ((closestOutsideNode - enemyPos).sqrMagnitude < (closestInsideNode - enemyPos).sqrMagnitude))
        {
            this.SetEnemyOutside(true);
            isSpawnInside = true;
        }
        else if (this.isOutside && ((closestOutsideNode - enemyPos).sqrMagnitude > (closestInsideNode - enemyPos).sqrMagnitude))
        {
            this.SetEnemyOutside(false);
            isSpawnInside = false;
        }
        this.spawnTransform = this.transform;
        this.favoriteSpot = this.transform;
    }

    public override void Start()
    {
        base.Start();
        agent.speed = 0f;
        agent.acceleration = 0f;
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 323);
        if (enemyRandom.NextInt(1, 100) <= 5 && ShinyMaterials.Count != 0)
        {
            this.skinnedMeshRenderers[0].materials = ShinyMaterials.ToArray();
        }
        creatureVoice.PlayOneShot(SpawnSound);
        BaseOutsideOrInsideStart();
        if (!IsServer) return;
        HandleStateAnimationSpeedChanges(State.Spawning);
        StartCoroutine(SpawnTimer());
    }

    public IEnumerator SpawnTimer()
    {
        yield return new WaitForSeconds(1f);
        HandleStateAnimationSpeedChanges(State.Wandering);        
        SpawnEeveeInNest();
    }

    private void SpawnEeveeInNest()
    {
        NetworkObjectReference go = CodeRebirthUtils.Instance.SpawnScrap(EnemyHandler.Instance.PokemonEnemies.ChildEeveeItem, spawnTransform.position, false, true, 0);
        SpawnEggInNestClientRpc(go);
    }

    [ClientRpc]
    public void SpawnEggInNestClientRpc(NetworkObjectReference go)
    {
        childCreated = true;
        ChildEnemyAI _childEevee = ((GameObject)go).GetComponent<ChildEnemyAI>();
        _childEevee.mommyAlive = !isEnemyDead;
        _childEevee.NetworkObject.OnSpawn(() => {
            childEevee = _childEevee;
            childEevee.transform.SetParent(StartOfRound.Instance.propsContainer, true);
            Plugin.ExtendedLogging($"Spawned eevee: {this.transform.position}");
        });
    }


    [ServerRpc(RequireOwnership = false)]
    private void HandleStateAnimationSpeedChangesServerRpc(int state)
    {
        HandleStateAnimationSpeedChanges((State)state);
    }

    private void HandleStateAnimationSpeedChanges(State state) // This is for host
    {
        SwitchStateOrEmotionClientRpc((int)state);
        switch (state)
        {
            case State.Spawning:
                SetAnimatorBools(false, false, false, false);
                break;
            case State.Wandering:
                SetAnimatorBools(false, false, false, false);
                break;
            case State.Guarding:
                SetAnimatorBools(false, false, false, false);
                break;
            case State.ChasingPlayer:
                SetAnimatorBools(false, false, false, false);
                break;
            case State.Death:
                SetAnimatorBools(false, false, false, false);
                break;
        }
    }

    private void SetAnimatorBools(bool holding, bool attack, bool dance, bool activated)
    {
        creatureAnimator.SetBool("", holding);
        creatureAnimator.SetBool("", attack);
        creatureAnimator.SetBool("", dance);
        creatureAnimator.SetBool("", activated);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchStateOrEmotionServerRpc(int state)
    {
        SwitchStateOrEmotionClientRpc(state);
    }

    [ClientRpc]
    private void SwitchStateOrEmotionClientRpc(int state)
    {
        SwitchStateOrEmotion(state);
    }

    private void SwitchStateOrEmotion(int state) // this is for everyone.
    {
        State stateToSwitchTo = (State)state;
        if (state != -1)
        {
            switch (stateToSwitchTo)
            {
                case State.Spawning:
                    HandleStateSpawnChange();
                    break;
                case State.Wandering:
                    HandleStateWanderingChange();
                    break;
                case State.Guarding:
                    HandleStateGuardingChange();
                    break;
                case State.ChasingPlayer:
                    HandleStateChasingPlayerChange();
                    break;
                case State.Death:
                    HandleStateDeathChange();
                    break;
            }
            SwitchToBehaviourStateOnLocalClient(state);
        }
    }

    #region State Changes
    private void HandleStateSpawnChange()
    {
    }

    private void HandleStateWanderingChange()
    {
        smartAgentNavigator.StartSearchRoutine(transform.position, 40);
        agent.speed = WalkingSpeed;
        agent.acceleration = Acceleration; 
    }

    private void HandleStateGuardingChange()
    {
        smartAgentNavigator.StopSearchRoutine();
        GrabChild(childEevee, MouthTransform);
    }

    private void HandleStateChasingPlayerChange()
    {
        smartAgentNavigator.StopSearchRoutine();
    }

    private void HandleStateDeathChange()
    {
        smartAgentNavigator.StopSearchRoutine();
    }
    #endregion

    public override void Update()
    {
        base.Update();
        if (isEnemyDead) return;
        specialAttackTimer -= Time.deltaTime;
        timeSinceHittingLocalPlayer -= Time.deltaTime;
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead || !IsHost) return;
        
        switch (currentBehaviourStateIndex)
        {
            case (int)State.Wandering:
                DoWandering();
                break;
            case (int)State.Guarding:
                DoGuarding();
                break;
            case (int)State.ChasingPlayer:
                DoChasingPlayer();
                break;
        }
    }

    public void DoWandering()
    {
        if (CloseToEevee)
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                Vector3 directionToEevee = (childEevee.transform.position - player.gameplayCamera.transform.position).normalized;
                float dotProduct = Vector3.Dot(player.gameplayCamera.transform.forward, directionToEevee);
                float distance = Vector3.Distance(player.gameplayCamera.transform.position, childEevee.transform.position);
                if (distance <= 10 && dotProduct <= 0.5)
                {
                    smartAgentNavigator.DoPathingToDestination(childEevee.transform.position, childEevee.isInFactory, false, null);
                    this.transform.rotation = Quaternion.LookRotation(player.transform.position - this.transform.position);
                    // player is close and looking at eevee's direction.
                    if (distance < 5 && Vector3.Distance(player.transform.position, spawnTransform.position) <= 30)
                    {
                        SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
                        HandleStateAnimationSpeedChanges(State.ChasingPlayer);
                    }
                    return;
                }
            }
        }
        else
        {
            if (childEevee.playerHeldBy == null || Vector3.Distance(childEevee.transform.position, spawnTransform.position) < 30) return;
            SetTargetClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, childEevee.playerHeldBy));
            HandleStateAnimationSpeedChanges(State.ChasingPlayer);
        }        
    }

    public void DoGuarding()
    {
        if (!holdingChild && specialAttackTimer <= 0) // come to this state after hitting a player and making them drop the eevee, so eevee wont be held by anyone really
        {
            smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position, targetPlayer.isInsideFactory, true, targetPlayer);
            if (Vector3.Distance(targetPlayer.transform.position, this.transform.position) <= 5)
            {
                // do special attack
                specialAttackTimer = 10f;
            }
        }
        smartAgentNavigator.DoPathingToDestination(childEevee.transform.position, childEevee.isInFactory, true, null);
        
    }

    public void DoChasingPlayer()
    {
        // If the eevee is held by the player, keep chasing the player until the eevee is dropped
        if (childEevee == null)
        {
            Plugin.ExtendedLogging("Child eevee turned null somehow");
            HandleStateAnimationSpeedChanges(State.Wandering);
            return;
        }

        smartAgentNavigator.DoPathingToDestination(targetPlayer.transform.position, targetPlayer.isInsideFactory, true, targetPlayer);
        if (Vector3.Distance(targetPlayer.transform.position, this.transform.position) <= 5)
        {
            // do attacks and animation stuff.
        }
    }

    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        
        creatureVoice.PlayOneShot(hitSounds[enemyRandom.Next(0, hitSounds.Length)]);
        if (isEnemyDead || currentBehaviourStateIndex == (int)State.Death) return;

        enemyHP -= force;
        if (IsOwner && enemyHP <= 0 && !isEnemyDead)
        {
            KillEnemyOnOwnerClient();
            return;
        }

        if (holdingChild)
        {
            DropChild(true);
        }

        if (playerWhoHit != null)
        {
            if (specialAttackTimer <= 0)
            {
                specialAttackTimer = 15f;
                // Do animation for special attack
            }
            if (currentBehaviourStateIndex != (int)State.ChasingPlayer)
            {
                AggroOnHit(playerWhoHit);
            }
        }
        Plugin.ExtendedLogging($"Enemy HP: {enemyHP}");
        Plugin.ExtendedLogging($"Hit with force {force}");
    }

    public void AggroOnHit(PlayerControllerB playerWhoStunned)
    {
        SetTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerWhoStunned));
        HandleStateAnimationSpeedChanges(State.ChasingPlayer);
    }


    public override void KillEnemy(bool destroy = false) 
    {
        base.KillEnemy(destroy);
        if (IsServer)
        {
            HandleStateAnimationSpeedChanges(State.Death);
        }
        if (holdingChild)
        {
            DropChild(true);
        }
        if (childEevee != null) childEevee.mommyAlive = false;
        creatureVoice.PlayOneShot(deathSounds[enemyRandom.Next(0, deathSounds.Length)]);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DropChildServerRpc(bool wasHurt)
    {
        DropChildClientRpc(wasHurt);
    }

    [ClientRpc]
    public void DropChildClientRpc(bool wasHurt) 
    {
        DropChild(wasHurt);
    }

    public void DropChild(bool wasHurt)
    {
        childEevee.parentObject = null;
        if (StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(transform.position))
        {
            Plugin.ExtendedLogging($"Dropping childEevee in ship room: {childEevee}");
            childEevee.isInShipRoom = true;
            childEevee.isInElevator = true;
            childEevee.transform.SetParent(GameNetworkManager.Instance.localPlayerController.playersManager.elevatorTransform, true);
            childEevee.EnablePhysics(true);
            childEevee.EnableItemMeshes(true);
            childEevee.transform.localScale = childEevee.originalScale;
            childEevee.isHeld = false;
            childEevee.isPocketed = false;
            childEevee.fallTime = 0f;
            childEevee.startFallingPosition = childEevee.transform.parent.InverseTransformPoint(childEevee.transform.position);
            Vector3 vector2 = childEevee.GetItemFloorPosition(default(Vector3));
            childEevee.targetFloorPosition = GameNetworkManager.Instance.localPlayerController.playersManager.elevatorTransform.InverseTransformPoint(vector2);
            childEevee.floorYRot = -1;
            childEevee.grabbable = true;
            childEevee.isHeldByEnemy = false;
            childEevee.transform.rotation = Quaternion.Euler(childEevee.itemProperties.restingRotation);
        }
        else
        {
            childEevee.isInShipRoom = false;
            childEevee.isInElevator = false;
            childEevee.EnablePhysics(true);
            childEevee.fallTime = 0f;
            childEevee.startFallingPosition = childEevee.transform.parent.InverseTransformPoint(childEevee.transform.position);
            childEevee.targetFloorPosition = childEevee.transform.parent.InverseTransformPoint(childEevee.GetItemFloorPosition(default(Vector3)));
            childEevee.floorYRot = -1;
            childEevee.DiscardItemFromEnemy();
            childEevee.grabbable = true;
            childEevee.isHeldByEnemy = false;
            childEevee.transform.rotation = Quaternion.Euler(childEevee.itemProperties.restingRotation);
            childEevee.transform.SetParent(StartOfRound.Instance.propsContainer, true);
        }
        if (wasHurt)
        {
            // run an event on eevee that makes her run away.
        }
        // creatureVoice.PlayOneShot(TakeDropItemSounds[galRandom.NextInt(0, TakeDropItemSounds.Length - 1)]);
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayMiscSoundsServerRpc(int soundID)
    {
        PlayMiscSoundsClientRpc(soundID);
    }

    [ClientRpc]
    public void PlayMiscSoundsClientRpc(int soundID)
    {
        switch (soundID)
        {
            default:
                Plugin.ExtendedLogging($"Invalid sound ID: {soundID}");
                break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GrabChildServerRpc()
    {
        GrabChildClientRpc(new NetworkObjectReference(childEevee.NetworkObject));
    }

    [ClientRpc]
    private void GrabChildClientRpc(NetworkObjectReference networkObjectReference)
    {
        StartCoroutine(GrabChild(((GameObject)networkObjectReference).GetComponent<GrabbableObject>(), MouthTransform));
    }

    private IEnumerator GrabChild(GrabbableObject child, Transform mouthTransform)
    {
        yield return new WaitForSeconds(0.2f);
        child.isInElevator = false;
        child.isInShipRoom = false;
        child.playerHeldBy?.DiscardHeldObject();
        yield return new WaitForSeconds(0.2f);
        child.grabbable = false;
        child.isHeldByEnemy = true;
        child.hasHitGround = false;
        child.parentObject = mouthTransform;
        child.EnablePhysics(false);
        holdingChild = true;
        // creatureVoice.PlayOneShot(TakeDropItemSounds[galRandom.NextInt(0, TakeDropItemSounds.Length - 1)]);
        HoarderBugAI.grabbableObjectsInMap.Remove(child.gameObject);
    }

    public void OnCollideMeleeAttack(Collider other)
    {
        if (timeSinceHittingLocalPlayer > 0f || isEnemyDead) return;
        PlayerControllerB player = other.GetComponent<PlayerControllerB>();
        if (targetPlayer == null || player == null)
        {
            return;
        }

        timeSinceHittingLocalPlayer = 1.5f;
        Plugin.ExtendedLogging("Hitting player with normal melee attack");
        player.DamagePlayer(MeleeDamage, true, false, CauseOfDeath.Bludgeoning, 0, false, default);
        if (player.isPlayerDead)
        {
            Plugin.ExtendedLogging("Player is dead");
            SetTargetServerRpc(-1);
        }
    }

    public void OnCollideSpecialAttack(PlayerControllerB player)
    {
        HandleAttack(new List<PlayerControllerB> { player }, default, SpecialMeleeDamage);
    }

    public void OnCollideSpecialAOEAttackAnimEvent()
    {
        List<PlayerControllerB> playersToHit = new List<PlayerControllerB>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, 5, LayerMask.GetMask("Player"));
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out PlayerControllerB player))
            {
                playersToHit.Add(player);
            }
        }
        // do a spherecast, pass it onto handleattack
        HandleAttack(playersToHit, SpecialAttackPushForce, SpecialAOEDamage);
    }

    private void HandleAttack(List<PlayerControllerB> playersHit, float force, int damage)
    {
        if (timeSinceHittingLocalPlayer > 0f || isEnemyDead || currentBehaviourStateIndex != (int)State.ChasingPlayer) return;
        foreach (PlayerControllerB player in playersHit)
        {
            if (player == GameNetworkManager.Instance.localPlayerController) timeSinceHittingLocalPlayer = 1.5f;
            Plugin.ExtendedLogging("Hitting player with special melee attack");
            Vector3 direction = (player.transform.position - transform.position).normalized;
            player.externalForces += direction * force;
            player.DamagePlayer(damage, true, false, CauseOfDeath.Bludgeoning, 0, false, direction * force);
            if (player.isPlayerDead)
            {
                Plugin.ExtendedLogging("Player is dead");
                SetTargetServerRpc(-1);
            }
        }
    }

    #region Animation Events
    public void PlayFootstepSound()
    {
        creatureSFX.PlayOneShot(FootstepSounds[enemyRandom.Next(0, FootstepSounds.Length)]);
    } // Animation Event

    public void PlayFastFootstepSound()
    {
        creatureSFX.PlayOneShot(FastFootstepSounds[enemyRandom.Next(0, FastFootstepSounds.Length)]);
    } // Animation Event
    #endregion
}