using System;
using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirthLib.Util;
using CodeRebirthLib.Util.INetworkSerializables;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Util;
public enum CodeRebirthStatusEffects
{
    None,
    Water,
    Fire,
    Smoke,
    // Add other status effects here
}

public class CodeRebirthPlayerManager : NetworkSingleton<CodeRebirthPlayerManager>
{
    private bool previousDoorClosed;
    public static List<InteractTrigger> triggersTampered = new();
    internal static Dictionary<PlayerControllerB, CRPlayerData> dataForPlayer = new Dictionary<PlayerControllerB, CRPlayerData>();
    public static event EventHandler<bool>? OnDoorStateChange;
    public void Awake()
    {
        if (StartOfRound.Instance != null) previousDoorClosed = StartOfRound.Instance.hangarDoorsClosed;
    }

    public void Update()
    {
        if (StartOfRound.Instance == null) return;
        if (previousDoorClosed != StartOfRound.Instance.hangarDoorsClosed)
        {
            Plugin.ExtendedLogging("Door opened/closed!!");
            OnDoorStateChange?.Invoke(null, StartOfRound.Instance.hangarDoorsClosed);
        }
        previousDoorClosed = StartOfRound.Instance.hangarDoorsClosed;

        if (SlowDownEffect.isSlowDownEffectActive)
        {
            if (GameNetworkManager.Instance.localPlayerController.hoveringOverTrigger && GameNetworkManager.Instance.localPlayerController.currentTriggerInAnimationWith != null && !triggersTampered.Contains(GameNetworkManager.Instance.localPlayerController.currentTriggerInAnimationWith))
            {
                GameNetworkManager.Instance.localPlayerController.currentTriggerInAnimationWith.timeToHold /= 5f;
                triggersTampered.Add(GameNetworkManager.Instance.localPlayerController.currentTriggerInAnimationWith);
            }
        }
        else if (triggersTampered.Count > 0)
        {
            foreach (InteractTrigger trigger in triggersTampered)
            {
                trigger.timeToHold *= 5f;
            }
            triggersTampered.Clear();
        }
    }

