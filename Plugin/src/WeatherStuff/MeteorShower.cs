using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.Misc;
using Newtonsoft.Json.Serialization;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static Steamworks.InventoryItem;
using Random = System.Random;

namespace CodeRebirth.WeatherStuff;

public class MeteorShower : MonoBehaviour {
	Coroutine spawnHandler;

	List<GameObject> nodes;
    [Space(5f)]
    [Header("Time between Meteor Spawns")]
	[SerializeField]
	[Tooltip("Minimum Time between Meteor Spawns")]
    int minTimeBetweenSpawns;
    [Space(5f)]
    [SerializeField]
    [Tooltip("Maximum Time between Meteor Spawns")]
    int maxTimeBetweenSpawns;
    [Space(5f)]
    [SerializeField]
    [Tooltip("Minimum Amount of Meteors per Spawn")]
    int minMeteorsPerSpawn;
    [Space(5f)]
    [SerializeField]
    [Tooltip("Maximum Amount of Meteors per Spawn")]
    int maxMeteorsPerSpawn;
    private List<GameObject> alreadyUsedNodes;

	readonly List<Meteors> meteors = new List<Meteors>(); // Proper initialization
	readonly List<CraterController> craters = new List<CraterController>(); // Similarly for craters
	
	Random random;
	
	public static MeteorShower Instance { get; private set; }
	public static bool Active => Instance != null;
	
	private void OnEnable() { // init weather
		Plugin.Logger.LogInfo("Initing Meteor Shower Weather on " + RoundManager.Instance.currentLevel.name);
		Instance = this;
        random = new Random(StartOfRound.Instance.randomMapSeed);
		alreadyUsedNodes = new List<GameObject>();
        nodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
		nodes = CullNodesByProximity(nodes, 5.0f, true).ToList();
		SpawnOverheadVisualMeteors(random.Next(15, 45));
		
		if(!IsAuthority()) return; // Only run on the host.
        
		random = new Random();
		spawnHandler = StartCoroutine(MeteorSpawnerHandler());
	}

