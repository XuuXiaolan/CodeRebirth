using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class SeamineGalAI : GalAI
{
    public List<AnimationClip> JojoAnimations = new();
    public InteractTrigger hugInteractTrigger = null!;
    public GameObject flashLightLight = null!;
    public InteractTrigger flashLightInteractTrigger = null!;
    public AudioSource RidingBruceSource = null!;
    public AudioClip explosionSound = null!;
    public AudioClip hazardPingSound = null!;
    public AudioClip rechargeChargesSound = null!;
    public AudioClip spotEnemySound = null!;
    public AudioClip hugSound = null!;
    public List<AudioClip> bruceSwimmingAudioClips = new();
    public List<AudioClip> startOrEndRidingBruceAudioClips = new();
    public AudioClip squeezeFishSound = null!;

    private float hazardRevealTimer = 10f;
    private bool inHugAnimation = false;
    private bool huggingOwner = false;
    [NonSerialized] public SeamineCharger SeamineCharger = null!;
    private bool ridingBruce = false;
    private State galState = State.Inactive;
    private bool jojoPosing = false;
    private readonly static int chargeCountInt = Animator.StringToHash("chargeCount"); // int
    private readonly static int revealHazardsAnimation = Animator.StringToHash("revealHazards"); // trigger
    private readonly static int hugAnimation = Animator.StringToHash("doHug"); // trigger
    private readonly static int jojoAnimationInt = Animator.StringToHash("jojoPoseInt"); // should be an int to choose specific anim
    private readonly static int attackModeAnimation = Animator.StringToHash("attackMode"); // bool
    private readonly static int ridingBruceAnimation = Animator.StringToHash("ridingBruce"); // bool
    private readonly static int startExplodeAnimation = Animator.StringToHash("doExplode"); // trigger
    private readonly static int danceAnimation = Animator.StringToHash("dancing"); // bool
    private readonly static int activatedAnimation = Animator.StringToHash("activated"); // bool
    private readonly static int runSpeedFloat = Animator.StringToHash("RunSpeed"); // float

    public enum State
    {
        Inactive = 0,
        Active = 1,
        FollowingPlayer = 2,
        Dancing = 3,
        AttackMode = 4,
    }

    private void StartUpDelay()
    {
        SeamineCharger[] seamineChargers = FindObjectsByType<SeamineCharger>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
        if (seamineChargers.Length <= 0)
        {
            if (IsServer) NetworkObject.Despawn();
            Plugin.Logger.LogError($"SeamineCharger not found in scene. SeamineGalAI will not be functional.");
            return;
        }
        SeamineCharger seamineCharger = seamineChargers.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).First();;
        seamineCharger.GalAI = this;
        this.SeamineCharger = seamineCharger;
        transform.position = seamineCharger.ChargeTransform.position;
        transform.rotation = seamineCharger.ChargeTransform.rotation;
        // Automatic activation if configured
        if (Plugin.ModConfig.ConfigSeamineTinkAutomatic.Value)
        {
            StartCoroutine(SeamineCharger.ActivateGalAfterLand());
        }

        // Adding listener for interaction trigger
        hugInteractTrigger.onInteract.AddListener(OnHugInteract);
        flashLightInteractTrigger.onInteract.AddListener(OnFlashLightInteract);
        SeamineCharger.ActivateOrDeactivateTrigger.onInteract.AddListener(SeamineCharger.OnActivateGal);

        StartCoroutine(CheckForNearbyEnemiesToOwner());
        StartCoroutine(UpdateRidingBruceSound());
    }

    private IEnumerator UpdateRidingBruceSound()
    {
        while (true)
        {
            yield return new WaitUntil(() => !RidingBruceSource.isPlaying);
            RidingBruceSource.clip = bruceSwimmingAudioClips[UnityEngine.Random.Range(0, bruceSwimmingAudioClips.Count)];
            RidingBruceSource.Play();
        }
    }

    private void OnFlashLightInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        StartFlashLightInteractServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartFlashLightInteractServerRpc()
    {
        StartFlashLightInteractClientRpc();
    }

    [ClientRpc]
    private void StartFlashLightInteractClientRpc()
    {
        GalSFX.PlayOneShot(squeezeFishSound);
        flashLightLight.SetActive(!flashLightLight.activeSelf);
    }

    private void OnHugInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        StartHugInteractServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartHugInteractServerRpc()
    {
        StartHugInteractClientRpc();
    }

    [ClientRpc]
    private void StartHugInteractClientRpc()
    {
        if (ownerPlayer == null) return;
        if (physicsEnabled) EnablePhysics(false);
        ownerPlayer.enteringSpecialAnimation = true;
        ownerPlayer.disableMoveInput = true;
        ownerPlayer.disableLookInput = true;
        huggingOwner = true;
    }

    public override void ActivateGal(PlayerControllerB owner)
    {
        base.ActivateGal(owner);
        ResetToChargerStation(State.Active);
    }

    private void ResetToChargerStation(State state)
    {
        if (!IsServer) return;
        if (Agent.enabled) Agent.Warp(SeamineCharger.ChargeTransform.position);
        else transform.position = SeamineCharger.ChargeTransform.position;
        transform.rotation = SeamineCharger.ChargeTransform.rotation;
        HandleStateAnimationSpeedChanges(state);
    }

    public override void DeactivateGal()
    {
        base.DeactivateGal();
        ResetToChargerStation(State.Inactive);
    }

    private IEnumerator ResetSpeedBackToNormal()
    {
        jojoPosing = true;
        yield return new WaitForSeconds(0.9f);
        Animator.SetInteger(jojoAnimationInt, -1);
        jojoPosing = false;
    }

    private void InteractTriggersUpdate()
    {
        bool interactable = !inActive && (ownerPlayer != null && GameNetworkManager.Instance.localPlayerController == ownerPlayer);
        bool idleInteractable = galState != State.AttackMode && interactable;
        
        hugInteractTrigger.interactable = idleInteractable;
        flashLightInteractTrigger.interactable = interactable;
    }

    private void StoppingDistanceUpdate()
    {
        Agent.stoppingDistance = galState == State.AttackMode ? 6f : 3f * (huggingOwner ? 0.33f : 1f);
    }

    private void SetIdleDefaultStateForEveryone()
    {
        if (SeamineCharger == null)
        {
            Plugin.Logger.LogInfo("Syncing for client");
            galRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            chargeCount = Plugin.ModConfig.ConfigSeamineTinkCharges.Value;
            Animator.SetInteger(chargeCountInt, chargeCount);
            maxChargeCount = chargeCount;
            Agent.enabled = false;
            foreach (string enemy in Plugin.ModConfig.ConfigSeamineTinkEnemyBlacklist.Value.Split(','))
            {
                enemyTargetBlacklist.Add(enemy.Trim());
            }
            StartUpDelay();
        }
    }

    public override void InActiveUpdate()
    {
        base.InActiveUpdate();
        inActive = galState == State.Inactive;
    }

    public override void Update()
    {
        base.Update();
        SetIdleDefaultStateForEveryone();
        InteractTriggersUpdate();
        if (inActive && SeamineCharger != null)
        {
            this.transform.position = SeamineCharger.transform.position;
            this.transform.rotation = SeamineCharger.transform.rotation;
            return;
        }
        if (ownerPlayer != null && ownerPlayer.isPlayerDead) ownerPlayer = null;
        StoppingDistanceUpdate();

        if (!IsHost) return;
        HostSideUpdate();
    }

    private float GetCurrentSpeedMultiplier()
    {
        float speedMultiplier = 1f * (galState == State.FollowingPlayer ? 2f : 1f) * (galState == State.AttackMode ? 4f : 1f) * (jojoPosing || inHugAnimation ? 0f : 1f);
        if (inHugAnimation && Vector3.Distance(transform.position, Agent.pathEndPosition) <= Agent.stoppingDistance) Agent.velocity = Vector3.zero;
        return speedMultiplier;
    }

    private void HostSideUpdate()
    {
        if (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.inShipPhase)
        {
            SeamineCharger.ActivateGirlServerRpc(-1);
            return;
        }
        if (Agent.enabled) smartAgentNavigator.AdjustSpeedBasedOnDistance(GetCurrentSpeedMultiplier());
        Animator.SetFloat(runSpeedFloat, Agent.velocity.magnitude / 2);
        switch (galState)
        {
            case State.Inactive:
                break;
            case State.Active:
                DoActive();
                break;
            case State.FollowingPlayer:
                DoFollowingPlayer();
                break;
            case State.Dancing:
                DoDancing();
                break;
            case State.AttackMode:
                DoAttackMode();
                break;
        }
    }

    public override void OnEnableOrDisableAgent(bool agentEnabled)
    {
        base.OnEnableOrDisableAgent(agentEnabled);
        Animator.SetBool(ridingBruceAnimation, !agentEnabled);
    }

    private void DoActive()
    {
        if (ownerPlayer == null)
        {
            GoToChargerAndDeactivate();
            return;
        }
        if (Vector3.Distance(transform.position, ownerPlayer.transform.position) > 3f)
        {
            HandleStateAnimationSpeedChanges(State.FollowingPlayer);
        }
    }

    private void DoFollowingPlayer()
    {
        if (ownerPlayer == null)
        {
            GoToChargerAndDeactivate();
            return;
        }

        if (smartAgentNavigator.DoPathingToDestination(ownerPlayer.transform.position, ownerPlayer.isInsideFactory, true, ownerPlayer))
        {
            return;
        }

        DoStaringAtOwner(ownerPlayer);
        DoRevealingHazards();

        if (DoHuggingOwner(ownerPlayer))
        {
            return;
        }

        if (DoDancingAction())
        {
            return;
        }

        if (!jojoPosing && UnityEngine.Random.Range(0f, 25000f) <= 10f && Agent.velocity.sqrMagnitude <= 0.01f && Vector3.Distance(Agent.transform.position, ownerPlayer.transform.position) <= 5f)
        {
            DoJojoPoselol();
            return;
        }
    }

    private void DoDancing()
    {
    }

    private void DoAttackMode()
    {
        if (targetEnemy == null || targetEnemy.isEnemyDead || chargeCount <= 0 || ownerPlayer == null)
        {
            if (targetEnemy != null && targetEnemy.isEnemyDead) SetEnemyTargetServerRpc(-1);
            if (ownerPlayer != null)
            {
                HandleStateAnimationSpeedChanges(State.FollowingPlayer);
            }
            else
            {
                GoToChargerAndDeactivate();
            }
            return;
        }
        if (!currentlyAttacking)
        {
            smartAgentNavigator.DoPathingToDestination(targetEnemy.transform.position, !targetEnemy.isOutside, true, ownerPlayer);
        }
        float distanceToTarget = Vector3.Distance(transform.position, targetEnemy.transform.position);
        if (distanceToTarget <= Agent.stoppingDistance + 4 && !currentlyAttacking)
        {
            Vector3 targetPosition = targetEnemy.transform.position;
            Vector3 direction = (targetPosition - this.transform.position).normalized;
            direction.y = 0; // Keep the y component zero to prevent vertical rotation

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
            if (distanceToTarget <= Agent.stoppingDistance)
            {
                currentlyAttacking = true;
                NetworkAnimator.SetTrigger(startExplodeAnimation);
            }
        }
    }

    private void DoJojoPoselol()
    {
        int jojoAnimationNumber = UnityEngine.Random.Range(0, JojoAnimations.Count);
        Animator.SetInteger(jojoAnimationInt, jojoAnimationNumber);
        StartCoroutine(ResetSpeedBackToNormal());
    }

    private void DoRevealingHazards()
    {
        hazardRevealTimer -= Time.deltaTime;
        if (hazardRevealTimer <= 0)
        {
            NetworkAnimator.SetTrigger(revealHazardsAnimation);
            hazardRevealTimer = UnityEngine.Random.Range(7.5f, 12.5f);
        }
    }

    private void DoHazardActionsAnimEvent()
    {
        // plays the visual effect from gabriel
        GalVoice.PlayOneShot(hazardPingSound);
    }

    private bool DoDancingAction()
    {
        if (boomboxPlaying)
        {
            HandleStateAnimationSpeedChanges(State.Dancing);
            StartCoroutine(StopDancingDelay());
            return true;
        }
        return false;
    }

    private bool DoHuggingOwner(PlayerControllerB ownerPlayer)
    {
        if (!huggingOwner) return false;
        if (Vector3.Distance(transform.position, ownerPlayer.transform.position) <= Agent.stoppingDistance && Agent.enabled && !inHugAnimation)
        {
            NetworkAnimator.SetTrigger(hugAnimation);
            DoHugSoundClientRpc();
            inHugAnimation = true;
        }
        return true;
    }

    [ClientRpc]
    private void DoHugSoundClientRpc()
    {
        GalVoice.PlayOneShot(hugSound);
    }

    public void EndHugAnimEvent()
    {
        inHugAnimation = false;
        huggingOwner = false;
        if (ownerPlayer == null) return;
        ownerPlayer.enteringSpecialAnimation = false;
        ownerPlayer.disableMoveInput = false;
        ownerPlayer.disableLookInput = false;
    }

    private IEnumerator StopDancingDelay()
    {
        yield return new WaitUntil(() => !boomboxPlaying || galState != State.Dancing);
        if (galState != State.Dancing) yield break;  
        HandleStateAnimationSpeedChanges(State.FollowingPlayer);
    }

    private IEnumerator CheckForNearbyEnemiesToOwner()
    {
        if (!IsServer) yield break;

        var delay = new WaitForSeconds(1f);
        while (true)
        {
            yield return delay;

            if (galState != State.FollowingPlayer || ownerPlayer == null || !Agent.enabled || chargeCount <= 0 || !smartAgentNavigator.isOutside && !ownerPlayer.isInsideFactory || smartAgentNavigator.isOutside && ownerPlayer.isInsideFactory) continue;

            // Use OverlapSphereNonAlloc to reduce garbage collection
            Collider[] hitColliders = new Collider[20];  // Size accordingly to expected max enemies
            int numHits = Physics.OverlapSphereNonAlloc(ownerPlayer.gameplayCamera.transform.position, 15, hitColliders, LayerMask.GetMask("Enemies"), QueryTriggerInteraction.Collide);

            for (int i = 0; i < numHits; i++)
            {
                Collider collider = hitColliders[i];
                if (!collider.gameObject.activeSelf) continue;

                if (collider.TryGetComponent(out EnemyAICollisionDetect enemyCollisionDetect))
                {
                    EnemyAI enemy = enemyCollisionDetect.mainScript;

                    if (enemy == null || enemy.isEnemyDead || enemyTargetBlacklist.Contains(enemy.enemyType.enemyName))
                        continue;

                    // First, do a simple direction check to see if the enemy is in front of the player
                    Vector3 directionToEnemy = (collider.transform.position - ownerPlayer.gameplayCamera.transform.position).normalized;
                    // Then check if there's a clear line of sight
                    if (!Physics.Raycast(ownerPlayer.gameplayCamera.transform.position, directionToEnemy, out RaycastHit hit, 15, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("Enemies"), QueryTriggerInteraction.Ignore))
                    {
                        //Plugin.ExtendedLogging("Missed Hit: " + hit.collider.name);
                        continue;
                    }
                    Plugin.ExtendedLogging("Correct Hit: " + hit.collider.name);


                    SetEnemyTargetServerRpc(RoundManager.Instance.SpawnedEnemies.IndexOf(enemy));
                    HandleStateAnimationSpeedChanges(State.AttackMode);
                    break;  // Exit loop after targeting one enemy, depending on game logic
                }
            }
        }
    }

    private void CheckIfEnemyIsHitAnimEvent()
    {
        GalSFX.PlayOneShot(explosionSound);
        List<EnemyAI> enemiesToKill = new();
        Collider[] colliders = Physics.OverlapSphere(transform.position, 15, LayerMask.GetMask("Enemies"), QueryTriggerInteraction.Collide);

        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out EnemyAICollisionDetect enemyCollisionDetect))
            {
                EnemyAI enemyDetected = enemyCollisionDetect.mainScript;
                if (enemyDetected != null && !enemyDetected.isEnemyDead)
                {
                    // Ensure there's a line of sight from SeamineGalAI to the enemy
                    Vector3 directionToEnemy = (enemyDetected.transform.position - transform.position).normalized;
                    float distanceToEnemy = Vector3.Distance(transform.position, enemyDetected.transform.position);
                    if (Physics.Raycast(transform.position, directionToEnemy, out RaycastHit hit, distanceToEnemy, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                    {
                        Plugin.ExtendedLogging("No line of sight to enemy: " + enemyDetected);
                        continue;
                    }
                    Plugin.ExtendedLogging("Enemy hit: " + enemyDetected);
                    enemiesToKill.Add(enemyDetected);
                }
            }
            else
            {
                Plugin.ExtendedLogging("Thing hit: " + collider.name);
            }
        }

        foreach (EnemyAI enemy in enemiesToKill)
        {
            if (enemy == null || enemy.isEnemyDead || !enemy.IsOwner)
                continue;

            enemy.KillEnemyOnOwnerClient(true);
        }
    }

    private void EndAttackAnimEvent()
    {
        currentlyAttacking = false;
        chargeCount--;
        Animator.SetInteger(chargeCountInt, chargeCount);
    }

    private void PlayFootstepSoundAnimEvent()
    {
        GalSFX.PlayOneShot(FootstepSounds[galRandom.NextInt(0, FootstepSounds.Length - 1)]);
    }

    private void StartRidingBruceAnimEvent()
    {
        SetRidingBruce(true);
        StartCoroutine(FlyAnimationDelay());
    }

    private IEnumerator FlyAnimationDelay()
    {
        smartAgentNavigator.cantMove = true;
        yield return new WaitForSeconds(1f);
        smartAgentNavigator.cantMove = false;
    }

    private void StopRidingBruceAnimEvent()
    {
        SetRidingBruce(false);
    }

    private void SetRidingBruce(bool RidingBruce)
    {
        this.ridingBruce = RidingBruce;
        if (RidingBruce)
        {
            GalSFX.PlayOneShot(startOrEndRidingBruceAudioClips[galRandom.NextInt(0, startOrEndRidingBruceAudioClips.Count - 1)]);
            RidingBruceSource.volume = Plugin.ModConfig.ConfigSeamineTinkRidingBruceVolume.Value;
        }
        else RidingBruceSource.volume = 0f;
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleStateAnimationSpeedChangesServerRpc(int state)
    {
        HandleStateAnimationSpeedChanges((State)state);
    }

    private void HandleStateAnimationSpeedChanges(State state) // This is for host
    {
        SwitchStateClientRpc((int)state);
        switch (state)
        {
            case State.Inactive:
                SetAnimatorBools(attackMode: false, dance: false, activated: false);
                break;
            case State.Active:
                SetAnimatorBools(attackMode: false, dance: false, activated: true);
                break;
            case State.FollowingPlayer:
                SetAnimatorBools(attackMode: false, dance: false, activated: true);
                break;
            case State.Dancing:
                SetAnimatorBools(attackMode: false, dance: true, activated: true);
                break;
            case State.AttackMode:
                SetAnimatorBools(attackMode: true, dance: false, activated: true);
                break;
        }
    }

    private void SetAnimatorBools(bool attackMode, bool dance, bool activated)
    {
        Animator.SetBool(attackModeAnimation, attackMode);
        Animator.SetBool(danceAnimation, dance);
        Animator.SetBool(activatedAnimation, activated);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchStateServerRpc(int state)
    {
        SwitchStateClientRpc(state);
    }

    [ClientRpc]
    private void SwitchStateClientRpc(int state)
    {
        SwitchState(state);
    }

    private void SwitchState(int state) // this is for everyone.
    {
        State stateToSwitchTo = (State)state;
        if (state != -1)
        {
            switch (stateToSwitchTo)
            {
                case State.Inactive:
                    HandleStateInactiveChange();
                    break;
                case State.Active:
                    HandleStateActiveChange();
                    break;
                case State.FollowingPlayer:
                    HandleStateFollowingPlayerChange();
                    break;
                case State.Dancing:
                    HandleStateDancingChange();
                    break;
                case State.AttackMode:
                    HandleStateAttackModeChange();
                    break;
            }
            galState = stateToSwitchTo;
        }
    }

    #region State Changes
    private void HandleStateInactiveChange()
    {
        ownerPlayer = null;
        Agent.enabled = false;
    }

    private void HandleStateActiveChange()
    {
        Agent.enabled = true;
    }

    private void HandleStateFollowingPlayerChange()
    {
        GalVoice.PlayOneShot(GreetOwnerSound);
    }

    private void HandleStateDancingChange()
    {
    }

    private void HandleStateAttackModeChange()
    {
        GalVoice.PlayOneShot(spotEnemySound);
    }
    #endregion

    public override void OnUseEntranceTeleport(bool setOutside)
    {
        base.OnUseEntranceTeleport(setOutside);
    }
}