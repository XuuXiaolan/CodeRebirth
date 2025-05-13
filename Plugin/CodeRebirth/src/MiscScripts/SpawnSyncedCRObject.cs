using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Content.Maps;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace CodeRebirth.src.MiscScripts;
public class SpawnSyncedCRObject : MonoBehaviour
{
    [Range(0f, 100f)]
    public float chanceOfSpawningAny = 100f;
    public bool automaticallyAlignWithTerrain = false;
    public List<CRObjectTypeWithRarity> objectTypesWithRarity = new();
    public enum CRObjectType
    {
        None,
        Merchant,
        LaserTurret,
        FunctionalMicrowave,
        FlashTurret,
        IndustrialFan,
        BugZapper,
        AirControlUnit,
        MimicMetalCrate,
        MimicWoodenCrate,
        MetalCrate,
        WoodenCrate,
        BearTrap,
        BoomTrap,
        ShredderSarah,
        CompactorToby,
        GunslingerGreg,
        AutonomousCrane,
        Coin,
    }

    public IEnumerator Start()
    {
        if (!NetworkManager.Singleton.IsServer)
            yield break;

        if (UnityEngine.Random.Range(0, 100) >= chanceOfSpawningAny)
            yield break;

        List<(GameObject objectType, float weight)> spawnableObjectsList = new();
        foreach (var objectTypeWithRarity in objectTypesWithRarity)
        {
            GameObject? selectedPrefab = MapObjectHandler.Instance.GetPrefabFor(objectTypeWithRarity.CRObjectType);
            if (selectedPrefab == null)
            {
                Plugin.Logger.LogWarning($"No prefab found for spawning: {objectTypeWithRarity.CRObjectType}");
                continue;
            }
            spawnableObjectsList.Add((selectedPrefab, objectTypeWithRarity.Rarity));
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
            Plugin.Logger.LogError($"Did you really set something to spawn at a weight of 0? Couldn't find prefab for spawning: {string.Join(", ", objectTypesWithRarity.Select(objectType => objectType.CRObjectType))}");
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