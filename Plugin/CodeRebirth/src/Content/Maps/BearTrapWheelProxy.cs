using System.Collections.Generic;
using Dawn;
using Dusk;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public abstract class BearTrapWheelProxy : MonoBehaviour
{
    internal static int MapHazardsLayerMask = -1;

    private static readonly NamespacedKey BearTrapWheelProxyKey = NamespacedKey.From("code_rebirth", "bear_trap_wheel_proxy");
    public virtual void Start()
    {
        PersistentDataContainer saveContainer = DawnLib.GetCurrentContract()!;
        if (!saveContainer.Has(BearTrapWheelProxyKey))
        {
            return;
        }

        List<string> gameObjectNames = saveContainer.GetOrSetDefault<List<string>>(BearTrapWheelProxyKey, []);
        foreach (string gameObjectName in gameObjectNames)
        {
            if (gameObject.name == gameObjectName)
            {
                PunctureWheel();
                break;
            }
        }
    }

    internal static void Init()
    {
        On.GameNetworkManager.Start += EditVehicles;
    }

    private static void EditVehicles(On.GameNetworkManager.orig_Start orig, GameNetworkManager self)
    {
        orig(self);

        int vehicleLayer = LayerMask.NameToLayer("Vehicle");
        List<WheelCollider> vehiclesTyres = new();
        foreach (NetworkPrefab potentialVehicleNetworkPrefab in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
        {
            GameObject? prefab = potentialVehicleNetworkPrefab.Prefab;
            if (prefab == null || prefab.layer != vehicleLayer)
            {
                continue;
            }

            if (prefab.GetComponent<VehicleBase>() || prefab.GetComponent<VehicleController>())
            {
                foreach (WheelCollider wheelCollider in prefab.GetComponentsInChildren<WheelCollider>())
                {
                    vehiclesTyres.Add(wheelCollider);
                }
            }
        }

        MapHazardsLayerMask = LayerMask.GetMask("MapHazards");
        foreach (WheelCollider vehicleTyre in vehiclesTyres)
        {
            UnityWheelColliderProxy wheelProxy = vehicleTyre.gameObject.AddComponent<UnityWheelColliderProxy>();
            wheelProxy.SetupWheel();
        }
    }

    public abstract void SetupWheel();

    public virtual void PunctureWheel()
    {
        PersistentDataContainer saveContainer = DawnLib.GetCurrentContract()!;
        List<string> gameObjectNames = saveContainer.GetOrSetDefault<List<string>>(BearTrapWheelProxyKey, []);
        gameObjectNames.Add(gameObject.name);
        saveContainer.Set(BearTrapWheelProxyKey, gameObjectNames);
    }
}