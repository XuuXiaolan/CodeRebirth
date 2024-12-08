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

        if (other.gameObject.layer == 19 && other.gameObject.name == "DoorCollider")
        {
            self.enemyDoorMeter += Time.deltaTime * 0.5f;
            if (self.enemyDoorMeter > 1f)
            {
                self.enemyDoorMeter = 0f;
                self.gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(false, true, false);
                self.OpenDoorAsEnemyServerRpc();
            }
        }
        if (other.gameObject.layer == 19 && other.gameObject.name == "ShockwaveGalDoorCollider")
        {
            float openDoorSpeedMultiplier = 1f;
            ShockwaveGalAI? shockwave = other.gameObject.transform.parent.GetComponent<ShockwaveGalAI>();
            SeamineGalAI? seamine = other.gameObject.transform.parent.GetComponent<SeamineGalAI>();
            if (shockwave != null)
            {
                openDoorSpeedMultiplier = shockwave.DoorOpeningSpeed;
            }
            else if (seamine != null)
            {
                openDoorSpeedMultiplier = seamine.DoorOpeningSpeed;
            }
            self.enemyDoorMeter += Time.deltaTime * openDoorSpeedMultiplier;
    		if (self.enemyDoorMeter > 1f)
			{
				self.enemyDoorMeter = 0f;
				self.gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(false, true, false);
				self.OpenDoorAsEnemyServerRpc();
			}
        }
        if (other.gameObject.layer == 19 && other.gameObject.name == "MicrowaveCollider")
        {
            self.enemyDoorMeter += Time.deltaTime * 0.5f;
            if (self.enemyDoorMeter > 1f)
            {
                self.enemyDoorMeter = 0f;
                self.gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(false, true, false);
                self.OpenDoorAsEnemyServerRpc();
            }
        }
    ret:
        orig(self, other);
    }
}