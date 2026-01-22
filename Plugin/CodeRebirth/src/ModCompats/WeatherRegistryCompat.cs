using BepInEx.Bootstrap;

namespace CodeRebirth.src.ModCompats;
internal static class WeatherRegistryCompat
{
    internal static bool WeatherRegistryAPIExists = Chainloader.PluginInfos.ContainsKey(WeatherRegistry.PluginInfo.PLUGIN_GUID);
}