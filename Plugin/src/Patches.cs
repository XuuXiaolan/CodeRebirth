using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using UnityEngine.Assertions;

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
    [HarmonyPrefix]
    public static void StartOfRound_Start(ref StartOfRound __instance)
    {
        __instance.StartCoroutine(WaitForNetworkObject(__instance, CreateNetworkManager));
    }

    private static IEnumerator WaitForNetworkObject(StartOfRound __instance, Action<StartOfRound> action)
    {
        while (__instance.NetworkObject.IsSpawned == false)
        {
            yield return null;
        }
        action(__instance);
    }

    private static void CreateNetworkManager(StartOfRound __instance)
    {
        Plugin.Logger.LogInfo($"IsServer: {__instance.IsServer}");
        if (__instance.IsServer)
        {
            if (CodeRebirthUtils.Instance == null)
            {
                GameObject go = new("CodeRebirthUtils")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                go.AddComponent<CodeRebirthUtils>();
                go.AddComponent<NetworkObject>();
                go.GetComponent<NetworkObject>().GlobalObjectIdHash = 3003411660;
                go.GetComponent<NetworkObject>().Spawn(false);
                Plugin.Logger.LogInfo("Created CodeRebirthUtils.");
            } else {
                Plugin.Logger.LogWarning("CodeRebirthUtils already exists?");
            }
        }
    }
}