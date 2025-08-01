using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Maps;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

public class GuardsmanTurret : MonoBehaviour
{
    [SerializeField]
    private AudioSource _audioSource = null!;
    [SerializeField]
    private AudioClip[] _shootSounds = null!;
    [SerializeField]
    private Guardsman GuardsmanOwner = null!;

    [HideInInspector]
    public EnemyAI? targetEnemy = null;

    internal List<GuardsmanBullet> bulletsPool = new();
    private float hitTimer = 5f;

    private IEnumerator Start()
    {
        if (!NetworkManager.Singleton.IsServer)
            yield break;

        for (int i = 0; i < 5; i++)
        {
            yield return null;

            var bullet = Instantiate(MapObjectHandler.Instance.Merchant!.ProjectilePrefab, transform.position, Quaternion.identity);
            GuardsmanBullet GSbullet = bullet.GetComponent<GuardsmanBullet>();
            GSbullet.GuardsmanTurret = this;
            GSbullet.NetworkObject.Spawn();
        }
    }

    public void Update()
    {
        if (GuardsmanOwner.isEnemyDead)
            return;

        if (bulletsPool.Count == 0)
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

        ShootShot();
    }

    private void ShootShot()
    {
        Vector3 startingPosition = this.transform.position + (this.transform.forward * 2f);
        Vector3 directionOfTravel = this.transform.forward;
        _audioSource.transform.position = startingPosition;
        _audioSource.PlayOneShot(_shootSounds[Random.Range(0, _shootSounds.Length)]); // play this on OnNetworkSpawn
        bulletsPool[0].SetMovingDirection(startingPosition, directionOfTravel, 30f);
    }

    private void HandleFindingTargetEnemy()
    {
        foreach (var enemy in RoundManager.Instance.SpawnedEnemies)
        {
            if (enemy is Guardsman)
                continue;

            if (GuardsmanOwner.targetEnemy == enemy)
                continue;

            if (GuardsmanOwner._internalEnemyBlacklist.Contains(enemy.enemyType.enemyName))
                continue;

            Vector3 direction = (enemy.transform.position - transform.position).normalized;
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy > 140f)
                continue;

            if (Physics.Raycast(this.transform.position, direction, distanceToEnemy, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                continue;

            targetEnemy = enemy;
            break;
        }
    }

    public void OnDestroy()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        foreach (var bullet in bulletsPool.ToArray())
        {
            bullet.NetworkObject.Despawn(true);
        }
    }
}