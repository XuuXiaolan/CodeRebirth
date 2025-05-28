using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class CRDropShip : ItemDropship
{
    public AudioSource rumbleSource = null!;
    [SerializeField]
    private Collider colliderEncompasingDropship = null!;

    private VehicleController? _lastSpawnedVehicle = null;
    // If it's items, then make it so it stays there for a set time forcibly, like 30 seconds, seems to work fine by itself.
    // If it's a vehicle, make it so the dropship stays for 30 seconds again.
    // Add a physics region onto the drop ship's floor.

    public void SpawnVehicleAnimEvent()
    {
        shipAnimator.SetBool("landing", true);
        shipTimer = 0f;
        if (IsServer)
        {
            var vehicleGO = GameObject.Instantiate<GameObject>(terminalScript.buyableVehicles[terminalScript.orderedVehicleFromTerminal].vehiclePrefab, deliverVehiclePoint.position, deliverVehiclePoint.rotation, RoundManager.Instance.VehiclesContainer);
            vehicleGO.GetComponent<NetworkObject>().Spawn(false);
            if (terminalScript.buyableVehicles[terminalScript.orderedVehicleFromTerminal].secondaryPrefab != null)
            {
                GameObject.Instantiate<GameObject>(terminalScript.buyableVehicles[terminalScript.orderedVehicleFromTerminal].secondaryPrefab, RoundManager.Instance.VehiclesContainer).GetComponent<NetworkObject>().Spawn(false);
            }
            _lastSpawnedVehicle = vehicleGO.GetComponent<VehicleController>();
        }
        untetheredVehicle = false;
        deliveringOrder = true;
        terminalScript.orderedVehicleFromTerminal = -1;
        terminalScript.vehicleInDropship = false;
        deliveringVehicle = false;
        triggerScript.interactable = false;
    }

    [ClientRpc]
    public void PlayRumbleClientRpc()
    {
        rumbleSource.Play();
    }

    public void PlayRumbleAnimEvent()
    {
        Plugin.ExtendedLogging($"PlayRumbleAnimEvent");
        rumbleSource.Play();
    }

    public void DestroyAnyVehiclesAnimEvent()
    {
        if (_lastSpawnedVehicle == null)
            return;

        if (!colliderEncompasingDropship.bounds.Contains(_lastSpawnedVehicle.transform.position))
            return;

        _lastSpawnedVehicle.DestroyCar();
    }
}