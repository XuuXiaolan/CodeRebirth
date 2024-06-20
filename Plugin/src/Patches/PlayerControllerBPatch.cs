using CodeRebirth.MapStuff;
using CodeRebirth.Util.PlayerManager;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB))]
static class PlayerControllerBPatch {
	[HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.PlayFootstepSound)), HarmonyPrefix]
	public static bool PlayFootstepSound(PlayerControllerB __instance) {
        if (__instance.gameObject.GetComponent<CodeRebirthPlayerManager>().ridingHoverboard) {
            return false;
        } else {
            return true;
        }
	}
}