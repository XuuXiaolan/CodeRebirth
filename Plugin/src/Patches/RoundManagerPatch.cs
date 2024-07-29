using System.Collections.Generic;
using CodeRebirth.MapStuff;
using CodeRebirth.Util.Extensions;
using CodeRebirth.Util.Spawning;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch {
	internal static List<SpawnableFlora> spawnableFlora = [];
    
	[HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards)), HarmonyPostfix]
	static void SpawnOutsideMapObjects() {
		if (Plugin.ModConfig.ConfigFloraEnabled.Value) SpawnFlora();
        
		if (!RoundManager.Instance.IsHost) return;
		if (Plugin.ModConfig.ConfigItemCrateEnabled.Value) SpawnCrates();
		
	}

	static void SpawnFlora() {
		Plugin.Logger.LogDebug("Spawning flora!!!");
		System.Random random = new(StartOfRound.Instance.randomMapSeed + 2358);

		foreach (SpawnableFlora flora in spawnableFlora) {
			for (int i = 0; i < flora.spawnCurve.Evaluate(random.NextFloat(0,1)); i++) {
				Vector3 position = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
				Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

				if(!Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
					continue;

				bool isValid = true;
				foreach (string tag in flora.blacklistedTags) {
					if (hit.transform.gameObject.CompareTag(tag)) {
						isValid = false;
						break;
					}
				}
				if(!isValid) continue;

				GameObject spawnedFlora = GameObject.Instantiate(flora.prefab, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
			
				spawnedFlora.transform.up = hit.normal;
			}
		}
        
		
	}

	static void SpawnCrates() {
		Plugin.Logger.LogDebug("Spawning crates!!!");
		System.Random random = new();
		int minValue = 0;
		for (int i = 0; i < random.Next(minValue, Mathf.Clamp(Plugin.ModConfig.ConfigCrateAbundance.Value, minValue, 1000)); i++) {
			Vector3 position = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

			GameObject crate = random.NextBool() ? MapObjectHandler.Instance.Crate.MetalCratePrefab : MapObjectHandler.Instance.Crate.ItemCratePrefab;
			
			GameObject spawnedCrate = GameObject.Instantiate(crate, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
			Plugin.Logger.LogDebug($"Spawning {crate.name} at {hit.point}");
			spawnedCrate.transform.up = hit.normal;
			spawnedCrate.GetComponent<NetworkObject>().Spawn();
		}
	}
	
	[HarmonyPatch(nameof(RoundManager.UnloadSceneObjectsEarly)), HarmonyPostfix]
	static void PatchFix_DespawnOldCrates() {
		foreach (ItemCrate crate in GameObject.FindObjectsOfType<ItemCrate>()) {
			crate.NetworkObject.Despawn();
		}
	}

	/*[HarmonyPatch("LoadNewLevelWait")]
	[HarmonyPrefix]
	public static void LoadNewLevelWaitPatch(RoundManager __instance)
	{
		if (__instance.currentLevel.levelID == 3 && TimeOfDay.Instance.daysUntilDeadline == 0)
		{
			Plugin.Logger.LogInfo("Spawning Devil deal objects");
			if (RoundManager.Instance.IsServer) CodeRebirthUtils.Instance.SpawnDevilPropsServerRpc();
		}
	}

	[HarmonyPatch("DespawnPropsAtEndOfRound")]
	[HarmonyPostfix]
	public static void DespawnPropsAtEndOfRoundPatch(RoundManager __instance)
	{
		if (__instance.currentLevel.levelID == 3 && TimeOfDay.Instance.daysUntilDeadline == 0)
		{
			Plugin.Logger.LogInfo("Despawning Devil deal objects");
			if (RoundManager.Instance.IsServer) CodeRebirthUtils.Instance.DespawnDevilPropsServerRpc();
		}
	}*/
}