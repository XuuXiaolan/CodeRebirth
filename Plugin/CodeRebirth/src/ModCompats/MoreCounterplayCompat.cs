using System;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;

namespace CodeRebirth.src.ModCompats;

internal static class MoreCounterplayCompat
{
    internal static bool MoreCounterplayExists = Chainloader.PluginInfos.TryGetValue("BaronDrakula.MoreCounterplay", out var info) && info.Metadata.Version >= new Version(1, 4, 1);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static bool CoilheadCounterplayEnabled()
    {
        return MoreCounterplay.MoreCounterplay.Settings.EnableCoilheadCounterplay;
    }
}