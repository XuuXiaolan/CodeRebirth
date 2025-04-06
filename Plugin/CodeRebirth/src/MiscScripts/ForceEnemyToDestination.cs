using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class ForceEnemyToDestination : MonoBehaviour
{
    public EnemyAI? enemy = null;
    public Vector3 Destination = Vector3.zero;
    public bool needed = true;

    public void Update()
    {
        if (!needed)
        {
            Destroy(this);
            return;
        }
        if (enemy == null) return;
        if (enemy.targetPlayer != null || Vector3.Distance(enemy.transform.position, Destination) <= 8f)
        {
            needed = false;
            return;
        }
        if (enemy.agent == null || !enemy.agent.enabled) return;
        enemy.agent.SetDestination(Destination);
    }
}