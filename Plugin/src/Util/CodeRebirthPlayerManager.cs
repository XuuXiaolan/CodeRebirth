using System;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content;
using CodeRebirth.src.Content.Items;
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
    public static Dictionary<PlayerControllerB, CRPlayerData> dataForPlayer = new Dictionary<PlayerControllerB, CRPlayerData>();
    public static GameObject[] playerParticles = new GameObject[6];
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
            Plugin.Logger.LogDebug("Door opened/closed!!");
            OnDoorStateChange?.Invoke(null, StartOfRound.Instance.hangarDoorsClosed);
        }
        previousDoorClosed = StartOfRound.Instance.hangarDoorsClosed;
    }

    public static void UpdateStatusEffect(PlayerControllerB player, CodeRebirthStatusEffects effect, bool isActive)
    {
        if (dataForPlayer.ContainsKey(player))
        {
            var playerData = dataForPlayer[player];

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
    }

    public static void ChangeActiveStatus(PlayerControllerB player, CodeRebirthStatusEffects effect, bool isActive)
    {
        UpdateStatusEffect(player, effect, isActive);
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
    public List<Collider>? playerColliders = null;
    public AnimatorOverrideController? playerOverrideController;

    internal CodeRebirthLocalSave persistentData => CodeRebirthSave.Current.PlayerData[CodeRebirthPlayerManager.dataForPlayer.FirstOrDefault(it => it.Value == this).Key.playerSteamId];
}

internal static class PlayerControllerBExtensions
{
    internal static CRPlayerData GetCRPlayerData(this PlayerControllerB player) =>
        CodeRebirthPlayerManager.dataForPlayer[player];

    internal static Hoverboard? TryGetHoverboardRiding(this PlayerControllerB player) =>
        player.GetCRPlayerData().hoverboardRiding;
}