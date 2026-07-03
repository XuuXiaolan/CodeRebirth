using CodeRebirth.src.Content.Enemies;
using Dawn;
using UnityEngine;

namespace CodeRebirth.src.Patches;

public static class SpikeTrapPatch
{
    public static void Init()
    {
        LethalContent.MapObjects.OnFreeze += FixSpikeRoofTrap;
        On.SpikeRoofTrap.OnTriggerStay += SpikeRoofTrap_OnTrigger;
    }

    private static void FixSpikeRoofTrap()
    {
        Transform mapObject = LethalContent.MapObjects[MapObjectKeys.SpikeRoofTrapHazard].GetMapObjectPrefab()!.transform;
        mapObject.gameObject.layer = 21;
        Transform parent = mapObject.Find("Container/AnimContainer");
        parent.Find("BaseSupport").gameObject.layer = 21;
        parent.Find("SpikeRoof").gameObject.layer = 21;
        parent.Find("SpikeRoof/MovingBar").gameObject.layer = 21;
    }

    private static void SpikeRoofTrap_OnTrigger(On.SpikeRoofTrap.orig_OnTriggerStay orig, SpikeRoofTrap self, Collider other)
    {
        orig(self, other);
        if (EnemyHandler.Instance.ManorLord == null)
        {
            return;
        }

        if (!self.IsServer)
        {
            return;
        }

        if (!other.TryGetComponent(out PuppeteersVoodoo puppet))
        {
            return;
        }

        if (puppet.lastTimeTakenDamageFromEnemy <= 0.5f)
        {
            return;
        }

        puppet.Hit(2, self.transform.position, null, false, -1);
    }
}