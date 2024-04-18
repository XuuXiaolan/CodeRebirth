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

namespace CodeRebirth {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)] 
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin {
        internal static new ManualLogSource Logger;
        public static Item Wallet;
        internal static IngameKeybinds InputActionsInstance;
        public static CodeRebirthConfig ModConfig { get; private set; } // prevent from accidently overriding the config

        private void Awake() {
            Logger = base.Logger;
            // This should be ran before Network Prefabs are registered.
            Assets.PopulateAssets();
            ModConfig = new CodeRebirthConfig(this.Config); // Create the config with the file from here.


            // Hammer Item/Scrap + keybinds
            InputActionsInstance = new IngameKeybinds();
            
            Wallet = Assets.MainAssetBundle.LoadAsset<Item>("WalletObj");
            Utilities.FixMixerGroups(Wallet.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(Wallet.spawnPrefab);
            TerminalNode wTerminalNode = Assets.MainAssetBundle.LoadAsset<TerminalNode>("wTerminalNode");
            RegisterShopItemWithConfig(ModConfig.ConfigWalletEnabled.Value, false, Wallet, wTerminalNode, ModConfig.ConfigWalletCost.Value, "");
            InitializeNetworkBehaviours();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
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