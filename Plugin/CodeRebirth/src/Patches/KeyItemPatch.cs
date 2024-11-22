using CodeRebirth.src.Content.Items;
using HarmonyLib;
using UnityEngine;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(KeyItem))]
static class KeyItemPatch {
	[HarmonyPatch(nameof(KeyItem.ItemActivate)), HarmonyPostfix]
	public static void CustomPickableObjects(KeyItem __instance) {
		if (__instance.playerHeldBy == null || !__instance.IsOwner) {
			return;
		}
		if (Physics.Raycast(new Ray(__instance.playerHeldBy.gameplayCamera.transform.position, __instance.playerHeldBy.gameplayCamera.transform.forward), out RaycastHit raycastHit, 3f, 2816))
		{
			if (raycastHit.transform.TryGetComponent(out Pickable pickable) && pickable.enabled && pickable.IsLocked)
			{
				pickable.Unlock();
				if (__instance.playerHeldBy.currentlyHeldObjectServer != null && __instance.playerHeldBy.currentlyHeldObjectServer.IsSpawned) __instance.playerHeldBy.DespawnHeldObject();
			}
		}
	}
}