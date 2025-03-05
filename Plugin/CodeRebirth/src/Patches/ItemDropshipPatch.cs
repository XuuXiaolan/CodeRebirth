using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Patches;
public static class ItemDropshipPatch
{
    public static void Init()
    {
        On.ItemDropship.DeliverVehicleOnServer += ItemDropship_DeliverVehicleOnServer;
    }

    private static void ItemDropship_DeliverVehicleOnServer(On.ItemDropship.orig_DeliverVehicleOnServer orig, ItemDropship self)
    {
        if (self is CRDropShip cRDropShip)
        {
            cRDropShip.shipTimer = 0f;
            Plugin.ExtendedLogging("ItemDropship_DeliverVehicleOnServer");
            cRDropShip.shipAnimator.SetTrigger("landingVehicle");
            // cRDropShip.SpawnVehicleAnimEvent();
            return;
        }
        orig(self);
    }
}