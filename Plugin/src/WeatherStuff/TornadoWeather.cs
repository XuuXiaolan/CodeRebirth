using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.Misc;
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
    int minTornadosToSpawn;
    [SerializeField]
    [Tooltip("Maximum Amount of Tornados per Spawn")]
    int maxTornadosToSpawn;
    private List<GameObject> alreadyUsedNodes;

	readonly List<Tornados> tornados = new List<Tornados>(); // Proper initialization
	
	Random random;
	
	public static TornadoWeather Instance { get; private set; }
	public static bool Active => Instance != null;

    public enum TornadoType
    {
        Water,
        Fire,
        Sand
    }
	
	private void OnEnable() { // init weather
		Plugin.Logger.LogInfo("Initing Tornado Weather on " + RoundManager.Instance.currentLevel.name);
		Instance = this;
        random = new Random(StartOfRound.Instance.randomMapSeed);
		alreadyUsedNodes = new List<GameObject>();
        nodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
		nodes = CullNodesByProximity(nodes, 5.0f, true).ToList();
		
		if(!IsAuthority()) return; // Only run on the host.
        
		random = new Random();
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
		while (true) { // this is fine because it gets stopped in OnDisable.

			for (int i = 0; i < random.Next(minTornadosToSpawn, maxTornadosToSpawn); i++) {
				SpawnTornado(GetRandomTargetPosition(random, nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
				yield return new WaitForSeconds(random.NextFloat(0f, 0.5f));
			}
			int delay = random.Next(200, 500);
			yield return new WaitForSeconds(delay);
		}
	}

	private void SpawnTornado(Vector3 target) {
		Vector3 origin = target;
            
		Tornados tornado = Instantiate(WeatherHandler.Instance.Assets.TornadoPrefab, origin, Quaternion.identity).GetComponent<Tornados>();
		tornado.NetworkObject.OnSpawn(() => {
			tornado.SetupTornadoClientRpc(origin);
		});
		tornado.NetworkObject.Spawn();
	}

    private void AddRandomMovement(Meteors meteor, float speed)
    {
        var rb = meteor.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = meteor.gameObject.AddComponent<Rigidbody>();
            rb.mass = 1000f; // A high mass to minimize environmental impact while still allowing movement
            rb.useGravity = false; // Ensure the meteor doesn't fall due to gravity
            rb.isKinematic = false; // The Rigidbody will respond to physics but isn't subject to gravity
        }

        // Initial direction setup to mostly avoid downward movement
        Vector3 initialDirection = new Vector3(
            (float)random.NextDouble() * 2 - 1,  // X-axis: Full random
            Mathf.Max(0.5f, (float)random.NextDouble()),  // Y-axis: Strong upward bias
            (float)random.NextDouble() * 2 - 1   // Z-axis: Full random
        );
        rb.velocity = initialDirection.normalized * speed;

        // Limit rotation to Y-axis to minimize influence on velocity direction
        rb.angularVelocity = new Vector3(0, (float)random.NextDouble() * 100 - 50, 0);

        // Continuously adjust direction to ensure stability if necessary
        meteor.gameObject.AddComponent<StabilizeMovement>().Initialize(rb, initialDirection.normalized * speed);
    }

	public void AddTornado(Tornados meteor)
	{
		tornados.Add(meteor);
	}

	public void RemoveTornado(Tornados meteor)
	{
		tornados.Remove(meteor);
	}
}