using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class GuardsmanTurret : MonoBehaviour
{
    [SerializeField]
    private Guardsman GuardsmanOwner = null!;

    [HideInInspector]
    public EnemyAI? targetEnemy = null;

    private float hitTimer = 5f;

    public void LateUpdate()
    {
        if (GuardsmanOwner.isEnemyDead)
            return;

        if (targetEnemy == null)
        {
            HandleFindingTargetEnemy();
            return;
        }

        if (targetEnemy.isEnemyDead)
        {
            targetEnemy = null;
            return;
        }

        hitTimer -= Time.deltaTime;
        if (hitTimer > 0)
            return;

        hitTimer = 5f;

        Vector3 direction = targetEnemy.transform.position - transform.position;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5 * Time.deltaTime);

        if (Vector3.Dot(transform.forward, direction.normalized) > 0.7f)
            return;

        // CRUtilities.CreateExplosion(targetEnemy.transform.position, true, 25, 0, 6, 1, null, null, 25f);
    }

    private void HandleFindingTargetEnemy()
    {
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy is Guardsman)
                continue;

            if (GuardsmanOwner.targetEnemy != null && GuardsmanOwner.targetEnemy == enemy)
                continue;

            if (GuardsmanOwner.EnemyHasLineOfSightToPosition(this.transform.position, 360, 70, 0))
            {
                targetEnemy = enemy;
                break;
            }
        }
    }
}