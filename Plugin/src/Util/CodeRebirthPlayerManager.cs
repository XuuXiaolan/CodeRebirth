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
    public static event Action<PlayerControllerB, CodeRebirthStatusEffects, bool>? OnStatusEffectChanged;
    public void Awake()
    {
        if (Plugin.ModConfig.ConfigTornadosEnabled.Value) InitPlayerParticles();
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

    public void InitPlayerParticles()
    {
        playerParticles[0] = Instantiate(WeatherHandler.Instance.Tornado.WaterPlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[1] = Instantiate(WeatherHandler.Instance.Tornado.ElectricPlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[2] = Instantiate(WeatherHandler.Instance.Tornado.FirePlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[3] = Instantiate(WeatherHandler.Instance.Tornado.SmokePlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[4] = Instantiate(WeatherHandler.Instance.Tornado.WindPlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        playerParticles[5] = Instantiate(WeatherHandler.Instance.Tornado.BloodPlayerParticles, this.gameObject.transform.position, Quaternion.identity, this.gameObject.transform);
        foreach (GameObject particleSystem in playerParticles)
        {
            particleSystem.gameObject.SetActive(false);
        }
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

            OnStatusEffectChanged?.Invoke(player, effect, isActive);
            HandleStatusEffectChanged(player, effect, isActive);
        }
    }

    private void OnEnable()
    {
        OnStatusEffectChanged += HandleStatusEffectChanged;
    }

    private void OnDisable()
    {
        OnStatusEffectChanged -= HandleStatusEffectChanged;
    }

    private static void HandleStatusEffectChanged(PlayerControllerB player, CodeRebirthStatusEffects effect, bool isActive)
    {
        var index = GetEffectIndex(effect);
        if (index >= 0 && index < playerParticles.Length)
        {
            playerParticles[index].SetActive(isActive);
        }
    }

    private static int GetEffectIndex(CodeRebirthStatusEffects effect)
    {
        return effect switch
        {
            CodeRebirthStatusEffects.Water => 0,
            CodeRebirthStatusEffects.Electric => 1,
            CodeRebirthStatusEffects.Fire => 2,
            CodeRebirthStatusEffects.Smoke => 3,
            CodeRebirthStatusEffects.Windy => 4,
            CodeRebirthStatusEffects.Blood => 5,
            _ => -1,
        };
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