using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;
using Random = System.Random;

namespace CodeRebirth.src.Content.Weathers;
public class MeteorShower : CodeRebirthWeathers {
	private Coroutine spawnHandler = null!;

	private List<GameObject> nodes = new List<GameObject>();
    [Space(5f)]
    [Header("Time between Meteor Spawns")]
	[SerializeField]
	[Tooltip("Minimum Time between Meteor Spawns")]
    private float minTimeBetweenSpawns = 1;
    [Space(5f)]
    [SerializeField]
    [Tooltip("Maximum Time between Meteor Spawns")]
    private float maxTimeBetweenSpawns = 3;
    [Space(5f)]
    [SerializeField]
    [Tooltip("Minimum Amount of Meteors per Spawn")]
    private int minMeteorsPerSpawn = 1;
    [Space(5f)]
    [SerializeField]
    [Tooltip("Maximum Amount of Meteors per Spawn")]
    private int maxMeteorsPerSpawn = 3;
    private List<GameObject> alreadyUsedNodes = new List<GameObject>();
	private enum Direction {
		Random,
		East,
		West,
		North,
		South
	}
	private Direction direction = Direction.Random;
	private float normalisedTimeToLeave = 1f;
	readonly List<Meteors> meteors = new List<Meteors>(); // Proper initialization
	readonly List<CraterController> craters = new List<CraterController>(); // Similarly for craters
	
	private Random random = null!;
	
	public static MeteorShower? Instance { get; private set; }
	public static bool Active => Instance != null;
	
	private void OnEnable()
	{ // init weather
		Plugin.ExtendedLogging("Initing Meteor Shower Weather on " + RoundManager.Instance.currentLevel.name);
		normalisedTimeToLeave = Plugin.ModConfig.ConfigMeteorShowerTimeToLeave.Value;
		minMeteorsPerSpawn = Plugin.ModConfig.ConfigMinMeteorSpawnCount.Value;
		maxMeteorsPerSpawn = Plugin.ModConfig.ConfigMaxMeteorSpawnCount.Value;
		ChangeCurrentLevelMaximumPower(outsidePower: -3, insidePower: 6, dayTimePower: -3);
		if (minMeteorsPerSpawn > maxMeteorsPerSpawn)
		{
			Plugin.Logger.LogWarning("Min Meteor Spawn Count is greater than Max Meteor Spawn Count. Swapping values.");
			(int, int) temp = (minMeteorsPerSpawn, maxMeteorsPerSpawn);
			minMeteorsPerSpawn = temp.Item2;
			maxMeteorsPerSpawn = temp.Item1;
		}
		Instance = this;
        random = new Random(StartOfRound.Instance.randomMapSeed);
		alreadyUsedNodes = new List<GameObject>();
        nodes = GameObject.FindGameObjectsWithTag("OutsideAINode").ToList();
		nodes = CullNodesByProximity(nodes, 5.0f, true).ToList();
		// eventually gonna have a config bool to turn on override prefabs and maybe have more prefab options.
		SpawnOverheadVisualMeteors(random.NextInt(15, 45));
		
		if(!IsAuthority()) return; // Only run on the host.
		random = new Random();

		Direction[] directions = { Direction.Random, Direction.East, Direction.West, Direction.North, Direction.South };
        int index = random.NextInt(0, directions.Length-1);
        direction = directions[index];

		spawnHandler = StartCoroutine(MeteorSpawnerHandler());
	}

	private void OnDisable()
	{ // clean up weather
		try
		{
			Plugin.Logger.LogDebug("Cleaning up Weather.");
			ClearMeteorites();
			ClearCraters();
			ChangeCurrentLevelMaximumPower(outsidePower: 3, insidePower: -6, dayTimePower: 3);
			Instance = null;

			if(!IsAuthority()) return; // Only run on the host.
			if (spawnHandler != null) StopCoroutine(spawnHandler);
		}
		catch (Exception e)
		{
			Plugin.Logger.LogFatal("Cleaning up Weather failed." + e.Message);
		}
	}
	private void Update()
	{
		if (TimeOfDay.Instance.timeHasStarted && normalisedTimeToLeave <= TimeOfDay.Instance.normalizedTimeOfDay && IsAuthority())
		{
			StopCoroutine(spawnHandler);
		}
	}

