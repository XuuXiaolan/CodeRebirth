using System.Collections;
using CodeRebirth.src.Util;
using Dawn;
using Dawn.Utils;
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

        DawnEnemyAdditionalData enemyAdditionalData = DawnEnemyAdditionalData.CreateOrGet(enemyAI);
        foreach (var enemyAICollisionDetect in enemyAdditionalData.EnemyAICollisionDetects)
        {
            Destroy(enemyAICollisionDetect);
        }

        EnemyLevelSpawner.entitiesSpawned[enemyAI.enemyType]--;
        enemyAI.enabled = false;
        Destroy(this);
    }
}