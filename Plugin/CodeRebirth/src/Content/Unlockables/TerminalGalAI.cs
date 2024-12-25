using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.MiscScripts.CustomPasses;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Unlockables;
public class TerminalGalAI : GalAI
{
    public SkinnedMeshRenderer FaceSkinnedMeshRenderer = null!;
    public TerrainScanner terrainScanner = null!;
    public InteractTrigger keyboardInteractTrigger = null!;
    public InteractTrigger zapperInteractTrigger = null!;
    public InteractTrigger keyInteractTrigger = null!;
    public InteractTrigger teleporterInteractTrigger = null!;
    public AudioSource FlyingSource = null!;
    public AudioClip scrapPingSound = null!;
    public List<AudioClip> flyingAudioClips = new();
    public List<AudioClip> startOrEndFlyingAudioClips = new();
    public TerminalFaceController terminalFaceController = null!;

    private List<Coroutine> customPassRoutines = new();
    private List<GameObject> pointsOfInterest = new();
    private float scrapRevealTimer = 10f;
    private bool flying = false;
    private Coroutine? unlockingSomething = null;
    private Coroutine? zapperRoutine = null;
    private State galState = State.Inactive;
    [HideInInspector] public Emotion galEmotion = Emotion.Sleeping;
    private readonly static int revealScrapAnimation = Animator.StringToHash("revealScrap"); // trigger
    private readonly static int flyingAnimation = Animator.StringToHash("flying"); // bool
    private readonly static int danceAnimation = Animator.StringToHash("dancing"); // bool
    private readonly static int activatedAnimation = Animator.StringToHash("activated"); // bool
    private readonly static int runSpeedFloat = Animator.StringToHash("RunSpeed"); // float
    private readonly static int specialAnimationInt = Animator.StringToHash("specialAnimationInt"); // int (-1 is nothing, 0 is DoorUnlock, 1 is RechargeItem, 2 is Teleport).

    public enum State
    {
        Inactive = 0,
        Active = 1,
        FollowingPlayer = 2,
        Dancing = 3,
        UnlockingObjects = 4,
    }

    public enum Emotion
    {
        Basis = -1,
        VeryHappy = 0,
        Mood = 1,
        Angy = 2,
        Winky = 3,
        Crying = 4,
        Sleeping = 5,
        Flustered = 6,
        Love = 7,
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        NetworkObject.TrySetParent(GalCharger.transform, false);
        ResetToChargerStation(galState, galEmotion);
    }

    private void StartUpDelay()
    {
        List<TerminalCharger> terminalChargers = new();
        foreach (var charger in Charger.Instances)
        {
            if (charger is TerminalCharger actuallyATerminalCharger)
            {
                terminalChargers.Add(actuallyATerminalCharger);
            }
        }
        if (terminalChargers.Count <= 0)
        {
            if (IsServer) NetworkObject.Despawn();
            Plugin.Logger.LogError($"TerminalCharger not found in scene. SeamineGalAI will not be functional.");
            return;
        }
        TerminalCharger terminalCharger = terminalChargers.OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).First();;
        terminalCharger.GalAI = this;
        GalCharger = terminalCharger;
        // Automatic activation if configured
        if (Plugin.ModConfig.ConfigSeamineTinkAutomatic.Value)
        {
            StartCoroutine(GalCharger.ActivateGalAfterLand());
        }

