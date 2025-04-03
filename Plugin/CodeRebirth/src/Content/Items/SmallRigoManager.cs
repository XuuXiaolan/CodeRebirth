using System.Collections;
using System.Collections.Generic;
using PathfindingLib.Utilities;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class SmallRigoManager : NetworkBehaviour
{
    public GoldRigo goldRigo = null!;
    public int numberOfSmallRigos = 20;

    private bool active = false;
    private bool initalizing = true;
    private GameObject smallRigoPrefab = null!;
    private List<SmallRigo> smallRigosActive = new();

    public void Start()
    {
        if (ItemHandler.Instance.XuAndRigo == null)
        {
            Plugin.Logger.LogError($"How the fuck");
            return;
        }

        smallRigoPrefab = ItemHandler.Instance.XuAndRigo.SmallRigoPrefab;
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
            if (!IsServer) return;
            this.NetworkObject.Despawn();
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
            GameObject smallRigo = Instantiate(smallRigoPrefab, goldRigo.transform.position, goldRigo.transform.rotation, null);
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
            Plugin.ExtendedLogging($"SmallRigo: {smallRigo.transform.position}");
            smallRigo.smartAgentNavigator.agent.Warp(RoundManager.Instance.GetRandomNavMeshPositionInRadius(goldRigo.transform.position, 3, default));
        }
        while (active)
        {
            foreach (SmallRigo smallRigo in smallRigosActive)
            {
                yield return null;
                if (!smallRigo.smartAgentNavigator.agent.isOnNavMesh) smallRigo.smartAgentNavigator.agent.Warp(RoundManager.Instance.GetRandomNavMeshPositionInRadius(goldRigo.transform.position, 5, default));
                float distanceToKing = Vector3.Distance(smallRigo.transform.position, goldRigo.transform.position);
                if (goldRigo.playerHeldBy != null)
                {
                    if (distanceToKing <= 2f)
                    {
                        if (smallRigo.jumping) continue;
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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer && goldRigo != null && goldRigo.IsSpawned)
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