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
using CodeRebirth.src;
using CodeRebirth.Misc;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using CodeRebirth.WeatherStuff;
using LethalLib;
using System.Collections.ObjectModel;
using CodeRebirth.MapStuff;
using CodeRebirth.Util.Extensions;
using System.Runtime.CompilerServices;

namespace CodeRebirth;
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)] 
[BepInDependency(WeatherRegistry.Plugin.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin {
    internal static new ManualLogSource Logger;
    private readonly Harmony _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
    
    internal static readonly Dictionary<string, AssetBundle> LoadedBundles = [];
    
    internal static readonly Dictionary<string, Item> samplePrefabs = [];
    internal static IngameKeybinds InputActionsInstance;
    public static CodeRebirthConfig ModConfig { get; private set; } // prevent from accidently overriding the config

    internal static MainAssets Assets { get; private set; }
    internal class MainAssets(string bundleName) : AssetBundleLoader<MainAssets>(bundleName) {
        [LoadFromBundle("CodeRebirthUtils.prefab")]
        public GameObject UtilsPrefab { get; private set; }
    }
    
    private void Awake() {
        Logger = base.Logger;
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
        if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Piggy.PiggyVarietyMod")) Logger.LogFatal("You are using a mod (Piggy's Variety mod) that breaks the player animator and the snow globe will not work properly with this mod, you have been warned.");
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