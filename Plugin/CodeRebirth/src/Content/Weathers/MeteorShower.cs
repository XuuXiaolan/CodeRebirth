using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using UnityEngine;

namespace CodeRebirth.src.Content.Weathers;
public class MeteorShower : CodeRebirthWeathers
{
    private Coroutine? spawnHandler = null;
    private IEnumerable<GameObject> nodes = [];

    [Space(5f)]
    [Header("Time between Meteor Spawns")]
    [Tooltip("Minimum Time between Meteor Spawns")]
    public float minTimeBetweenSpawns = 1;
    [Space(5f)]
    [Tooltip("Maximum Time between Meteor Spawns")]
    public float maxTimeBetweenSpawns = 3;
    [Space(5f)]
    [Tooltip("Minimum Amount of Meteors per Spawn")]
    public int minMeteorsPerSpawn = 1;
    [Space(5f)]
    [Tooltip("Maximum Amount of Meteors per Spawn")]
    public int maxMeteorsPerSpawn = 3;

    private List<GameObject> alreadyUsedNodes = new();
    private enum Direction
    {
        Random,
        East,
        West,
        North,
        South
    }
    private Direction direction = Direction.Random;
    private float normalisedTimeToLeave = 1f;
    [HideInInspector] public static List<Meteors> meteors = new();
    [HideInInspector] public static List<CraterController> craters = new();

    [HideInInspector] public System.Random random = new();

    private void OnEnable()
    {
        Plugin.ExtendedLogging("Initing Meteor Shower Weather on " + RoundManager.Instance.currentLevel.name);
        alreadyUsedNodes = new();
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
        random = new System.Random(StartOfRound.Instance.randomMapSeed);
        nodes = CullNodesByProximity(RoundManager.Instance.outsideAINodes, 5.0f, true);
        // eventually gonna have a config bool to turn on override prefabs and maybe have more prefab options.
        SpawnOverheadVisualMeteors(random.Next(15, 45));

        if (!IsAuthority()) return;
        Direction[] directions = [Direction.Random, Direction.East, Direction.West, Direction.North, Direction.South];
        int index = random.Next(directions.Length);
        direction = directions[index];

        spawnHandler = StartCoroutine(MeteorSpawnerHandler());
    }

    private void OnDisable()
    {
        Plugin.ExtendedLogging("Cleaning up Weather.");
        ChangeCurrentLevelMaximumPower(outsidePower: 3, insidePower: -6, dayTimePower: 3);

        ClearCraters();
        ClearMeteors();
        if (!IsAuthority()) return;

        if (spawnHandler != null) StopCoroutine(spawnHandler);
        spawnHandler = null;
    }

    private void ClearMeteors()
    {
        foreach (var meteor in meteors.ToArray())
        {
            if (meteor == null) continue;
            if (meteor.NetworkObject.IsSpawned)
            {
                if (IsAuthority()) meteor.NetworkObject.Despawn();
            }
            else Destroy(meteor.gameObject);
        }
        meteors.Clear();
    }

    private void ClearCraters()
    {
        foreach (var crater in craters.ToArray())
        {
            if (crater == null) continue;
            Destroy(crater.gameObject);
        }
        craters.Clear();
    }

    private void Update()
    {
        if (spawnHandler != null && TimeOfDay.Instance.timeHasStarted && normalisedTimeToLeave <= TimeOfDay.Instance.normalizedTimeOfDay)
        {
            StopCoroutine(spawnHandler);
            spawnHandler = null;
        }
    }

    private void SpawnOverheadVisualMeteors(int amount = 50, GameObject? overridePrefab = null) // todo: make em rotate
    {
        Vector3 averageLocation = CRUtilities.CalculateAverageLandNodePosition(nodes);
        Vector3 centralLocation = averageLocation + new Vector3(0, random.NextFloat(150, 200), 0);
        for (int i = 0; i < amount; i++)
        {
            SpawnVisualMeteors(
                overridePrefab: overridePrefab,
                centralLocation: centralLocation,
                offset: new Vector3(random.NextFloat(-175, 175), random.NextFloat(-50, 50), random.NextFloat(-175, 175)),
                speed: 2f,
                sizeMultiplier: random.NextFloat(1.5f, 4));
        }
        for (int i = 0; i < 1; i++)
        {
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
        bool isBig = sizeMultiplier > 5f;
        meteor.SetupAsLooping(isBig);
    }

    private IEnumerator MeteorSpawnerHandler()
    {
        yield return new WaitForSeconds(25f); // inital delay to get everything started
        while (true)
        {
            for (int i = 0; i < random.Next(minMeteorsPerSpawn, maxMeteorsPerSpawn); i++)
            {
                SpawnMeteor(GetRandomTargetPosition(nodes, alreadyUsedNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25));
                yield return null;
            }
            float delay = random.NextFloat(minTimeBetweenSpawns, maxTimeBetweenSpawns);
            yield return new WaitForSeconds(delay);
        }
    }

    public Vector3 CalculateSkyOrigin(Vector3 target)
    {
        float x = 0, z = 0;
        float distanceX = random.NextFloat(250, 500);
        float distanceZ = random.NextFloat(250, 500);

        switch (direction)
        {
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

        float y = random.NextFloat(600, 900); // Fixed vertical range

        return target + new Vector3(x, y, z);
    }

    public void SpawnMeteor(Vector3 target, GameObject? overridePrefab = null)
    {
        Plugin.ExtendedLogging("spawning meteor");
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
            origin = CalculateSkyOrigin(target);
        }

        GameObject prefab = overridePrefab ?? WeatherHandler.Instance.Meteorite.MeteorPrefab;
        CodeRebirthUtils.Instance.CreateFallingObject<Meteors>(prefab, origin, target, Plugin.ModConfig.ConfigMeteorSpeed.Value);
    }
}