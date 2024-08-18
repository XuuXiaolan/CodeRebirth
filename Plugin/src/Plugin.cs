using System;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using CodeRebirth.src.Configs;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Keybinds;
using HarmonyLib;
using CodeRebirth.src.Util.AssetLoading;
using CodeRebirth.src.Util.Extensions;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Patches;
using CodeRebirth.src.Util;

namespace CodeRebirth.src;
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)] 
[BepInDependency(WeatherRegistry.Plugin.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(Imperium.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Surfaced", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("MoreShipUpgrades", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(LethalLevelLoader.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)] // todo: soft depend on subtitles api and add support.
[BepInDependency("JustJelly.SubtitlesAPI", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin {
    internal static new ManualLogSource Logger = null!;
    private readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    internal static readonly Dictionary<string, AssetBundle> LoadedBundles = [];
    internal static bool ImperiumIsOn = false;
    internal static bool SurfacedIsOn = false;
    internal static bool LGUIsOn = false;
    internal static bool SubtitlesAPIIsOn = false;
    internal static readonly Dictionary<string, Item> samplePrefabs = [];
    internal static IngameKeybinds InputActionsInstance = null!;
    public static CodeRebirthConfig ModConfig { get; private set; } = null!; // prevent from accidently overriding the config

    internal static MainAssets Assets { get; private set; } = null!;
    internal class MainAssets(string bundleName) : AssetBundleLoader<MainAssets>(bundleName) {
        [LoadFromBundle("CodeRebirthUtils.prefab")]
        public GameObject UtilsPrefab { get; private set; } = null!;
    }
    
    private void Awake() {
        Logger = base.Logger;
        ModConfig = new CodeRebirthConfig(this.Config); // Create the config with the file from here.
#if DEBUG
        ModConfig.ConfigEnableExtendedLogging.Value = true;
#endif
        if (ImperiumCompatibilityChecker.Enabled) {
            ImperiumCompatibilityChecker.Init();
        } else {
            // Logger.LogWarning("Imperium not found. Special Debugs will not be activated.");
        }

        if (SurfacedCompatibilityChecker.Enabled) {
            SurfacedCompatibilityChecker.Init();
        } else {
            // Logger.LogWarning("Surfaced not found. Sharknado will not be activated.");
        }

        if (LGUCompatibilityChecker.Enabled) {
            LGUCompatibilityChecker.Init();
        } else {
            // Logger.LogWarning("lategameupgrades not found. Custom hunter samples will not be activated.");
        }

        if (SubtitlesAPICompatibilityChecker.Enabled) {
            SubtitlesAPICompatibilityChecker.Init();
        } else {
            // Logger.LogWarning("SubtitlesAPI not found. Subtitles will not be activated.");
        }

        _harmony.PatchAll(Assembly.GetExecutingAssembly());
        PlayerControllerBPatch.Init();
        EnemyAIPatch.Init();
        // This should be ran before Network Prefabs are registered.
        
        Assets = new MainAssets("coderebirthasset");
        InitializeNetworkBehaviours();
        // Register Keybinds
        InputActionsInstance = new IngameKeybinds();
        
        Logger.LogInfo("Registering content.");

        List<Type> contentHandlers = Assembly.GetExecutingAssembly().GetLoadableTypes().Where(x =>
            x.BaseType != null
            && x.BaseType.IsGenericType
            && x.BaseType.GetGenericTypeDefinition() == typeof(ContentHandler<>)
        ).ToList();
        
        foreach(Type type in contentHandlers) {
            type.GetConstructor([]).Invoke([]);
        }
        if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("impulse.CentralConfig")) Logger.LogFatal("You are using a mod (CentralConfig) that potentially changes how weather works and is potentially removing this mod's custom weather from moons, you have been warned.");

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void OnDisable() {
        foreach (AssetBundle bundle in LoadedBundles.Values) {
            bundle.Unload(false);
        }
        Logger.LogDebug("Unloaded assetbundles.");
        LoadedBundles.Clear();
    }

    internal static void ExtendedLogging(object text) {
        if (ModConfig.ConfigEnableExtendedLogging.Value) {
            Logger.LogInfo(text);
        }
    }

    private void InitializeNetworkBehaviours() {
        var types = Assembly.GetExecutingAssembly().GetLoadableTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }
}