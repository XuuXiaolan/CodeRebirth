using System;
using System.Reflection;
using UnityEngine;
using BepInEx;
using LethalLib.Modules;
using BepInEx.Logging;
using System.IO;
using CodeRebirth.Configs;
using System.Collections.Generic;
using static LethalLib.Modules.Levels;
using System.Linq;
using static LethalLib.Modules.Items;
using CodeRebirth.Keybinds;
using HarmonyLib;
using CodeRebirth.src;
using LethalLib.Extras;
using CodeRebirth.Misc;
using CodeRebirth.ScrapStuff;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using CodeRebirth.WeatherStuff;
using LethalLib;
using System.Collections.ObjectModel;

namespace CodeRebirth;
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)] 
[BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(CustomStoryLogs.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
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
        _harmony.PatchAll(typeof(StartOfRoundPatcher));
        // This should be ran before Network Prefabs are registered.

        Assets = new MainAssets("coderebirthasset");
        
        InitializeNetworkBehaviours();

        ModConfig = new CodeRebirthConfig(this.Config); // Create the config with the file from here.
        // Register Keybinds
        InputActionsInstance = new IngameKeybinds();
        
        Logger.LogInfo("Registering content.");
        List<Type> creatureHandlers = Assembly.GetExecutingAssembly().GetLoadableTypes().Where(x =>
            x.BaseType != null
            && x.BaseType.IsGenericType
            && x.BaseType.GetGenericTypeDefinition() == typeof(ContentHandler<>)
        ).ToList();
        
        foreach(Type type in creatureHandlers) {
            Logger.LogDebug($"Invoking {type.Name}");
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