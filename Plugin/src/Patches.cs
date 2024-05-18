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
                GameObject utilsInstance = GameObject.Instantiate(Plugin.CRUtils);
                SceneManager.MoveGameObjectToScene(utilsInstance, StartOfRound.Instance.gameObject.scene);
                utilsInstance.GetComponent<NetworkObject>().Spawn();
                Plugin.Logger.LogInfo($"Created CodeRebirthUtils. Scene is: '{utilsInstance.scene.name}'");
            } else {
                Plugin.Logger.LogWarning("CodeRebirthUtils already exists?");
            }
        }
    }
}