    public static void RevivePlayer(PlayerControllerB player, Vector3 position)
    {
        if(!player.isPlayerDead) return;

        if (Instance.IsHost)
        {
            Instance.RevivePlayerClientRPC(player, position);
        }
        else
        {
            Instance.RevivePlayerServerRPC(player, position);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    void RevivePlayerServerRPC(PlayerControllerReference playerRef, Vector3 position)
    {
        RevivePlayerClientRPC(playerRef, position);
    }

    [ClientRpc]
    void RevivePlayerClientRPC(PlayerControllerReference playerRef, Vector3 position)
    {
        PlayerControllerB player = playerRef;
        player.isInsideFactory = false;
        player.isInElevator = true;
        player.isInHangarShipRoom = true;
        player.ResetPlayerBloodObjects(player.isPlayerDead);
        player.health = 5;
        player.isClimbingLadder = false;
        player.clampLooking = false;
        player.inVehicleAnimation = false;
        player.disableMoveInput = false;
        player.disableLookInput = false;
        player.disableInteract = false;
        player.ResetZAndXRotation();
        player.thisController.enabled = true;
        if (player.isPlayerDead)
        {
            player.thisController.enabled = true;
            player.isPlayerDead = false;
            player.isPlayerControlled = true;
            player.health = 5;
            player.hasBeenCriticallyInjured = false;
            player.criticallyInjured = false;
            player.playerBodyAnimator.SetBool("Limp", value: false);
            player.TeleportPlayer(position, false, 0f, false, true);
            player.parentedToElevatorLastFrame = false;
            player.overrideGameOverSpectatePivot = null;
            StartOfRound.Instance.SetPlayerObjectExtrapolate(enable: false);
            player.setPositionOfDeadPlayer = false;
            player.DisablePlayerModel(player.gameObject, enable: true, disableLocalArms: true);
            player.helmetLight.enabled = false;
            player.Crouch(crouch: false);
            player.playerBodyAnimator?.SetBool("Limp", false);
            player.bleedingHeavily = false;
            if (player.deadBody != null)
            {
                player.deadBody.enabled = false;
                player.deadBody.gameObject.SetActive(false);
            }
            player.bleedingHeavily = true;
            player.deadBody = null;
            player.activatingItem = false;
            player.twoHanded = false;
            player.inShockingMinigame = false;
            player.inSpecialInteractAnimation = false;
            player.freeRotationInInteractAnimation = false;
            player.disableSyncInAnimation = false;
            player.inAnimationWithEnemy = null;
            player.holdingWalkieTalkie = false;
            player.speakingToWalkieTalkie = false;
            player.isSinking = false;
            player.isUnderwater = false;
            player.sinkingValue = 0f;
            player.statusEffectAudio.Stop();
            player.DisableJetpackControlsLocally();
            player.mapRadarDotAnimator.SetBool("dead", value: false);
            player.hasBegunSpectating = false;
            player.externalForceAutoFade = Vector3.zero;
            player.hinderedMultiplier = 1f;
            player.isMovementHindered = 0;
            player.sourcesCausingSinking = 0;
            player.reverbPreset = StartOfRound.Instance.shipReverb;
            SoundManager.Instance.earsRingingTimer = 0f;
            player.voiceMuffledByEnemy = false;
            SoundManager.Instance.playerVoicePitchTargets[Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player)] = 1f;
            SoundManager.Instance.SetPlayerPitch(1f, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));

            if (player.currentVoiceChatIngameSettings == null)
            {
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            }
            if (player.currentVoiceChatIngameSettings != null)
            {
                if (player.currentVoiceChatIngameSettings.voiceAudio == null)
                {
                    player.currentVoiceChatIngameSettings.InitializeComponents();
                }
                if (player.currentVoiceChatIngameSettings.voiceAudio == null)
                {
                    return;
                }
                player.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
            }

            HUDManager.Instance.UpdateBoxesSpectateUI();
            HUDManager.Instance.UpdateSpectateBoxSpeakerIcons();
        }
        if (GameNetworkManager.Instance.localPlayerController == player)
        {
            player.bleedingHeavily = false;
            player.criticallyInjured = false;
            player.health = 5;
            HUDManager.Instance.UpdateHealthUI(5, true);
            player.playerBodyAnimator?.SetBool("Limp", false);
            player.spectatedPlayerScript = null;
            StartOfRound.Instance.SetSpectateCameraToGameOverMode(false, player);
            StartOfRound.Instance.SetPlayerObjectExtrapolate(false);
            HUDManager.Instance.audioListenerLowPass.enabled = false;
            HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
            HUDManager.Instance.RemoveSpectateUI();
            HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
        }
        StartOfRound.Instance.allPlayersDead = false;
        StartOfRound.Instance.livingPlayers++;
        StartOfRound.Instance.UpdatePlayerVoiceEffects();
    }
}

public class CRPlayerData
{
    public bool Water = false;
    public bool Fire = false;
    public bool Smoke = false;
    public bool ridingHoverboard = false;
    public bool flingingAway = false;
    public bool flung = false;
    public bool pseudoDead = false;
    public Hoverboard? hoverboardRiding;
    // public AnimatorOverrideController animatorOverrideController = null!;
    public List<Collider> playerColliders = new();
}

internal static class PlayerControllerBExtensions
{
    internal enum ApplyEffectResults : sbyte
    {
        Applied,
        Removed,
        None,
    }

    internal static ApplyEffectResults ApplyStatusEffect(this PlayerControllerB player, CodeRebirthStatusEffects effect, float range, float distance)
    {
        if (distance < range && !player.HasEffectActive(effect))
        {
            player.ApplyStatusEffect(effect, true);
            return ApplyEffectResults.Applied;
        }
        else if (distance >= range && player.HasEffectActive(effect))
        {
            player.ApplyStatusEffect(effect, false);
            return ApplyEffectResults.Removed;
        }
        if (player.HasEffectActive(effect) && effect == CodeRebirthStatusEffects.Fire)
        {
            CodeRebirthUtils.Instance.FireyVolume.weight = Mathf.Clamp01(range / distance);
        }
        else if (player.HasEffectActive(effect) && effect == CodeRebirthStatusEffects.Smoke)
        {
            CodeRebirthUtils.Instance.SmokyVolume.weight = Mathf.Clamp01(range / distance);
        }
        return ApplyEffectResults.None;
    }

