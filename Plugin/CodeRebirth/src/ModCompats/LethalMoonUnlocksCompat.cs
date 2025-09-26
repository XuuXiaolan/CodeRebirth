using System;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using LethalLevelLoader;

namespace CodeRebirth.src.ModCompats;

internal static class LethalMoonUnlocksCompat
{
    internal static bool LethalMoonUnlocksExists = Chainloader.PluginInfos.TryGetValue("com.xmods.lethalmoonunlocks", out var info) && info.Metadata.Version >= new Version(2, 2, 0);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void ReleaseOxydeStoryLock(ExtendedLevel extendedLevel)
    {
        LethalMoonUnlocks.UnlockManager.TryReleaseStoryLock(extendedLevel.NumberlessPlanetName);
    }
}