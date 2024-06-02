using HarmonyLib;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(Shovel))]
static class ShovelPatch {
	[HarmonyPatch(nameof(Shovel.HitShovel)), HarmonyPrefix]
	public static void RemoveShovelLayerLimitation(Shovel __instance) {
		__instance.shovelMask = -1;
	}
}