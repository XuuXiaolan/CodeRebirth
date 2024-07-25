using CodeRebirth.Dependency;
using CodeRebirth.Util.Spawning;
using CodeRebirth.Util.Extensions;
using CodeRebirth.WeatherStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameNetcodeStuff;
using CodeRebirth.Util.PlayerManager;
using WeatherRegistry;

namespace CodeRebirth.Patches;

[HarmonyPatch(typeof(StartOfRound))]
static class StartOfRoundPatch {
	[HarmonyPatch(nameof(StartOfRound.Awake))]
	[HarmonyPostfix]
	public static void StartOfRound_Awake(ref StartOfRound __instance)
	{
		__instance.NetworkObject.OnSpawn(CreateNetworkManager);
		
	}
	
	[HarmonyPatch(nameof(StartOfRound.OnDisable))]
	[HarmonyPrefix]
	public static void DisableWeathersPatch() {
		if (MeteorShower.Active) { 
			// patch to fix OnDisable not being triggered as its not actually in the scene.
			WeatherHandler.Instance.MeteorShowerWeather.Effect.DisableEffect();
		}
		if (TornadoWeather.Active) {
			WeatherHandler.Instance.TornadoesWeather.Effect.DisableEffect();
		}
	}

	[HarmonyPatch(nameof(StartOfRound.ArriveAtLevel)), HarmonyPostfix]
	static void DisplayWindyWarning(StartOfRound __instance) {
		if(WeatherRegistryCompatibilityChecker.Enabled)	WeatherRegistryCompatibilityChecker.DisplayWindyWarning();
	}
	
	private static void CreateNetworkManager()
	{
		if (StartOfRound.Instance.IsServer || StartOfRound.Instance.IsHost)
		{
			if (CodeRebirthUtils.Instance == null) {
				GameObject utilsInstance = GameObject.Instantiate(Plugin.Assets.UtilsPrefab);
				SceneManager.MoveGameObjectToScene(utilsInstance, StartOfRound.Instance.gameObject.scene);
				utilsInstance.GetComponent<NetworkObject>().Spawn();
				utilsInstance.AddComponent<CodeRebirthPlayerManager>();
				Plugin.Logger.LogInfo($"Created CodeRebirthUtils. Scene is: '{utilsInstance.scene.name}'");
			} else {
				Plugin.Logger.LogWarning("CodeRebirthUtils already exists?");
			}
		}
	}
}