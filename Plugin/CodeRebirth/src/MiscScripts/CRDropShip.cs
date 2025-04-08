using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class CRDropShip : ItemDropship
{
    public Collider colliderEncompasingDropship = null!;
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
            StartCoroutine(DestroyUnmovedCruiser(vehicleGO.GetComponent<VehicleController>()));
        }
        untetheredVehicle = false;
        deliveringOrder = true;
        terminalScript.orderedVehicleFromTerminal = -1;
        terminalScript.vehicleInDropship = false;
        deliveringVehicle = false;
        triggerScript.interactable = false;
    }

    private IEnumerator DestroyUnmovedCruiser(VehicleController vehicle)
    {
        yield return new WaitUntil(() => shipTimer > 40f);
        if (colliderEncompasingDropship.bounds.Contains(vehicle.transform.position))
        {
            vehicle.DestroyCar();
        }
        Plugin.ExtendedLogging($"Doors closing, blowing up cruiser");
        // vehicle.
    }
}