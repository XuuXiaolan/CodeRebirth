using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using Unity.Netcode;
using UnityEngine;

public class EnemyLevelSpawner : MonoBehaviour
{
    [Header("Misc")]
    public bool daytimeSpawner = false;
    public Transform spawnPosition = null!;
    public int maxEnemiesToSpawn = 999;
    [Header("Timers")]
    public float spawnTimerStart = 10f;
    public float spawnTimerMin = 20f;
    public float spawnTimerMax = 30f;
    [Header("Special Enemies")]
    [Tooltip("Use EnemyType.EnemyName here.")]
    public List<StringWithRarity> specialEnemiesToInclude = new();

    private float spawnTimer = 10f;
    private System.Random pipeRandom = new();

    public static Dictionary<EnemyType, int> entitiesSpawned = new();
    private float enemiesSpawnedByPipe = 0;
    private List<(EnemyType, float)> specialEnemies = new();
    private IEnumerable<(EnemyType, float)> outsideEnemies;
    private IEnumerable<(EnemyType, float)> daytimeEnemies;

    public void Start()
    {
        foreach (var enemyNameWithRarity in specialEnemiesToInclude)
        {
            foreach (var enemyType in CodeRebirthUtils.EnemyTypes)
            {
                if (enemyType.enemyName == enemyNameWithRarity.EnemyName)
                    continue;

                specialEnemies.Add((enemyType, enemyNameWithRarity.Rarity));
                break;
            }
        }
        pipeRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        foreach (var entity in entitiesSpawned)
        {
            Plugin.ExtendedLogging($"{entity.Key.enemyName} spawned {entity.Value} times");
        }
        outsideEnemies = RoundManager.Instance.currentLevel.OutsideEnemies.Select(x => (x.enemyType, (float)x.rarity)).Concat(specialEnemies);
        daytimeEnemies = RoundManager.Instance.currentLevel.DaytimeEnemies.Select(x => (x.enemyType, (float)x.rarity));
        spawnTimer = spawnTimerStart;
    }

    public void Update()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if ((!daytimeSpawner && RoundManager.Instance.currentOutsideEnemyPower > RoundManager.Instance.currentLevel.maxOutsideEnemyPowerCount) || (daytimeSpawner && RoundManager.Instance.currentDaytimeEnemyPower > RoundManager.Instance.currentLevel.maxDaytimeEnemyPowerCount))
            return;

        if (enemiesSpawnedByPipe >= maxEnemiesToSpawn)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0)
            return;

        spawnTimer = pipeRandom.NextFloat(spawnTimerMin, spawnTimerMax);

        EnemyType? enemyType;
        if (!daytimeSpawner)
        {
            enemyType = CRUtilities.ChooseRandomWeightedType(outsideEnemies);
        }
        else
        {
            enemyType = CRUtilities.ChooseRandomWeightedType(daytimeEnemies);
        }

        if (enemyType == null)
            return;

        if (entitiesSpawned.TryGetValue(enemyType, out int value) && value >= enemyType.MaxCount)
            return;

        entitiesSpawned[enemyType] = value + 1;
        enemiesSpawnedByPipe++;
        Plugin.ExtendedLogging($"Spawning {enemyType.enemyName}");
        GameObject gameObject = RoundManager.Instance.SpawnEnemyGameObject(spawnPosition.position, -1, -3, enemyType);
        var tracker = gameObject.AddComponent<EnemySpawnerTracker>();
        tracker.enemyAI = gameObject.GetComponent<EnemyAI>();
    }

    public void OnDestroy()
    {
        entitiesSpawned.Clear();
    }
}