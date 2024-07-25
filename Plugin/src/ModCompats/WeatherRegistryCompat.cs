using System.Runtime.CompilerServices;
using CodeRebirth.WeatherStuff;
using WeatherRegistry;

namespace CodeRebirth.Dependency;

public static class WeatherRegistryCompatibilityChecker {
    public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("mrov.WeatherRegistry"); } }
    
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Init() {
        Plugin.Logger.LogInfo("No way weather registry is on?!");
        Plugin.WeatherRegistryIsOn = true;
    }

    internal static void DisplayWindyWarning() {
        if(WeatherHandler.Instance.TornadoesWeather == null) return; // tornado weather didn't load
        if (WeatherManager.GetCurrentWeather(StartOfRound.Instance.currentLevel) == WeatherHandler.Instance.TornadoesWeather) {
            Plugin.Logger.LogWarning("Displaying Windy Weather Warning.");
            HUDManager.Instance.DisplayTip("Weather alert!", "You have routed to a Windy moon. Exercise caution if you are sensitive to flashing lights!", true, true, "CR_WindyTip");
        }
    }
}