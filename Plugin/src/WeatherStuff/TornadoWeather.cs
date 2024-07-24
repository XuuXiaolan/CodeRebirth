using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.Misc;
using CodeRebirth.Util.Extensions;
using Newtonsoft.Json.Serialization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace CodeRebirth.WeatherStuff;
public class TornadoWeather : CodeRebirthWeathers {
	private Coroutine spawnHandler = null!;
	private List<GameObject> nodes = null!;
    private List<GameObject> alreadyUsedNodes = new List<GameObject>();
	private List<Tornados> tornados = new List<Tornados>(); // Proper initialization
	private Random random = null!;
	private List<Tornados.TornadoType> tornadoTypeIndices = new List<Tornados.TornadoType>();

	public static TornadoWeather? Instance { get; private set; }
	public static bool Active => Instance != null;

	private void OnEnable() { // init weather
		Plugin.Logger.LogInfo("Initing Tornado Weather on " + RoundManager.Instance.currentLevel.name);
		RoundManager.Instance.currentLevel.maxOutsideEnemyPowerCount = Mathf.Clamp(RoundManager.Instance.currentLevel.maxOutsideEnemyPowerCount -= 3, 0, 999);
		RoundManager.Instance.currentLevel.maxEnemyPowerCount = Mathf.Clamp(RoundManager.Instance.currentLevel.maxEnemyPowerCount += 3, 0, 999);
		RoundManager.Instance.currentLevel.maxDaytimeEnemyPowerCount = Mathf.Clamp(RoundManager.Instance.currentLevel.maxDaytimeEnemyPowerCount -= 3, 0, 999);
		Instance = this;
        random = new Random(StartOfRound.Instance.randomMapSeed);
		alreadyUsedNodes = new List<GameObject>();
        nodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
		nodes = CullNodesByProximity(nodes, 5.0f, true, true).ToList();
		
		if(!IsAuthority()) return; // Only run on the host.

        Plugin.Logger.LogInfo(Plugin.ModConfig.ConfigTornadoWeatherTypes.Value); //convert config type to string with acceptable values
		
		tornadoTypeIndices = new List<Tornados.TornadoType>();
        var types = Plugin.ModConfig.ConfigTornadoWeatherTypes.Value.Split(',');

        foreach (string type in types)
        {
            switch (type.Trim().ToLower())
            {
                case "fire":
                    tornadoTypeIndices.Add(Tornados.TornadoType.Fire);
                    break;
                case "blood":
                    tornadoTypeIndices.Add(Tornados.TornadoType.Blood);
                    break;
                case "windy":
                    tornadoTypeIndices.Add(Tornados.TornadoType.Windy);
                    break;
                case "smoke":
                    tornadoTypeIndices.Add(Tornados.TornadoType.Smoke);
                    break;
                case "water":
                    tornadoTypeIndices.Add(Tornados.TornadoType.Water);
                    break;
                case "electric":
                    tornadoTypeIndices.Add(Tornados.TornadoType.Electric);
                    break;
                case "random":
                    var randomType = (Tornados.TornadoType)Enum.GetValues(typeof(Tornados.TornadoType)).GetValue(new Random().Next(6));
                    tornadoTypeIndices.Add(randomType);
                    break;
                default:
                    var defaultType = (Tornados.TornadoType)Enum.GetValues(typeof(Tornados.TornadoType)).GetValue(new Random().Next(6));
                    tornadoTypeIndices.Add(defaultType);
                    break;
            }
        }

        // Remove duplicates if any (optional)
        tornadoTypeIndices.Distinct().ToList();
		Plugin.Logger.LogInfo($"Tornado types: {tornadoTypeIndices}");
		spawnHandler = StartCoroutine(TornadoSpawnerHandler());
	}

	private void OnDisable() { // clean up weather
		try {
			Plugin.Logger.LogDebug("Cleaning up Weather.");
			ClearTornados();
			Instance = null!;

			if(!IsAuthority()) return; // Only run on the host.
			StopCoroutine(spawnHandler);
		} catch (Exception e) {
			Plugin.Logger.LogFatal("Cleaning up Weather failed." + e.Message);
		}
	}
	private void ClearTornados()
	{
        foreach (Tornados tornado in tornados)
        {
			if (tornado == null) continue;
            if (!tornado.NetworkObject.IsSpawned || IsAuthority())
                Destroy(tornado.gameObject);
        }
        tornados.Clear();
    }

	private IEnumerator TornadoSpawnerHandler() {
		yield return new WaitForSeconds(5f); // inital delay so clients don't get Tornados before theyve inited everything.
		SpawnTornado(GetRandomTargetPosition(random, nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
	}

	private void SpawnTornado(Vector3 target) {
		Vector3 origin = target;
            
		Tornados tornado = Instantiate(WeatherHandler.Instance.Tornado.TornadoPrefab, origin, Quaternion.identity).GetComponent<Tornados>();
		tornado.NetworkObject.OnSpawn(() => {
            int randomTypeIndex = (int)tornadoTypeIndices[random.Next(tornadoTypeIndices.Count)];
            tornado.SetupTornadoClientRpc(origin, randomTypeIndex);
        });
		tornado.NetworkObject.Spawn();
	}

	public void AddTornado(Tornados tornado)
	{
		tornados.Add(tornado);
	}

	public void RemoveTornado(Tornados tornado)
	{
		tornados.Remove(tornado);
	}
}