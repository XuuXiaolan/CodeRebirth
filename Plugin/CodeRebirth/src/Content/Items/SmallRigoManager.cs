using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class SmallRigoManager : MonoBehaviour
{
    public GoldRigo goldRigo = null!;
    public int numberOfSmallRigos = 20;

    private bool active = false;
    private GameObject smallRigoPrefab = null!;
    private List<SmallRigo> smallRigosActive = new();

    public void Start()
    {
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
            foreach (var smallRigo in smallRigosActive)
            {
                if (smallRigo == null) continue;
                Destroy(smallRigo.gameObject);
            }
            Destroy(this.gameObject);
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
            GameObject smallRigo = Instantiate(smallRigoPrefab, goldRigo.transform.position, goldRigo.transform.rotation, null);
            smallRigosActive.Add(smallRigo.GetComponent<SmallRigo>()); // let the prefab be deactivated by default.
        }
        if (!hideSmallRigos)
        {
            Activate();
        }
    }

    public void Activate()
    {
        active = true;
        StartCoroutine(ActiveRoutine());
    }

    public IEnumerator ActiveRoutine()
    {
        foreach (var smallRigo in smallRigosActive)
        {
            smallRigo.gameObject.SetActive(true);
            smallRigo.smartAgentNavigator.agent.Warp(goldRigo.transform.position);
            yield return null;
        }
        while (active)
        {
            foreach (SmallRigo smallRigo in smallRigosActive)
            {
                yield return null;
                if (Vector3.Distance(smallRigo.transform.position, goldRigo.transform.position) <= 5f) continue;
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

    public void OnDestroy()
    {
        if (goldRigo != null && goldRigo.IsServer)
        {
            goldRigo.NetworkObject.Despawn(true);
        }
        foreach (var smallRigo in smallRigosActive)
        {
            if (smallRigo == null) continue;
            Destroy(smallRigo.gameObject);
        }
        return;
    }
}