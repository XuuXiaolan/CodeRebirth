using System;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using Dawn;

namespace CodeRebirth.src.ModCompats;

internal static class LethalMoonUnlocksCompat
{
    internal static bool LethalMoonUnlocksExists = Chainloader.PluginInfos.TryGetValue("com.xmods.lethalmoonunlocks", out var info) && info.Metadata.Version >= new Version(2, 2, 0);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void ReleaseOxydeStoryLock(SelectableLevel selectableLevel)
    {
        LethalMoonUnlocks.UnlockManager.TryReleaseStoryLock(selectableLevel.GetDawnInfo().GetNumberlessPlanetName());
    }
}