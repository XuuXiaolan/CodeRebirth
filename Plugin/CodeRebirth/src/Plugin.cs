using System;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using CodeRebirth.src.Util.AssetLoading;
using CodeRebirth.src.Util.Extensions;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Patches;
using CodeRebirth.src.Util;
using Unity.Netcode;
using BepInEx.Configuration;

namespace CodeRebirth.src;
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)] 
[BepInDependency(WeatherRegistry.Plugin.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(LethalLevelLoader.Plugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("JustJelly.SubtitlesAPI", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Zaggy1024.OpenBodyCams", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Zaggy1024.PathfindingLib", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(MoreCompany.PluginInformation.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger = null!;
    internal static readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    internal static readonly Dictionary<string, AssetBundle> LoadedBundles = [];
    internal static bool MoreSuitsIsOn = false;
    internal static readonly Dictionary<string, Item> samplePrefabs = [];
    internal static IngameKeybinds InputActionsInstance = null!;
    public static ConfigFile configFile { get; private set; } = null!;
    public static CodeRebirthConfig ModConfig { get; private set; } = null!; // prevent from accidently overriding the config
    internal const ulong GLITCH_STEAM_ID = 9;
    internal class MainAssets(string bundleName) : AssetBundleLoader<MainAssets>(bundleName)
    {
        [LoadFromBundle("CodeRebirthUtils.prefab")]
        public GameObject UtilsPrefab { get; private set; } = null!;

        [LoadFromBundle("EmptyNetworkObject.prefab")]
        public GameObject EmptyNetworkObject { get; private set; } = null!;

        [LoadFromBundle("CodeRebirthContent.asset")]
        public CodeRebirthContent CodeRebirthContent { get; private set; } = null!;
    }
    internal static MainAssets Assets { get; private set; } = null!;
    
    private void Awake()
    {
        Logger = base.Logger;
        configFile = this.Config;
        ModConfig = new CodeRebirthConfig(); // Create the config with the file from here.

        ModConfig.ConfigExtendedLogging = configFile.Bind("Debug Options",
                                            "Debug Mode | Extended Logging",
                                            false,
                                            "Whether ExtendedLogging is enabled.");

        _harmony.PatchAll(typeof(QuickMenuManagerPatch));
        _harmony.PatchAll(typeof(PlayerControllerBPatch));
        _harmony.PatchAll(typeof(EnemyAIPatch));
        _harmony.PatchAll(typeof(ShovelPatch));
        _harmony.PatchAll(typeof(DoorLockPatch));
        _harmony.PatchAll(typeof(MineshaftElevatorControllerPatch));
        _harmony.PatchAll(typeof(KeyItemPatch));
        _harmony.PatchAll(typeof(RoundManagerPatch));
        _harmony.PatchAll(typeof(StartOfRoundPatch));
        _harmony.PatchAll(typeof(NetworkBehaviourPatch));
        _harmony.PatchAll(typeof(HDAdditionalLightDataPatch));

        ItemDropshipPatch.Init();
        TerminalPatch.Init();
        KnifeItemPatch.Init();
        PlayerControllerBPatch.Init();
        EnemyAIPatch.Init();
        ShovelPatch.Init();
        DoorLockPatch.Init();
        MineshaftElevatorControllerPatch.Init();
        SpikeTrapPatch.Init();
        EnemyAICollisionDetectPatch.Init();
        LandminePatch.Init();
        ShotgunItemPatch.Init();
        TurretPatch.Init();
        GameNetworkManagerPatch.Init();
        DeleteFileButtonPatch.Init();
        // This should be ran before Network Prefabs are registered.
        InitializeNetworkBehaviours();
        InputActionsInstance = new IngameKeybinds();
        
        Assets = new MainAssets("coderebirthasset");
        // Register Keybinds
        
        Logger.LogInfo("Registering CodeRebirth content.");

        RegisterContentHandlers(Assembly.GetExecutingAssembly());
        ModConfig.InitCodeRebirthConfig(configFile);

        if (SubtitlesAPICompatibilityChecker.Enabled)
        {
            SubtitlesAPICompatibilityChecker.Init();
        }

        if (OpenBodyCamCompatibilityChecker.Enabled)
        {
            OpenBodyCamCompatibilityChecker.Init();
        }

        if (MoreSuitsCompatibilityChecker.Enabled)
        {
            MoreSuitsCompatibilityChecker.Init();
        }

        Logger.LogInfo("Cleaning config");
        Config.ClearUnusedEntries();
        Config.Save();
        
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    public static void RegisterContentHandlers(Assembly assembly) {
        IEnumerable<Type> contentHandlers = assembly.GetLoadableTypes().Where(x =>
            x.BaseType != null
            && x.BaseType.IsGenericType
            && x.BaseType.GetGenericTypeDefinition() == typeof(ContentHandler<>)
        );
        
        foreach (Type type in contentHandlers)
        {
            type.GetConstructor([]).Invoke([]);
        }
    }
    
    private void OnDisable()
    {
        foreach (AssetBundle bundle in LoadedBundles.Values)
        {
            bundle.Unload(false);
        }
        Logger.LogDebug("Unloaded assetbundles.");
        LoadedBundles.Clear();
    }

    internal static void ExtendedLogging(object text)
    {
        if (ModConfig.ConfigExtendedLogging.Value)
        {
            Logger.LogInfo(text);
        }
    }

    private void InitializeNetworkBehaviours()
    {
        var types = Assembly.GetExecutingAssembly().GetLoadableTypes();
        foreach (var type in types)
        {
            if (type.IsNested || !typeof(NetworkBehaviour).IsAssignableFrom(type))
            {
                continue; // we do not care about fixing it, if it is not a network behaviour
            }
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