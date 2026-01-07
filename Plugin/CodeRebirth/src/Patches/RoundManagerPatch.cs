using System.Collections.Generic;
using CodeRebirth.src.Content.Maps;
using Dawn.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using CodeRebirth.src.Content.Unlockables;
using Dawn;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(RoundManager))]
static class RoundManagerPatch
{
    internal static List<SpawnableFlora> spawnableFlora = [];
    internal static List<GrabbableObject> plushiesCollectedToday = new();

    [HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards)), HarmonyPrefix]
    private static void SpawnOutsideMapObjects()
    {
        if (Plugin.ModConfig.ConfigFloraEnabled.Value)
        {
            SpawnFlora();
        }
    }

    private static void SpawnFlora()
    {
        Plugin.ExtendedLogging("Spawning flora!!!");
        System.Random random = new(StartOfRound.Instance.randomMapSeed + 2358);
        int spawnCount = 0;
        GameObject staticBatchedParent = new("Flora Parent");
        if (RoundManager.Instance.mapPropsContainer != null)
        {
            staticBatchedParent.transform.SetParent(RoundManager.Instance.mapPropsContainer.transform);
        }

        foreach (SpawnableFlora flora in spawnableFlora)
        {
            SpawnFlora(staticBatchedParent, random, flora, ref spawnCount);
        }

        // StaticBatchingUtility.Combine(staticBatchedParent);
    }

    private static bool TryGetValidFloraSpawnPoint(System.Random random, out RaycastHit hit)
    {
        Vector3 randomPosition = GetRandomPointNearPointsOfInterest(random, 20);

        hit = default;

        if (!NavMesh.SamplePosition(randomPosition, out NavMeshHit navMeshHit, 20f, NavMesh.AllAreas))
            return false;

        if (!Physics.Raycast(navMeshHit.position, Vector3.down, out hit, 5, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    private static void SpawnFlora(GameObject staticBatchedParent, System.Random random, SpawnableFlora flora, ref int spawnCount)
    {
        AnimationCurve animationCurve = flora.spawnCurveFunction(RoundManager.Instance.currentLevel.GetDawnInfo());
        int targetSpawns = Mathf.FloorToInt(animationCurve.Evaluate(random.NextFloat(0, 1)) + 0.5f);
        for (int i = 0; i < targetSpawns; i++)
        {
            if (!TryGetValidFloraSpawnPoint(random, out RaycastHit hit))
                continue;

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

            GameObject spawnedFlora = GameObject.Instantiate(flora.prefab, spawnPosition, Quaternion.identity, staticBatchedParent.transform);
            spawnedFlora.transform.up = hit.normal;
            spawnCount++;
        }
    }

    public static Vector3 GetRandomPointNearPointsOfInterest(System.Random random, float offsetRange = 20f)
    {
        // Get all points of interest
        GameObject[] pointsOfInterest = RoundManager.Instance.outsideAINodes;

        // Check if there are any points of interest
        if (pointsOfInterest.Length == 0)
        {
            Plugin.Logger.LogWarning("No points of interest found.");
            return Vector3.zero; // Return default if no points exist
        }

        // Choose a random point of interest
        Vector3 chosenPoint = pointsOfInterest[random.Next(pointsOfInterest.Length)].transform.position;

        // Generate a random offset within the specified range
        Vector3 offset = new(
            random.NextFloat(-offsetRange, offsetRange),
            random.NextFloat(0, offsetRange),
            random.NextFloat(-offsetRange, offsetRange)
        );

        return chosenPoint + offset;
    }

    [HarmonyPatch(nameof(RoundManager.UnloadSceneObjectsEarly)), HarmonyPostfix]
    private static void ReturnToOrbitMiscPatch()
    {
        plushiesCollectedToday.Clear();
        foreach (GalAI gal in GalAI.Instances)
        {
            gal.RefillChargesServerRpc();
        }

        foreach (SCP999GalAI gal in SCP999GalAI.Instances)
        {
            gal.MakeTriggerInteractableServerRpc(false);
        }
    }

    [HarmonyPatch(nameof(RoundManager.LoadNewLevelWait))]
    [HarmonyPrefix]
    public static void LoadNewLevelWaitPatch(RoundManager __instance)
    {
        if (!__instance.currentLevel.planetHasTime && TimeOfDay.Instance.daysUntilDeadline == 0)
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