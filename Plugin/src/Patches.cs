using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;
using CodeRebirth;
using CodeRebirth.ItemStuff;
using CodeRebirth.MapStuff;
using CodeRebirth.Misc;
using CodeRebirth.WeatherStuff;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using System.Linq;

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

        string[] meteorLevelOverrides = Plugin.ModConfig.ConfigMeteorShowerMoonsBlacklist.Value.Split(',')
                                    .Select(name => name.Trim())
                                    .ToArray();
        string[] tornadoLevelOverrides = Plugin.ModConfig.ConfigTornadoMoonsBlacklist.Value.Split(',')
                                    .Select(name => name.Trim())
                                    .ToArray();
        LethalLib.Modules.Weathers.RemoveWeather("Meteor Shower", levelOverrides: meteorLevelOverrides);
        LethalLib.Modules.Weathers.RemoveWeather("Tornados", levelOverrides: tornadoLevelOverrides);
    }

    [HarmonyPatch(nameof(StartOfRound.OnDisable))]
    [HarmonyPrefix]
    public static void DisableWeathersPatch() {
        if (MeteorShower.Active) { // patch to fix OnDisable not being triggered as its not actually in the scene.
            WeatherHandler.Instance.MeteorShowerWeather.effectObject.SetActive(false);
            WeatherHandler.Instance.MeteorShowerWeather.effectPermanentObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(KeyItem), nameof(KeyItem.ItemActivate)), HarmonyPostfix]
    public static void CustomPickableObjects(KeyItem __instance) {
        if (__instance.playerHeldBy == null || !__instance.IsOwner) {
            return;
        }
        if (Physics.Raycast(new Ray(__instance.playerHeldBy.gameplayCamera.transform.position, __instance.playerHeldBy.gameplayCamera.transform.forward), out RaycastHit raycastHit, 3f, 2816))
        {
            if (raycastHit.transform.TryGetComponent(out Pickable pickable) && pickable.IsLocked) {
                pickable.Unlock();
                __instance.playerHeldBy.DespawnHeldObject();
            }
        }
    }
    
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnOutsideHazards)), HarmonyPostfix]
    public static void SpawnOutsideMapObjects() {
        if(!RoundManager.Instance.IsHost) return;
        if(!Plugin.ModConfig.ConfigItemCrateEnabled.Value) return;
        System.Random random = new();
        for (int i = 0; i < random.Next(0, Mathf.Clamp(Plugin.ModConfig.ConfigCrateAbundance.Value, 0, 1000)); i++) {
            Vector3 position = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
            Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

            Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100);
            
            GameObject spawnedCrate = GameObject.Instantiate(MapObjectHandler.Instance.Assets.ItemCratePrefab, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            spawnedCrate.transform.up = hit.normal;
            spawnedCrate.GetComponent<NetworkObject>().Spawn();
        }
    }

    [HarmonyPatch(typeof(Shovel), nameof(Shovel.HitShovel)), HarmonyPrefix]
    public static void RemoveShovelLayerLimitation(Shovel __instance) {
        __instance.shovelMask = -1;
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

