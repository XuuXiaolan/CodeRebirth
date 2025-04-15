using System;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Util.Extensions;
using HarmonyLib;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.AI;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Util;
using static LethalLib.Modules.MapObjects;
using Unity.Netcode;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch
{
    internal static List<RegisteredCRMapObject> registeredMapObjects = [];
    internal static List<SpawnableFlora> spawnableFlora = [];

    [HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards)), HarmonyPostfix]
    private static void SpawnOutsideMapObjects()
    {
        if (Plugin.ModConfig.ConfigFloraEnabled.Value) SpawnFlora();
        System.Random random = new(StartOfRound.Instance.randomMapSeed + 69);
        foreach (RegisteredCRMapObject registeredMapObject in registeredMapObjects)
        {
            HandleSpawningOutsideMapObjects(registeredMapObject, random);
        }
    }

    private static void HandleSpawningOutsideMapObjects(RegisteredCRMapObject mapObjDef, System.Random random)
    {
        SelectableLevel level = RoundManager.Instance.currentLevel;
        AnimationCurve animationCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
        GameObject prefabToSpawn = mapObjDef.outsideObject.spawnableObject.prefabToSpawn;

        NetworkObject networkObject = prefabToSpawn.GetComponent<NetworkObject>();
        if (networkObject != null && !NetworkManager.Singleton.IsServer)
            return;

        animationCurve = mapObjDef.spawnRateFunction(level);
        int randomNumberToSpawn = Mathf.FloorToInt(animationCurve.Evaluate(random.NextFloat(0f, 1f)) + 0.5f);
        Plugin.ExtendedLogging($"Spawning {randomNumberToSpawn} of {prefabToSpawn.name} for level {level}");
        for (int i = 0; i < randomNumberToSpawn; i++)
        {
            Vector3 spawnPos = RoundManager.Instance.outsideAINodes[random.Next(0, RoundManager.Instance.outsideAINodes.Length)].transform.position;
            spawnPos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(spawnPos, 10f, default, random, -1) + (Vector3.up * 2);
            Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 100, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
            if (hit.collider == null) continue;
            GameObject spawnedPrefab = GameObject.Instantiate(prefabToSpawn, hit.point, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
            Plugin.ExtendedLogging($"Spawning {spawnedPrefab.name} at {hit.point}");
            if (mapObjDef.alignWithTerrain)
            {
                spawnedPrefab.transform.up = hit.normal;
            }
            if (networkObject == null)
                return;

            spawnedPrefab.GetComponent<NetworkObject>().Spawn(true);
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

    private static void SpawnFlora(System.Random random, SpawnableFlora flora, ref int spawnCount)
    {
        var targetSpawns = flora.spawnCurve.Evaluate(random.NextFloat(0, 1));
        for (int i = 0; i < targetSpawns; i++)
        {
            if (!TryGetValidFloraSpawnPoint(random, out RaycastHit hit))
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

    private static bool TryGetValidFloraSpawnPoint(System.Random random, out RaycastHit hit)
    {
        Vector3 basePosition = GetRandomPointNearPointsOfInterest(random, 20);
        Vector3 randomPosition = basePosition;

        hit = default;

        if (!NavMesh.SamplePosition(randomPosition, out NavMeshHit navMeshHit, 20f, NavMesh.AllAreas))
            return false;

        Vector3 navMeshPosition = navMeshHit.position;
        Vector3 vector = navMeshPosition;
        if (!Physics.Raycast(vector, Vector3.down, out hit, 150, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            return false;
        return true;
    }

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

    [HarmonyPatch(nameof(RoundManager.UnloadSceneObjectsEarly)), HarmonyPostfix]
    private static void ReturnToOrbitMiscPatch()
    {
        PiggyBank.Instance?.RepairPiggyBankServerRpc();
        foreach (GalAI gal in GalAI.Instances)
        {
            gal.RefillChargesServerRpc();
        }

        foreach (SCP999GalAI gal in SCP999GalAI.Instances)
        {
            gal.MakeTriggerInteractableServerRpc(false);
        }
        CodeRebirthUtils.Instance.ResetEntrancePointsServerRpc();
    }

    [HarmonyPatch(nameof(RoundManager.PlayAudibleNoise)), HarmonyPostfix]
    public static void PlayAudibleNoiseForShockwaveGalPostfix(RoundManager __instance, ref Vector3 noisePosition, ref float noiseRange, ref float noiseLoudness, ref int timesPlayedInSameSpot, ref bool noiseIsInsideClosedShip, ref int noiseID)
    {
        if (noiseID != 5 && noiseID != 6) return;
        if (noiseIsInsideClosedShip)
        {
            noiseRange /= 2f;
        }
        int numHits = Physics.OverlapSphereNonAlloc(noisePosition, noiseRange, RoundManager.Instance.tempColliderResults, CodeRebirthUtils.Instance.propsAndHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (!RoundManager.Instance.tempColliderResults[i].TryGetComponent(out INoiseListener noiseListener)) continue;
            GalAI? gal = RoundManager.Instance.tempColliderResults[i].gameObject.GetComponent<GalAI>();
            SCP999GalAI? scp999Gal = RoundManager.Instance.tempColliderResults[i].gameObject.GetComponent<SCP999GalAI>();
            BellCrabGalAI? bellCrabGal = RoundManager.Instance.tempColliderResults[i].gameObject.GetComponent<BellCrabGalAI>();
            FlashTurret? flashTurret = RoundManager.Instance.tempColliderResults[i].gameObject.GetComponent<FlashTurret>();
            if (gal == null && flashTurret == null && scp999Gal == null && bellCrabGal == null)
            {
                continue;
            }
            noiseListener.DetectNoise(noisePosition, noiseLoudness, timesPlayedInSameSpot, noiseID);
        }
    }

    [HarmonyPatch(nameof(RoundManager.LoadNewLevelWait))]
    [HarmonyPrefix]
    public static void LoadNewLevelWaitPatch(RoundManager __instance)
    {
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