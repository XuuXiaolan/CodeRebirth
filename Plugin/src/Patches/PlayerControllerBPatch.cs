using System;
using CodeRebirth.EnemyStuff;
using CodeRebirth.Util.PlayerManager;
using CodeRebirth.WeatherStuff;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB))]
static class PlayerControllerBPatch {
    public static bool TornadoKinematicPatch = false;
    public static PlayerControllerB? TornadoKinematicPlayer = null;
    public static CodeRebirthPlayerManager? TornadoKinematicPlayerManager = null;

    [HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.PlayerHitGroundEffects)), HarmonyPrefix]
    public static bool PlayerHitGroundEffects(PlayerControllerB __instance) {
        if (TornadoKinematicPatch && Tornados.Instance != null) {
            Tornados.Instance.ResetPlayerRigidBodyStuffServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, __instance));
        }
        return true;
    }

	[HarmonyPatch(nameof(GameNetcodeStuff.PlayerControllerB.PlayFootstepSound)), HarmonyPrefix]
	public static bool PlayFootstepSound(PlayerControllerB __instance) {
        if (__instance.gameObject.GetComponent<CodeRebirthPlayerManager>().ridingHoverboard) {
            return false;
        } else {
            return true;
        }
	}
}