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
	List<GameObject> nodes = null!;
    [Space(5f)]
    [SerializeField]
    [Tooltip("Minimum Amount of Tornados per Spawn")]
    private int minTornadosToSpawn = 1;
    [SerializeField]
    [Tooltip("Maximum Amount of Tornados per Spawn")]
    private int maxTornadosToSpawn = 1;
    private List<GameObject> alreadyUsedNodes = new List<GameObject>();

	private List<Tornados> tornados = new List<Tornados>(); // Proper initialization
	
	private Random random = null!;
	
	public static TornadoWeather? Instance { get; private set; }
	public static bool Active => Instance != null;
	public Tornados.TornadoType tornadoTypeIndex { get; private set; }
	public enum TornadoWeatherType {
		Fire,
		Blood,
		Windy,
		Smoke,
		Water,
		Electric
	}
	
	private void OnEnable() { // init weather
		Plugin.Logger.LogInfo("Initing Tornado Weather on " + RoundManager.Instance.currentLevel.name);
		Instance = this;
        random = new Random(StartOfRound.Instance.randomMapSeed);
		alreadyUsedNodes = new List<GameObject>();
        nodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
		nodes = CullNodesByProximity(nodes, 5.0f, true, true).ToList();
		
		if(!IsAuthority()) return; // Only run on the host.
        Plugin.Logger.LogInfo(Plugin.ModConfig.ConfigTornadoWeatherType.Value.ToString());
		switch (Plugin.ModConfig.ConfigTornadoWeatherType.Value) {
			case TornadoWeatherType.Fire:
				tornadoTypeIndex = Tornados.TornadoType.Fire;
				break;
			case TornadoWeatherType.Blood:
				tornadoTypeIndex = Tornados.TornadoType.Blood;
				break;
			case TornadoWeatherType.Windy:
				tornadoTypeIndex = Tornados.TornadoType.Windy;
				break;
			case TornadoWeatherType.Smoke:
				tornadoTypeIndex = Tornados.TornadoType.Smoke;
				break;
			case TornadoWeatherType.Water:
				tornadoTypeIndex = Tornados.TornadoType.Water;
				break;
			case TornadoWeatherType.Electric:
				tornadoTypeIndex = Tornados.TornadoType.Electric;
				break;
			default:
				tornadoTypeIndex = (Tornados.TornadoType)Enum.GetValues(typeof(Tornados.TornadoType)).GetValue(random.Next(6));
				break;
		}

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
		SpawnTornado(GetRandomTargetPosition(random, nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
	}

	private void SpawnTornado(Vector3 target) {
		Vector3 origin = target;
            
		Tornados tornado = Instantiate(WeatherHandler.Instance.Tornado.TornadoPrefab, origin, Quaternion.identity).GetComponent<Tornados>();
		tornado.NetworkObject.OnSpawn(() => {
			tornado.SetupTornadoClientRpc(origin, (int)tornadoTypeIndex);
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