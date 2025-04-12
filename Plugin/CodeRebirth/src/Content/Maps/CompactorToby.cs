using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class CompactorToby : NetworkBehaviour, IHittable
{
    public Animator tobyAnimator = null!;
    public NetworkAnimator tobyNetworkAnimator = null!;
    public GameObject[] spawnTransforms = [];

    [HideInInspector] public bool compacting = false;
    private static readonly int HitAnimation = Animator.StringToHash("hit"); // Trigger
    private static readonly int StartCompactAnimation = Animator.StringToHash("startCompact"); // Trigger
    private static readonly int FastEndCompactAnimation = Animator.StringToHash("fastEnd"); // Trigger
    private static readonly int SlowEndCompactAnimation = Animator.StringToHash("slowEnd"); // Trigger

    public void Start()
    {
        spawnTransforms = GameObject.FindGameObjectsWithTag("EnemySpawn");
    }

    public void CompactorInteract(PlayerControllerB player)
    {
        if (compacting) return;
        if (!player.IsOwner) return;
        int valueOfItems = 0;
        bool isFast = true;
        List<GrabbableObject> grabbableObjects = new();
        List<Vector3> vectorPositions = new();
        foreach (var grabbableObject in transform.GetComponentsInChildren<GrabbableObject>())
        {
            if (grabbableObject.isHeld) continue;
            grabbableObjects.Add(grabbableObject);
            vectorPositions.Add(grabbableObject.transform.position);
            if (grabbableObject.itemProperties.itemName.Contains("Shredded Scraps"))
            {
                valueOfItems += grabbableObject.scrapValue + 12;
                continue;
            }
            else if (grabbableObject is RagdollGrabbableObject)
            {
                valueOfItems += grabbableObject.scrapValue + 10;
                continue;
            }
            isFast = false;
            valueOfItems += grabbableObject.scrapValue - 5;
        }
        if (valueOfItems == 0) return;
        foreach (GrabbableObject grabbableObject in grabbableObjects)
        {
            DespawnItemServerRpc(new NetworkBehaviourReference(grabbableObject));
        }
        Vector3 randomPosition = vectorPositions[UnityEngine.Random.Range(0, vectorPositions.Count)];
        TryCompactItemServerRpc(randomPosition, valueOfItems, false, isFast);
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        Plugin.ExtendedLogging($"Hit");
        if (compacting) return false;
        TriggerAnimationServerRpc(HitAnimation);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryCompactItemServerRpc(Vector3 randomPosition, int value, bool deadPlayer, bool fast)
    {
        StartOrStopCompactingClientRpc(true);
        StartCoroutine(CompactProcess(randomPosition, value, deadPlayer, fast));
    }

    private IEnumerator CompactProcess(Vector3 randomPosition, int value, bool deadPlayer, bool fast)
    {
        Plugin.ExtendedLogging($"Value: {value} | Dead Player: {deadPlayer} | Fast: {fast}");
        tobyNetworkAnimator.SetTrigger(StartCompactAnimation);
        yield return new WaitForSeconds(20);
        float timeElapsed = 0f;
        float timeToWait = fast ? 5f : 30f;
        if (fast)
        {
            tobyNetworkAnimator.SetTrigger(FastEndCompactAnimation);
        }
        else
        {
            tobyNetworkAnimator.SetTrigger(SlowEndCompactAnimation);
        }

        float Timethreshold = 2.4f;
        while (timeElapsed < timeToWait)
        {
            yield return null;
            timeElapsed += Time.deltaTime;
            Timethreshold -= Time.deltaTime;
            if (Timethreshold <= 0f)
            {
                Timethreshold = 2.4f;
                Plugin.ExtendedLogging("Spawning Enemy");
                int randomIndex = UnityEngine.Random.Range(0, spawnTransforms.Length);
                EnemyType? enemyType = CRUtilities.ChooseRandomWeightedType(RoundManager.Instance.currentLevel.OutsideEnemies.Select(x => (x.enemyType, (float)x.rarity)));
                if (enemyType != null && enemyType.MaxCount == 1 && RoundManager.Instance.SpawnedEnemies.Any(x => x.enemyType == enemyType))
                {
                    enemyType = null;
                }

                var NetObjRef = RoundManager.Instance.SpawnEnemyGameObject(spawnTransforms[randomIndex].transform.position, -1, -1, enemyType);
                if (((GameObject)NetObjRef).TryGetComponent(out EnemyAI enemyAI) && enemyAI.agent != null)
                {
                    var component = enemyAI.gameObject.AddComponent<ForceEnemyToDestination>();
                    component.enemy = enemyAI;
                    component.Destination = this.transform.position;
                }
            }
        }
        StartOrStopCompactingClientRpc(false);
        if (deadPlayer)
        {
            CodeRebirthUtils.Instance.SpawnScrap(MapObjectHandler.Instance.CompactorToby?.ItemDefinitions.GetCRItemDefinitionWithItemName("Flat Shredded Scrap")?.item, randomPosition, false, true, value);
            yield break;
        }
        CodeRebirthUtils.Instance.SpawnScrap(MapObjectHandler.Instance.CompactorToby?.ItemDefinitions.GetCRItemDefinitionWithItemName("Sally Cube")?.item, randomPosition, false, true, value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TriggerAnimationServerRpc(int hash)
    {
        tobyNetworkAnimator.SetTrigger(hash);
    }

    [ClientRpc]
    public void StartOrStopCompactingClientRpc(bool starting)
    {
        compacting = starting;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DespawnItemServerRpc(NetworkBehaviourReference networkBehaviourReference)
    {
        networkBehaviourReference.TryGet(out GrabbableObject grabbableObject);
        grabbableObject.NetworkObject.Despawn();
    }
}