	private void OnDisable() { // clean up weather
		try {
			Plugin.Logger.LogDebug("Cleaning up Weather.");
			ClearMeteorites();
			ClearCraters();

            Instance = null;

			if(!IsAuthority()) return; // Only run on the host.
			StopCoroutine(spawnHandler);
		} catch (Exception e) {
			Plugin.Logger.LogFatal("Cleaning up Weather failed." + e.Message);
		}
	}
	void ClearMeteorites()
	{
        foreach (Meteors meteor in meteors)
        {
			Plugin.Logger.LogInfo($"Destroying Meteor: {meteor}");
			if (meteor == null) continue;
            if (!meteor.NetworkObject.IsSpawned || IsAuthority())
                Destroy(meteor.gameObject);
        }
        meteors.Clear();
    }
	void ClearCraters()
	{
        foreach (CraterController crater in craters)
        {
			Plugin.Logger.LogInfo($"Destroying Crater: {crater}");
			if (crater == null) continue;
            Destroy(crater.gameObject);
        }
        craters.Clear();
    }
    private Vector3 CalculateAverageLandNodePosition()
    {
        Vector3 sumPosition = Vector3.zero;
        int count = 0;

        foreach (GameObject node in nodes)
        {
            sumPosition += node.transform.position;
            count++;
        }

        return count > 0 ? sumPosition / count : Vector3.zero;
    }
	private void SpawnOverheadVisualMeteors(int amount = 50) {
        Vector3 averageLocation = CalculateAverageLandNodePosition();
        Vector3 centralLocation = averageLocation + new Vector3(0, random.Next(250, 300), 0);
		for (int i = 0; i < amount; i++) {
			SpawnVisualMeteors(
				centralLocation: centralLocation,
				offset: new Vector3(random.Next(-175, 175), random.Next(-50, 50), random.Next(-175, 175)),
				speed: 2f,
				sizeMultiplier: (float)random.NextDouble() * 12f + 2f);

        }
        for (int i = 0; i < 1; i++) {
			SpawnVisualMeteors(
				centralLocation: centralLocation,
				offset: Vector3.zero,
				speed: 1.5f,
				sizeMultiplier: random.Next(30, 50)
				);
        }
	}
	private void SpawnVisualMeteors(Vector3 centralLocation, Vector3 offset = default, float speed = 0f, float sizeMultiplier = 1f, GameObject overridePrefab = null)
    {
        Meteors meteor = Instantiate(overridePrefab != null ? overridePrefab : WeatherHandler.Instance.Assets.MeteorPrefab, centralLocation + offset, Quaternion.identity).GetComponent<Meteors>();
        meteor.transform.localScale *= sizeMultiplier;
        AddRandomMovement(meteor, speed);
        meteor.SetupAsLooping();
    }
	private IEnumerator MeteorSpawnerHandler() {
		yield return new WaitForSeconds(5f); // inital delay so clients don't get meteors before theyve inited everything.
		Plugin.Logger.LogInfo("Began spawning meteors.");
		while (true) { // this is fine because it gets stopped in OnDisable.
			Plugin.Logger.LogDebug("Spawning Meteor.");

			for (int i = 0; i < random.Next(minMeteorsPerSpawn, maxMeteorsPerSpawn); i++) {
				SpawnMeteor(GetRandomTargetPosition(minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
				yield return new WaitForSeconds(random.NextFloat(0f, 0.5f));
			}
			int delay = random.Next(minTimeBetweenSpawns, maxTimeBetweenSpawns);
			Plugin.Logger.LogDebug($"Next meteor in {delay} seconds.");
			yield return new WaitForSeconds(delay);
		}
	}

	private void SpawnMeteor(Vector3 target) {
		Vector3 origin = target + new Vector3(
			random.NextFloat(250, 500) * random.NextSign(), 
			random.NextFloat(500, 800), 
			random.NextFloat(250, 500) * random.NextSign()
		);
            
		Meteors meteor = Instantiate(WeatherHandler.Instance.Assets.MeteorPrefab, origin, Quaternion.identity).GetComponent<Meteors>();
		meteor.NetworkObject.OnSpawn(() => {
			meteor.SetupMeteorClientRpc(origin, target, false);
		});
		meteor.NetworkObject.Spawn();
	}
	
	private IEnumerable<GameObject> CullNodesByProximity(List<GameObject> nodes, float minDistance = 5f, bool cullDoors = false)
	{
		var nodeList = new List<GameObject>(nodes);
		var toCull = new HashSet<GameObject>();

		// Compare each node with every other node
		for (int i = 0; i < nodeList.Count; i++)
		{
			
			for (int j = i + 1; j < nodeList.Count; j++)
			{
				if (Vector3.Distance(nodeList[i].transform.position, nodeList[j].transform.position) < minDistance)
				{
					// Mark the second node in each pair for culling
					toCull.Add(nodeList[j]);
				}
			}
		}

		// Remove the marked nodes
		nodeList.RemoveAll(n => toCull.Contains(n));

		if (cullDoors)
		{
			var entrances = FindObjectsOfType<EntranceTeleport>().ToList();
			nodeList.RemoveAll(n => entrances.Exists(e => Vector3.Distance(n.transform.position, e.transform.position) < minDistance));
		}

		return nodeList;
	}

	private Vector3 GetRandomTargetPosition(float minX, float maxX, float minY, float maxY, float minZ, float maxZ, float radius) {
		try {
			var nextNode = random.NextItem(nodes);
			Vector3 position = nextNode.transform.position;
			if (!alreadyUsedNodes.Contains(nextNode)) {
				alreadyUsedNodes.Add(nextNode);
			}
			position += new Vector3(random.NextFloat(minX, maxX), random.NextFloat(minY, maxY), random.NextFloat(minZ, maxZ));
			position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: position, radius: radius, randomSeed: random);
		return position;
		} catch {
			Plugin.Logger.LogFatal("Selecting random position failed.");
			return new Vector3(0,0,0);
		}
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
    private bool IsAuthority()
    {
        return NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
    }

	public void AddMeteor(Meteors meteor)
	{
		meteors.Add(meteor);
	}

	public void RemoveMeteor(Meteors meteor)
	{
		meteors.Remove(meteor);
	}

	public void AddCrater(CraterController crater)
	{
		craters.Add(crater);
	}

	public void RemoveCrater(CraterController crater)
	{
		craters.Remove(crater);
	}
}

public class StabilizeMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 targetVelocity;

    public void Initialize(Rigidbody rigidbody, Vector3 velocity)
    {
        rb = rigidbody;
        targetVelocity = velocity;
    }

    void FixedUpdate()
    {
        rb.velocity = targetVelocity; // Continuously reset velocity to the intended direction
    }
}