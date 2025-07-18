using System.Collections;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class ForceEnemyToDestination : MonoBehaviour
{
    public EnemyAI? enemy = null;
    public Vector3 Destination = Vector3.zero;
    public bool needed = true;

    public IEnumerator Start()
    {
        yield return new WaitUntil(() => enemy != null && Destination != Vector3.zero);
        if (enemy is Janitor janitor)
        {
            janitor.CalculateAndSetNewPath(Destination);
        }
    }

    public void Update()
    {
        if (!needed)
        {
            this.enabled = false;
        }
        if (enemy == null || Destination == Vector3.zero)
            return;

        if (enemy.agent == null || !enemy.agent.enabled)
            return;

        if (enemy.targetPlayer != null || Vector3.Distance(enemy.transform.position, Destination) <= 10f + enemy.agent.stoppingDistance)
        {
            needed = false;
            return;
        }

        if (enemy is CodeRebirthEnemyAI codeRebirthEnemyAI)
        {
            if (codeRebirthEnemyAI is Janitor janitor)
            {
                if (janitor.currentBehaviourStateIndex != (int)Janitor.JanitorStates.Idle)
                {
                    needed = false;
                }
                return;
            }
            codeRebirthEnemyAI.smartAgentNavigator.DoPathingToDestination(Destination);
            return;
        }
        enemy.agent.SetDestination(Destination);
    }
}