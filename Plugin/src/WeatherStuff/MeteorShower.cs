using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.Misc;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace CodeRebirth.WeatherStuff;

public class MeteorShower : MonoBehaviour {
	Coroutine spawnHandler;

	List<GameObject> nodes;
    public int minTimeBetweenSpawns;
    public int maxTimeBetweenSpawns;
    public int maxMeteorsPerSpawn;
    public int minMeteorsPerSpawn;

	public List<Meteors> meteors = new List<Meteors>(); // Proper initialization
	public List<CraterController> craters = new List<CraterController>(); // Similarly for craters
	
	Random random;
	
	public static MeteorShower Instance { get; private set; }
	public static bool Active => Instance != null;
	
	private void OnEnable() { // init weather
		Plugin.Logger.LogInfo("Initing Meteor Shower Weather.");
		Instance = this;
        random = new Random(StartOfRound.Instance.randomMapSeed);
        nodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
		nodes = CullNodesByProximity(nodes, 5.0f, 50, true).ToList();
		SpawnOverheadVisualMeteors(random.Next(15, 45));
		
		if(!IsAuthority()) return; // Only run on the host.
        
		random = new Random();
		spawnHandler = StartCoroutine(MeteorSpawnerHandler());
	}

	private void OnDisable() { // clean up weather
		try {
			Plugin.Logger.LogDebug("Cleaning up Weather.");
			foreach (Meteors meteor in meteors) {
				if(!meteor.NetworkObject.IsSpawned || IsAuthority())
				Destroy(meteor.gameObject);
			}
			foreach (CraterController crater in craters) {
				Destroy(crater.gameObject);
			}

			meteors = [];
			craters = [];
			Instance = null;

			if(!IsAuthority()) return; // Only run on the host.
			StopCoroutine(spawnHandler);
		} catch {
			Plugin.Logger.LogFatal("Dont mind me~ - Xu");
		}
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

        if (count > 0)
            return sumPosition / count;
        else
            return Vector3.zero; // Return a default position if no nodes are found
    }
	private void SpawnOverheadVisualMeteors(int amount = 50) {
        Vector3 averageLocation = CalculateAverageLandNodePosition();
        Vector3 centralLocation = averageLocation + new Vector3(0, random.Next(250, 300), 0);
		for (int i = 0; i < amount; i++) {
            Vector3 randomOffset = new Vector3(random.Next(-175, 175), random.Next(-50, 50), random.Next(-175, 175));
			Meteors SmallMeteor = Instantiate(Plugin.Meteor, centralLocation + randomOffset, Quaternion.identity).GetComponent<Meteors>();
            SmallMeteor.transform.localScale *= (float)random.NextDouble()*8f+2f;
            AddRandomMovement(SmallMeteor, 4f);
			SmallMeteor.SetupAsLooping();
		}
        for (int i = 0; i < 1; i++) {
            Meteors LargeMeteor = Instantiate(Plugin.Meteor, centralLocation, Quaternion.identity).GetComponent<Meteors>();
            LargeMeteor.transform.localScale *= random.Next(40,60);
            AddRandomMovement(LargeMeteor, 3f);
            LargeMeteor.SetupAsLooping();
        }
	}

	private IEnumerator MeteorSpawnerHandler() {
		yield return new WaitForSeconds(5f); // inital delay so clients don't get meteors before theyve inited everything.
		Plugin.Logger.LogInfo("Began spawning meteors.");
		while (true) { // this is fine because it gets stopped in OnDisable.
			Plugin.Logger.LogDebug("Spawning Meteor.");

			for (int i = 0; i < random.Next(minMeteorsPerSpawn, maxMeteorsPerSpawn); i++) {
				SpawnMeteor(GetRandomTargetPosition());
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
            
		Meteors meteor = Instantiate(Plugin.Meteor, origin, Quaternion.identity).GetComponent<Meteors>();
		meteor.NetworkObject.OnSpawn(() => {
			meteor.SetupMeteorClientRpc(origin, target);
		});
		meteor.NetworkObject.Spawn();
	}
	
	private IEnumerable<GameObject> CullNodesByProximity(List<GameObject> nodes, float minDistance = 5f, float minShipDistance = 50f, bool cullDoors = false)
	{
		var nodeList = new List<GameObject>(nodes);
		var toCull = new HashSet<GameObject>();
        Transform shipBoundaries = StartOfRound.Instance.shipBounds.transform;

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
			nodeList.RemoveAll(n => entrances.Any(e => Vector3.Distance(n.transform.position, e.transform.position) < minDistance));
		}

		return nodeList;
	}

	private Vector3 GetRandomTargetPosition() {
		try {
			Vector3 position = random.NextItem(nodes).transform.position;
			position += new Vector3(random.NextFloat(-2, 2), random.NextFloat(-5, 5), random.NextFloat(-2, 2));
			position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position, 25, default, random, -1);
		return position;
		} catch {
			Plugin.Logger.LogFatal("Did the colour scare you? it's so fancy right? - Xu");
			return new Vector3(0,0,0);
		}
	}

	private bool IsAuthority() {
		return NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
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