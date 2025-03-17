using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Content.Maps;
using System.Collections.Generic;
using System.Linq;

namespace CodeRebirth.src.MiscScripts
{
    public class SpawnSyncedCRObject : NetworkBehaviour
    {
        [Range(0f, 100f)]
        public float chanceOfSpawningAny = 100f;
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
        }

        public void Start()
        {
            // Look up the prefab via the registry in MapObjectHandler.
            if (!IsServer) return;
            if (UnityEngine.Random.Range(0, 100) >= chanceOfSpawningAny) return;
            List<(GameObject objectType, int cumulativeWeight)> cumulativeList = new();
            int cumulativeWeight = 0;
            foreach (var objectTypeWithRarity in objectTypesWithRarity)
            {
                cumulativeWeight += objectTypeWithRarity.Rarity;
                cumulativeList.Add((MapObjectHandler.Instance.GetPrefabFor(objectTypeWithRarity.CRObjectType), cumulativeWeight));
            }
            if (cumulativeList.Count <= 0)
            {
                Plugin.Logger.LogWarning($"No prefabs found for spawning: {string.Join(", ", objectTypesWithRarity.Select(objectType => objectType.CRObjectType))}");
                return;
            }

            // Instantiate and spawn the object on the network.
            int randomWeight = UnityEngine.Random.Range(0, cumulativeWeight) + 1;
            var prefab = cumulativeList.FirstOrDefault(x => x.cumulativeWeight <= randomWeight).objectType;
            if (prefab == null)
            {
                Plugin.Logger.LogError($"Did you really set something to spawn at a weight of 0? Couldn't find prefab for spawning: {string.Join(", ", objectTypesWithRarity.Select(objectType => objectType.CRObjectType))}");
                return;
            }
            var spawnedObject = Instantiate(prefab, transform.position, transform.rotation, transform);
            spawnedObject.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}
