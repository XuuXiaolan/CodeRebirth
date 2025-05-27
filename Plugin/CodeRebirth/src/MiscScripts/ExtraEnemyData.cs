using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ExtraEnemyData : MonoBehaviour
{
    [HideInInspector]
    public EnemyAI enemyAI = null!;

    [HideInInspector]
    public EnemyAICollisionDetect[] enemyAICollisionDetects = [];

    public void Start()
    {
        enemyAICollisionDetects = enemyAI.gameObject.GetComponentsInChildren<EnemyAICollisionDetect>(true);
    }
}