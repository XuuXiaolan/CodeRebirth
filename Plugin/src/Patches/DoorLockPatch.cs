using CodeRebirth.src.Content.Unlockables;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class DoorLockPatch
{
    public static void Init()
    {
        On.DoorLock.OnTriggerStay += DoorLock_OnTriggerStay;
    }

    private static void DoorLock_OnTriggerStay(On.DoorLock.orig_OnTriggerStay orig, DoorLock self, Collider other)
    {
		if (NetworkManager.Singleton == null || !self.IsServer)
		{
			goto ret;
		}
		if (self.isLocked || self.isDoorOpened)
		{
			goto ret;
		}

        if (other.CompareTag("Enemy") && other.gameObject.name == "ShockwaveGalDoorColider")
        {
            float openDoorSpeedMultiplier = other.gameObject.transform.parent.GetComponent<ShockwaveGalAI>().doorOpeningSpeed;
            self.enemyDoorMeter += Time.deltaTime * openDoorSpeedMultiplier;
        }
    ret:
        orig(self, other);
    }
}