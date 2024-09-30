using System;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Util.Extensions;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.AI;
using System.Diagnostics;
using Random = System.Random;
using CodeRebirth.src.Content.Unlockables;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch {
	internal static List<SpawnableFlora> spawnableFlora = [];
    
	[HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards)), HarmonyPostfix]
	private static void SpawnOutsideMapObjects()
	{
		if (Plugin.ModConfig.ConfigFloraEnabled.Value) SpawnFlora();
        
		if (!RoundManager.Instance.IsHost) return;
		if (Plugin.ModConfig.ConfigItemCrateEnabled.Value) SpawnCrates();
		if (Plugin.ModConfig.ConfigBiomesEnabled.Value) SpawnRandomBiomes();
	}

	private static void SpawnFlora()
	{
		Plugin.ExtendedLogging("Spawning flora!!!");
		System.Random random = new(StartOfRound.Instance.randomMapSeed + 2358);
		int spawnCount = 0;
		
		Stopwatch timer = new();
		timer.Start();
		
		var validFlora = GetValidFlora();

		foreach (var tagGroup in validFlora)
		{
			foreach (SpawnableFlora flora in tagGroup)
			{
				SpawnFlora(random, flora, ref spawnCount);
			}
		}
		
		timer.Stop();
		Plugin.ExtendedLogging($"Spawned {spawnCount} flora in {timer.ElapsedTicks} ticks and {timer.ElapsedMilliseconds}ms");
	}

	private static void SpawnFlora(Random random, SpawnableFlora flora, ref int spawnCount)
	{
		var targetSpawns = flora.spawnCurve.Evaluate(random.NextFloat(0, 1));
		for (int i = 0; i < targetSpawns; i++)
		{
			if(!TryGetValidFloraSpawnPoint(random, out RaycastHit hit))
				continue; // spawn failed
			
			bool isValid = true;

			foreach (string floorTag in flora.blacklistedTags)
			{
				if (hit.transform.gameObject.CompareTag(floorTag))
				{
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

	private static bool TryGetValidFloraSpawnPoint(Random random, out RaycastHit hit)
	{
		Vector3 basePosition = GetRandomPointNearPointsOfInterest(random, 20);
		Vector3 randomPosition = basePosition;

		hit = default;

		if (!NavMesh.SamplePosition(randomPosition, out NavMeshHit navMeshHit, 20f, NavMesh.AllAreas))
			return false;
		
		
		Vector3 navMeshPosition = navMeshHit.position;
		Vector3 vector = navMeshPosition;
		if (!Physics.Raycast(vector, Vector3.down, out hit, 150, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) 
			return false;
		return true;
	}

	// todo: change this so that it takes a moon's content tag and checks whether it fits for custom moons and vanilla moons a specific tag using FloraTag.
	private static IEnumerable<IGrouping<FloraTag, SpawnableFlora>> GetValidFlora()
	{
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
		Dictionary<FloraTag, bool> validTags = new();
		foreach (var tag in tagToMoonLists.Keys)
		{
			if (tagToMoonLists.TryGetValue(tag, out var moonLists))
			{
				bool isLevelValid = IsCurrentMoonInConfig(moonLists.MoonsWhiteList, moonLists.MoonsBlackList);
				validTags[tag] = isLevelValid;
			}
		}

		return spawnableFlora.GroupBy(flora => flora.floraTag).Where(it => validTags.TryGetValue(it.Key, out bool isLevelValid) && isLevelValid);
	}

	public static Vector3 GetRandomPointNearPointsOfInterest(System.Random random, float offsetRange = 20f) {
		// Get all points of interest
		Vector3[] pointsOfInterest = RoundManager.Instance.outsideAINodes
									.Select(node => node.transform.position)
									.ToArray();
		
		// Check if there are any points of interest
		if (pointsOfInterest.Length == 0) {
			Plugin.Logger.LogWarning("No points of interest found.");
			return Vector3.zero; // Return default if no points exist
		}

		// Choose a random point of interest
		Vector3 chosenPoint = pointsOfInterest[random.Next(0, pointsOfInterest.Length)];

		// Generate a random offset within the specified range
		Vector3 offset = new Vector3(
			random.NextFloat(-offsetRange, offsetRange),
			random.NextFloat(0, offsetRange), 
			random.NextFloat(-offsetRange, offsetRange)
		);
		
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
		System.Random random = new ();
		for (int i = 0; i < random.NextInt(0, Mathf.Clamp(Plugin.ModConfig.ConfigWoodenCrateAbundance.Value, 0, 1000)); i++)
		{
			Vector3 position = RoundManager.Instance.outsideAINodes[random.NextInt(0, RoundManager.Instance.outsideAINodes.Length-1)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

			GameObject crate = MapObjectHandler.Instance.Crate.WoodenCratePrefab;
			
			GameObject spawnedCrate = GameObject.Instantiate(crate, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
			Plugin.ExtendedLogging($"Spawning {crate.name} at {hit.point}");
			spawnedCrate.transform.up = hit.normal;
			spawnedCrate.GetComponent<NetworkObject>().Spawn();
		}

		for (int i = 0; i < random.NextInt(0, Mathf.Clamp(Plugin.ModConfig.ConfigMetalCrateAbundance.Value, 0, 1000)); i++) {
			Vector3 position = RoundManager.Instance.outsideAINodes[random.NextInt(0, RoundManager.Instance.outsideAINodes.Length-1)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

			GameObject crate = MapObjectHandler.Instance.Crate.MetalCratePrefab;
			
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

		foreach (BiomeManager biome in GameObject.FindObjectsOfType<BiomeManager>()) {
			biome.NetworkObject.Despawn();
		}
	}

	

	[HarmonyPatch(nameof(RoundManager.PlayAudibleNoise)), HarmonyPostfix]
	public static void PlayAudibleNoiseForShockwaveGalPostfix(RoundManager __instance, ref Vector3 noisePosition, ref float noiseRange, ref float noiseLoudness, ref int timesPlayedInSameSpot, ref bool noiseIsInsideClosedShip, ref int noiseID)
	{
		if (noiseID != 5) return;
		if (noiseIsInsideClosedShip)
		{
			noiseRange /= 2f;
		}
		Collider[] hitColliders = Physics.OverlapSphere(noisePosition, noiseRange, LayerMask.GetMask("Props"));
		for (int i = 0; i < hitColliders.Length; i++)
		{
			INoiseListener noiseListener;
			if (hitColliders[i].TryGetComponent<INoiseListener>(out noiseListener))
			{
				ShockwaveGalAI component = hitColliders[i].gameObject.GetComponent<ShockwaveGalAI>();
				if (component == null)
				{
					return;
				}
				noiseListener.DetectNoise(noisePosition, noiseLoudness, timesPlayedInSameSpot, noiseID);
			}
		}
	}

	internal static void Init()
    {
        IL.RoundManager.SpawnOutsideHazards += ILHook_RoundManager_SpawnOutsideHazards;
    }

    private static void ILHook_RoundManager_SpawnOutsideHazards(ILContext il)
    {
        ILCursor c = new(il);
        
        // Make sure we are at the second for loop which uses `spawnDenialPoints`

        // IL_02e8: ldfld class [UnityEngine.CoreModule]UnityEngine.GameObject[] RoundManager::spawnDenialPoints /* 04000AEB */
        // IL_02ed: ldloc.s 18      // Also known as int n, used in for loop

        if (!c.TryGotoNext(MoveType.After,
            x => x.MatchLdfld<RoundManager>(nameof(RoundManager.spawnDenialPoints)),
            x => x.MatchLdloc(18)
        ))
        {
            Plugin.Logger.LogError($"[{nameof(ILHook_RoundManager_SpawnOutsideHazards)}] Could not match first predicates!");
            return;
        }

        // Now we can find the logic for the float argument for the Vector3.Distance: 

        // IL_0300: ldarg.0
        // IL_0301: ldfld class SelectableLevel RoundManager::currentLevel /* 04000B04 */
        // IL_0306: ldfld class SpawnableOutsideObjectWithRarity[] SelectableLevel::spawnableOutsideObjects /* 040010E1 */
        // IL_030b: ldloc.s 9
        // IL_030d: ldelem.ref
        // IL_030e: ldfld class SpawnableOutsideObject SpawnableOutsideObjectWithRarity::spawnableObject /* 040010FB */
        // IL_0313: ldfld int32 SpawnableOutsideObject::objectWidth /* 04001106 */
        // IL_0318: conv.r4

        if (!c.TryGotoNext(MoveType.Before,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<RoundManager>(nameof(RoundManager.currentLevel)),
            x => x.MatchLdfld<SelectableLevel>(nameof(SelectableLevel.spawnableOutsideObjects)),
            x => x.MatchLdloc(9),
            x => x.MatchLdelemRef(),
            x => x.MatchLdfld<SpawnableOutsideObjectWithRarity>(nameof(SpawnableOutsideObjectWithRarity.spawnableObject)),
            x => x.MatchLdfld<SpawnableOutsideObject>(nameof(SpawnableOutsideObject.objectWidth)),
            x => x.MatchConvR4(),
            x => x.MatchLdcR4(6),
            x => x.MatchAdd()
        ))
        {
            Plugin.Logger.LogError($"[{nameof(ILHook_RoundManager_SpawnOutsideHazards)}] Could not match second predicates!");
            return;
        }

        // Find the end of the previous match
        ILCursor cAtEnd = new ILCursor(c).GotoNext(MoveType.After,
            x => x.MatchLdcR4(6),
            x => x.MatchAdd());

        ILLabel label_original_logic = il.DefineLabel(c.Next);
        ILLabel label_past_original_logic = il.DefineLabel(cAtEnd.Next);

        //      if !thing
        //          goto original_logic;
        //
        //      custom thing;
        //      goto past_original_logic;
        //
        //  original_logic:
        //      original thing;
        //
        //  past_original_logic:

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc_S, (byte)18); // int n, used in for loop
        c.EmitDelegate<Func<RoundManager, int, bool>>((self, n) =>
        {
            return self.spawnDenialPoints[n].gameObject.name.Contains("_XuPatch");
        });

        // If the previous boolean is false, jump over our custom logic
        c.Emit(OpCodes.Brfalse_S, label_original_logic);

        // We emit our custom logic here
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc_S, (byte)18); // int n, used in for loop
        c.EmitDelegate<Func<RoundManager, int, float>>((self, n) =>
        {
            return self.spawnDenialPoints[n].transform.localScale.x;
        });

        c.Emit(OpCodes.Br_S, label_past_original_logic);
    }
}