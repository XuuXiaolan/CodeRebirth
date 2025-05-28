using System;
using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Patches;
public static class ItemDropshipPatch
{
    public static void Init()
    {
        On.ItemDropship.Update += ItemDropship_Update;
        On.ItemDropship.ShipLeave += ItemDropship_ShipLeave;
        On.ItemDropship.DeliverVehicleOnServer += ItemDropship_DeliverVehicleOnServer;
    }

    private static void ItemDropship_ShipLeave(On.ItemDropship.orig_ShipLeave orig, ItemDropship self)
    {
        if (self is CRDropShip cRDropShip)
        {
            cRDropShip.rumbleSource.Play();
            cRDropShip.shipTimer = 0f;
        }
        orig(self);
    }

    private static void ItemDropship_Update(On.ItemDropship.orig_Update orig, ItemDropship self)
    {
        orig(self);
        if (self is CRDropShip cRDropShip)
        {
            if (!cRDropShip.deliveringOrder && cRDropShip.IsServer && cRDropShip.shipTimer < 34f && cRDropShip.shipTimer + cRDropShip.rumbleSource.clip.length >= 40f)
            {
                if (!cRDropShip.rumbleSource.isPlaying)
                {
                    Plugin.ExtendedLogging($"PlayRumbleClientRpc");
                    cRDropShip.PlayRumbleClientRpc();
                }
            }
            if (cRDropShip.rumbleSource.isPlaying)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
            }
        }
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