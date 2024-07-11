using CodeRebirth.WeaponStuff;
using CodeRebirth.Util.Extensions;
using HarmonyLib;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(Shovel))]
static class ShovelPatch {
	public static System.Random? random;
	
	[HarmonyPatch(nameof(CodeRebirthWeapons.HitShovel)), HarmonyPrefix]
	public static void CritHitShovelPre(CodeRebirthWeapons __instance) {
		__instance.defaultForce = __instance.shovelHitForce;
		if (random == null) {
			if (StartOfRound.Instance != null) {
				random = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
			} else {
				random = new System.Random(69);
			}
		}
		if (__instance.critPossible && Plugin.ModConfig.ConfigAllowCrits.Value) {
			__instance.shovelHitForce = ShovelExtensions.CriticalHit(__instance.shovelHitForce, random, __instance.critChance);
		}
	}

	[HarmonyPatch(nameof(CodeRebirthWeapons.HitShovel)), HarmonyPostfix]
	public static void CritHitShovelPost(CodeRebirthWeapons __instance) {
		__instance.shovelHitForce = __instance.defaultForce;
	}
}