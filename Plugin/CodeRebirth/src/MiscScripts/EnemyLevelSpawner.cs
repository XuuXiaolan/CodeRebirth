using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using Unity.Netcode;
using UnityEngine;

public class EnemyLevelSpawner : MonoBehaviour
{
    public Transform spawnPosition = null!;
    public float spawnTimerStart = 10f;
    public float spawnTimerMin = 20f;
    public float spawnTimerMax = 30f;

    private float spawnTimer = 10f;

    public void Start()
    {
        spawnTimer = spawnTimerStart;
    }

    public void Update()
    {
        if (!NetworkManager.Singleton.IsServer || RoundManager.Instance.currentOutsideEnemyPower <= 0) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = Random.Range(spawnTimerMin, spawnTimerMax);
            EnemyType? enemyType = CRUtilities.ChooseRandomWeightedType(RoundManager.Instance.currentLevel.OutsideEnemies.Select(x => (x.enemyType, (float)x.rarity)));
            RoundManager.Instance.SpawnEnemyGameObject(spawnPosition.position, -1, -3, enemyType);
        }
    }
}