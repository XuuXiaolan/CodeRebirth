using BepInEx.Bootstrap;
using System.Runtime.CompilerServices;

namespace CodeRebirth.src.ModCompats;
internal static class LateGameUpgradesCompat
{
    private static bool LateGameUpgradesExists = Chainloader.PluginInfos.ContainsKey("com.malco.lethalcompany.moreshipupgrades");

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static float TryGetItemWeight(float currentItemWeight)
    {
        if (LateGameUpgradesExists)
        {
            Plugin.ExtendedLogging($"Current item weight: {currentItemWeight}");
            return GetItemWeight(currentItemWeight);
        }
        return currentItemWeight - 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static float GetItemWeight(float currentItemWeight)
    {
        Plugin.ExtendedLogging($"New item weight: {MoreShipUpgrades.UpgradeComponents.TierUpgrades.AttributeUpgrades.BackMuscles.DecreasePossibleWeight(currentItemWeight - 1)}");
        return MoreShipUpgrades.UpgradeComponents.TierUpgrades.AttributeUpgrades.BackMuscles.DecreasePossibleWeight(currentItemWeight - 1);
    }
}