using System.Collections.Generic;
using System.Reflection.Emit;
using CodeRebirth.ItemStuff;
using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;
using Steamworks.Data;
using UnityEngine;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(KeyItem))]
static class KeyItemPatch {
	static bool PickableIsValid(Pickable pickable) {
		return pickable != null && pickable.enabled && pickable.IsLocked;
	}
	
	[
		HarmonyPatch(nameof(KeyItem.ItemActivate)), 
		HarmonyTranspiler,
		HarmonyBefore("ShaosilGaming.GeneralImprovements") // for future compat
	]
	public static IEnumerable<CodeInstruction> CustomPickableObjects(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
		return new CodeMatcher(instructions, generator)
			   .MatchForward(false, new CodeMatch(OpCodes.Ret))
			   .CreateLabel(out Label earlyReturn)
			   .MatchForward(false,
				   new CodeMatch(),
				   new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(RaycastHit), nameof(RaycastHit.transform))),
				   new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Component), nameof(Component.GetComponent)).MakeGenericMethod(typeof(DoorLock))),
				   new CodeMatch(OpCodes.Stloc_1)
			   )
			   .ThrowIfInvalid("Failed to find main raycast code in KeyItem.ItemActivate")
			   .InsertAndAdvance(
					// pickable = raycastHit.GetComponent<Pickable>
					new CodeInstruction(OpCodes.Ldloca_S, 0),
					new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(RaycastHit), nameof(RaycastHit.transform))),
					new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Component), nameof(Component.GetComponent)).MakeGenericMethod(typeof(Pickable))),
					new CodeInstruction(OpCodes.Stloc_1),
					
					// leave IL land as soon as possible lmao
					new CodeInstruction(OpCodes.Ldloc_1),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(KeyItemPatch), nameof(PickableIsValid))),
					new CodeInstruction(OpCodes.Brfalse, earlyReturn),
					
					// we can unlock, do that stuff
					new CodeInstruction(OpCodes.Ldloc_1),
					new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Pickable), nameof(Pickable.Unlock))),
					
					// finally remove the item from the player
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GrabbableObject), nameof(GrabbableObject.playerHeldBy))),
					new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.DespawnHeldObject)))
				)
			.InstructionEnumeration();
	}
}