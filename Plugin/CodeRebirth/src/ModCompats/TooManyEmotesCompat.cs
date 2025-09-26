using System.Runtime.CompilerServices;

namespace CodeRebirth.src.ModCompats;

internal class TooManyEmotesCompat
{
    internal static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("FlipMods.TooManyEmotes"); } }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddCredits(int profit)
    {
        if (Enabled)
        {
            AddToEmoteCredits(profit);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void AddToEmoteCredits(int profit)
    {
        TooManyEmotes.Patches.TerminalPatcher.OnGainGroupCredits(profit, 0, null);
    }
}