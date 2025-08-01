using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.MiscScripts.CustomPasses;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using CodeRebirthLib.ContentManagement.Enemies;
using CodeRebirthLib.ContentManagement.Unlockables;
using CodeRebirthLib.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class SeamineGalAI : GalAI
{
    public TerrainScanner terrainScanner = null!;
    public List<AnimationClip> JojoAnimations = new();
    public InteractTrigger hugInteractTrigger = null!;
    public InteractTrigger beltInteractTrigger = null!;
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
    public Pickable pickable = null!;
    public Light light = null!;

    private Collider[] cachedColliders = new Collider[10];
    private bool physicsTemporarilyDisabled = false;
    private List<Coroutine> customPassRoutines = new();
    private float hazardRevealTimer = 10f;
    private bool inHugAnimation = false;
    private bool huggingOwner = false;
    private bool ridingBruce = false;
    private State galState = State.Inactive;
    private bool jojoPosing = false;
    private readonly static int inElevatorAnimation = Animator.StringToHash("inElevator"); // bool
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
        List<SeamineCharger> seamineChargers = new();
        foreach (var charger in Charger.Instances)
        {
            if (charger is SeamineCharger seamineChargers1)
            {
                seamineChargers.Add(seamineChargers1);
            }
        }
        if (seamineChargers.Count <= 0)
        {
            if (IsServer) NetworkObject.Despawn();
            Plugin.Logger.LogError($"SeamineCharger not found in scene. SeamineGalAI will not be functional.");
            return;
        }
        SeamineCharger seamineCharger = seamineChargers.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).First(); ;
        seamineCharger.GalAI = this;
        GalCharger = seamineCharger;
        // Automatic activation if configured
        if (Plugin.ModConfig.ConfigSeamineTinkAutomatic.Value)
        {
            StartCoroutine(GalCharger.ActivateGalAfterLand());
        }

        // Adding listener for interaction trigger
        hugInteractTrigger.onInteract.AddListener(OnHugInteract);
        flashLightInteractTrigger.onInteract.AddListener(OnFlashLightInteract);
        GalCharger.ActivateOrDeactivateTrigger.onInteract.AddListener(GalCharger.OnActivateGal);
        pickable.IsLocked = false;
        StartCoroutine(CheckForNearbyEnemiesToOwner());
        StartCoroutine(UpdateRidingBruceSound());
    }

    public void OnBeltInteract()
    {
        if (GameNetworkManager.Instance.localPlayerController != ownerPlayer) return;
        if (ownerPlayer.currentlyHeldObjectServer == null || ownerPlayer.currentlyHeldObjectServer.itemProperties.itemName != "Key") return;
        ownerPlayer.DespawnHeldObject();
        StartBeltInteractServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartBeltInteractServerRpc()
    {
        PlayChargeSoundClientRpc();
        RefillChargesClientRpc();
        Animator.SetInteger(chargeCountInt, chargeCount);
    }

    [ClientRpc]
    private void PlayChargeSoundClientRpc()
    {
        GalSFX.PlayOneShot(rechargeChargesSound);
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
        if (!playerInteracting.IsLocalPlayer() || playerInteracting != ownerPlayer) return;
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
        CullFactorySoftCompat.TryRefreshDynamicLight(light);
    }

    private void OnHugInteract(PlayerControllerB playerInteracting)
    {
        if (!playerInteracting.IsLocalPlayer() || playerInteracting != ownerPlayer) return;
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
        if (physicsEnabled)
        {
            physicsTemporarilyDisabled = true;
            EnablePhysics(false);
        }
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
        if (Agent.enabled) Agent.Warp(GalCharger.ChargeTransform.position);
        else transform.position = GalCharger.ChargeTransform.position;
        transform.rotation = GalCharger.ChargeTransform.rotation;
        HandleStateAnimationSpeedChangesServerRpc((int)state);
    }

    public override void DeactivateGal()
    {
        base.DeactivateGal();
        ResetToChargerStation(State.Inactive);
    }

    private IEnumerator ResetSpeedBackToNormal()
    {
        jojoPosing = true;
        yield return new WaitForSeconds(2f);
        Animator.SetInteger(jojoAnimationInt, -1);
        jojoPosing = false;
    }

    private void InteractTriggersUpdate()
    {
        bool interactable = !inActive && (ownerPlayer != null && GameNetworkManager.Instance.localPlayerController == ownerPlayer);
        bool idleInteractable = galState != State.AttackMode && interactable;

        beltInteractTrigger.interactable = idleInteractable && chargeCount <= 0;
        pickable.IsLocked = beltInteractTrigger.interactable;
        hugInteractTrigger.interactable = idleInteractable;
        flashLightInteractTrigger.interactable = interactable;
    }

    private void StoppingDistanceUpdate()
    {
        Agent.stoppingDistance = galState == State.AttackMode ? 2.5f : 3f * (huggingOwner ? 0.33f : 1f);
    }

    private void SetIdleDefaultStateForEveryone()
    {
        if (GalCharger == null || (IsServer && !doneOnce))
        {
            doneOnce = true;
            Plugin.Logger.LogInfo("Syncing for client");
            galRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            chargeCount = Plugin.ModConfig.ConfigSeamineTinkCharges.Value;
            Animator.SetInteger(chargeCountInt, chargeCount);
            maxChargeCount = chargeCount;
            Agent.enabled = false;

            if (Plugin.Mod.UnlockableRegistry().TryGetFromUnlockableName("SEA", out CRUnlockableDefinition? seamineUnlockableDefinition))
            {
                var enemyBlacklist = seamineUnlockableDefinition.GetGeneralConfig<string>("Seamine Tink | Enemy Blacklist").Value.Split(',').Select(s => s.Trim());
                foreach (var nameEntry in enemyBlacklist)
                {
                    enemyTargetBlacklist.UnionWith(VanillaEnemies.AllEnemyTypes.Where(et => et.enemyName.Equals(nameEntry, System.StringComparison.OrdinalIgnoreCase)).Select(et => et.enemyName));
                }
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

        if (inActive) return;
        StoppingDistanceUpdate();
        if (!IsHost) return;
        HostSideUpdate();
    }

    private float GetCurrentSpeedMultiplier()
    {
        float speedMultiplier = 1f * (galState == State.FollowingPlayer ? 2f : 1f) * (galState == State.AttackMode ? 4f : 1f) * (jojoPosing || inHugAnimation || currentlyAttacking ? 0f : 1f);
        if (inHugAnimation && Vector3.Distance(transform.position, Agent.pathEndPosition) <= Agent.stoppingDistance) Agent.velocity = Vector3.zero;
        return speedMultiplier;
    }

    private void HostSideUpdate()
    {
        if (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.inShipPhase)
        {
            GalCharger.ActivateGirlServerRpc(-1);
            return;
        }
        if (Agent.enabled)
            smartAgentNavigator.AdjustSpeedBasedOnDistance(0, 40, 0, 10, GetCurrentSpeedMultiplier());

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
        else
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

        if (smartAgentNavigator.DoPathingToDestination(ownerPlayer.transform.position))
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

        if (!jojoPosing && UnityEngine.Random.Range(0f, 2500f) <= 5f && Agent.velocity.sqrMagnitude <= 0.01f && Vector3.Distance(Agent.transform.position, ownerPlayer.transform.position) <= 5f)
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
            if (targetEnemy != null && targetEnemy.isEnemyDead) ClearEnemyTargetServerRpc();
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
            smartAgentNavigator.DoPathingToDestination(targetEnemy.transform.position);
            if (!smartAgentNavigator.CurrentPathIsValid() || (Vector3.Distance(this.transform.position, ownerPlayer.transform.position) > 15 && Plugin.ModConfig.ConfigDontTargetFarEnemies.Value))
            {
                HandleStateAnimationSpeedChanges(State.FollowingPlayer);
                return;
            }
        }
        float distanceToTarget = Vector3.Distance(transform.position, targetEnemy.transform.position);
        if (distanceToTarget <= (Agent.stoppingDistance + 4 + (targetEnemy is CentipedeAI || smartAgentNavigator.IsAgentOutside() ? 5 : 0)) && !currentlyAttacking)
        {
            Vector3 targetPosition = targetEnemy.transform.position;
            Vector3 direction = (targetPosition - this.transform.position).normalized;
            direction.y = 0; // Keep the y component zero to prevent vertical rotation

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
            if (distanceToTarget <= (Agent.stoppingDistance + (targetEnemy is CentipedeAI || smartAgentNavigator.IsAgentOutside() ? 5 : 0)))
            {
                currentlyAttacking = true;
                NetworkAnimator.SetTrigger(startExplodeAnimation);
                if (ridingBruce)
                {
                    SetRidingBruce(false);
                }
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
            hazardRevealTimer = UnityEngine.Random.Range(Plugin.ModConfig.ConfigSeamineScanFrequency.Value - 5, Plugin.ModConfig.ConfigSeamineScanFrequency.Value + 5);
        }
    }

    private void DoHazardActionsAnimEvent()
    {
        // plays the visual effect from gabriel
        GalVoice.PlayOneShot(hazardPingSound);
        if (Plugin.ModConfig.ConfigOnlyOwnerSeesScanEffects.Value && GameNetworkManager.Instance.localPlayerController != ownerPlayer) return;
        ParticleSystem particleSystem = DoTerrainScan(terrainScanner, transform.position);
        particleSystem.gameObject.transform.parent.gameObject.SetActive(true);
        if (customPassRoutines.Count <= 0)
        {
            customPassRoutines.Add(StartCoroutine(DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughEnemies, Plugin.ModConfig.ConfigSeamineScanRange.Value)));
            customPassRoutines.Add(StartCoroutine(DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughHazards, Plugin.ModConfig.ConfigSeamineScanRange.Value)));
        }
        else
        {
            foreach (Coroutine coroutine in customPassRoutines)
            {
                StopCoroutine(coroutine);
            }
            customPassRoutines.Add(StartCoroutine(DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughEnemies, Plugin.ModConfig.ConfigSeamineScanRange.Value)));
            customPassRoutines.Add(StartCoroutine(DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughHazards, Plugin.ModConfig.ConfigSeamineScanRange.Value)));
        }
    }

    public static ParticleSystem DoTerrainScan(TerrainScanner _terrainScanner, Vector3 position)
    {
        //if (GameNetworkManager.Instance.localPlayerController != ownerPlayer || Vector3.Distance(transform.position, ownerPlayer.transform.position) > 10) return;
        return _terrainScanner.SpawnTerrainScanner(position);
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
        if (ownerPlayer == null)
            return;

        ownerPlayer.enteringSpecialAnimation = true;
        ownerPlayer.disableMoveInput = true;
        StartCoroutine(WaitUntilStopped());
    }

    private IEnumerator WaitUntilStopped()
    {
        yield return new WaitUntil(() => Agent.velocity == Vector3.zero);
        yield return new WaitForSeconds(3.2f);
        GalVoice.PlayOneShot(hugSound);
    }

    public void EndHugAnimEvent()
    {
        inHugAnimation = false;
        huggingOwner = false;
        EnablePhysics(!physicsTemporarilyDisabled);
        physicsTemporarilyDisabled = false;
        if (ownerPlayer == null) return;
        ownerPlayer.enteringSpecialAnimation = false;
        ownerPlayer.disableMoveInput = false;
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

            if (galState != State.FollowingPlayer || ownerPlayer == null || !Agent.enabled || chargeCount <= 0 || (!smartAgentNavigator.IsAgentOutside() && !ownerPlayer.isInsideFactory) || (smartAgentNavigator.IsAgentOutside() && ownerPlayer.isInsideFactory))
                continue;

            int numHits = Physics.OverlapSphereNonAlloc(ownerPlayer.gameplayCamera.transform.position, 15, cachedColliders, MoreLayerMasks.EnemiesMask, QueryTriggerInteraction.Collide);
            Plugin.ExtendedLogging($"Found {numHits} enemies");
            for (int i = 0; i < numHits; i++)
            {
                Collider collider = cachedColliders[i];
                if (!collider.gameObject.activeSelf)
                    continue;

                if (!collider.TryGetComponent(out EnemyAICollisionDetect enemyCollisionDetect))
                    continue;

                EnemyAI enemy = enemyCollisionDetect.mainScript;

                if (enemy == null || enemy.isEnemyDead || enemyTargetBlacklist.Contains(enemy.enemyType.enemyName) || enemy is Transporter)
                    continue;

                // First, do a simple direction check to see if the enemy is in front of the player
                if (Physics.Linecast(ownerPlayer.gameplayCamera.transform.position, collider.transform.position, out RaycastHit hit, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
                {
                    Plugin.ExtendedLogging("Missed Hit: " + hit.collider.name);
                    continue;
                }
                //Plugin.ExtendedLogging("Correct Hit: " + hit.collider.name);


                SetEnemyTargetServerRpc(new NetworkBehaviourReference(enemy));
                HandleStateAnimationSpeedChanges(State.AttackMode);
                break;
            }
        }
    }

    private void CheckIfEnemyIsHitAnimEvent()
    {
        GalSFX.PlayOneShot(explosionSound);
        CRUtilities.CreateExplosion(beltInteractTrigger.gameObject.transform.position, true, 10, 0, 6, 1, null, null, 5f);
        List<EnemyAI> enemiesToKill = new();
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, 15, cachedColliders, MoreLayerMasks.EnemiesMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < numHits; i++)
        {
            if (cachedColliders[i].TryGetComponent(out EnemyAICollisionDetect enemyCollisionDetect))
            {
                EnemyAI enemyDetected = enemyCollisionDetect.mainScript;
                if (enemyDetected != null && !enemyDetected.isEnemyDead)
                {
                    // Ensure there's a line of sight from SeamineGalAI to the enemy
                    Plugin.ExtendedLogging("Enemy hit: " + enemyDetected);
                    if (enemiesToKill.Contains(enemyDetected)) continue;
                    enemiesToKill.Add(enemyDetected);
                }
            }
            else
            {
                Plugin.ExtendedLogging("Thing hit: " + cachedColliders[i].name);
            }
        }

        foreach (EnemyAI enemy in enemiesToKill)
        {
            if (enemy == null || enemy.isEnemyDead || !enemy.IsOwner)
                continue;

            enemy.HitEnemyOnLocalClient(999, this.transform.position, ownerPlayer, true, -1);
            if (enemy.enemyType.canDie && !enemy.enemyType.destroyOnDeath)
            {
                enemy.KillEnemyOnOwnerClient(false);
            }
            else
            {
                enemy.KillEnemyOnOwnerClient(true);
            }
        }
    }

    private void EndAttackAnimEvent()
    {
        currentlyAttacking = false;
        chargeCount--;
        Animator.SetInteger(chargeCountInt, chargeCount);
    }

    public override void RefillCharges()
    {
        base.RefillCharges();
        Animator.SetInteger(chargeCountInt, chargeCount);
    }

    private void PlayFootstepSoundAnimEvent()
    {
        GalSFX.PlayOneShot(FootstepSounds[galRandom.Next(FootstepSounds.Length)]);
    }

    private void StartRidingBruceAnimEvent()
    {
        SetRidingBruce(true);
        StartCoroutine(FlyAnimationDelay());
    }

    private IEnumerator FlyAnimationDelay()
    {
        smartAgentNavigator.cantMove = true;
        yield return new WaitForSeconds(1.5f);
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
            GalSFX.PlayOneShot(startOrEndRidingBruceAudioClips[galRandom.Next(startOrEndRidingBruceAudioClips.Count)]);
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
        if (inHugAnimation)
        {
            inHugAnimation = false;
            huggingOwner = false;
            EnablePhysics(!physicsTemporarilyDisabled);
            physicsTemporarilyDisabled = false;
            if (ownerPlayer != null)
            {
                ownerPlayer.enteringSpecialAnimation = false;
                ownerPlayer.disableMoveInput = false;
            }
        }

        ownerPlayer = null;
        Agent.enabled = false;
        GalSFX.PlayOneShot(squeezeFishSound);
        flashLightLight.SetActive(false);
        Animator.SetBool(inElevatorAnimation, false);
        Animator.SetBool(ridingBruceAnimation, false);
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
        CullFactorySoftCompat.TryRefreshDynamicLight(light);
    }

    public override void OnEnterOrExitElevator(bool enteredElevator)
    {
        base.OnEnterOrExitElevator(enteredElevator);
        Animator.SetBool(inElevatorAnimation, enteredElevator);
    }

    public static IEnumerator DoCustomPassThing(ParticleSystem particleSystem, CustomPassManager.CustomPassType customPassType, float scanRange)
    {
        if (CustomPassManager.Instance.EnableCustomPass(customPassType, true) is not SeeThroughCustomPass customPass) yield break;

        customPass.maxVisibilityDistance = 0f;

        yield return new WaitWhile(() =>
        {
            float percentLifetime = particleSystem.time / particleSystem.main.startLifetime.constant;
            customPass.maxVisibilityDistance = particleSystem.sizeOverLifetime.size.Evaluate(percentLifetime) * 300; // takes some odd seconds
            return customPass.maxVisibilityDistance < scanRange;
        });

        yield return new WaitForSeconds(5);

        yield return new WaitWhile(() =>
        {
            customPass.maxVisibilityDistance -= Time.deltaTime * scanRange / 3f; // takes 3s
            return customPass.maxVisibilityDistance > 0f;
        });
        CustomPassManager.Instance.RemoveCustomPass(customPassType);
    }
}