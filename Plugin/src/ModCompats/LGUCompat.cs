using System.Runtime.CompilerServices;
namespace CodeRebirth.Dependency;

public static class LGUCompatibilityChecker {
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("MoreShipUpgrades"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init() {
        Plugin.ExtendedLogging("No way lategameupgrades is on?!");
        Plugin.LGUIsOn = true;
    }
} // tbd.