using CodeRebirth.src.Util;
using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace CodeRebirth.src.Patches;

public static class EntranceTeleportPatch
{
    public static void Init()
    {
        On.EntranceTeleport.Awake += EntranceTeleport_Awake;
        new Hook(AccessTools.DeclaredMethod(typeof(EntranceTeleport), "OnDestroy"), EntranceTeleport_OnDestroy);
    }

    private static void EntranceTeleport_Awake(On.EntranceTeleport.orig_Awake orig, EntranceTeleport self)
    {
        CodeRebirthUtils.EntrancePoints.Add(self);
        orig(self);
    }

    private static void EntranceTeleport_OnDestroy(RuntimeILReferenceBag.FastDelegateInvokers.Action<EntranceTeleport> orig, EntranceTeleport self)
    {
        CodeRebirthUtils.EntrancePoints.Remove(self);
        self.OnDestroy();
        orig(self);
    }
}