using System.Collections;
using Dawn.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class CodeRebirthHazard : NetworkBehaviour
{
    private Collider[] cachedColliders = new Collider[6];
    public virtual void Start()
    {
        if (IsServer)
        {
            StartCoroutine(DecideHazardSpawningStuff());
        }
    }

    private IEnumerator DecideHazardSpawningStuff()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0, 2f));
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, 1f, cachedColliders, MoreLayerMasks.InteractableMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < numHits; i++)
        {
            if (!cachedColliders[i].GetComponent<DoorLock>())
                continue;

            NetworkObject.Despawn(true);
            yield break;
        }
    }
}