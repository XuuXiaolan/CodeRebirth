using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Content.Maps;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using CodeRebirth.src.Util;
using CodeRebirthLib.ContentManagement.MapObjects;

namespace CodeRebirth.src.MiscScripts;
public class SpawnSyncedCRObject : MonoBehaviour
{
    [Range(0f, 100f)]
    public float chanceOfSpawningAny = 100f;
    public bool automaticallyAlignWithTerrain = false;
    public List<CRObjectTypeWithRarity> objectTypesWithRarity = new();

    public IEnumerator Start()
    {
        if (!NetworkManager.Singleton.IsServer)
            yield break;

        if (UnityEngine.Random.Range(0, 100) >= chanceOfSpawningAny)
            yield break;

        List<(GameObject objectType, float weight)> spawnableObjectsList = new();
        foreach (var objectTypeWithRarity in objectTypesWithRarity)
        {
            if (!Plugin.Mod.MapObjectRegistry().TryGetFromMapObjectName(objectTypeWithRarity.CRObjectName, out CRMapObjectDefinition? CRMapObjectDefinition))
                continue;

            spawnableObjectsList.Add((CRMapObjectDefinition.GameObject, objectTypeWithRarity.Rarity));
        }

        if (spawnableObjectsList.Count <= 0)
        {
            Plugin.Logger.LogWarning($"No prefabs found for spawning in game object: {this.gameObject.name}");
            yield break;
        }

        GameObject? prefabToSpawn = CRUtilities.ChooseRandomWeightedType(spawnableObjectsList);

        // Instantiate and spawn the object on the network.
        if (prefabToSpawn == null)
        {
            Plugin.Logger.LogError($"Did you really set something to spawn at a weight of 0? Couldn't find prefab for spawning: {string.Join(", ", objectTypesWithRarity.Select(objectType => objectType.CRObjectName))}");
            yield break;
        }

        if (automaticallyAlignWithTerrain)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point;
                transform.up = hit.normal;
            }
        }

        var spawnedObject = Instantiate(prefabToSpawn, transform.position, transform.rotation, transform);
        spawnedObject.GetComponent<NetworkObject>().Spawn(true);
    }
}