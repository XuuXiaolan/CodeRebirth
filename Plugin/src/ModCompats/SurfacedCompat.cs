using System.Runtime.CompilerServices;

namespace CodeRebirth.src.ModCompats;
public static class SurfacedCompatibilityChecker
{
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Surfaced"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init()
    {
        Plugin.ExtendedLogging("No way surfaced is on?!");
        Plugin.SurfacedIsOn = true;
    }
} // tbd.