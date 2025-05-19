using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

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
        const float maxSampleDistance = 2f; // how far from current position to look for NavMesh
        NavMeshHit hit;

        // Initial placement or re‐placement onto NavMesh
        foreach (var smallRigo in smallRigosActive)
        {
            if (smallRigo == null) continue;

            // Try to sample the NavMesh at or around the current position
            if (NavMesh.SamplePosition(goldRigo.transform.position, out hit, maxSampleDistance, smallRigo.agent.areaMask))
            {
                smallRigo.transform.position = hit.position;
                smallRigo.agent.enabled = true;
                smallRigo.agent.Warp(hit.position);
            }
            else
            {
                smallRigo.agent.enabled = false;
            }
        }

        // Main loop while active
        while (active)
        {
            foreach (var smallRigo in smallRigosActive)
            {
                if (smallRigo == null) continue;

                // If agent is disabled, keep checking for NavMesh to re‐enable it
                if (!smallRigo.agent.enabled || !smallRigo.agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(goldRigo.transform.position, out hit, maxSampleDistance, smallRigo.agent.areaMask))
                    {
                        smallRigo.transform.position = hit.position;
                        smallRigo.agent.enabled = true;
                        smallRigo.agent.Warp(hit.position);
                    }
                    else
                    {
                        // Still no NavMesh nearby – skip pathing this frame
                        continue;
                    }
                }

                // At this point, agent is on NavMesh
                float distanceToKing = Vector3.Distance(smallRigo.transform.position, goldRigo.transform.position);

                // Handle jumping logic if GoldRigo is being held
                if (goldRigo.playerHeldBy != null)
                {
                    if (distanceToKing <= 2f && !smallRigo.jumping)
                    {
                        smallRigo.SetJumping(true);
                        continue;
                    }
                    else if (distanceToKing > 2f && smallRigo.jumping)
                    {
                        smallRigo.SetJumping(false);
                    }

                    smallRigo.DoPathingToPosition(goldRigo.playerHeldBy.transform.position);
                }
                else
                {
                    if (distanceToKing > 1f)
                        smallRigo.DoPathingToPosition(goldRigo.transform.position);
                }

                yield return null;
            }

            // A short delay between waves of pathing checks
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