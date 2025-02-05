using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class CodeRebirthHazard : NetworkBehaviour
{
    private Collider[] cachedColliders = new Collider[5];
    public virtual void Start()
    {
        if (!IsServer) return;
        StartCoroutine(DecideHazardSpawningStuff());
    }

    private IEnumerator DecideHazardSpawningStuff()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0, 2f));
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, 1f, cachedColliders, LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore);
        for (int i = 0; i < numHits; i++)
        {
            if (!cachedColliders[i].GetComponent<DoorLock>()) continue;
            NetworkObject.Despawn(true);
            yield break;
        }
    }
}