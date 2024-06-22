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
	Coroutine spawnHandler;
	List<GameObject> nodes;
    [Space(5f)]
    [SerializeField]
    [Tooltip("Minimum Amount of Tornados per Spawn")]
    private int minTornadosToSpawn;
    [SerializeField]
    [Tooltip("Maximum Amount of Tornados per Spawn")]
    private int maxTornadosToSpawn;
    private List<GameObject> alreadyUsedNodes;

	private List<Tornados> tornados = new List<Tornados>(); // Proper initialization
	
	Random random;
	
	public static TornadoWeather Instance { get; private set; }
	public static bool Active => Instance != null;
	public int tornadoTypeIndex = 0;
	
	private void OnEnable() { // init weather
		Plugin.Logger.LogInfo("Initing Tornado Weather on " + RoundManager.Instance.currentLevel.name);
		Instance = this;
        random = new Random(StartOfRound.Instance.randomMapSeed);
		alreadyUsedNodes = new List<GameObject>();
        nodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
		nodes = CullNodesByProximity(nodes, 5.0f, true, true).ToList();
		
		if(!IsAuthority()) return; // Only run on the host.
        
		random = new Random();
		switch (Plugin.ModConfig.ConfigTornadoWeatherType.Value) {
			case 0:
				tornadoTypeIndex = random.Next(0, 6);
				break;
			case 1:
				tornadoTypeIndex = 0;
				break;
			case 2:
				tornadoTypeIndex = 1;
				break;
			case 3:
				tornadoTypeIndex = 2;
				break;
			case 4:
				tornadoTypeIndex = 3;
				break;
			case 5:
				tornadoTypeIndex = 4;
				break;
			case 6:
				tornadoTypeIndex = 5;
				break;
			default:
				tornadoTypeIndex = random.Next(0, 6);
				break;
		}
		spawnHandler = StartCoroutine(TornadoSpawnerHandler());
	}

	private void OnDisable() { // clean up weather
		try {
			Plugin.Logger.LogDebug("Cleaning up Weather.");
			ClearTornados();
			Instance = null;

			if(!IsAuthority()) return; // Only run on the host.
			StopCoroutine(spawnHandler);
		} catch (Exception e) {
			Plugin.Logger.LogFatal("Cleaning up Weather failed." + e.Message);
		}
	}
	void ClearTornados()
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
		for (int i = 0; i < random.Next(1, 1); i++) {
			SpawnTornado(GetRandomTargetPosition(random, nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
		}
		int delay = random.Next(700, 1000);
		yield return new WaitForSeconds(delay);
		for (int i = 0; i < random.Next(minTornadosToSpawn, maxTornadosToSpawn); i++) {
			SpawnTornado(GetRandomTargetPosition(random, nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
		}
	}

	private void SpawnTornado(Vector3 target) {
		Vector3 origin = target;
            
		Tornados tornado = Instantiate(WeatherHandler.Instance.Assets.TornadoPrefab, origin, Quaternion.identity).GetComponent<Tornados>();
		tornado.NetworkObject.OnSpawn(() => {
			tornado.SetupTornadoClientRpc(origin, tornadoTypeIndex);
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