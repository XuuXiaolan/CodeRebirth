using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CodeRebirthESKR.Patches;

namespace CodeRebirthESKR;
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(CodeRebirth.MyPluginInfo.PLUGIN_GUID)]
[BepInDependency("antlershed.lethalcompany.enemyskinregistry", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    public static ConfigFile configFile { get; private set; } = null!;
    public static Configuration ModConfig { get; private set; } = null!; // prevent from accidently overriding the config

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        configFile = this.Config;
        ModConfig = new Configuration(); // Create the config with the file from here.

        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("antlershed.lethalcompany.enemyskinregistry"))
        {
            Logger.LogWarning("EnemySkinRegistry is not installed, skipping!");
            return;
        }

        return;
        EnemyAIPatches.Init();
        SkinsHandler.RegisterSkins();

        ModConfig.InitConfiguration(Config);
        PropertyInfo orphanedEntriesProp = Config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(Config, null);
        orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
        Config.Save(); // Save the config file to save these changes

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded! :3");
    }

    internal static void ExtendedLogging(object text)
    {
        if (CodeRebirth.src.Plugin.ModConfig.ConfigExtendedLogging.Value)
        {
            Logger.LogInfo(text);
        }
    }
}