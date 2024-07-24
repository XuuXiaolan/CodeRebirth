using System;
using System.Collections.Generic;
using CodeRebirth.Util.PlayerManager;
using CodeRebirth.WeatherStuff;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB))]
static class PlayerControllerBPatch {
    public static bool TornadoKinematicPatch = false;
    public static PlayerControllerB? TornadoKinematicPlayer = null;

    [HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects)), HarmonyPrefix]
    public static bool PlayerHitGroundEffects(PlayerControllerB __instance) {
        if (TornadoKinematicPatch && TornadoKinematicPlayer == __instance) {
            CodeRebirthPlayerManager.dataForPlayer[__instance].flingingAway = false;
            TornadoKinematicPatch = false;
            __instance.playerRigidbody.isKinematic = true;
            TornadoKinematicPlayer = null;
        }
        return true;
    }

	[HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.PlayFootstepSound)), HarmonyPrefix]
	public static bool PlayFootstepSound(PlayerControllerB __instance) {
        if (__instance == GameNetworkManager.Instance.localPlayerController && CodeRebirthPlayerManager.dataForPlayer.ContainsKey(__instance) && CodeRebirthPlayerManager.dataForPlayer[__instance].ridingHoverboard) {
            return false;
        } else {
            return true;
        }
	}

    [HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.Awake)), HarmonyPostfix]
    public static void Awake(PlayerControllerB __instance) {
        if (!CodeRebirthPlayerManager.dataForPlayer.ContainsKey(__instance)) {
            CodeRebirthPlayerManager.dataForPlayer.Add(__instance, new CRPlayerData());
            List<Collider> colliders = new List<Collider>(__instance.GetComponentsInChildren<Collider>());
            CodeRebirthPlayerManager.dataForPlayer[__instance].playerColliders = colliders;
        }
    }

    [HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.Update)), HarmonyPrefix]
    public static void Update(PlayerControllerB __instance) {
        if (GameNetworkManager.Instance.localPlayerController == null) return;
        if (CodeRebirthPlayerManager.dataForPlayer[__instance].playerOverrideController != null) return;
        CodeRebirthPlayerManager.dataForPlayer[__instance].playerOverrideController = new AnimatorOverrideController(__instance.playerBodyAnimator.runtimeAnimatorController);
        __instance.playerBodyAnimator.runtimeAnimatorController = CodeRebirthPlayerManager.dataForPlayer[__instance].playerOverrideController; 
    }
}