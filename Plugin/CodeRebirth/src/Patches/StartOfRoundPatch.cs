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

namespace CodeRebirth.src.Patches;
[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch {
	[HarmonyPatch(nameof(StartOfRound.Awake))]
	[HarmonyPostfix]
	public static void StartOfRound_Awake(ref StartOfRound __instance)
	{
		__instance.NetworkObject.OnSpawn(CreateNetworkManager);
		
	}

	[HarmonyPatch(nameof(StartOfRound.ArriveAtLevel)), HarmonyPostfix]
	static void DisplayWindyWarning(StartOfRound __instance) {
        if(__instance == null || WeatherHandler.Instance.TornadoesWeather == null) return; // tornado weather didn't load
		if (WeatherManager.GetCurrentWeather(StartOfRound.Instance.currentLevel) == WeatherHandler.Instance.TornadoesWeather) {
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
	
		foreach (var plant in PlantPot.Instances)
		{
			plant.grewThisOrbit = false;
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

	[HarmonyPatch(nameof(StartOfRound.ResetShip)), HarmonyPostfix]
	static void ResetSave()
	{
		CodeRebirthSave.Current = new CodeRebirthSave(CodeRebirthSave.Current.FileName);
		if(CodeRebirthUtils.Instance.IsHost || CodeRebirthUtils.Instance.IsServer) CodeRebirthSave.Current.Save();
	}
}