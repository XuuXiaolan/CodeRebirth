using System.Runtime.CompilerServices;
namespace CodeRebirth.Dependency;

public static class ImperiumCompatibilityChecker {
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("giosuel.Imperium"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init() {
        Plugin.Logger.LogInfo("No way imperium is on?!");
        Plugin.ImperiumIsOn = true;
    }
} // tbd.