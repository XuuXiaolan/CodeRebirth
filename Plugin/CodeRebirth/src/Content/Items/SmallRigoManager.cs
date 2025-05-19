using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class SmallRigoManager : MonoBehaviour
{
    public GoldRigo goldRigo = null!;
    public int numberOfSmallRigos = 20;

    private bool active = false;
    private bool initalizing = true;
    private GameObject smallRigoPrefab = null!;
    private List<SmallRigo> smallRigosActive = new();

    public void Start()
    {
        goldRigo = this.gameObject.GetComponent<GoldRigo>();
        smallRigoPrefab = ItemHandler.Instance.XuAndRigo!.SmallRigoPrefab;
        StartCoroutine(SpawnSmallRigos(numberOfSmallRigos));
        // Spawn and hide the SmallRigo's on the following conditions: we're in orbit, leaving and/or landing whilst the goldrigo isnt in the ship
    }

    public void Update()
    {
        if (goldRigo == null)
        {
            foreach (var smallRigo in smallRigosActive)
            {
                if (smallRigo == null) continue;
                Destroy(smallRigo);
            }
            if (!NetworkManager.Singleton.IsServer) return;
            Destroy(this);
            return;
        }

        if (initalizing) return;
        if (active)
        {
            if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving)
            {
                Deactivate();
            }
        }
        else
        {
            if (!StartOfRound.Instance.inShipPhase && StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.shipIsLeaving)
            {
                Activate();
            }
        }
    }

    public IEnumerator SpawnSmallRigos(int numberOfSmallRigos)
    {
        while (smallRigosActive.Count < numberOfSmallRigos)
        {
            yield return new WaitForSeconds(0.1f);
            if (goldRigo == null)
            {
                Plugin.Logger.LogError($"GoldRigo is null");
                yield break;
            }
            GameObject smallRigo = Instantiate(smallRigoPrefab, goldRigo.transform.position, goldRigo.transform.rotation);
            smallRigosActive.Add(smallRigo.GetComponent<SmallRigo>());
        }
        initalizing = false;
    }

    public void Activate()
    {
        active = true;
        foreach (var smallRigo in smallRigosActive)
        {
            smallRigo.gameObject.SetActive(true);
        }
        StartCoroutine(VoiceRoutine());
        StartCoroutine(ActiveRoutine());
    }

    public IEnumerator ActiveRoutine()
    {
        foreach (var smallRigo in smallRigosActive)
        {
            yield return null;
            if (!smallRigo.agent.enabled || !smallRigo.agent.isOnNavMesh)
                continue;

            smallRigo.agent.Warp(RoundManager.Instance.GetRandomNavMeshPositionInRadius(goldRigo.transform.position, 3, default));
        }
        while (active)
        {
            foreach (SmallRigo smallRigo in smallRigosActive)
            {
                yield return null;
                if (smallRigo.agent.path.status == UnityEngine.AI.NavMeshPathStatus.PathPartial || smallRigo.agent.path.status == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
                {
                    if (!smallRigo.agent.enabled)
                    {
                        smallRigo.agent.enabled = true;
                    }
                    if (!smallRigo.agent.enabled || !smallRigo.agent.isOnNavMesh)
                        continue;

                    smallRigo.agent.Warp(RoundManager.Instance.GetRandomNavMeshPositionInRadius(goldRigo.transform.position, 5, default));
                }
                float distanceToKing = Vector3.Distance(smallRigo.transform.position, goldRigo.transform.position);
                if (goldRigo.playerHeldBy != null)
                {
                    if (distanceToKing <= 2f)
                    {
                        if (smallRigo.jumping)
                            continue;

                        smallRigo.SetJumping(true);
                        continue;
                    }
                    else
                    {
                        if (smallRigo.jumping)
                        {
                            smallRigo.SetJumping(false);
                        }
                    }
                    smallRigo.DoPathingToPosition(goldRigo.playerHeldBy.transform.position);
                    continue;
                }
                if (distanceToKing <= 1f) continue;
                smallRigo.DoPathingToPosition(goldRigo.transform.position);
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    public IEnumerator VoiceRoutine()
    {
        while (active)
        {
            int randomRigo = Random.Range(0, smallRigosActive.Count - 1);
            smallRigosActive[randomRigo].audioSource.PlayOneShot(smallRigosActive[randomRigo].imarigoSounds[Random.Range(0, smallRigosActive[randomRigo].imarigoSounds.Length)], UnityEngine.Random.Range(0.75f, 1f));
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.75f, 2.5f));
        }
    }

    public void Deactivate()
    {
        active = false;
        foreach (SmallRigo smallRigo in smallRigosActive)
        {
            smallRigo.gameObject.SetActive(false);
        }
    }

    public void OnDestroy()
    {
        if (NetworkManager.Singleton.IsServer && goldRigo != null && goldRigo.IsSpawned)
        {
            goldRigo.NetworkObject.Despawn(true);
        }

        foreach (var smallRigo in smallRigosActive)
        {
            if (smallRigo == null) continue;
            Destroy(smallRigo.gameObject);
        }
    }
}