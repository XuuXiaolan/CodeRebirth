using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class SmallRigoManager : MonoBehaviour
{
    public GoldRigo goldRigo = null!;
    public int numberOfSmallRigos = 20;

    private GameObject smallRigoPrefab = null!;
    private List<SmallRigo> smallRigosActive = new();

    public void Start()
    {
        // smallRigoPrefab = ItemHandler.Instance.GoldRigo.smallRigoPrefab;

        if (StartOfRound.Instance.inShipPhase)
        {
            StartCoroutine(SpawnSmallRigos(numberOfSmallRigos, true));
        }
        // Spawn and hide the SmallRigo's on the following conditions: we're in orbit, leaving and/or landing whilst the goldrigo isnt in the ship
    }

    public void Update()
    {
        if (goldRigo == null)
        {
            Destroy(this.gameObject);
            return;
        }
    }

    public IEnumerator SpawnSmallRigos(int numberOfSmallRigos, bool hideSmallRigos)
    {
        while (smallRigosActive.Count < numberOfSmallRigos)
        {
            yield return new WaitForSeconds(0.1f);
            GameObject smallRigo = Instantiate(smallRigoPrefab, goldRigo.transform.position, goldRigo.transform.rotation, null);
            smallRigosActive.Add(smallRigo.GetComponent<SmallRigo>());
        }
    }

    public void Activate()
    {

    }
}