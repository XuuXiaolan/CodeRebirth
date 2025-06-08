using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class ExtraEnemyData : MonoBehaviour
{
    [HideInInspector]
    public EnemyAI enemyAI = null!;

    [HideInInspector]
    public float coinDropChance = 0f;

    [HideInInspector]
    public bool rolledForCoin = false;

    [HideInInspector]
    public bool enemyKilledByPlayer = false;

    [HideInInspector]
    public EnemyAICollisionDetect[] enemyAICollisionDetects = [];

    public void Start()
    {
        enemyAICollisionDetects = enemyAI.gameObject.GetComponentsInChildren<EnemyAICollisionDetect>(true);
    }
}