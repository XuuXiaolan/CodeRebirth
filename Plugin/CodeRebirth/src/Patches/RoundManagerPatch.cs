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
using Random = System.Random;
using CodeRebirth.src.Content.Unlockables;

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
		if (Plugin.ModConfig.ConfigBearTrapEnabled.Value) SpawnBearTrap();
		if (Plugin.ModConfig.ConfigAirControlUnitEnabled.Value) SpawnAirControlUnit();
	}

	private static void SpawnAirControlUnit()
	{
		Plugin.ExtendedLogging("Spawning air control unit!!!");

		// Parse the configuration string to get the spawn counts for different moons
		Dictionary<string, int> moonSpawnCounts = ParseMoonSpawnConfig(Plugin.ModConfig.ConfigAirControlUnitSpawnWeight.Value);
		foreach (var moonSpawn in moonSpawnCounts.Keys)
		{
			Plugin.ExtendedLogging($"Moon {moonSpawn} spawn count: {moonSpawnCounts[moonSpawn]}");
		}

		// Get the current moon type
		string currentMoon = LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName.ToLowerInvariant();
		Plugin.ExtendedLogging("Current moon: " + currentMoon);

        // Determine the spawn count based on the current moon configuration

        if (!moonSpawnCounts.TryGetValue(currentMoon, out int spawnCount)) // Try to get the specific moon spawn count
        {
            if (!moonSpawnCounts.TryGetValue("all", out spawnCount)) // If not found, try to get the "all" spawn count
            {
                // Determine if it is a vanilla or custom moon and get the appropriate spawn count
                bool isVanillaMoon = LethalLevelLoader.PatchedContent.VanillaExtendedLevels
                    .Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel));

                if (isVanillaMoon)
                {
                    moonSpawnCounts.TryGetValue("vanilla", out spawnCount);
                }
                else
                {
                    moonSpawnCounts.TryGetValue("custom", out spawnCount);
                }
            }
        }

        // Log the determined spawn count
        Plugin.ExtendedLogging($"Determined spawn count for moon '{currentMoon}': {spawnCount}");

		// If no valid spawn count is found, return
		if (spawnCount <= 0) return;

		// Check if the current moon configuration is valid
		System.Random random = new();
		Plugin.ExtendedLogging($"Spawning {spawnCount} air control units");
		for (int i = 0; i < random.NextInt(0, Mathf.Clamp(spawnCount, 0, 1000)); i++)
		{
			Vector3 position = RoundManager.Instance.outsideAINodes[random.NextInt(0, RoundManager.Instance.outsideAINodes.Length - 1)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

			if (hit.collider != null) // Check to make sure we hit something
			{
				GameObject aircontrolunit = MapObjectHandler.Instance.AirControlUnit.AirControlUnitPrefab;

				GameObject spawnedAirControlUnit = GameObject.Instantiate(aircontrolunit, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
				Plugin.ExtendedLogging($"Spawning air control unit at: {hit.point}");
				spawnedAirControlUnit.transform.up = hit.normal;
				spawnedAirControlUnit.GetComponent<NetworkObject>().Spawn();
			}
		}
	}

    private static Dictionary<string, int> ParseMoonSpawnConfig(string config)
    {
        Dictionary<string, int> moonSpawnCounts = new();

        if (string.IsNullOrEmpty(config)) return moonSpawnCounts;

        string[] entries = config.Split(',');
        foreach (string entry in entries)
        {
            string[] parts = entry.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int count))
            {
                string key = parts[0].Trim().ToLowerInvariant();
                if (key.Equals("modded", StringComparison.OrdinalIgnoreCase))
                {
                    key = "custom";
                }
                moonSpawnCounts[key] = count;
            }
        }

        return moonSpawnCounts;
    }

    private static void SpawnBearTrap()
    {
        Plugin.ExtendedLogging("Spawning bear trap!!!");

        // Parse the configuration string to get the spawn counts for different moons
        Dictionary<string, int> moonSpawnCounts = ParseMoonSpawnConfig(Plugin.ModConfig.ConfigBearTrapSpawnWeight.Value);
        foreach (var moonSpawn in moonSpawnCounts.Keys)
        {
            Plugin.ExtendedLogging($"Moon {moonSpawn} spawn count: {moonSpawnCounts[moonSpawn]}");
        }

        // Get the current moon type
        string currentMoon = LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName.ToLowerInvariant();
        Plugin.ExtendedLogging("Current moon: " + currentMoon);

        // Determine the spawn count based on the current moon configuration
        if (!moonSpawnCounts.TryGetValue(currentMoon, out int spawnCount)) // Try to get the specific moon spawn count
        {
            if (!moonSpawnCounts.TryGetValue("all", out spawnCount)) // If not found, try to get the "all" spawn count
            {
                // Determine if it is a vanilla or custom moon and get the appropriate spawn count
                bool isVanillaMoon = LethalLevelLoader.PatchedContent.VanillaExtendedLevels
                    .Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel));

                if (isVanillaMoon)
                {
                    moonSpawnCounts.TryGetValue("vanilla", out spawnCount);
                }
                else
                {
                    moonSpawnCounts.TryGetValue("custom", out spawnCount);
                }
            }
        }

        // Log the determined spawn count
        Plugin.ExtendedLogging($"Determined spawn count for moon '{currentMoon}': {spawnCount}");

        // If no valid spawn count is found, return
        if (spawnCount <= 0) return;

        // Check if the current moon configuration is valid
        System.Random random = new();
        Plugin.ExtendedLogging($"Spawning {spawnCount} bear traps");
        for (int i = 0; i < random.NextInt(0, Mathf.Clamp(spawnCount, 0, 1000)); i++)
        {
            Vector3 position = RoundManager.Instance.outsideAINodes[random.NextInt(0, RoundManager.Instance.outsideAINodes.Length - 1)].transform.position;
            Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

            Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

            if (hit.collider != null) // Check to make sure we hit something
            {
                GameObject beartrap = MapObjectHandler.Instance.BearTrap.GravelMatPrefab;
                if (hit.collider.CompareTag("Grass"))
                {
                    beartrap = MapObjectHandler.Instance.BearTrap.GrassMatPrefab;
                }
                else if (hit.collider.CompareTag("Gravel"))
                {
                    beartrap = MapObjectHandler.Instance.BearTrap.GravelMatPrefab;
                }
                else if (hit.collider.CompareTag("Snow"))
                {
                    beartrap = MapObjectHandler.Instance.BearTrap.SnowMatPrefab;
                }

                GameObject spawnedTrap = GameObject.Instantiate(beartrap, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
                Plugin.ExtendedLogging($"Spawning {beartrap.name} at {hit.point}");
                spawnedTrap.transform.up = hit.normal;
                spawnedTrap.GetComponent<NetworkObject>().Spawn();
            }
        }
    }

	private static void SpawnFlora()
	{
		Plugin.ExtendedLogging("Spawning flora!!!");
		System.Random random = new(StartOfRound.Instance.randomMapSeed + 2358);
		int spawnCount = 0;
		
		var validFlora = GetValidFlora();

		foreach (var tagGroup in validFlora)
		{
			foreach (SpawnableFlora flora in tagGroup)
			{
				SpawnFlora(random, flora, ref spawnCount);
			}
		}
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
			switch (flora.floraTag)
			{
				case FloraTag.Grass:
					if (!hit.transform.gameObject.CompareTag("Grass"))
						continue;
					break;
				case FloraTag.Desert:
					if (!hit.transform.gameObject.CompareTag("Gravel"))
						continue;
					break;
				case FloraTag.Snow:
					if (!hit.transform.gameObject.CompareTag("Snow"))
						continue;
					break;
			}

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

	public static Vector3 GetRandomPointNearPointsOfInterest(System.Random random, float offsetRange = 20f)
	{
		// Get all points of interest
		Vector3[] pointsOfInterest = RoundManager.Instance.outsideAINodes
									.Select(node => node.transform.position)
									.ToArray();
		
		// Check if there are any points of interest
		if (pointsOfInterest.Length == 0)
		{
			Plugin.Logger.LogWarning("No points of interest found.");
			return Vector3.zero; // Return default if no points exist
		}

		// Choose a random point of interest
		Vector3 chosenPoint = pointsOfInterest[random.Next(0, pointsOfInterest.Length)];

		// Generate a random offset within the specified range
		Vector3 offset = new(
			random.NextFloat(-offsetRange, offsetRange),
			random.NextFloat(0, offsetRange), 
			random.NextFloat(-offsetRange, offsetRange)
		);
		
		return chosenPoint + offset;
	}

	public static Vector3 GetRandomNavMeshPosition(Vector3 center, float range, System.Random random)
	{
		for (int i = 0; i < 30; i++)
		{ // Try up to 30 times to find a valid position
			Vector3 randomPos = center + new Vector3(random.NextFloat(-range, range), 0, random.NextFloat(-range, range));
			if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, range, NavMesh.AllAreas))
			{
				return hit.position;
			}
		}
		return center; // Fallback to the center if no valid position found
	}

	public static bool IsCurrentMoonInConfig(string[] moonsWhiteList, string[] moonsBlackList)
	{
		// Prepare the current level name
		string currentLevelName = Regex.Replace(StartOfRound.Instance.currentLevel.PlanetName, "^(?:\\d+ )*(.*)", "$1Level").ToLowerInvariant();
		string currentLLLLevelName = LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName.ToLower();
		// Convert whitelist and blacklist to lowercase and sort them
		var whiteList = moonsWhiteList.Select(levelType => levelType.ToLowerInvariant()).ToArray();
		var blackList = moonsBlackList.Select(levelType => levelType.ToLowerInvariant()).ToArray();
		Array.Sort(whiteList);
		Array.Sort(blackList);

        // Function to check if an item exists in the sorted list using binary search
        static bool IsInList(string item, string[] list)
		{
			return Array.BinarySearch(list, item) >= 0;
		}

		// Check if "all" is in the whitelist
		if (IsInList("all", whiteList)) return true;
		
		bool isVanillaMoon = LethalLevelLoader.PatchedContent.VanillaExtendedLevels.Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel));

		// Check blacklist first
		if (IsInList(currentLevelName, blackList) || IsInList(currentLLLLevelName, blackList)) return false;

		// Check for vanilla moon conditions
		if (isVanillaMoon)
		{
			if (IsInList("vanilla", whiteList)) return true;
			if (IsInList(currentLevelName, whiteList)) return true;
			return false;
		}

		// Check for custom moon conditions
		if (IsInList("custom", whiteList)) return true;

		// Check for custom level name
		return IsInList(currentLLLLevelName, whiteList);
	}

	private static void SpawnCrates()
	{
		SpawnMetalCrates();
		SpawnWoodenCrates();
	}

	private static void SpawnWoodenCrates()
	{
		Plugin.ExtendedLogging("Spawning Wooden Crate!!!");

        // Parse the configuration string to get the spawn counts for different moons
        Dictionary<string, int> moonSpawnCounts = ParseMoonSpawnConfig(Plugin.ModConfig.ConfigWoodenCrateSpawnWeight.Value);
        foreach (var moonSpawn in moonSpawnCounts.Keys)
        {
            Plugin.ExtendedLogging($"Moon {moonSpawn} spawn count: {moonSpawnCounts[moonSpawn]}");
        }

        // Get the current moon type
        string currentMoon = LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName.ToLowerInvariant();
        Plugin.ExtendedLogging("Current moon: " + currentMoon);

        // Determine the spawn count based on the current moon configuration
        if (!moonSpawnCounts.TryGetValue(currentMoon, out int spawnCount)) // Try to get the specific moon spawn count
        {
            if (!moonSpawnCounts.TryGetValue("all", out spawnCount)) // If not found, try to get the "all" spawn count
            {
                // Determine if it is a vanilla or custom moon and get the appropriate spawn count
                bool isVanillaMoon = LethalLevelLoader.PatchedContent.VanillaExtendedLevels
                    .Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel));

                if (isVanillaMoon)
                {
                    moonSpawnCounts.TryGetValue("vanilla", out spawnCount);
                }
                else
                {
                    moonSpawnCounts.TryGetValue("custom", out spawnCount);
                }
            }
        }

        // Log the determined spawn count
        Plugin.ExtendedLogging($"Determined spawn count for moon '{currentMoon}': {spawnCount}");

        // If no valid spawn count is found, return
        if (spawnCount <= 0) return;

        // Check if the current moon configuration is valid
        System.Random random = new();
        Plugin.ExtendedLogging($"Spawning {spawnCount} Wooden crates");
		for (int i = 0; i < spawnCount; i++)
		{
			Vector3 position = RoundManager.Instance.outsideAINodes[random.NextInt(0, RoundManager.Instance.outsideAINodes.Length - 1)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

			if (hit.collider != null) // Check to make sure we hit something
			{
				GameObject crate = MapObjectHandler.Instance.Crate.WoodenCratePrefab;

				// Adjust the hit point deeper into the ground along the hit.normal direction
				Vector3 spawnPoint = hit.point + hit.normal * -0.6f; // Adjust -0.6f to control how deep you want it

				GameObject spawnedCrate = GameObject.Instantiate(crate, spawnPoint, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
				Plugin.ExtendedLogging($"Spawning {crate.name} at {spawnPoint}");
				spawnedCrate.transform.up = hit.normal;
				spawnedCrate.GetComponent<NetworkObject>().Spawn();
			}
		}
	}

	private static void SpawnMetalCrates()
	{
		Plugin.ExtendedLogging("Spawning Metal Crate!!!");

        // Parse the configuration string to get the spawn counts for different moons
        Dictionary<string, int> moonSpawnCounts = ParseMoonSpawnConfig(Plugin.ModConfig.ConfigMetalCrateSpawnWeight.Value);
        foreach (var moonSpawn in moonSpawnCounts.Keys)
        {
            Plugin.ExtendedLogging($"Moon {moonSpawn} spawn count: {moonSpawnCounts[moonSpawn]}");
        }

        // Get the current moon type
        string currentMoon = LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName.ToLowerInvariant();
        Plugin.ExtendedLogging("Current moon: " + currentMoon);

        // Determine the spawn count based on the current moon configuration
        if (!moonSpawnCounts.TryGetValue(currentMoon, out int spawnCount)) // Try to get the specific moon spawn count
        {
            if (!moonSpawnCounts.TryGetValue("all", out spawnCount)) // If not found, try to get the "all" spawn count
            {
                // Determine if it is a vanilla or custom moon and get the appropriate spawn count
                bool isVanillaMoon = LethalLevelLoader.PatchedContent.VanillaExtendedLevels
                    .Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel));

                if (isVanillaMoon)
                {
                    moonSpawnCounts.TryGetValue("vanilla", out spawnCount);
                }
                else
                {
                    moonSpawnCounts.TryGetValue("custom", out spawnCount);
                }
            }
        }

        // Log the determined spawn count
        Plugin.ExtendedLogging($"Determined spawn count for moon '{currentMoon}': {spawnCount}");

        // If no valid spawn count is found, return
        if (spawnCount <= 0) return;

        // Check if the current moon configuration is valid
        System.Random random = new();
        Plugin.ExtendedLogging($"Spawning {spawnCount} metal crates");
		for (int i = 0; i < spawnCount; i++)
		{
			Vector3 position = RoundManager.Instance.outsideAINodes[random.NextInt(0, RoundManager.Instance.outsideAINodes.Length - 1)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1) + (Vector3.up * 2);

			Physics.Raycast(vector, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault);

			if (hit.collider != null) // Check to make sure we hit something
			{
				GameObject crate = MapObjectHandler.Instance.Crate.MetalCratePrefab;

				// Adjust the hit point deeper into the ground along the hit.normal direction
				Vector3 spawnPoint = hit.point + hit.normal * -1.1f; // Adjust -1.2f to control how deep you want it

				GameObject spawnedCrate = GameObject.Instantiate(crate, spawnPoint, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
				Plugin.ExtendedLogging($"Spawning {crate.name} at {spawnPoint}");
				spawnedCrate.transform.up = hit.normal;
				spawnedCrate.GetComponent<NetworkObject>().Spawn();
			}
		}
	}

	private static void SpawnRandomBiomes()
	{
		Plugin.ExtendedLogging("Spawning Biome/s!!!");
		System.Random random = new();
		if (random.NextFloat(0f, 1f) <= Plugin.ModConfig.ConfigBiomesSpawnChance.Value) return;
		int minValue = 1;
		for (int i = 0; i < random.NextInt(minValue, 1); i++)
		{
			Vector3 position = RoundManager.Instance.outsideAINodes[random.NextInt(0, RoundManager.Instance.outsideAINodes.Length-1)].transform.position;
			Vector3 vector = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 10f, default, random, -1);

			GameObject biome = MapObjectHandler.Instance.Biome.BiomePrefab;
			
			GameObject spawnedBiome = GameObject.Instantiate(biome, vector, Quaternion.identity);
			Plugin.ExtendedLogging($"Spawning biome at {vector}");
			spawnedBiome.GetComponent<NetworkObject>().Spawn();
		}
	}

	/*[HarmonyPatch(nameof(RoundManager.SpawnMapObjects)), HarmonyPrefix]
	private static void PostFix_SpawnMapObjects(RoundManager __instance)
	{
		if (!Plugin.ModConfig.ConfigEnableExtendedLogging.Value) return;
		for (int j = 0; j < __instance.currentLevel.spawnableMapObjects.Length; j++)
		{
			Plugin.ExtendedLogging("Deciding spawn stuff for: " + __instance.currentLevel.spawnableMapObjects[j].prefabToSpawn.name);
			for (float i = 0; i <= 100; i++)
			{
				Plugin.ExtendedLogging("Possible Number to spawn for x-value in curve: " + i/100f);
				float numberToSpawnOfThisHazard = __instance.currentLevel.spawnableMapObjects[j].numberToSpawn.Evaluate(i/100f);
				Plugin.ExtendedLogging("Possible number to spawn: " + numberToSpawnOfThisHazard);
			}
		}
		/* for (int i = 0; i < __instance.currentLevel.spawnableMapObjects.Length; i++)
		{
			Plugin.ExtendedLogging("Deciding spawn stuff for: " + __instance.currentLevel.spawnableMapObjects[i].prefabToSpawn.name);
			int numberToSpawnOfThisHazard = (int)__instance.currentLevel.spawnableMapObjects[i].numberToSpawn.Evaluate((float)random.NextDouble());
			Plugin.ExtendedLogging("Possible number to spawn: " + numberToSpawnOfThisHazard);
			foreach (var possibleSpawner in possibleMapObjectSpawners)
			{
				Plugin.ExtendedLogging("Possible spawner: " + possibleSpawner.name);
				if (possibleSpawner.spawnablePrefabs.Contains(__instance.currentLevel.spawnableMapObjects[i].prefabToSpawn))
				{
					Plugin.ExtendedLogging("Adding this to list of Spawners: " + possibleSpawner.spawnablePrefabs + " - Because it contains this prefab: " + __instance.currentLevel.spawnableMapObjects[i].prefabToSpawn.name);
				}
			}
		}
	}*/

	[HarmonyPatch(nameof(RoundManager.UnloadSceneObjectsEarly)), HarmonyPostfix]
	private static void PatchFix_DespawnOldCrates()
	{
		foreach (ItemCrate crate in ItemCrate.Instances)
		{
			crate.NetworkObject.Despawn();
		}

		foreach (BiomeManager biome in BiomeManager.Instances)
		{
			biome.NetworkObject.Despawn();
		}

		foreach (GalAI gal in GalAI.Instances)
		{
			gal.RefillChargesServerRpc();
		}

		foreach (SCP999GalAI gal in SCP999GalAI.Instances)
		{
			gal.MakeTriggerInteractableServerRpc(false);
		}
	}

	[HarmonyPatch(nameof(RoundManager.PlayAudibleNoise)), HarmonyPostfix]
	public static void PlayAudibleNoiseForShockwaveGalPostfix(RoundManager __instance, ref Vector3 noisePosition, ref float noiseRange, ref float noiseLoudness, ref int timesPlayedInSameSpot, ref bool noiseIsInsideClosedShip, ref int noiseID)
	{
		if (noiseID != 5 && noiseID != 6) return;
		if (noiseIsInsideClosedShip)
		{
			noiseRange /= 2f;
		}
		Collider[] hitColliders = Physics.OverlapSphere(noisePosition, noiseRange, LayerMask.GetMask("Props", "MapHazards"), QueryTriggerInteraction.Collide);
		for (int i = 0; i < hitColliders.Length; i++)
		{
            if (hitColliders[i].TryGetComponent<INoiseListener>(out INoiseListener noiseListener))
            {
				GalAI? gal = hitColliders[i].gameObject.GetComponent<GalAI>();
				SCP999GalAI? scp999Gal = hitColliders[i].gameObject.GetComponent<SCP999GalAI>();
				BellCrabGalAI? bellCrabGal = hitColliders[i].gameObject.GetComponent<BellCrabGalAI>();
				FlashTurret? flashTurret = hitColliders[i].gameObject.GetComponent<FlashTurret>();
                if (gal == null && flashTurret == null && scp999Gal == null && bellCrabGal == null)
                {
                    continue;
                }
                noiseListener.DetectNoise(noisePosition, noiseLoudness, timesPlayedInSameSpot, noiseID);
            }
        }
	}

	[HarmonyPatch(nameof(RoundManager.LoadNewLevelWait))]
	[HarmonyPrefix]
	public static void LoadNewLevelWaitPatch(RoundManager __instance)
	{
		if (!NetworkManager.Singleton.IsServer) return;
		if (__instance.currentLevel.levelID == 3 && TimeOfDay.Instance.daysUntilDeadline == 0)
		{
			if (Plugin.ModConfig.Config999GalCompanyMoonRecharge.Value)
			{
				foreach (SCP999GalAI gal in SCP999GalAI.Instances)
				{
					gal.RechargeGalHealsAndRevivesServerRpc(true, true);
				}
			}
		}

		if (!Plugin.ModConfig.Config999GalCompanyMoonRecharge.Value)
		{
			foreach (SCP999GalAI gal in SCP999GalAI.Instances)
			{
				gal.RechargeGalHealsAndRevivesServerRpc(true, true);
			}
		}
	}
}