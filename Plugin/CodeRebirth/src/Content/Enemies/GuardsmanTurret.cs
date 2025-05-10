using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class GuardsmanTurret : MonoBehaviour
{
    [SerializeField]
    private Guardsman GuardsmanOwner = null!;

    [HideInInspector]
    public EnemyAI? targetEnemy = null;

    private float hitTimer = 5f;

    public void Update()
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

        Vector3 normalizedDirection = (targetEnemy.transform.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5 * Time.deltaTime);

        hitTimer -= Time.deltaTime;
        if (hitTimer > 0)
            return;

        hitTimer = 5f;

        float dot = Vector3.Dot(transform.forward, normalizedDirection);
        if (dot < 0.7)
            return;

        if (Physics.Raycast(this.transform.position, this.transform.forward, out RaycastHit hit, 999, CodeRebirthUtils.Instance.collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleAndDefaultMask, QueryTriggerInteraction.Ignore))
        {
            CRUtilities.CreateExplosion(hit.point, true, 25, 0, 6, 1, null, null, 25f);
        }
    }

    private void HandleFindingTargetEnemy()
    {
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy is Guardsman)
                continue;

            if (GuardsmanOwner.targetEnemy == enemy)
                continue;

            if (GuardsmanOwner._internalEnemyBlacklist.Contains(enemy.enemyType))
                continue;

            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy > 140f)
                continue;

            if (Physics.Raycast(this.transform.position, enemy.transform.position - this.transform.position, distanceToEnemy, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                continue;

            targetEnemy = enemy;
            break;
        }
    }
}