using System.Collections;
using CodeRebirth.src.Util;
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

        if (_removedFromSpawnedEnemies)
            return;

        if (enemyAI.isEnemyDead)
        {
            StartCoroutine(HandleRemovingFromSpawnedEnemies(false));
        }
    }

    public void OnDestroy()
    {
        StartCoroutine(HandleRemovingFromSpawnedEnemies(true));
    }

    private IEnumerator HandleRemovingFromSpawnedEnemies(bool immediate)
    {
        _removedFromSpawnedEnemies = true;

        if (!immediate)
        {
            yield return new WaitForSeconds(15);
        }

        if (enemyAI.creatureAnimator != null)
        {
            enemyAI.creatureAnimator.enabled = false;
        }

        if (enemyAI.creatureVoice != null)
        {
            enemyAI.creatureVoice.enabled = false;
        }

        if (enemyAI.creatureSFX != null)
        {
            enemyAI.creatureSFX.enabled = false;
        }

        ExtraEnemyData extraEnemyData = CodeRebirthUtils.ExtraEnemyDataDict[enemyAI];
        foreach (var enemyAICollisionDetect in extraEnemyData.enemyAICollisionDetects)
        {
            enemyAICollisionDetect.enabled = false;
        }

        EnemyLevelSpawner.entitiesSpawned[enemyAI.enemyType]--;
        enemyAI.enabled = false;
        Destroy(this);
    }
}