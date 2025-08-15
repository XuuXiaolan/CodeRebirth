using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using CodeRebirthLib.Utils;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Patches;
using Unity.Netcode;
using BepInEx.Configuration;
using CodeRebirthLib;
using CodeRebirthLib.CRMod;


/*
Big todo
Give the configs some sort of listener for lethal config so i can detect runtime changes.
*/
namespace CodeRebirth.src;
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("mrov.WeatherRegistry")]
[BepInDependency("com.rune580.LethalCompanyInputUtils")]
[BepInDependency(CRLib.PLUGIN_GUID)]
[BepInDependency(LethalLevelLoader.Plugin.ModGUID)]
[BepInDependency("Zaggy1024.OpenBodyCams", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(MoreCompany.PluginInformation.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(MoreShipUpgrades.Misc.Metadata.GUID, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger = null!;
    internal static readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    internal static IngameKeybinds InputActionsInstance = null!;
    public static ConfigFile configFile { get; private set; } = null!;
    public static CodeRebirthConfig ModConfig { get; private set; } = null!; // prevent from accidently overriding the config
    public static CRMod Mod { get; private set; } = null!;

    internal class MainAssets(AssetBundle bundle) : AssetBundleLoader<MainAssets>(bundle)
    {
        [LoadFromBundle("CodeRebirthUtils.prefab")]
        public GameObject UtilsPrefab { get; private set; } = null!;

        [LoadFromBundle("EmptyNetworkObject.prefab")]
        public GameObject EmptyNetworkObject { get; private set; } = null!;
    }
    internal static MainAssets Assets { get; private set; } = null!;

    internal const ulong GLITCH_STEAM_ID = 9;

    private void Awake()
    {
        Logger = base.Logger;
        configFile = this.Config;
        ModConfig = new CodeRebirthConfig();

        ModConfig.ConfigExtendedLogging = configFile.Bind("Debug Options",
                                            "Debug Mode | Extended Logging",
                                            false,
                                            "Whether ExtendedLogging is enabled.");

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
        SoccerBallPropPatch.Init();
        // This should be ran before Network Prefabs are registered.
        InitializeNetworkBehaviours();
        InputActionsInstance = new IngameKeybinds();

        ModConfig.InitMainCodeRebirthConfig(configFile);

        AssetBundle mainBundle = AssetBundleUtils.LoadBundle(Assembly.GetExecutingAssembly(), "coderebirthasset");
        Assets = new MainAssets(mainBundle);
        Mod = CRMod.RegisterMod(this, mainBundle);
        Mod.RegisterContentHandlers();

        ModConfig.InitCodeRebirthConfig(configFile);

        Logger.LogInfo("Registering CodeRebirth content.");

        if (OpenBodyCamCompatibilityChecker.Enabled)
        {
            OpenBodyCamCompatibilityChecker.Init();
        }

        if (ModConfig.ConfigCleanUnusedConfigs.Value)
        {
            Logger.LogInfo("Cleaning config");
            Config.ClearUnusedEntries();
        }

        Config.Save();

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
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
                continue; // we do not care about fixing it, if it is not a network behaviour

            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length <= 0)
                    continue;

                method.Invoke(null, null);
            }
        }
    }
}