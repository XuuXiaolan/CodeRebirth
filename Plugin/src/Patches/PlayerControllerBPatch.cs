using CodeRebirth.MapStuff;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.ScrapStuff;
using System.Linq;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(PlayerControllerB))]
static class PlayerControllerBPatch {
    public static PlayerControllerB[] PlayerControllingHoverboardList;
	[HarmonyPatch(nameof(PlayerControllerB.PlayerHitGroundEffects)), HarmonyPrefix]
	public static void PlayerHitGroundEffects(PlayerControllerB __instance) {
        if (PlayerControllingHoverboardList.Any(x => x == GameNetworkManager.Instance.localPlayerController)) {
            __instance.fallValue = 0f;
            __instance.fallValueUncapped = 0f;
        }
    }
}