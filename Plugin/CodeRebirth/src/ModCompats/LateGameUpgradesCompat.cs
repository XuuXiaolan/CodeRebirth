using BepInEx.Bootstrap;
using Dawn;
using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;

namespace CodeRebirth.src.ModCompats;
internal static class LateGameUpgradesCompat
{
    internal static bool LateGameUpgradesExists = Chainloader.PluginInfos.ContainsKey("com.malco.lethalcompany.moreshipupgrades");
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static float GetSellableScrapMultiplier()
    {
        if (LateGameUpgradesExists)
        {
            return GetSigurdAccessMultiplier();
        }
        return 1f;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static float GetSigurdAccessMultiplier()
    {
        return MoreShipUpgrades.UpgradeComponents.OneTimeUpgrades.Store.Sigurd.GetBuyingRate(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void PatchDropshipUpgrades()
    {
        _ = new Hook(AccessTools.DeclaredMethod(typeof(MoreShipUpgrades.UpgradeComponents.OneTimeUpgrades.Store.FasterDropPod), "CanLeaveEarly"), FixDropshipOnOxyde);
    }

    private static bool FixDropshipOnOxyde(RuntimeILReferenceBag.FastDelegateInvokers.Func<float, bool> orig, float shipTimer)
    {
        if (LethalContent.Moons[NamespacedKey<DawnMoonInfo>.From("code_rebirth", "oxyde")].Level == RoundManager.Instance.currentLevel)
        {
            return false;
        }
        return orig(shipTimer);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static float TryGetItemWeight(float currentItemWeight)
    {
        if (LateGameUpgradesExists)
        {
            Plugin.ExtendedLogging($"Current item weight: {currentItemWeight}");
            return GetItemWeight(currentItemWeight);
        }
        return currentItemWeight - 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static float GetItemWeight(float currentItemWeight)
    {
        Plugin.ExtendedLogging($"New item weight: {MoreShipUpgrades.UpgradeComponents.TierUpgrades.AttributeUpgrades.BackMuscles.DecreasePossibleWeight(currentItemWeight - 1)}");
        return MoreShipUpgrades.UpgradeComponents.TierUpgrades.AttributeUpgrades.BackMuscles.DecreasePossibleWeight(currentItemWeight - 1);
    }
}