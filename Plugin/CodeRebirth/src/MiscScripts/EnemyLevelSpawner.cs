using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using Unity.Netcode;
using UnityEngine;

public class EnemyLevelSpawner : MonoBehaviour
{
    public bool daytimeSpawner = false;
    public Transform spawnPosition = null!;
    public float spawnTimerStart = 10f;
    public float spawnTimerMin = 20f;
    public float spawnTimerMax = 30f;
    public int maxEnemiesToSpawn = 999;

    private float spawnTimer = 10f;

    public static Dictionary<EnemyType, int> entitiesSpawned = new();
    private float enemiesSpawnedByPipe = 0;
    private IEnumerable<(EnemyType, float)> outsideEnemies;
    private IEnumerable<(EnemyType, float)> daytimeEnemies;

    public void Start()
    {
        outsideEnemies = RoundManager.Instance.currentLevel.OutsideEnemies.Select(x => (x.enemyType, (float)x.rarity));
        daytimeEnemies = RoundManager.Instance.currentLevel.DaytimeEnemies.Select(x => (x.enemyType, (float)x.rarity));
        spawnTimer = spawnTimerStart;
    }

    public void Update()
    {
        if (!NetworkManager.Singleton.IsServer || RoundManager.Instance.currentOutsideEnemyPower > RoundManager.Instance.currentLevel.maxOutsideEnemyPowerCount) return;
        if (maxEnemiesToSpawn >= enemiesSpawnedByPipe) return;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0) return;
        spawnTimer = Random.Range(spawnTimerMin, spawnTimerMax);

        EnemyType? enemyType;
        if (!daytimeSpawner) enemyType = CRUtilities.ChooseRandomWeightedType(outsideEnemies);
        else enemyType = CRUtilities.ChooseRandomWeightedType(daytimeEnemies);
        if (enemyType == null || (entitiesSpawned.TryGetValue(enemyType, out int value) && value >= enemyType.MaxCount)) return;

        entitiesSpawned[enemyType] = value + 1;
        enemiesSpawnedByPipe++;
        RoundManager.Instance.SpawnEnemyGameObject(spawnPosition.position, -1, -3, enemyType);
    }

    public void OnDestroy()
    {
        entitiesSpawned.Clear();
    }
}