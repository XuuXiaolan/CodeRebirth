using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class EnemySpawnerTracker : MonoBehaviour
{
    [HideInInspector]
    public EnemyAI enemyAI = null!;

    private bool _removedFromSpawnedEnemies = false;
    public void Update()
    {
        if (enemyAI == null)
        {
            Destroy(this);
            return;
        }

        if (enemyAI.isEnemyDead && !_removedFromSpawnedEnemies)
        {
            EnemyLevelSpawner.entitiesSpawned[enemyAI.enemyType]--;
            _removedFromSpawnedEnemies = true;
            Destroy(this);
        }
    }

    public void OnDestroy()
    {
        if (_removedFromSpawnedEnemies)
            return;

        EnemyLevelSpawner.entitiesSpawned[enemyAI.enemyType]--;
        Destroy(this);
    }
}