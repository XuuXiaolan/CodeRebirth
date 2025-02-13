using CodeRebirth.src.Util;
using CodeRebirth.Util.Extensions;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodeRebirth.src.MiscScripts;
public class CRUtilities
{
    private static Dictionary<int, int> _masksByLayer = new();
    private static AudioReverbPresets? audioReverbPreset = null;
    private static Collider[] cachedColliders = new Collider[16];

    public static void Init()
    {
        GenerateLayerMap();
    }

    public static Transform? TryFindRoot(Transform child)
    {
        Transform current = child;
        while (current != null && current.GetComponent<NetworkObject>() == null)
        {
            current = current.transform.parent;
        }
        return current;
    }

    public static void GenerateLayerMap()
    {
        _masksByLayer = new Dictionary<int, int>();
        for (int i = 0; i < 32; i++)
        {
            int mask = 0;
            for (int j = 0; j < 32; j++)
            {
                if (!Physics.GetIgnoreLayerCollision(i, j))
                {
                    mask |= 1 << j;
                }
            }
            _masksByLayer.Add(i, mask);
        }
    }

    public static int MaskForLayer(int layer)
    {
        return _masksByLayer[layer];
    }

    public static void TeleportPlayerToShip(int playerObj, Vector3 teleportPos)
    {
        PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerObj];

