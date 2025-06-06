using System;
using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
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

public class CodeRebirthPlayerManager : NetworkBehaviour
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