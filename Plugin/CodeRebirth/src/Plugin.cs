using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Dawn.Utils;
using CodeRebirth.src.ModCompats;
using CodeRebirth.src.Patches;
using BepInEx.Configuration;
using Dusk;
using Dawn;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Content.DevTools;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;

namespace CodeRebirth.src;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("com.rune580.LethalCompanyInputUtils")]
[BepInDependency(DawnLib.PLUGIN_GUID)]
[BepInDependency(Dusk.MyPluginInfo.PLUGIN_GUID)]
[BepInDependency("Zaggy1024.OpenBodyCams", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger { get; private set; }
    internal static readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    public static IngameKeybinds InputActionsInstance { get; private set; }
    public static ConfigFile ConfigFile { get; private set; }
    public static CodeRebirthConfig ModConfig { get; private set; } // prevent from accidently overriding the config
    public static DuskMod Mod { get; private set; }
    public static PersistentDataContainer PersistentDataContainer { get; private set; }

    internal class MainAssets(AssetBundle bundle) : AssetBundleLoader<MainAssets>(bundle)
    {
        [LoadFromBundle("CodeRebirthUtils.prefab")]
        public GameObject UtilsPrefab { get; private set; }

        [LoadFromBundle("EmptyNetworkObject.prefab")]
        public GameObject EmptyNetworkObject { get; private set; }
    }
    internal static MainAssets Assets { get; private set; }

    internal const ulong GLITCH_STEAM_ID = 9;
    internal const int BURN_HIT_ID = 745737;

    private void Awake()
    {
        Logger = base.Logger;
        PersistentDataContainer = this.GetPersistentDataContainer();
        PersistentDataContainer.Set(NamespacedKey.From("code_rebirth", "last_version"), MyPluginInfo.PLUGIN_VERSION);

        ConfigFile = this.Config;
        ModConfig = new CodeRebirthConfig
        {
            ConfigExtendedLogging = ConfigFile.Bind("Debug Options",
                                                "Debug Mode | Extended Logging",
                                                false,
                                                "Whether ExtendedLogging is enabled.")
        };

        _harmony.PatchAll(typeof(PlayerControllerBPatch));
        _harmony.PatchAll(typeof(EnemyAIPatch));
        _harmony.PatchAll(typeof(ShovelPatch));
        _harmony.PatchAll(typeof(DoorLockPatch));
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
        SpikeTrapPatch.Init();
        EnemyAICollisionDetectPatch.Init();
        LandminePatch.Init();
        ShotgunItemPatch.Init();
        TurretPatch.Init();
        SoccerBallPropPatch.Init();
        VehicleControllerPatch.Init();
        MerchantTipPad.Init();
        StoatGun.Init();
        CommitKeyToSave.Init();
        HauntedTeddyBear.Init();
        BearTrapWheelProxy.Init();
        EntranceTeleportPatch.Init();
        BearTrap.Init();
        DuckyTube.Init();

        LethalContent.Enemies.OnFreezeWithContext += (_) => PuppeteersVoodoo.CreateBlacklist();

        // This should be ran before Network Prefabs are registered.
        InputActionsInstance = new IngameKeybinds();

        ModConfig.InitMainCodeRebirthConfig(ConfigFile);

        AssetBundle mainBundle = AssetBundleUtils.LoadBundle(Assembly.GetExecutingAssembly(), "coderebirthasset");
        Assets = new MainAssets(mainBundle);
        Mod = DuskMod.RegisterMod(this, mainBundle);
        Mod.RegisterContentHandlers();

        ModConfig.InitCodeRebirthConfig(ConfigFile);

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
}