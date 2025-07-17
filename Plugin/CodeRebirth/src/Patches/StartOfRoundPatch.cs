using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using System.Diagnostics;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.MiscScripts;
using LethalLevelLoader;
using System.Linq;
using CodeRebirthLib.ContentManagement.Weathers;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch
{
    [HarmonyPatch(nameof(StartOfRound.AutoSaveShipData)), HarmonyPostfix]
    static void SaveCodeRebirthData()
    {
        CodeRebirthUtils.Instance.SaveCodeRebirthData();
    }

    [HarmonyPatch(nameof(StartOfRound.Awake))]
    [HarmonyPostfix]
    public static void StartOfRound_Awake(ref StartOfRound __instance)
    {
        Plugin.ExtendedLogging("StartOfRound.Awake");
        __instance.NetworkObject.OnSpawn(CreateNetworkManager);
    }

    private static void CreateNetworkManager()
    {
        if (StartOfRound.Instance.IsServer || StartOfRound.Instance.IsHost)
        {
            if (CodeRebirthUtils.Instance == null)
            {
                GameObject utilsInstance = GameObject.Instantiate(Plugin.Assets.UtilsPrefab);
                SceneManager.MoveGameObjectToScene(utilsInstance, StartOfRound.Instance.gameObject.scene);
                utilsInstance.GetComponent<NetworkObject>().Spawn();
                Plugin.ExtendedLogging($"Created CodeRebirthUtils. Scene is: '{utilsInstance.scene.name}'");
            }
            else
            {
                Plugin.Logger.LogWarning("CodeRebirthUtils already exists?");
            }
        }

        if (EnemyHandler.Instance.DuckSong != null)
        {
            Plugin.ExtendedLogging("Creating duck UI");
            var canvasObject = GameObject.Find("Systems/UI/Canvas");
            var duckUI = GameObject.Instantiate(EnemyHandler.Instance.DuckSong.DuckUIPrefab, Vector3.zero, Quaternion.identity, canvasObject.transform);
        }
    }

    [HarmonyPatch(nameof(StartOfRound.OnShipLandedMiscEvents)), HarmonyPostfix]
    public static void OnShipLandedMiscEventsPatch(StartOfRound __instance)
    {
        Plugin.ExtendedLogging("Starting big object search");

        if (MapObjectHandler.Instance.Biome != null)
        {
            Stopwatch timer = new();
            timer.Start();
            var objs = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int FoundObject = 0;
            LayerMask foliageLayer = 10;
            if (LethalLevelLoader.DungeonManager.CurrentExtendedDungeonFlow != null) Plugin.ExtendedLogging("Current Interior: " + LethalLevelLoader.DungeonManager.CurrentExtendedDungeonFlow.name);
            foreach (var item in objs)
            {
                if (item.layer == foliageLayer)
                {
                    // figure out a way to make this better against static meshes.
                    item.AddComponent<BoxCollider>().isTrigger = true;
                    FoundObject++;
                }
            }

            timer.Stop();

            Plugin.ExtendedLogging($"Run completed in {timer.ElapsedTicks} ticks and {timer.ElapsedMilliseconds}ms and found {FoundObject} objects out of {objs.Length}");
        }

        foreach (var plant in PlantPot.Instances)
        {
            plant.grewThisOrbit = false;
        }

        foreach (SCP999GalAI gal in SCP999GalAI.Instances)
        {
            gal.MakeTriggerInteractable(true);
        }

        if (Plugin.ModConfig.ConfigRemoveInteriorFog.Value)
        {
            Plugin.ExtendedLogging("Disabling halloween fog");
            if (RoundManager.Instance.indoorFog.gameObject.activeSelf) RoundManager.Instance.indoorFog.gameObject.SetActive(false);
        }

        foreach (var gal in GalAI.Instances)
        {
            if (gal.IdleSounds.Length <= 0) continue;
            gal.GalVoice.PlayOneShot(gal.IdleSounds[gal.galRandom.Next(gal.IdleSounds.Length)]);
        }
    }

    [HarmonyPatch(nameof(StartOfRound.SetShipReadyToLand)), HarmonyPostfix]
    static void ForceChangeWeathersForOxyde()
    {
        LevelManager.TryGetExtendedLevel(StartOfRound.Instance.levels.Where(x => x.sceneName == "Oxyde").FirstOrDefault(), out ExtendedLevel? extendedLevel);
        Plugin.ExtendedLogging($"Extended level: {extendedLevel?.SelectableLevel}");
        if (extendedLevel == null)
            return;

        if (TimeOfDay.Instance.daysUntilDeadline <= 0)
        {
            WeatherRegistry.WeatherController.ChangeWeather(extendedLevel.SelectableLevel, LevelWeatherType.None);
            return;
        }

        string weatherName = WeatherRegistry.WeatherManager.GetCurrentWeather(extendedLevel.SelectableLevel).name.ToLowerInvariant().Trim();
        if (!Plugin.ModConfig.ConfigOxydeNeedsNightShift.Value && weatherName != "none")
            return;

        if (!Plugin.Mod.WeatherRegistry().TryGetFromWeatherName("night shift", out CRWeatherDefinition? nightShiftWeatherDefinition))
            return;

        WeatherRegistry.WeatherController.ChangeWeather(extendedLevel.SelectableLevel, nightShiftWeatherDefinition.Weather);
    }
}