        // Adding listener for interaction trigger
        GalCharger.ActivateOrDeactivateTrigger.onInteract.AddListener(GalCharger.OnActivateGal);
        keyboardInteractTrigger.onInteract.AddListener(OnKeyboardInteract);
        keyInteractTrigger.onInteract.AddListener(OnKeyHandInteract);
        zapperInteractTrigger.onInteract.AddListener(OnZapperInteract);
        teleporterInteractTrigger.onInteract.AddListener(OnTeleporterInteract);
        StartCoroutine(UpdateFlyingSound());
    }

    private IEnumerator UpdateFlyingSound()
    {
        while (true)
        {
            yield return new WaitUntil(() => !FlyingSource.isPlaying);
            // FlyingSource.clip = flyingAudioClips[UnityEngine.Random.Range(0, flyingAudioClips.Count)];
            FlyingSource.Play();
        }
    }

    private void OnTeleporterInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        TeleporterInteractServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
    }

    [ServerRpc(RequireOwnership = false)]
    private void TeleporterInteractServerRpc(int playerIndex)
    {
        TeleporterInteractClientRpc(playerIndex);
    }

    [ClientRpc]
    private void TeleporterInteractClientRpc(int playerIndex)
    {
        ResetToChargerStation(galState, galEmotion);
        CRUtilities.TeleportPlayerToShip(playerIndex, GalCharger.transform.position);
    }

    private void OnZapperInteract(PlayerControllerB playerInteracting)
    {
        if (zapperRoutine != null || playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        ZapperInteractServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
    }

    [ServerRpc(RequireOwnership = false)]
    private void ZapperInteractServerRpc(int playerIndex)
    {
        ZapperInteractClientRpc(playerIndex);
    }

    [ClientRpc]
    private void ZapperInteractClientRpc(int playerIndex)
    {
        zapperRoutine = StartCoroutine(RechargePlayerHeldEquipment(StartOfRound.Instance.allPlayerScripts[playerIndex]));
    }

    private IEnumerator RechargePlayerHeldEquipment(PlayerControllerB playerToRecharge)
    {
        if (playerToRecharge.isPlayerDead || !playerToRecharge.isPlayerControlled || playerToRecharge.currentlyHeldObjectServer == null || !playerToRecharge.currentlyHeldObjectServer.itemProperties.requiresBattery)
        {
            if (playerToRecharge == GameNetworkManager.Instance.localPlayerController) HUDManager.Instance.DisplayTip("Error", "What you're holding cannot be charged", false);
            yield break;
        }
        else
        {
            bool usedToBeTwoHanded = playerToRecharge.currentlyHeldObjectServer.itemProperties.twoHanded;
            playerToRecharge.currentlyHeldObjectServer.itemProperties.twoHanded = true;
            while (playerToRecharge.currentlyHeldObjectServer.insertedBattery.charge < 1f)
            {
                playerToRecharge.currentlyHeldObjectServer.insertedBattery.charge += 1 * (Time.deltaTime / playerToRecharge.currentlyHeldObjectServer.itemProperties.batteryUsage);
            }
            playerToRecharge.currentlyHeldObjectServer.itemProperties.twoHanded = usedToBeTwoHanded;
        }
    }

    private void OnKeyboardInteract(PlayerControllerB playerInteracting)
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        KeyboardInteractServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void KeyboardInteractServerRpc()
    {
        KeyboardInteractClientRpc();
    }

    [ClientRpc]
    private void KeyboardInteractClientRpc()
    {
        EnablePhysics(!physicsEnabled);
    }

    private void OnKeyHandInteract(PlayerControllerB playerInteracting) // this doesn't entirely work.
    {
        if (playerInteracting != GameNetworkManager.Instance.localPlayerController || playerInteracting != ownerPlayer) return;
        KeyHandInteractServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void KeyHandInteractServerRpc()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 25, LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Collide);
        pointsOfInterest.Clear();
        pointsOfInterest = colliders
        .Select(collider => collider.gameObject)
        .Where(gameObject => {
            NavMesh.CalculatePath(transform.position, gameObject.transform.position, NavMesh.AllAreas, smartAgentNavigator.agent.path);
            if (DoCalculatePathDistance(smartAgentNavigator.agent.path) > 20) return false;
            else return true;
        })
        .OrderBy(it => DoCalculatePathDistance(smartAgentNavigator.agent.path)).ToList();
        HandleStateAnimationSpeedChanges(State.UnlockingObjects, Emotion.Basis);
    }

    public float DoCalculatePathDistance(NavMeshPath path)
    {
        float length = 0.0f;
      
        if (path.status != NavMeshPathStatus.PathInvalid && path.corners.Length >= 1)
        {
            for (int i = 1; i < path.corners.Length; i++)
            {
                length += Vector3.Distance(path.corners[i-1], path.corners[i]);
            }
        }
        Plugin.ExtendedLogging($"Path distance: {length}");
        return length;
    }

    public override void ActivateGal(PlayerControllerB owner)
    {
        base.ActivateGal(owner);
        ResetToChargerStation(State.Active, Emotion.Basis);
        if (GalCharger is TerminalCharger terminalCharger)
        {
            terminalCharger.animator.SetBool(TerminalCharger.isOpenedAnimation, true);
        }
    }

    private void ResetToChargerStation(State state, Emotion emotion)
    {
        if (!IsServer) return;
        if (Agent.enabled) Agent.Warp(GalCharger.ChargeTransform.position);
        else transform.position = GalCharger.ChargeTransform.position;
        transform.rotation = GalCharger.ChargeTransform.rotation;
        HandleStateAnimationSpeedChangesServerRpc((int)state, (int)emotion);
    }

    public override void DeactivateGal()
    {
        base.DeactivateGal();
        ResetToChargerStation(State.Inactive, Emotion.Sleeping);
        if (GalCharger is TerminalCharger terminalCharger)
        {
            terminalCharger.animator.SetBool(TerminalCharger.isOpenedAnimation, false);
        }
    }

    private void InteractTriggersUpdate()
    {
        bool interactable = !inActive && (ownerPlayer != null && GameNetworkManager.Instance.localPlayerController == ownerPlayer);
        bool idleInteractable = galState != State.UnlockingObjects && interactable;
        keyboardInteractTrigger.interactable = interactable;
        keyInteractTrigger.interactable = idleInteractable;
        zapperInteractTrigger.interactable = idleInteractable;
        teleporterInteractTrigger.interactable = idleInteractable;
    }

    private void StoppingDistanceUpdate()
    {
    }

    private void SetIdleDefaultStateForEveryone()
    {
        if (GalCharger == null || (IsServer && !doneOnce))
        {
            doneOnce = true;
            Plugin.Logger.LogInfo("Syncing for client");
            galRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 69);
            chargeCount = 0;
            maxChargeCount = chargeCount;
            Agent.enabled = false;
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
        return 1f;
    }

    private void HostSideUpdate()
    {
        if (StartOfRound.Instance.shipIsLeaving || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.inShipPhase)
        {
            GalCharger.ActivateGirlServerRpc(-1);
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
            case State.UnlockingObjects:
                DoUnlockingObjects();
                break;
        }
    }

    public override void OnEnableOrDisableAgent(bool agentEnabled)
    {
        base.OnEnableOrDisableAgent(agentEnabled);
        Animator.SetBool(flyingAnimation, !agentEnabled);
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
            HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.Basis);
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
        DoRevealingScrap();

        if (DoDancingAction())
        {
            return;
        }
    }

    private void DoDancing()
    {
    }

    private void DoUnlockingObjects()
    {
        if (pointsOfInterest.Count <= 0)
        {
            HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.Basis);
            return;
        }
    
        if (unlockingSomething != null) return;
        smartAgentNavigator.DoPathingToDestination(pointsOfInterest[0].transform.position, false, false, null);
        if (Agent.remainingDistance <= Agent.stoppingDistance)
        {
            unlockingSomething = StartCoroutine(DoUnlockingObjectsRoutine(pointsOfInterest[0]));
            pointsOfInterest.Remove(pointsOfInterest[0]);
        }
    }

    private IEnumerator DoUnlockingObjectsRoutine(GameObject pointOfInterest)
    {
        if (pointOfInterest.TryGetComponent(out Pickable pickable))
        {
            if (!pickable.IsLocked) yield break;
            // play animation.
            yield return new WaitForSeconds(3f);
            pickable.UnlockStuffClientRpc();
        }
        else if (pointOfInterest.TryGetComponent(out DoorLock doorLock))
        {
            // play animation.
            yield return new WaitForSeconds(3f);
            doorLock.UnlockDoorClientRpc();
        }
        pointsOfInterest.Remove(pointOfInterest);
        unlockingSomething = null;
    }

    private void DoRevealingScrap()
    {
        scrapRevealTimer -= Time.deltaTime;
        if (scrapRevealTimer <= 0)
        {
            NetworkAnimator.SetTrigger(revealScrapAnimation);
            scrapRevealTimer = UnityEngine.Random.Range(12.5f, 17.5f);
        }
    }

    private void DoScrapActionsAnimEvent()
    {
        // plays the visual effect from gabriel
        GalVoice.PlayOneShot(scrapPingSound);
        if (Plugin.ModConfig.ConfigOnlyOwnerSeesScanEffectsTerminalGal.Value && GameNetworkManager.Instance.localPlayerController != ownerPlayer) return;
        ParticleSystem particleSystem = DoTerrainScan();
        particleSystem.gameObject.transform.parent.gameObject.SetActive(true);
        if (customPassRoutines.Count <= 0)
        {
            customPassRoutines.Add(StartCoroutine(DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughItems)));
        }
        else
        {
            foreach (Coroutine coroutine in customPassRoutines)
            {
                StopCoroutine(coroutine);
            }
            customPassRoutines.Add(StartCoroutine(DoCustomPassThing(particleSystem, CustomPassManager.CustomPassType.SeeThroughItems)));
        }
    }

    private ParticleSystem DoTerrainScan()
    {
        //if (GameNetworkManager.Instance.localPlayerController != ownerPlayer || Vector3.Distance(transform.position, ownerPlayer.transform.position) > 10) return;
        return terrainScanner.SpawnTerrainScanner(transform.position);
    }

    private bool DoDancingAction()
    {
        if (boomboxPlaying)
        {
            HandleStateAnimationSpeedChanges(State.Dancing, Emotion.VeryHappy);
            StartCoroutine(StopDancingDelay());
            return true;
        }
        return false;
    }

    private IEnumerator StopDancingDelay()
    {
        yield return new WaitUntil(() => !boomboxPlaying || galState != State.Dancing);
        if (galState != State.Dancing) yield break;  
        HandleStateAnimationSpeedChanges(State.FollowingPlayer, Emotion.Basis);
    }

    private void PlayFootstepSoundAnimEvent()
    {
        GalSFX.PlayOneShot(FootstepSounds[galRandom.NextInt(0, FootstepSounds.Length - 1)]);
    }

    private void StartFlyingAnimEvent()
    {
        SetFlying(true);
        StartCoroutine(FlyAnimationDelay());
    }

    private IEnumerator FlyAnimationDelay()
    {
        smartAgentNavigator.cantMove = true;
        yield return new WaitForSeconds(1.5f);
        smartAgentNavigator.cantMove = false;
    }

    private void StopFlyingAnimEvent()
    {
        SetFlying(false);
    }

    private void SetFlying(bool Flying)
    {
        this.flying = Flying;
        if (Flying)
        {
            GalSFX.PlayOneShot(startOrEndFlyingAudioClips[galRandom.NextInt(0, startOrEndFlyingAudioClips.Count - 1)]);
            FlyingSource.volume = Plugin.ModConfig.ConfigTerminalBotFlyingVolume.Value;
        }
        else FlyingSource.volume = 0f;
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleStateAnimationSpeedChangesServerRpc(int state, int emotion)
    {
        HandleStateAnimationSpeedChanges((State)state, (Emotion)emotion);
    }

    private void HandleStateAnimationSpeedChanges(State state, Emotion emotion) // This is for host
    {
        SwitchStateClientRpc((int)state, (int)emotion);
        switch (state)
        {
            case State.Inactive:
                SetAnimatorBools(dance: false, activated: false);
                break;
            case State.Active:
                SetAnimatorBools(dance: false, activated: true);
                break;
            case State.FollowingPlayer:
                SetAnimatorBools(dance: false, activated: true);
                break;
            case State.Dancing:
                SetAnimatorBools(dance: true, activated: true);
                break;
        }
    }

    private void SetAnimatorBools(bool dance, bool activated)
    {
        Animator.SetBool(danceAnimation, dance);
        Animator.SetBool(activatedAnimation, activated);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SwitchStateServerRpc(int state, int emotion)
    {
        SwitchStateClientRpc(state, emotion);
    }

    [ClientRpc]
    private void SwitchStateClientRpc(int state, int emotion)
    {
        SwitchState(state, emotion);
    }

    private void SwitchState(int state, int emotion) // this is for everyone.
    {
        State stateToSwitchTo = (State)state;
        Emotion emotionToSwitchTo = (Emotion)emotion;

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
            }
            galState = stateToSwitchTo;
        }

        if (emotion != -1)
        {
            terminalFaceController.SetFaceState(emotionToSwitchTo, 100);
        }
        else
        {
            foreach (Emotion emotionInEnum in Enum.GetValues(typeof(Emotion)))
            {
                terminalFaceController.SetFaceState(emotionInEnum, 0);
            }
        }
    }

    #region State Changes
    private void HandleStateInactiveChange()
    {
        ownerPlayer = null;
        Agent.enabled = false;
        Animator.SetBool(flyingAnimation, false);
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
    #endregion

    public override void OnUseEntranceTeleport(bool setOutside)
    {
        base.OnUseEntranceTeleport(setOutside);
    }

    public override void OnEnterOrExitElevator(bool enteredElevator)
    {
        base.OnEnterOrExitElevator(enteredElevator);
    }

    public IEnumerator DoCustomPassThing(ParticleSystem particleSystem, CustomPassManager.CustomPassType customPassType)
    {
        if (CustomPassManager.Instance.EnableCustomPass(customPassType, true) is not SeeThroughCustomPass customPass) yield break;

        customPass.maxVisibilityDistance = 0f;

        yield return new WaitWhile(() =>
        {
            float percentLifetime = particleSystem.time / particleSystem.main.startLifetime.constant;
            customPass.maxVisibilityDistance =  particleSystem.sizeOverLifetime.size.Evaluate(percentLifetime) * 300; // takes some odd seconds
            return customPass.maxVisibilityDistance < 50;
        });

        yield return new WaitForSeconds(5);

        yield return new WaitWhile(() =>
        {
            customPass.maxVisibilityDistance -= Time.deltaTime * 50 / 3f; // takes 3s
            return customPass.maxVisibilityDistance > 0f;
        });
        CustomPassManager.Instance.RemoveCustomPass(customPassType);
    }

    public override bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        base.Hit(force, hitDirection, playerWhoHit, playHitSFX, hitID);
        if (terminalFaceController.TemporarySwitchCoroutine != null)
        {
            StopCoroutine(terminalFaceController.TemporarySwitchCoroutine);
        }
        terminalFaceController.TemporarySwitchCoroutine = StartCoroutine(terminalFaceController.TemporarySwitchEffect((int)Emotion.Angy));
        terminalFaceController.glitchTimer = terminalFaceController.controllerRandom.NextFloat(4f, 8f);
        return true;
    }
}
