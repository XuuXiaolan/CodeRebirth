using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	static void SpawnFlora() {
		bool isLevelValid = IsCurrentMoonInConfig(spawnableFlora[0].moonsWhiteList);
		if (!isLevelValid) return;
		Plugin.Logger.LogInfo("Spawning flora!!!");
		System.Random random = new(StartOfRound.Instance.randomMapSeed + 2358);

		foreach (SpawnableFlora flora in spawnableFlora) {
			var targetSpawns = flora.spawnCurve.Evaluate(random.NextFloat(0,1));
			for (int i = 0; i < targetSpawns; i++) {
				Vector3 basePosition = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
				Vector3 offset = new Vector3(random.NextFloat(-5, 5), 0, random.NextFloat(-5, 5));
				Vector3 randomPosition = basePosition + offset;
				Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(randomPosition, 10f, default, random, -1) + (Vector3.up * 2);

				if(!Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
					continue;

				bool isValid = true;
				if (Plugin.LethalLevelLoaderIsOn) {
					flora.blacklistedTags = flora.blacklistedTags.Where(x => x != "Untagged").ToArray();
				}
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

	public static bool IsCurrentMoonInConfig(string[] moonsWhiteList) {
		// Prepare the current level name
		string currentLevelName = (StartOfRound.Instance.currentLevel.PlanetName + "Level").Split(' ')[1].Trim().ToLower();
		
		// Convert whitelist to lowercase and sort it
		var whiteList = moonsWhiteList.Select(levelType => levelType.ToLowerInvariant()).ToArray();
		Array.Sort(whiteList);
		
		// Function to check if an item exists in the sorted whitelist using binary search
		bool IsInWhiteList(string item) {
			return Array.BinarySearch(whiteList, item) >= 0;
		}

		// Check if "all" is in the whitelist
		if (IsInWhiteList("all")) return true;
		
		// Determine if the current level is a vanilla moon
		bool isVanillaMoon = Array.Exists(StartOfRound.Instance.levels, level => level.Equals(StartOfRound.Instance.currentLevel));
		
		// Check for vanilla moon conditions
		if (isVanillaMoon) {
			if (IsInWhiteList("vanilla")) return true;
			if (IsInWhiteList(currentLevelName)) return true;
		}

		// Check for custom moon conditions
		if (IsInWhiteList("custom")) return true;
		
		// Check for custom level name if LethalLevelLoader is on
		if (Plugin.LethalLevelLoaderIsOn) {
			currentLevelName = LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName.ToLower();
			Plugin.Logger.LogInfo($"Current level name: {currentLevelName}");
			return IsInWhiteList(currentLevelName);
		}
		
		// If none of the conditions match, return false
		return false;
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