using System;
using System.Collections.Generic;
using CodeRebirth.WeatherStuff;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.Util.PlayerManager;
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

    public void Update() {
        if (StartOfRound.Instance == null) return;
        if (previousDoorClosed != StartOfRound.Instance.hangarDoorsClosed) {
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
    public static Dictionary<PlayerControllerB, CRPlayerData>? dataForPlayer;
    public bool Water;
    public bool Electric;
    public bool Fire;
    public bool Smoke;
    public bool Windy;
    public bool Blood;
    public bool ridingHoverboard;
    public bool holdingWallet;
    public bool flingingAway = false;
    public bool flung = false;
    public List<Collider>? playerColliders;
    public AnimatorOverrideController? playerOverrideController;
}

internal static class PlayerControllerBExtensions
{
    internal static CRPlayerData GetCRPlayerData(this PlayerControllerB player) =>
        CodeRebirthPlayerManager.dataForPlayer[player];
}