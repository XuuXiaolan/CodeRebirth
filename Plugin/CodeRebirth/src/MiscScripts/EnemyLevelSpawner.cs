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

    private float spawnTimer = 10f;

    public static Dictionary<EnemyType, int> entitiesSpawned = new();
    private IEnumerable<(EnemyType, float)> outsideEnemies;

    public void Start()
    {
        outsideEnemies = RoundManager.Instance.currentLevel.OutsideEnemies.Select(x => (x.enemyType, (float)x.rarity));
        spawnTimer = spawnTimerStart;
    }

    public void Update()
    {
        if (!NetworkManager.Singleton.IsServer || RoundManager.Instance.currentOutsideEnemyPower > RoundManager.Instance.currentLevel.maxOutsideEnemyPowerCount) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0) return;
        spawnTimer = Random.Range(spawnTimerMin, spawnTimerMax);
        
        EnemyType? enemyType = CRUtilities.ChooseRandomWeightedType(outsideEnemies);
        if (enemyType == null || entitiesSpawned[enemyType] >= enemyType.MaxCount) return;

        entitiesSpawned[enemyType] += 1;
        RoundManager.Instance.SpawnEnemyGameObject(spawnPosition.position, -1, -3, enemyType);
    }

    public void OnDestroy()
    {
        entitiesSpawned.Clear();
    }
}