using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;
using CodeRebirth;
using CodeRebirth.Misc;
using CodeRebirth.WeatherStuff;
using UnityEngine.SceneManagement;

namespace CodeRebirth.src;
[HarmonyPatch(typeof(StartOfRound))]
internal static class StartOfRoundPatcher {
    [HarmonyPrefix]
    [HarmonyPatch(nameof(StartOfRound.Start))]
    static void RegisterScraps() {
        foreach (var item in Plugin.samplePrefabs.Values) {
            if (!StartOfRound.Instance.allItemsList.itemsList.Contains(item)) {
                StartOfRound.Instance.allItemsList.itemsList.Add(item);
            }
        }
    }
    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPostfix]
    public static void StartOfRound_Start(ref StartOfRound __instance)
    {
        __instance.NetworkObject.OnSpawn(CreateNetworkManager);
        LethalLib.Modules.Weathers.RemoveWeather("Meteor Shower", LethalLib.Modules.Levels.LevelTypes.None, [Plugin.ModConfig.ConfigMeteorShowerMoonsBlacklist.Value]);
    }

    [HarmonyPatch(nameof(StartOfRound.OnDisable))]
    [HarmonyPrefix]
    public static void DisableWeathersPatch() {
        if (MeteorShower.Active) { // patch to fix OnDisable not being triggered as its not actually in the scene.
            WeatherHandler.Instance.MeteorShowerWeather.effectObject.SetActive(false);
            WeatherHandler.Instance.MeteorShowerWeather.effectPermanentObject.SetActive(false);
        }
    }
    
    private static IEnumerator WaitForNetworkObject(StartOfRound __instance, Action<StartOfRound> action)
    {
        while (__instance.NetworkObject.IsSpawned == false)
        {
            yield return null;
        }
        action(__instance);
    }

    private static void CreateNetworkManager()
    {
        if (StartOfRound.Instance.IsServer || StartOfRound.Instance.IsHost)
        {
            if (CodeRebirthUtils.Instance == null) {
                GameObject utilsInstance = GameObject.Instantiate(Plugin.Assets.UtilsPrefab);
                SceneManager.MoveGameObjectToScene(utilsInstance, StartOfRound.Instance.gameObject.scene);
                utilsInstance.GetComponent<NetworkObject>().Spawn();
                Plugin.Logger.LogInfo($"Created CodeRebirthUtils. Scene is: '{utilsInstance.scene.name}'");
            } else {
                Plugin.Logger.LogWarning("CodeRebirthUtils already exists?");
            }
        }
    }
}

