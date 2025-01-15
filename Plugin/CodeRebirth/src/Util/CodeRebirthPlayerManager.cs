using System;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts.PathFinding;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Util;
public enum CodeRebirthStatusEffects
{
    None,
    Water,
    Electric,
    Fire,
    Smoke,
    Windy,
    Blood
    // Add other status effects here
}

public class CodeRebirthPlayerManager : NetworkBehaviour
{
    private bool previousDoorClosed;
    internal static Dictionary<PlayerControllerB, CRPlayerData> dataForPlayer = new Dictionary<PlayerControllerB, CRPlayerData>();
    public static List<SmartAgentNavigator> smartAgentNavigators = new();
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
    }
}

public class CRPlayerData
{
    public bool Water = false;
    public bool Electric = false;
    public bool Fire = false;
    public bool Smoke = false;
    public bool Windy = false;
    public bool Blood = false;
    public bool ridingHoverboard = false;
    public bool holdingWallet = false;
    public bool flingingAway = false;
    public bool flung = false;
    public Hoverboard? hoverboardRiding;
    public List<Collider> playerColliders = new();

    internal CodeRebirthLocalSave persistentData => CodeRebirthSave.Current.PlayerData[CodeRebirthPlayerManager.dataForPlayer.FirstOrDefault(it => it.Value == this).Key.playerSteamId];
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
            case CodeRebirthStatusEffects.Electric:
                playerData.Electric = isActive;
                break;
            case CodeRebirthStatusEffects.Fire:
                playerData.Fire = isActive;
                break;
            case CodeRebirthStatusEffects.Smoke:
                playerData.Smoke = isActive;
                break;
            case CodeRebirthStatusEffects.Windy:
                playerData.Windy = isActive;
                break;
            case CodeRebirthStatusEffects.Blood:
                playerData.Blood = isActive;
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
            CodeRebirthStatusEffects.Electric => playerData.Electric,
            CodeRebirthStatusEffects.Fire => playerData.Fire,
            CodeRebirthStatusEffects.Smoke => playerData.Smoke,
            CodeRebirthStatusEffects.Windy => playerData.Windy,
            CodeRebirthStatusEffects.Blood => playerData.Blood,
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
    }

    internal static bool IsHoldingWallet(this PlayerControllerB player) =>
        player.GetCRPlayerData().holdingWallet;

    internal static void SetHoldingWallet(this PlayerControllerB player, bool holdingWallet) =>
        player.GetCRPlayerData().holdingWallet = holdingWallet;

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