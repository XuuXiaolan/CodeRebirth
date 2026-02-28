using System.Collections.Generic;
using Dawn.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class SpawnAndParentObject : NetworkBehaviour
{
    [field: SerializeField]
    public List<GameObject> ObjectsToSpawnList { get; private set; }

    private GameObject objectToSpawn = null!;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            return;
        }

        int indexToSpawn = Random.Range(0, ObjectsToSpawnList.Count);
        objectToSpawn = ObjectsToSpawnList[indexToSpawn];
        GameObject objectInstantiated = Instantiate(objectToSpawn, transform.position, transform.rotation, this.transform);
        if (objectInstantiated.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Spawn(true);
            networkObject.OnSpawn(() =>
            {
                networkObject.TrySetParent(this.transform);
            });
        }
        else
        {
            SpawnGameObjectOnIndexServerRpc(indexToSpawn);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnGameObjectOnIndexServerRpc(int indexToSpawn)
    {
        SpawnGameObjectOnIndexClientRpc(indexToSpawn);
    }

    [ClientRpc]
    private void SpawnGameObjectOnIndexClientRpc(int indexToSpawn)
    {
        if (IsServer)
        {
            return;
        }

        GameObject objectInstantiated = Instantiate(ObjectsToSpawnList[indexToSpawn], transform.position, transform.rotation, this.transform);
    }
}