    internal static void ApplyStatusEffect(this PlayerControllerB player, CodeRebirthStatusEffects effect, bool isActive)
    {
        if (!player.ContainsCRPlayerData()) return;
        CRPlayerData playerData = player.GetCRPlayerData();

        switch (effect)
        {
            case CodeRebirthStatusEffects.Water:
                playerData.Water = isActive;
                break;
            case CodeRebirthStatusEffects.Fire:
                CodeRebirthUtils.Instance.FireyVolume.weight = 0;
                playerData.Fire = isActive;
                break;
            case CodeRebirthStatusEffects.Smoke:
                CodeRebirthUtils.Instance.SmokyVolume.weight = 0;
                playerData.Smoke = isActive;
                break;
        }
    }

    internal static bool HasEffectActive(this PlayerControllerB player, CodeRebirthStatusEffects effect)
    {
        if (!player.ContainsCRPlayerData()) return false;
        CRPlayerData playerData = player.GetCRPlayerData();

        return effect switch
        {
            CodeRebirthStatusEffects.Water => playerData.Water,
            CodeRebirthStatusEffects.Fire => playerData.Fire,
            CodeRebirthStatusEffects.Smoke => playerData.Smoke,
            _ => false,
        };
    }
    internal static CRPlayerData GetCRPlayerData(this PlayerControllerB player) =>
        CodeRebirthPlayerManager.dataForPlayer[player];

    internal static bool ContainsCRPlayerData(this PlayerControllerB player) =>
        CodeRebirthPlayerManager.dataForPlayer.ContainsKey(player);

    internal static void AddCRPlayerData(this PlayerControllerB player)
    {
        CodeRebirthPlayerManager.dataForPlayer.Add(player, new CRPlayerData());
        player.GetCRPlayerData().playerColliders = new List<Collider>(player.GetComponentsInChildren<Collider>());
        // player.GetCRPlayerData().animatorOverrideController = new AnimatorOverrideController(player.playerBodyAnimator.runtimeAnimatorController);
        // player.playerBodyAnimator.runtimeAnimatorController = player.GetCRPlayerData().animatorOverrideController;
    }

    internal static bool IsPseudoDead(this PlayerControllerB player) =>
        player.GetCRPlayerData().pseudoDead;

    internal static bool SetPseudoDead(this PlayerControllerB player, bool pseudoDead) =>
        player.GetCRPlayerData().pseudoDead = pseudoDead;

    internal static Hoverboard? TryGetHoverboardRiding(this PlayerControllerB player) =>
        player.GetCRPlayerData().hoverboardRiding;

    internal static void SetHoverboardRiding(this PlayerControllerB player, Hoverboard? hoverboard) =>
        player.GetCRPlayerData().ridingHoverboard = hoverboard;

    internal static bool IsRidingHoverboard(this PlayerControllerB player) =>
        player.GetCRPlayerData().ridingHoverboard;

    internal static void SetRidingHoverboard(this PlayerControllerB player, bool ridingHoverboard) =>
        player.GetCRPlayerData().ridingHoverboard = ridingHoverboard;

    internal static bool HasFlung(this PlayerControllerB player) =>
        player.GetCRPlayerData().flung;

    internal static void SetFlung(this PlayerControllerB player, bool flung) =>
        player.GetCRPlayerData().flung = flung;

    internal static bool IsFlingingAway(this PlayerControllerB player) =>
        player.GetCRPlayerData().flingingAway;

    internal static void SetFlingingAway(this PlayerControllerB player, bool flingingAway) =>
        player.GetCRPlayerData().flingingAway = flingingAway;

    internal static IEnumerable<Collider> GetPlayerColliders(this PlayerControllerB player) =>
        player.GetCRPlayerData().playerColliders;
}