using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.src.Content.Weathers;
public class TornadoWeather : CodeRebirthWeathers {
	private Coroutine spawnHandler = null!;
	private List<GameObject> nodes = null!;
    private List<GameObject> alreadyUsedNodes = new List<GameObject>();
	private List<Tornados> tornados = new List<Tornados>(); // Proper initialization
	private Random random = null!;

	public static TornadoWeather? Instance { get; private set; }
	public static bool Active => Instance != null;

	private void OnEnable()
	{ // init weather
		Plugin.ExtendedLogging("Initing Tornado Weather on " + RoundManager.Instance.currentLevel.name);
		ChangeCurrentLevelMaximumPower(outsidePower: -3, insidePower: 6, dayTimePower: -3);
		Instance = this;
        random = new Random(StartOfRound.Instance.randomMapSeed);
		alreadyUsedNodes = new();
        nodes = RoundManager.Instance.outsideAINodes.ToList();
		nodes = CullNodesByProximity(nodes, 5.0f, true, true).ToList();
		
		if(!IsAuthority()) return; // Only run on the host.
		spawnHandler = StartCoroutine(TornadoSpawnerHandler());
	}

	private void OnDisable()
	{ // clean up weather
		try {
			Plugin.Logger.LogDebug("Cleaning up Weather.");
			ClearTornados();
			ChangeCurrentLevelMaximumPower(outsidePower: 3, insidePower: -6, dayTimePower: 3);
			Instance = null;

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

	private IEnumerator TornadoSpawnerHandler()
	{
		yield return new WaitForSeconds(5f);
		SpawnTornado(GetRandomTargetPosition(random, nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
	}

	private void SpawnTornado(Vector3 target)
	{
		Vector3 origin = target;
            
		Tornados tornado = Instantiate(WeatherHandler.Instance.Tornado.TornadoObj.enemyPrefab, origin, Quaternion.identity).GetComponent<Tornados>();
		tornado.NetworkObject.OnSpawn(() =>
		{
            tornado.SetupTornadoClientRpc(origin);
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