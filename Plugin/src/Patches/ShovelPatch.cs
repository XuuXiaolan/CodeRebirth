using CodeRebirth.WeaponStuff;
using CodeRebirth.Util.Extensions;
using HarmonyLib;
using BepInEx.AssemblyPublicizer;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(Shovel))]
static class ShovelPatch {
	public static System.Random random;
	[HarmonyPatch(nameof(Shovel.HitShovel)), HarmonyPrefix]
	public static void RemoveShovelLayerLimitation(Shovel __instance) {
		__instance.shovelMask = -1;
	}
	
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
		Plugin.Logger.LogInfo(__instance.critPossible.ToString());
		Plugin.Logger.LogInfo(__instance.critChance.ToString());
		if (__instance.critPossible) {
			__instance.shovelHitForce = ShovelExtensions.CriticalHit(__instance.shovelHitForce, random, __instance.critChance);
			Plugin.Logger.LogInfo("crit possible");
		}
	}

	[HarmonyPatch(nameof(CodeRebirthWeapons.HitShovel)), HarmonyPostfix]
	public static void CritHitShovelPost(EpicAxe __instance) {
		Plugin.Logger.LogInfo($"Shovel Hit: {__instance.shovelHitForce}");
		__instance.shovelHitForce = __instance.defaultForce;
		Plugin.Logger.LogInfo($"Shovel Hit: {__instance.shovelHitForce}");
	}
}