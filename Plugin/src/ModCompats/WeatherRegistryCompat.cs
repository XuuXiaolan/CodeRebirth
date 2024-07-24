using System.Runtime.CompilerServices;
namespace CodeRebirth.Dependency;

public static class WeatherRegistryCompatibilityChecker {
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("mrov.WeatherRegistry"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init() {
        Plugin.Logger.LogInfo("No way weather registry is on?!");
        Plugin.WeatherRegistryIsOn = true;
    }
}