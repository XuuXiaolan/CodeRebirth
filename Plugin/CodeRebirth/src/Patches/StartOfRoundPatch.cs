using CodeRebirth.src.Content;
using CodeRebirth.src.Content.Weathers;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CodeRebirth.src.Util;
using WeatherRegistry;
using CodeRebirth.src.Util.Extensions;
using System.Diagnostics;
using CodeRebirth.src.Content.Unlockables;
using System.Linq;
using CodeRebirth.src.Content.Enemies;

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch
{
	[HarmonyPatch(nameof(StartOfRound.Awake))]
	[HarmonyPostfix]
	public static void StartOfRound_Awake(ref StartOfRound __instance)
	{
		__instance.NetworkObject.OnSpawn(CreateNetworkManager);
	}

	[HarmonyPatch(nameof(StartOfRound.ArriveAtLevel)), HarmonyPostfix]
	static void DisplayWindyWarning(StartOfRound __instance)
	{
        if(__instance == null || WeatherHandler.Instance.TornadoesWeather == null) return; // tornado weather didn't load
		if (WeatherManager.GetCurrentWeather(StartOfRound.Instance.currentLevel) == WeatherHandler.Instance.TornadoesWeather)
		{
			Plugin.Logger.LogWarning("Displaying Windy Weather Warning.");
			HUDManager.Instance.DisplayTip("Weather alert!", "You have routed to a Windy moon. Exercise caution if you are sensitive to flashing lights!", true, true, "CR_WindyTip");
		}
	}

	[HarmonyPatch(nameof(StartOfRound.AutoSaveShipData)), HarmonyPostfix]
	static void SaveCodeRebirthData()
	{
		if(CodeRebirthUtils.Instance.IsHost || CodeRebirthUtils.Instance.IsServer) CodeRebirthSave.Current.Save();
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
				utilsInstance.AddComponent<CodeRebirthPlayerManager>();
				Plugin.ExtendedLogging($"Created CodeRebirthUtils. Scene is: '{utilsInstance.scene.name}'");
			}
			else
			{
				Plugin.Logger.LogWarning("CodeRebirthUtils already exists?");
			}

			if (EnemyHandler.Instance.DuckSong != null)
			{
				Plugin.ExtendedLogging("Creating duck UI");
				var canvasObject = GameObject.Find("Systems/UI/Canvas");
				var duckUI = GameObject.Instantiate(EnemyHandler.Instance.DuckSong.DuckUIPrefab, Vector3.zero, Quaternion.identity, canvasObject.transform);
				duckUI.GetComponent<NetworkObject>().Spawn();
			}
		}
	}

	[HarmonyPatch(nameof(StartOfRound.OnShipLandedMiscEvents)), HarmonyPostfix]
	public static void OnShipLandedMiscEventsPatch(StartOfRound __instance)
	{
		Plugin.ExtendedLogging("Starting big object search");

		Stopwatch timer = new();
		timer.Start();
		var objs = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		int FoundObject = 0;
		LayerMask layerMask = LayerMask.NameToLayer("Foliage");
		if (LethalLevelLoader.DungeonManager.CurrentExtendedDungeonFlow != null) Plugin.ExtendedLogging("Current Interior: " + LethalLevelLoader.DungeonManager.CurrentExtendedDungeonFlow.name);
		foreach (var item in objs)
		{
			if (LethalLevelLoader.DungeonManager.CurrentExtendedDungeonFlow != null && LethalLevelLoader.DungeonManager.CurrentExtendedDungeonFlow.name == "Toy Store") HandleWesleyChangesCuzHeIsStupid(item);
			if (item.layer == layerMask)
			{
				item.AddComponent<BoxCollider>().isTrigger = true;
				FoundObject++;
			}
		}

		timer.Stop();

		Plugin.ExtendedLogging($"Run completed in {timer.ElapsedTicks} ticks and {timer.ElapsedMilliseconds}ms and found {FoundObject} objects out of {objs.Length}");
	
		foreach (var plant in PlantPot.Instances)
		{
			plant.grewThisOrbit = false;
		}

		foreach (SCP999GalAI gal in SCP999GalAI.Instances)
		{
			gal.MakeTriggerInteractable(false);
		}

		if (Plugin.ModConfig.ConfigRemoveInteriorFog.Value)
		{
			Plugin.ExtendedLogging("Disabling halloween fog");
			if (RoundManager.Instance.indoorFog.gameObject.activeSelf) RoundManager.Instance.indoorFog.gameObject.SetActive(false);
		}

		foreach (var gal in GalAI.Instances)
		{
			gal.GalVoice.PlayOneShot(gal.IdleSounds[gal.galRandom.Next(0, gal.IdleSounds.Length)]);
		}
	}

	public static void HandleWesleyChangesCuzHeIsStupid(GameObject gameObject)
	{
		string[] stringsToCompare = ["GunBarrel", "GunBarrel (9)", "Cake", "Cake 0", "Cake (1)", "Cake (2)", "Cake (3)", "coilmesh", "MaskMesh", "MaskMesh (1)"];
		if (stringsToCompare.Contains(gameObject.name) && gameObject.GetComponent<MeshRenderer>() != null && gameObject.layer != 21 && gameObject.layer != 19)
		{
			Plugin.ExtendedLogging("Changing layer of " + gameObject.name + "To layer MapHazards (21)");
			gameObject.layer = 21;
		}
	}

	[HarmonyPatch(nameof(StartOfRound.ResetShip)), HarmonyPostfix]
	static void ResetSave()
	{
		CodeRebirthSave.Current = new CodeRebirthSave(CodeRebirthSave.Current.FileName);
		if(CodeRebirthUtils.Instance.IsHost || CodeRebirthUtils.Instance.IsServer) CodeRebirthSave.Current.Save();
	}
}