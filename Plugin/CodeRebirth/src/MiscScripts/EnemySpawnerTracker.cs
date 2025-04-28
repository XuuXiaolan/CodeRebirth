using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class EnemySpawnerTracker : MonoBehaviour
{
    [HideInInspector]
    public EnemyAI enemyAI = null!;
    public void Update()
    {
        if (enemyAI == null || enemyAI.isEnemyDead)
        {
            Destroy(this);
            return;
        }

        if (enemyAI.isEnemyDead)
        {
            EnemyLevelSpawner.entitiesSpawned[enemyAI.enemyType]--;
            Destroy(this);
        }
    }
}