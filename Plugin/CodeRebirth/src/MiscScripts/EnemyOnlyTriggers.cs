using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class EnemyOnlyTriggers : MonoBehaviour
{
    public EnemyAI mainScript = null!;

    public void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect) && !enemyAICollisionDetect.mainScript.isEnemyDead)
        {
            if (enemyAICollisionDetect.mainScript == mainScript)
                return;

            mainScript.OnCollideWithEnemy(other, enemyAICollisionDetect.mainScript);
        }
    }
}