
using System;

namespace CodeRebirthMRAPI;
static class GrabbableObjectPatches
{
	public static void Init()
    {
        On.GrabbableObject.DiscardItem += GrabbableObject_DiscardItem;
        On.GrabbableObject.EquipItem += GrabbableObject_EquipItem;
    }

    private static void GrabbableObject_EquipItem(On.GrabbableObject.orig_EquipItem orig, GrabbableObject self)
    {
        orig(self);
        if (self is JetpackItem)
        {
            Plugin.Logger.LogInfo("Equipped Jetpack");
        }
    }

    private static void GrabbableObject_DiscardItem(On.GrabbableObject.orig_DiscardItem orig, GrabbableObject self)
    {
        orig(self);
        if (self is JetpackItem)
        {
            Plugin.Logger.LogInfo("Discarded Jetpack");
        }
    }
}