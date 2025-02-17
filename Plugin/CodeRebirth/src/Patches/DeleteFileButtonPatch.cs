using CodeRebirth.src.Util;
using HarmonyLib;

namespace CodeRebirth.src.Patches;

[HarmonyPatch(typeof(DeleteFileButton))]
static class DeleteFileButtonPatch
{
	[HarmonyPatch(nameof(DeleteFileButton.DeleteFile)), HarmonyPostfix]
	static void DeleteCodeRebirthData(DeleteFileButton __instance)
	{
		PersistentDataHandler.TryDelete($"CRSave{__instance.fileToDelete}");
	}
}