        if (audioReverbPreset != null)
        {
            audioReverbPreset.audioPresets[3].ChangeAudioReverbForPlayer(playerControllerB);
        }
        else
        {
            audioReverbPreset = UnityEngine.Object.FindObjectOfType<AudioReverbPresets>();
            audioReverbPreset.audioPresets[3].ChangeAudioReverbForPlayer(playerControllerB);
        }
        playerControllerB.isInElevator = true;
        playerControllerB.isInHangarShipRoom = true;
        playerControllerB.isInsideFactory = false;
        playerControllerB.averageVelocity = 0f;
        playerControllerB.velocityLastFrame = Vector3.zero;
        StartOfRound.Instance.allPlayerScripts[playerObj].TeleportPlayer(teleportPos);
        StartOfRound.Instance.allPlayerScripts[playerObj].beamOutParticle.Play();
        if (playerControllerB == GameNetworkManager.Instance.localPlayerController)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
        }
    }

    public static IEnumerator TeleportPlayerBody(int playerObj, Vector3 teleportPosition)
    {
        float startTime = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => StartOfRound.Instance.allPlayerScripts[playerObj].deadBody != null || Time.realtimeSinceStartup - startTime > 2f);
        if (StartOfRound.Instance.inShipPhase || SceneManager.sceneCount <= 1)
        {
            yield break;
        }
        DeadBodyInfo deadBody = StartOfRound.Instance.allPlayerScripts[playerObj].deadBody;
        if (deadBody != null)
        {
            deadBody.attachedTo = null;
            deadBody.attachedLimb = null;
            deadBody.secondaryAttachedLimb = null;
            deadBody.secondaryAttachedTo = null;
            if (deadBody.grabBodyObject != null && deadBody.grabBodyObject.isHeld && deadBody.grabBodyObject.playerHeldBy != null)
            {
                deadBody.grabBodyObject.playerHeldBy.DropAllHeldItems();
            }
            deadBody.isInShip = false;
            deadBody.parentedToShip = false;
            deadBody.transform.SetParent(null, worldPositionStays: true);
            deadBody.SetRagdollPositionSafely(teleportPosition, disableSpecialEffects: true);
        }
    }

    public static void TeleportEnemy(EnemyAI enemy, Vector3 teleportPos)
    {
        enemy.serverPosition = teleportPos;
        enemy.transform.position = teleportPos;
        enemy.agent.Warp(teleportPos);
        enemy.SyncPositionToClients();
    }

    private static Dictionary<PlayerControllerB, int> playerControllerBToDamage = new();
    private static Dictionary<EnemyAICollisionDetect, int> enemyAICollisionDetectToDamage = new();
    private static List<Landmine> landmineList = new();
    private static List<IHittable> hittablesList = new();

    public static void CreateExplosion(Vector3 explosionPosition, bool spawnExplosionEffect, int damage, float minDamageRange, float maxDamageRange, int enemyHitForce, PlayerControllerB? attacker, GameObject? overridePrefab, float pushForce)
    {
        Plugin.ExtendedLogging($"Spawning explosion at pos: {explosionPosition}");

        Transform? holder = null;

        if (RoundManager.Instance != null && RoundManager.Instance.mapPropsContainer != null && RoundManager.Instance.mapPropsContainer.transform != null)
        {
            holder = RoundManager.Instance.mapPropsContainer.transform;
        }

        if (spawnExplosionEffect)
        {
            if (overridePrefab == null)
            {
                UnityEngine.Object.Instantiate(StartOfRound.Instance.explosionPrefab, explosionPosition, Quaternion.Euler(-90f, 0f, 0f), holder).SetActive(true);
            }
            else
            {
                UnityEngine.Object.Instantiate(overridePrefab, explosionPosition, Quaternion.Euler(-90f, 0f, 0f), holder).SetActive(true);
            }
        }

        float playerDistanceFromExplosion = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, explosionPosition);
        if (playerDistanceFromExplosion <= 14)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        }
        else if (playerDistanceFromExplosion <= 25)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
        }

        int numHits = Physics.OverlapSphereNonAlloc(explosionPosition, maxDamageRange, cachedColliders, CodeRebirthUtils.Instance.playersAndEnemiesAndHazardMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < numHits; i++)
        {
            if (!cachedColliders[i].TryGetComponent(out IHittable ihittable)) continue;
            float distanceOfObjectFromExplosion = Vector3.Distance(explosionPosition, cachedColliders[i].ClosestPoint(explosionPosition));
            if (distanceOfObjectFromExplosion > 4f && Physics.Linecast(explosionPosition, cachedColliders[i].transform.position + Vector3.up * 0.3f, out _, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            if (cachedColliders[i].gameObject.layer == 3 && cachedColliders[i].TryGetComponent(out PlayerControllerB player))
            {
                int damageToDeal = (int)(damage * (1f - Mathf.Clamp01((distanceOfObjectFromExplosion - minDamageRange) / (maxDamageRange - minDamageRange))));
                if (playerControllerBToDamage.ContainsKey(player) && playerControllerBToDamage[player] < damageToDeal)
                {
                    playerControllerBToDamage[player] = damageToDeal;
                }
                else
                {
                    playerControllerBToDamage.Add(player, damageToDeal);
                }
                continue;
            }

            if (cachedColliders[i].gameObject.layer == 19 && cachedColliders[i].TryGetComponent(out EnemyAICollisionDetect enemy))
            {
                if (enemyAICollisionDetectToDamage.ContainsKey(enemy) && enemyAICollisionDetectToDamage[enemy] < enemyHitForce)
                {
                    enemyAICollisionDetectToDamage[enemy] = enemyHitForce;
                }
                else
                {
                    enemyAICollisionDetectToDamage.Add(enemy, enemyHitForce);
                }
                continue;
            }

            Landmine? componentInChildren = cachedColliders[i].gameObject.GetComponentInChildren<Landmine>();
            if (cachedColliders[i].gameObject.layer == 21 && componentInChildren != null && distanceOfObjectFromExplosion < 6f && !landmineList.Contains(componentInChildren))
            {
                landmineList.Add(componentInChildren);
                continue;
            }
            hittablesList.Add(ihittable);
        }

        foreach (PlayerControllerB player in playerControllerBToDamage.Keys)
        {
            Vector3 directionFromCenter = (player.transform.position - explosionPosition).normalized;
            player.DamagePlayer(playerControllerBToDamage[player], true, false, CauseOfDeath.Burning, 6, false, directionFromCenter * pushForce * 5f);
            player.externalForceAutoFade += directionFromCenter * pushForce;
        }

        foreach (EnemyAICollisionDetect enemy in enemyAICollisionDetectToDamage.Keys)
        {
            enemy.mainScript.HitEnemyOnLocalClient(enemyAICollisionDetectToDamage[enemy], playerWhoHit: attacker);
        }

        foreach (Landmine landmine in landmineList)
        {
            landmine.StartCoroutine(landmine.TriggerOtherMineDelayed(landmine));
        }

        foreach (IHittable hittable in hittablesList)
        {
            hittable.Hit(5, explosionPosition, attacker, true, -1);
        }

        playerControllerBToDamage.Clear();
        enemyAICollisionDetectToDamage.Clear();
        landmineList.Clear();
        hittablesList.Clear();
    }
}