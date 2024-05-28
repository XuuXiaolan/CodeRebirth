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
        ModConfig = new CodeRebirthConfig(this.Config); // Create the config with the file from here.
        _harmony.PatchAll(typeof(StartOfRoundPatcher));
        // This should be ran before Network Prefabs are registered.

        Assets = new MainAssets("coderebirthasset");
        
        InitializeNetworkBehaviours();
        // Register Keybinds
        InputActionsInstance = new IngameKeybinds();
        
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
        if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("impulse.CentralConfig")) Logger.LogFatal("You are using a mod that potentially changes how weather works and is potentially removing this mod's custom weather from moons, you have been warned.");
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