	private void ClearMeteorites()
	{
        foreach (Meteors meteor in meteors)
        {
			if (meteor == null) continue;
            if (!meteor.NetworkObject.IsSpawned || IsAuthority())
                Destroy(meteor.gameObject);
        }
        meteors.Clear();
    }

	private void ClearCraters()
	{
        foreach (CraterController crater in craters)
        {
			if (crater == null) continue;
            Destroy(crater.gameObject);
        }
        craters.Clear();
    }

	private void SpawnOverheadVisualMeteors(int amount = 50, GameObject? overridePrefab = null) {
        Vector3 averageLocation = CalculateAverageLandNodePosition(nodes);
        Vector3 centralLocation = averageLocation + new Vector3(0, random.NextFloat(150, 200), 0);
		for (int i = 0; i < amount; i++) {
			SpawnVisualMeteors(
				overridePrefab: overridePrefab,
				centralLocation: centralLocation,
				offset: new Vector3(random.NextFloat(-175, 175), random.NextFloat(-50, 50), random.NextFloat(-175, 175)),
				speed: 2f,
				sizeMultiplier: random.NextFloat(1.5f, 4));
		}
        for (int i = 0; i < 1; i++) {
			SpawnVisualMeteors(
				overridePrefab: overridePrefab,
				centralLocation: centralLocation,
				offset: Vector3.zero,
				speed: 1.5f,
				sizeMultiplier: random.NextFloat(7.5f, 12.5f)
				);
        }
	}

	private void SpawnVisualMeteors(Vector3 centralLocation, Vector3 offset = default, float speed = 0f, float sizeMultiplier = 1f, GameObject? overridePrefab = null)
    {
        Meteors meteor = Instantiate(overridePrefab ?? WeatherHandler.Instance.Meteorite.FloatingMeteorPrefab, centralLocation + offset, Quaternion.identity).GetComponent<Meteors>();
		meteor.transform.localScale *= sizeMultiplier;
        AddRandomMovement(meteor, speed);
        bool isBig = sizeMultiplier > 5f;
		meteor.SetupAsLooping(isBig);
    }

	private IEnumerator MeteorSpawnerHandler()
	{
		yield return new WaitForSeconds(5f); // inital delay so clients don't get meteors before theyve inited everything.
		while (true) { // this is fine because it gets stopped in OnDisable.

			for (int i = 0; i < random.NextInt(minMeteorsPerSpawn, maxMeteorsPerSpawn); i++)
			{
				SpawnMeteor(GetRandomTargetPosition(random, nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
			}
			float delay = random.NextFloat(minTimeBetweenSpawns, maxTimeBetweenSpawns);
			yield return new WaitForSeconds(delay);
		}
	}

    public Vector3 CalculateVector(Vector3 target)
	{
        float x = 0, z = 0;
        float distanceX = random.NextFloat(250, 500);
        float distanceZ = random.NextFloat(250, 500);

        switch (direction) {
            case Direction.East:
                x = distanceX;  // Move east
                break;
            case Direction.West:
                x = -distanceX; // Move west
                break;
            case Direction.North:
                z = distanceZ;  // Move north
                break;
            case Direction.South:
                z = -distanceZ; // Move south
                break;
        }

        // Assume y is upwards and we want to keep it within a certain range
        float y = random.NextFloat(600, 900); // Fixed vertical range

        return target + new Vector3(x, y, z);
    }

	private void SpawnMeteor(Vector3 target, GameObject? overridePrefab = null)
	{
		if (target == Vector3.zero)
		{
			Plugin.Logger.LogWarning("Target is zero, not spawning meteor");
			return;
		}
		Vector3 origin = new();
		if (direction == Direction.Random)
		{
			origin = target + new Vector3(
				random.NextFloat(250, 500) * random.NextSign(), 
				random.NextFloat(600, 900), 
				random.NextFloat(250, 500) * random.NextSign()
			);
		}
		else
		{
			origin = CalculateVector(target);
		}
		
		Meteors meteor = Instantiate(overridePrefab ?? WeatherHandler.Instance.Meteorite.MeteorPrefab, origin, Quaternion.identity).GetComponent<Meteors>();
		meteor.NetworkObject.OnSpawn(() => {
			meteor.SetupMeteorClientRpc(origin, target);
		});
		meteor.NetworkObject.Spawn();
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
    private Rigidbody rb = null!;
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