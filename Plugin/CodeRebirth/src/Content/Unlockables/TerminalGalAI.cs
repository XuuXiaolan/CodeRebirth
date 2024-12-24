using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts.CustomPasses;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


namespace CodeRebirth.src.Content.Unlockables;
public class TerminalGalAI : GalAI
{
    public TerrainScanner terrainScanner = null!;
    // public InteractTrigger hugInteractTrigger = null!; keyboard?
    public AudioSource FlyingSource = null!;
    public AudioClip scrapPingSound = null!;
    public List<AudioClip> flyingAudioClips = new();
    public List<AudioClip> startOrEndFlyingAudioClips = new();

    private List<Coroutine> customPassRoutines = new();
    private float scrapRevealTimer = 10f;
    private bool flying = false;
    private State galState = State.Inactive;
    private readonly static int inElevatorAnimation = Animator.StringToHash("inElevator"); // bool
    private readonly static int revealScrapAnimation = Animator.StringToHash("revealScrap"); // trigger
    private readonly static int flyingAnimation = Animator.StringToHash("Flying"); // bool
    private readonly static int danceAnimation = Animator.StringToHash("dancing"); // bool
    private readonly static int activatedAnimation = Animator.StringToHash("activated"); // bool
    private readonly static int runSpeedFloat = Animator.StringToHash("RunSpeed"); // float

    public enum State
    {
        Inactive = 0,
        Active = 1,
        FollowingPlayer = 2,
        Dancing = 3,
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        NetworkObject.TrySetParent(GalCharger.transform, false);
        ResetToChargerStation(galState);
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
        StartCoroutine(UpdateFlyingSound());
    }

    private IEnumerator UpdateFlyingSound()
    {
        while (true)
        {
            yield return new WaitUntil(() => !FlyingSource.isPlaying);
            FlyingSource.clip = flyingAudioClips[UnityEngine.Random.Range(0, flyingAudioClips.Count)];
            FlyingSource.Play();
        }
    }

    /*private void OnHugInteract(PlayerControllerB playerInteracting)
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
        EnablePhysics(!physicsEnabled);
    }*/

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

    private void InteractTriggersUpdate()
    {
        bool interactable = !inActive && (ownerPlayer != null && GameNetworkManager.Instance.localPlayerController == ownerPlayer);
        // bool idleInteractable = galState != State.AttackMode && interactable;
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
        DoRevealingScrap();

        if (DoDancingAction())
        {
            return;
        }
    }

    private void DoDancing()
    {
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

    private void DoHazardActionsAnimEvent()
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
            HandleStateAnimationSpeedChanges(State.Dancing);
            StartCoroutine(StopDancingDelay());
            return true;
        }
        return false;
    }

    private IEnumerator StopDancingDelay()
    {
        yield return new WaitUntil(() => !boomboxPlaying || galState != State.Dancing);
        if (galState != State.Dancing) yield break;  
        HandleStateAnimationSpeedChanges(State.FollowingPlayer);
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
            }
            galState = stateToSwitchTo;
        }
    }

    #region State Changes
    private void HandleStateInactiveChange()
    {
        ownerPlayer = null;
        Agent.enabled = false;
        Animator.SetBool(inElevatorAnimation, false);
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
        Animator.SetBool(inElevatorAnimation, enteredElevator);
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
}
