using CodeRebirth.src.Util;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(HDAdditionalLightData))]
static class HDAdditionalLightDataPatch
{
    [HarmonyPatch(nameof(HDAdditionalLightData.OnEnable)), HarmonyPostfix]
    static void OnEnablePatch(HDAdditionalLightData __instance)
    {
        if (!__instance.gameObject.TryGetComponent(out Light light)) return;
        CodeRebirthUtils.currentRoundLightData.Add((light, __instance));
    }

    [HarmonyPatch(nameof(HDAdditionalLightData.OnDisable)), HarmonyPostfix]
    static void OnDisablePatch(HDAdditionalLightData __instance)
    {
        if (!__instance.gameObject.TryGetComponent(out Light light)) return;
        CodeRebirthUtils.currentRoundLightData.Remove((light, __instance));
    }
}