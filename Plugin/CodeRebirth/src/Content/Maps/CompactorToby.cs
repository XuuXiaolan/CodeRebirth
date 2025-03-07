using System.Collections;
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
    public Transform outputTransform = null!;
    public Transform[] spawnTransforms = null!;

    [HideInInspector] public bool compacting = false;
    private static readonly int HitAnimation = Animator.StringToHash("hit"); // Trigger
    private static readonly int StartCompactAnimation = Animator.StringToHash("startCompact"); // Trigger
    private static readonly int FastEndCompactAnimation = Animator.StringToHash("fastEnd"); // Trigger
    private static readonly int SlowEndCompactAnimation = Animator.StringToHash("slowEnd"); // Trigger

    public void CompactorInteract(PlayerControllerB player)
    {
        if (compacting) return;
        if (!player.IsOwner) return;
        int valueOfItems = 0;
        foreach (var grabbableObject in transform.GetComponentsInChildren<GrabbableObject>())
        {
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

            valueOfItems += grabbableObject.scrapValue - 5;
        }
        TryCompactItemServerRpc(valueOfItems, false, false);
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (compacting) return false;
        TriggerAnimationServerRpc(HitAnimation);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryCompactItemServerRpc(int value, bool deadPlayer, bool fast)
    {
        StartOrStopCompactingClientRpc(true);
        StartCoroutine(CompactProcess(value, deadPlayer, fast));
    }

    private IEnumerator CompactProcess(int value, bool deadPlayer, bool fast)
    {
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
                int randomIndex = UnityEngine.Random.Range(0, spawnTransforms.Length);
                EnemyType randomEnemy = RoundManager.Instance.currentLevel.OutsideEnemies[UnityEngine.Random.Range(0, RoundManager.Instance.currentLevel.OutsideEnemies.Count)].enemyType;
                var NetObjRef = RoundManager.Instance.SpawnEnemyGameObject(spawnTransforms[randomIndex].position, -1, -1, randomEnemy);
                if (((GameObject)NetObjRef).TryGetComponent(out EnemyAI enemyAI))
                {
                    enemyAI.agent.SetDestination(this.transform.position);
                }
            }
        }
        StartOrStopCompactingClientRpc(false);
        if (deadPlayer)
        {
            CodeRebirthUtils.Instance.SpawnScrap(MapObjectHandler.Instance.CompactorToby.FlatDeadPlayerScrap, outputTransform.position, false, true, value);
            yield break;
        }
        CodeRebirthUtils.Instance.SpawnScrap(MapObjectHandler.Instance.CompactorToby.SallyCubesScrap, outputTransform.position, false, true, value);
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