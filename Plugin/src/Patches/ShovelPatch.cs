using CodeRebirth.WeaponStuff;
using CodeRebirth.Util.Extensions;
using HarmonyLib;
using System.Collections.Generic;
using GameNetcodeStuff;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(Shovel))]
static class ShovelPatch {
	public static System.Random? random;
	private static bool postFixWorks = true;
	
	[HarmonyPatch(nameof(Shovel.HitShovel)), HarmonyPrefix]
	public static void CritHitShovelPre(Shovel __instance) {
		if (random == null) {
			if (StartOfRound.Instance != null) {
				random = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
			} else {
				random = new System.Random(69);
			}
		}
		if (__instance is CodeRebirthWeapons CRWeapon) {
			if (!postFixWorks) {
				Plugin.Logger.LogError("postfix doesn't work");
			}
			postFixWorks = false;
			CRWeapon.defaultForce = CRWeapon.shovelHitForce;
			if (CRWeapon.critPossible && Plugin.ModConfig.ConfigAllowCrits.Value) {
				CRWeapon.shovelHitForce = ShovelExtensions.CriticalHit(CRWeapon.shovelHitForce, random, CRWeapon.critChance);
			}
		}

		if (__instance is NaturesMace naturesMace) {
			List<PlayerControllerB> playerList = naturesMace.HitNaturesMace();
			Plugin.ExtendedLogging("playerList: " + playerList.Count);
			foreach (PlayerControllerB player in playerList) {
				naturesMace.Heal(player);
			}
		}
	}

	[HarmonyPatch(nameof(Shovel.HitShovel)), HarmonyPostfix]
	public static void CritHitShovelPost(Shovel __instance) {
		if (__instance is CodeRebirthWeapons CRWeapon) {
			postFixWorks = true;
			CRWeapon.shovelHitForce = CRWeapon.defaultForce;	
		}
	}
}