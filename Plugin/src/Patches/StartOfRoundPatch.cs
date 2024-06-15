using System.Linq;
using CodeRebirth.Misc;
using CodeRebirth.src;
using CodeRebirth.Util.Extensions;
using CodeRebirth.WeatherStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch {
	[HarmonyPrefix]
	[HarmonyPatch(nameof(StartOfRound.Start))]
	static void RegisterScraps() {
		foreach (var item in Plugin.samplePrefabs.Values) {
			if (!StartOfRound.Instance.allItemsList.itemsList.Contains(item)) {
				StartOfRound.Instance.allItemsList.itemsList.Add(item);
			}
		}
	}
	
	[HarmonyPatch(nameof(StartOfRound.Awake))]
	[HarmonyPostfix]
	public static void StartOfRound_Awake(ref StartOfRound __instance)
	{
		__instance.NetworkObject.OnSpawn(CreateNetworkManager);

		string[] meteorLevelOverrides = Plugin.ModConfig.ConfigMeteorShowerMoonsBlacklist.Value.Split(',')
											    .Select(name => name.Trim())
											    .ToArray();
		string[] tornadoLevelOverrides = Plugin.ModConfig.ConfigTornadoMoonsBlacklist.Value.Split(',')
											    .Select(name => name.Trim())
											    .ToArray();
		LethalLib.Modules.Weathers.RemoveWeather("Meteor Shower", levelOverrides: meteorLevelOverrides);
		LethalLib.Modules.Weathers.RemoveWeather("Windy", levelOverrides: tornadoLevelOverrides);
	}
	
	[HarmonyPatch(nameof(StartOfRound.OnDisable))]
	[HarmonyPrefix]
	public static void DisableWeathersPatch() {
		if (MeteorShower.Active) { // patch to fix OnDisable not being triggered as its not actually in the scene.
			WeatherHandler.Instance.MeteorShowerWeather.effectObject.SetActive(false);
			WeatherHandler.Instance.MeteorShowerWeather.effectPermanentObject.SetActive(false);
		}
	}
	
	private static void CreateNetworkManager()
	{
		if (StartOfRound.Instance.IsServer || StartOfRound.Instance.IsHost)
		{
			if (CodeRebirthUtils.Instance == null) {
				GameObject utilsInstance = GameObject.Instantiate(Plugin.Assets.UtilsPrefab);
				SceneManager.MoveGameObjectToScene(utilsInstance, StartOfRound.Instance.gameObject.scene);
				utilsInstance.GetComponent<NetworkObject>().Spawn();
				Plugin.Logger.LogInfo($"Created CodeRebirthUtils. Scene is: '{utilsInstance.scene.name}'");
			} else {
				Plugin.Logger.LogWarning("CodeRebirthUtils already exists?");
			}
		}
	}
}