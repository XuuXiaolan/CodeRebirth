using System.Runtime.CompilerServices;

namespace CodeRebirth.src.ModCompats;
internal static class ImperiumCompat
{
    internal static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(Imperium.PluginInfo.PLUGIN_GUID); } }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void ToggleInputs(bool enable)
    {
        if (enable)
        {
            Imperium.Imperium.InputBindings.InterfaceMap.Enable();
            Imperium.Imperium.InputBindings.BaseMap.Enable();
            Imperium.Imperium.InputBindings.StaticMap.Enable();
            Imperium.Imperium.InputBindings.FreecamMap.Enable();
        }
        else
        {
            Imperium.Imperium.InputBindings.BaseMap.Disable();
            Imperium.Imperium.InputBindings.StaticMap.Disable();
            Imperium.Imperium.InputBindings.FreecamMap.Disable();
            Imperium.Imperium.InputBindings.InterfaceMap.Disable();
        }
    }
}