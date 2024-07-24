using System;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using CodeRebirth.Configs;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.Keybinds;
using HarmonyLib;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using CodeRebirth.Util.Extensions;
using CodeRebirth.Dependency;

namespace CodeRebirth;
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)] 
[BepInDependency(WeatherRegistry.Plugin.GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(Imperium.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Surfaced", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("MoreShipUpgrades", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(LethalLevelLoader.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)] // todo: soft depend on subtitles api and add support.
[BepInDependency("JustJelly.SubtitlesAPI", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin {
    internal static new ManualLogSource Logger = null!;
    private readonly Harmony _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
    internal static readonly Dictionary<string, AssetBundle> LoadedBundles = [];
    internal static bool ImperiumIsOn = false;
    internal static bool SurfacedIsOn = false;
    internal static bool WeatherRegistryIsOn = false;
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

        if (ImperiumCompatibilityChecker.Enabled) {
            ImperiumCompatibilityChecker.Init();
        }

        if (SurfacedCompatibilityChecker.Enabled) {
            SurfacedCompatibilityChecker.Init();
        }
        
        if (WeatherRegistryCompatibilityChecker.Enabled) {
            WeatherRegistryCompatibilityChecker.Init();
        } else {
            Plugin.Logger.LogWarning("Weather registry not found. Custom Weathers will not be activated.");
        }

        if (LGUCompatibilityChecker.Enabled) {
            LGUCompatibilityChecker.Init();
        }

        if (SubtitlesAPICompatibilityChecker.Enabled) {
            SubtitlesAPICompatibilityChecker.Init();
        }

        ModConfig = new CodeRebirthConfig(this.Config); // Create the config with the file from here.
        _harmony.PatchAll(Assembly.GetExecutingAssembly());
        // This should be ran before Network Prefabs are registered.
        
        Assets = new MainAssets("coderebirthasset");
        
        InitializeNetworkBehaviours();
        // Register Keybinds
        InputActionsInstance = new IngameKeybinds();
        
        Logger.LogInfo("Registering content.");

        List<Type> creatureHandlers = Assembly.GetExecutingAssembly().GetLoadableTypes().Where(x =>
            x.BaseType != null
            && x.BaseType.IsGenericType
            && x.BaseType.GetGenericTypeDefinition() == typeof(ContentHandler<>)
        ).ToList();
        
        foreach(Type type in creatureHandlers) {
            type.GetConstructor([]).Invoke([]);
        }

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    void OnDisable() {
        foreach (AssetBundle bundle in LoadedBundles.Values) {
            bundle.Unload(false);
        }
        Logger.LogDebug("Unloaded assetbundles.");
        LoadedBundles.Clear();
        if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("impulse.CentralConfig")) Logger.LogFatal("You are using a mod (CentralConfig) that potentially changes how weather works and is potentially removing this mod's custom weather from moons, you have been warned.");
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