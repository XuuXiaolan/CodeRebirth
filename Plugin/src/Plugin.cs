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

namespace CodeRebirth {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin {
        internal static new ManualLogSource Logger;
        private readonly Harmony _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        internal static GameObject BigExplosion;
        internal static GameObject CRUtils;
        internal static Item Wallet;
        internal static Item Money;
        internal static Item Meteorite;
        internal static WeatherEffect meteorShower;
        internal static GameObject Meteor;
        internal static Dictionary<string, Item> samplePrefabs = [];
        internal static GameObject effectObject;
        internal static GameObject effectPermanentObject;
        internal static IngameKeybinds InputActionsInstance;
        public static CodeRebirthConfig ModConfig { get; private set; } // prevent from accidently overriding the config

        private void Awake() {
            Logger = base.Logger;
            _harmony.PatchAll(typeof(StartOfRoundPatcher));
            // This should be ran before Network Prefabs are registered.
            Assets.PopulateAssets();
            
            CRUtils = Assets.MainAssetBundle.LoadAsset<GameObject>("CodeRebirthUtils");
            ModConfig = new CodeRebirthConfig(this.Config); // Create the config with the file from here.
            // Register Keybinds
            InputActionsInstance = new IngameKeybinds();
            CodeRebirthWeather();
            CodeRebirthScrap();

            InitializeNetworkBehaviours();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
        private void CodeRebirthWeather() {
            // Instantiate the weather effect objects
            Meteor = Assets.MainAssetBundle.LoadAsset<GameObject>("Meteor");
            if (Meteor == null) {
                Logger.LogError("Failed to load meteor prefab");
            }
            BigExplosion = Assets.MainAssetBundle.LoadAsset<GameObject>("BigExplosion");
            Meteorite = Assets.MainAssetBundle.LoadAsset<Item>("MeteoriteObj");
            Utilities.FixMixerGroups(Meteorite.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(Meteorite.spawnPrefab);
            samplePrefabs.Add("Meteorite", Meteorite);
            RegisterScrap(Meteorite, 0, LevelTypes.All);

            effectObject = Instantiate(Assets.MainAssetBundle.LoadAsset<GameObject>("MeteorContainer"), Vector3.zero, Quaternion.identity);
            effectObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(effectObject);

            effectPermanentObject = Instantiate(Assets.MainAssetBundle.LoadAsset<GameObject>("MeteorShowerWeather"), Vector3.zero, Quaternion.identity);
            effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(effectPermanentObject);

            // Create a new WeatherEffect instance
            meteorShower = new WeatherEffect()
            {
                name = "MeteorShower",
                effectObject = effectObject,
                effectPermanentObject = effectPermanentObject,
                lerpPosition = false,
                sunAnimatorBool = "",
                transitioning = false
            };
            Weathers.RegisterWeather("Meteor Shower", meteorShower, Levels.LevelTypes.All, 0, 0);
        }
        private void CodeRebirthScrap() {
            // Wallet+Coin register
            Wallet = Assets.MainAssetBundle.LoadAsset<Item>("WalletObj");
            Utilities.FixMixerGroups(Wallet.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(Wallet.spawnPrefab);
            TerminalNode wTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("wTerminalNode");
            RegisterShopItemWithConfig(ModConfig.ConfigWalletEnabled.Value, false, Wallet, wTerminalNode, ModConfig.ConfigWalletCost.Value, "");
            
            Money = Assets.MainAssetBundle.LoadAsset<Item>("MoneyObj");
            Utilities.FixMixerGroups(Money.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(Money.spawnPrefab);
            RegisterScrapWithConfig(ModConfig.ConfigMoneyScrapEnabled.Value, ModConfig.ConfigMoneyRarity.Value, Money);
        }
        private void RegisterScrapWithConfig(bool enabled, string configMoonRarity, Item scrap) {
            if (enabled) { 
                (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
                RegisterScrap(scrap, spawnRateByLevelType, spawnRateByCustomLevelType);
            } else {
                RegisterScrap(scrap, 0, LevelTypes.All);
            }
            return;
        }
        private void RegisterShopItemWithConfig(bool enabledShopItem, bool enabledScrap, Item item, TerminalNode terminalNode, int itemCost, string configMoonRarity) {
            if (enabledShopItem) { 
                RegisterShopItem(item, null, null, terminalNode, itemCost);
            }
            if (enabledScrap) {
                RegisterScrapWithConfig(true, configMoonRarity, item);
            }
            return;
        }
        private (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) ConfigParsing(string configMoonRarity) {
            Dictionary<LevelTypes, int> spawnRateByLevelType = new Dictionary<LevelTypes, int>();
            Dictionary<string, int> spawnRateByCustomLevelType = new Dictionary<string, int>();

            foreach (string entry in configMoonRarity.Split(',').Select(s => s.Trim())) {
                string[] entryParts = entry.Split('@');

                if (entryParts.Length != 2)
                {
                    continue;
                }

                string name = entryParts[0];
                int spawnrate;

                if (!int.TryParse(entryParts[1], out spawnrate))
                {
                    continue;
                }

                if (System.Enum.TryParse<LevelTypes>(name, true, out LevelTypes levelType))
                {
                    spawnRateByLevelType[levelType] = spawnrate;
                    Plugin.Logger.LogInfo($"Registered spawn rate for level type {levelType} to {spawnrate}");
                }
                else
                {
                    spawnRateByCustomLevelType[name] = spawnrate;
                    Plugin.Logger.LogInfo($"Registered spawn rate for custom level type {name} to {spawnrate}");
                }
            }
            return (spawnRateByLevelType, spawnRateByCustomLevelType);
        }
        private void InitializeNetworkBehaviours() {
            var types = Assembly.GetExecutingAssembly().GetTypes();
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
        public static class Assets {
            public static AssetBundle MainAssetBundle = null;
            public static void PopulateAssets() {
                string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "coderebirthasset"));
                if (MainAssetBundle == null) {
                    Plugin.Logger.LogError("Failed to load custom assets.");
                    return;
                }
            }
        }
    }
}