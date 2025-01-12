using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class CodeRebirthHazard : NetworkBehaviour
{
    public virtual void Start()
    {
        if (!IsServer) return;
        StartCoroutine(DecideHazardSpawningStuff());
    }

    private IEnumerator DecideHazardSpawningStuff()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0, 2f));
        Collider[] colliders = new Collider[5];
        Physics.OverlapSphereNonAlloc(transform.position, 1f, colliders, LayerMask.GetMask("InteractableObject"), QueryTriggerInteraction.Ignore);
        foreach (Collider collider in colliders)
        {
            if (collider == null) continue;
            yield return null;
            if (collider.gameObject.GetComponent<DoorLock>() == null) continue;
            NetworkObject.Despawn(true);
            yield break;
        }
    }
}