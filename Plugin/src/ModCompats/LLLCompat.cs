using System.Runtime.CompilerServices;
namespace CodeRebirth.Dependency;

public static class LethalLevelLoaderCompatibilityChecker {
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("imabatby.lethallevelloader"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init() {
        Plugin.Logger.LogInfo("No way lethallevelloader is on?!");
        Plugin.LethalLevelLoaderIsOn = true;
    }
} // tbd.