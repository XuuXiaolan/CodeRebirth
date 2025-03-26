using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class SmallRigoManager : NetworkBehaviour
{
    public GoldRigo goldRigo = null!;
    public int numberOfSmallRigos = 20;

    private bool active = false;
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

        if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving)
        {
            StartCoroutine(SpawnSmallRigos(numberOfSmallRigos, true));
        }
        else
        {
            StartCoroutine(SpawnSmallRigos(numberOfSmallRigos, false));
        }
        // Spawn and hide the SmallRigo's on the following conditions: we're in orbit, leaving and/or landing whilst the goldrigo isnt in the ship
    }

    public void Update()
    {
        if (goldRigo == null)
        {
            if (!IsServer) return;
            foreach (var smallRigo in smallRigosActive)
            {
                if (smallRigo == null) continue;
                smallRigo.NetworkObject.Despawn();
            }
            this.NetworkObject.Despawn();
            return;
        }

        if (active)
        {
            if (StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipHasLanded || StartOfRound.Instance.shipIsLeaving)
            {
                Deactivate();
            }
        }
        else
        {
            if (StartOfRound.Instance.inShipPhase && StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.shipIsLeaving)
            {
                Activate();
            }
        }
    }

    public IEnumerator SpawnSmallRigos(int numberOfSmallRigos, bool hideSmallRigos)
    {
        while (smallRigosActive.Count < numberOfSmallRigos)
        {
            yield return new WaitForSeconds(0.1f);
            if (!IsServer) continue;
            GameObject smallRigo = Instantiate(smallRigoPrefab, goldRigo.transform.position, goldRigo.transform.rotation, null);
            smallRigo.GetComponent<NetworkObject>().Spawn();
            var smallRigoScript = smallRigo.GetComponent<SmallRigo>();
            SyncSmallRigoListClientRpc(new NetworkBehaviourReference(smallRigoScript));
        }

        yield return new WaitForSeconds(2f); // just to make sure there's enough time.
        if (!hideSmallRigos)
        {
            Activate();
        }
    }

    [ClientRpc]
    public void SyncSmallRigoListClientRpc(NetworkBehaviourReference smallRigoScript)
    {
        if (smallRigoScript.TryGet(out SmallRigo smallRigo))
        {
            smallRigosActive.Add(smallRigo);
        }
    }

    public void Activate()
    {
        active = true;
        foreach (var smallRigo in smallRigosActive)
        {
            smallRigo.gameObject.SetActive(true);
        }
        StartCoroutine(ActiveRoutine());
    }

    public IEnumerator ActiveRoutine()
    {
        foreach (var smallRigo in smallRigosActive)
        {
            smallRigo.smartAgentNavigator.agent.Warp(goldRigo.transform.position);
            yield return null;
        }
        while (active)
        {
            foreach (SmallRigo smallRigo in smallRigosActive)
            {
                yield return null;
                if (Vector3.Distance(smallRigo.transform.position, goldRigo.transform.position) <= 5f)
                {
                    if (smallRigo.jumping) continue;
                    smallRigo.SetJumping(true);
                    continue;
                }
                if (smallRigo.jumping)
                {
                    smallRigo.SetJumping(false);
                }
                if (goldRigo.playerHeldBy != null)
                {
                    smallRigo.DoPathingToPosition(goldRigo.playerHeldBy.transform.position);
                    continue;
                }
                smallRigo.DoPathingToPosition(goldRigo.transform.position);
            }
            yield return new WaitForSeconds(1f);
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
        if (!IsServer) return;
        if (goldRigo != null && goldRigo.IsSpawned)
        {
            goldRigo.NetworkObject.Despawn(true);
        }
        foreach (var smallRigo in smallRigosActive)
        {
            if (smallRigo == null || !smallRigo.IsSpawned) continue;
            smallRigo.NetworkObject.Despawn(true);
        }
    }
}