using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using CodeRebirth.MapStuff;
using CodeRebirth.Util.Extensions;
using CodeRebirth.Util.Spawning;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.Util;
using System.Text.RegularExpressions;
using UnityEngine.AI;
using System.Diagnostics;
using Random = System.Random;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch {
	internal static List<SpawnableFlora> spawnableFlora = [];

	internal static float PATCH_SIZE = 15;
	
	[HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards)), HarmonyPostfix]
	static void SpawnOutsideMapObjects() {
		if (Plugin.ModConfig.ConfigFloraEnabled.Value) SpawnFlora();
        
		if (!RoundManager.Instance.IsHost) return;
		if (Plugin.ModConfig.ConfigItemCrateEnabled.Value) SpawnCrates();
		if (Plugin.ModConfig.ConfigBiomesEnabled.Value) SpawnRandomBiomes();
	}

	static void SpawnFlora() {
		Plugin.ExtendedLogging("Spawning flora!!!");
		System.Random random = new(StartOfRound.Instance.randomMapSeed + 2358);
		int spawnCount = 0;
        
		Stopwatch timer = new();
		timer.Start();
		// Create a dictionary mapping FloraTag to the corresponding moonsWhiteList
		var tagToMoonLists = spawnableFlora
			.GroupBy(flora => flora.floraTag)
			.ToDictionary(
				g => g.Key,
				g => new
				{
					MoonsWhiteList = g.First().moonsWhiteList,
					MoonsBlackList = g.First().moonsBlackList
				}
			);

		// Cache the valid tags based on the current moon configuration
		Dictionary<FloraTag, bool> validTags = new Dictionary<FloraTag, bool>();
		foreach (var tag in tagToMoonLists.Keys) {
			if (tagToMoonLists.TryGetValue(tag, out var moonLists))
			{
				bool isLevelValid = IsCurrentMoonInConfig(moonLists.MoonsWhiteList, moonLists.MoonsBlackList);
				validTags[tag] = isLevelValid;
			}
		}

		

		var sortedFlora = spawnableFlora.GroupBy(flora => flora.floraTag).ToList();
		

		foreach(IGrouping<FloraTag, SpawnableFlora> floraGroup in sortedFlora) {
			Plugin.Logger.LogDebug("starting patch");
			if (!validTags.TryGetValue(floraGroup.Key, out bool isLevelValid) || !isLevelValid) {
				continue;
			}

			int patches = random.NextInt(7, 10);
			Plugin.Logger.LogDebug("valid");

			for (int i = 0; i < patches; i++) {
				Vector3 patchOrigin = GetPatchOrigin(random);
				List<SpawnableFlora> floraList = floraGroup.ToList();

				int variety = Mathf.Min(random.NextInt(4, 6), floraList.Count - 1);

				Plugin.Logger.LogDebug($"vairety: {variety}");


				for (int j = 0; j < variety; j++) {
					SpawnableFlora flora = floraList[j];

					var targetSpawns = flora.spawnCurve.Evaluate(random.NextFloat(0, 1));
					for (int k = 0; k < targetSpawns; k++) {
						Vector3 randomPosition = patchOrigin + new Vector3(random.NextFloat(-PATCH_SIZE, PATCH_SIZE), 0, random.NextFloat(-PATCH_SIZE, PATCH_SIZE));

						// Use NavMesh.SamplePosition to get the nearest point on the NavMesh
						if (NavMesh.SamplePosition(randomPosition, out NavMeshHit navMeshHit, PATCH_SIZE*2, NavMesh.AllAreas)) {
							Vector3 navMeshPosition = navMeshHit.position;
							Vector3 vector = navMeshPosition + (Vector3.up * 2);

							if (Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) {
								bool isValid = true;
								foreach (string floorTag in flora.blacklistedTags) {
									if (hit.transform.gameObject.CompareTag(floorTag)) {
										isValid = false;
										break;
									}
								}

								if (!isValid) continue;

								Vector3 spawnPosition = hit.point;
								Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

								GameObject spawnedFlora = GameObject.Instantiate(flora.prefab, spawnPosition, rotation, RoundManager.Instance.mapPropsContainer.transform);
								spawnedFlora.transform.up = hit.normal;
								spawnCount++;
							}
						}
					}
				}
			}

		}
		timer.Stop();
		Plugin.ExtendedLogging($"Spawned {spawnCount} flora in {timer.ElapsedTicks} ticks and {timer.ElapsedMilliseconds}ms");
	}

	static Vector3 GetPatchOrigin(Random random) {
		Vector3 patchOrigin = GetRandomPointNearPointsOfInterest(random) + new Vector3(random.NextFloat(-5, 5), 0, random.NextFloat(-5, 5));
		NavMeshHit hit = new();
		while (!NavMesh.SamplePosition(patchOrigin, out hit, 20, NavMesh.AllAreas)) {
			patchOrigin = GetRandomPointNearPointsOfInterest(random) + new Vector3(random.NextFloat(-5, 5), 0, random.NextFloat(-5, 5));
		}

		Physics.Raycast(hit.position + (Vector3.up * 2), Vector3.down, out RaycastHit rayHit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

		return rayHit.point;
	}

	public static Vector3 GetRandomPointNearPointsOfInterest(System.Random random) {
		// Get all points of interest
		Vector3[] pointsOfInterest = RoundManager.Instance.outsideAINodes.Select(node => node.transform.position).ToArray();
		
		// Choose a random point of interest
		Vector3 chosenPoint = pointsOfInterest[random.NextInt(0, pointsOfInterest.Length-1)];
		
		// Calculate an offset to avoid too much bunching up
		Vector3 offset = new Vector3(random.NextFloat(-20, 20), 0, random.NextFloat(-20, 20));
		return chosenPoint + offset;
	}

	public static Vector3 GetRandomNavMeshPosition(Vector3 center, float range, System.Random random) {
		for (int i = 0; i < 30; i++) { // Try up to 30 times to find a valid position
			Vector3 randomPos = center + new Vector3(random.NextFloat(-range, range), 0, random.NextFloat(-range, range));
			if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, range, NavMesh.AllAreas)) {
				return hit.position;
			}
		}
		return center; // Fallback to the center if no valid position found
	}

	public static bool IsCurrentMoonInConfig(string[] moonsWhiteList, string[] moonsBlackList) {
		// Prepare the current level name
		string currentLevelName = Regex.Replace(StartOfRound.Instance.currentLevel.PlanetName, "^(?:\\d+ )*(.*)", "$1Level").ToLowerInvariant();
		string currentLLLLevelName = LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName.ToLower();
		// Convert whitelist and blacklist to lowercase and sort them
		var whiteList = moonsWhiteList.Select(levelType => levelType.ToLowerInvariant()).ToArray();
		var blackList = moonsBlackList.Select(levelType => levelType.ToLowerInvariant()).ToArray();
		Array.Sort(whiteList);
		Array.Sort(blackList);

		// Function to check if an item exists in the sorted list using binary search
		bool IsInList(string item, string[] list) {
			return Array.BinarySearch(list, item) >= 0;
		}

		// Check if "all" is in the whitelist
		if (IsInList("all", whiteList)) return true;
		
		bool isVanillaMoon = LethalLevelLoader.PatchedContent.VanillaExtendedLevels.Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel));

		// Check blacklist first
		if (IsInList(currentLevelName, blackList) || IsInList(currentLLLLevelName, blackList)) return false;

		// Check for vanilla moon conditions
		if (isVanillaMoon) {
			if (IsInList("vanilla", whiteList)) return true;
			if (IsInList(currentLevelName, whiteList)) return true;
			return false;
		}

		// Check for custom moon conditions
		if (IsInList("custom", whiteList)) return true;

		// Check for custom level name
		return IsInList(currentLLLLevelName, whiteList);
	}

	static void SpawnCrates() {
		Plugin.ExtendedLogging("Spawning crates!!!");
		System.Random random = new();
		int minValue = 0;
		for (int i = 0; i < random.NextInt(minValue, Mathf.Clamp(Plugin.ModConfig.ConfigCrateAbundance.Value, minValue, 1000)); i++) {
			Vector3 position = RoundManager.Instance.outsideAINodes[random.NextInt(0, RoundManager.Instance.outsideAINodes.Length-1)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

			GameObject crate = random.NextBool() ? MapObjectHandler.Instance.Crate.MetalCratePrefab : MapObjectHandler.Instance.Crate.ItemCratePrefab;
			
			GameObject spawnedCrate = GameObject.Instantiate(crate, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
			Plugin.ExtendedLogging($"Spawning {crate.name} at {hit.point}");
			spawnedCrate.transform.up = hit.normal;
			spawnedCrate.GetComponent<NetworkObject>().Spawn();
		}
	}
	
	static void SpawnRandomBiomes() {
		Plugin.ExtendedLogging("Spawning Biome/s!!!");
		System.Random random = new();
		int minValue = 1;
		for (int i = 0; i < random.NextInt(minValue, 1); i++) {
			Vector3 position = RoundManager.Instance.outsideAINodes[random.NextInt(0, RoundManager.Instance.outsideAINodes.Length-1)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1);

			GameObject biome = MapObjectHandler.Instance.Biome.BiomePrefab;
			
			GameObject spawnedBiome = GameObject.Instantiate(biome, vector, Quaternion.identity);
			Plugin.ExtendedLogging($"Spawning biome at {vector}");
			spawnedBiome.GetComponent<NetworkObject>().Spawn();
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
			Plugin.ExtendedLogging("Spawning Devil deal objects");
			if (RoundManager.Instance.IsServer) CodeRebirthUtils.Instance.SpawnDevilPropsServerRpc();
		}
	}

	[HarmonyPatch("DespawnPropsAtEndOfRound")]
	[HarmonyPostfix]
	public static void DespawnPropsAtEndOfRoundPatch(RoundManager __instance)
	{
		if (__instance.currentLevel.levelID == 3 && TimeOfDay.Instance.daysUntilDeadline == 0)
		{
			Plugin.ExtendedLogging("Despawning Devil deal objects");
			if (RoundManager.Instance.IsServer) CodeRebirthUtils.Instance.DespawnDevilPropsServerRpc();
		}
	}*/

	[HarmonyPostfix]
	[HarmonyPatch(typeof(StartOfRound), "OnShipLandedMiscEvents")]
	public static void OnShipLandedMiscEventsPatch(StartOfRound __instance)
	{
		Plugin.ExtendedLogging("Starting big object search");

		Stopwatch timer = new();
		timer.Start();
		var objs = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		int FoundObject = 0;
		LayerMask layerMask = LayerMask.NameToLayer("Foliage");
		foreach (var item in objs)
		{
			if (item.layer == layerMask)
			{
				item.AddComponent<BoxCollider>().isTrigger = true;
				FoundObject++;
			}
		}

		timer.Stop();

		Plugin.ExtendedLogging($"Run completed in {timer.ElapsedTicks} ticks and {timer.ElapsedMilliseconds}ms and found {FoundObject} objects out of {objs.Length}");
	}
}