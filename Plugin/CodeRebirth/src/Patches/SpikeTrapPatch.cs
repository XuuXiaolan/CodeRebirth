using CodeRebirth.src.Content.Enemies;
using UnityEngine;

namespace CodeRebirth.src.Patches;
public static class SpikeTrapPatch
{
    public static void Init()
    {
        On.SpikeRoofTrap.Start += SpikeRoofTrap_Start;
        On.SpikeRoofTrap.OnTriggerStay += SpikeRoofTrap_OnTrigger;
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

    private static void SpikeRoofTrap_Start(On.SpikeRoofTrap.orig_Start orig, SpikeRoofTrap self)
    {
        orig(self);
        Transform parent = self.gameObject.transform.parent;
        self.NetworkObject.gameObject.layer = 21;
        parent.transform.Find("BaseSupport").gameObject.layer = 21;
        parent.transform.Find("SpikeRoof").gameObject.layer = 21;
        parent.transform.Find("SpikeRoof").Find("MovingBar").gameObject.layer = 21;
    }
}