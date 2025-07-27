using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class EnemyLevelSpawner : MonoBehaviour
{
    [Header("Misc")]
    public bool daytimeSpawner = false;
    public List<EnemySize> enemySizesAllowed = new()
    {
        EnemySize.Tiny,
        EnemySize.Medium,
        EnemySize.Giant
    };
    public Transform spawnPosition = null!;
    public int maxEnemiesToSpawn = 999;
    [Header("Timers")]
    public float spawnTimerStart = 10f;
    public float spawnTimerMin = 20f;
    public float spawnTimerMax = 30f;
    [Header("Special Enemies")]
    [Tooltip("Use EnemyType.EnemyName here.")]
    public List<string> specialEnemiesToSpawn = new();

    internal float spawnTimer = 10f;
    public static Dictionary<EnemyType, int> entitiesSpawned = new(); // fix a bunch of this stuff here, make this a list for example
    private float enemiesSpawnedByPipe = 0;
    private List<(EnemyType, float)> specialEnemies = new();
    private IEnumerable<(EnemyType, float)> mainEnemiesToSpawn;
    private IEnumerable<(EnemyType, float)> ambientEnemiesToSpawn;
    [HideInInspector]
    public static List<EnemyLevelSpawner> enemyLevelSpawners = new();

    public void Start()
    {
        enemyLevelSpawners.Add(this);
        foreach (var enemyName in specialEnemiesToSpawn)
        {
            foreach (var enemyTypeWithRarity in RoundManager.Instance.currentLevel.Enemies)
            {
                if (enemyTypeWithRarity.enemyType.enemyName != enemyName)
                    continue;

                specialEnemies.Add((enemyTypeWithRarity.enemyType, enemyTypeWithRarity.rarity));
                break;
            }
        }
        foreach (var entity in entitiesSpawned)
        {
            Plugin.ExtendedLogging($"{entity.Key.enemyName} spawned {entity.Value} times");
        }
        mainEnemiesToSpawn = RoundManager.Instance.currentLevel.OutsideEnemies.Select(x => (x.enemyType, (float)x.rarity)).Concat(specialEnemies);
        mainEnemiesToSpawn = mainEnemiesToSpawn.Where(x => enemySizesAllowed.Contains(x.Item1.EnemySize));
        ambientEnemiesToSpawn = RoundManager.Instance.currentLevel.DaytimeEnemies.Select(x => (x.enemyType, (float)x.rarity));
        spawnTimer = spawnTimerStart * 4f;
        spawnTimerMax *= 4f;
        spawnTimerMin *= 4f;
        if (spawnTimerMin > spawnTimerMax)
        {
            spawnTimerMin = spawnTimerMax;
        }
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

        spawnTimer = UnityEngine.Random.Range(spawnTimerMin, spawnTimerMax);

        SpawnRandomEnemy();
    }

    public EnemyAI? SpawnRandomEnemy()
    {
        EnemyType? enemyType;
        if (!daytimeSpawner)
        {
            enemyType = CRUtilities.ChooseRandomWeightedType(mainEnemiesToSpawn);
        }
        else
        {
            enemyType = CRUtilities.ChooseRandomWeightedType(ambientEnemiesToSpawn);
        }

        if (enemyType == null)
            return null;

        if (entitiesSpawned.TryGetValue(enemyType, out int value) && value >= enemyType.MaxCount)
            return null;

        entitiesSpawned[enemyType] = value + 1;
        enemiesSpawnedByPipe++;
        Plugin.ExtendedLogging($"Spawning {enemyType.enemyName}");
        GameObject enemyGameObject = RoundManager.Instance.SpawnEnemyGameObject(spawnPosition.position, -1, -3, enemyType);
        var tracker = enemyGameObject.AddComponent<EnemySpawnerTracker>();
        EnemyAI enemyAI = enemyGameObject.GetComponent<EnemyAI>();
        tracker.enemyAI = enemyAI;
        if (enemyAI.enemyType.canDie)
        {
            RoundManager.Instance.currentOutsideEnemyPower += enemyAI.enemyType.PowerLevel;
        }
        return tracker.enemyAI;
    }

    public void OnDestroy()
    {
        enemyLevelSpawners.Remove(this);
        entitiesSpawned.Clear();
    }
}