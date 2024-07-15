using System.Runtime.CompilerServices;
namespace CodeRebirth.Dependency;

public static class SurfacedCompatibilityChecker {
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Surfaced"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init() {
        Plugin.Logger.LogInfo("No way imperium is on?!");
        Plugin.SurfacedIsOn = true;
    }
}