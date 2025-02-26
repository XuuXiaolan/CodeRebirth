using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Content.Maps;

namespace CodeRebirth.src.MiscScripts
{
    public class SpawnSyncedCRObject : NetworkBehaviour
    {
        public CRObjectType objectType = CRObjectType.None;
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
            BoomTrap
        }

        public void Start()
        {
            // Look up the prefab via the registry in MapObjectHandler.
            GameObject prefab = MapObjectHandler.Instance.GetPrefabFor(objectType);
            if (prefab == null)
            {
                Plugin.Logger.LogWarning($"No prefab found for {objectType}");
                return;
            }

            if (!IsServer) return;
            // Instantiate and spawn the object on the network.
            var spawnedObject = Instantiate(prefab, transform.position, transform.rotation, transform);
            spawnedObject.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}
