
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace CodeRebirth.Configs {
    public class CodeRebirthConfig {
        public ConfigEntry<int> ConfigWalletCost { get; private set; }
        public ConfigEntry<bool> ConfigWalletEnabled { get; private set; }
        public ConfigEntry<string> ConfigMoneyRarity { get; private set; }
        public ConfigEntry<bool> ConfigMoneyScrapEnabled { get; private set; }
        public CodeRebirthConfig(ConfigFile configFile) {
            ConfigWalletEnabled = configFile.Bind("Shop Options",
                                                "Wallet Item | Enabled",
                                                true,
                                                "Enables/Disables the Wallet showing up in shop");
            ConfigWalletCost = configFile.Bind("Shop Options",
                                                "Wallet Item | Cost",
                                                250,
                                                "Cost of Wallet");
            ConfigMoneyRarity = configFile.Bind("Scrap Options",
                                                "Money Scrap | Rarity",
                                                "Modded@1000,ExperimentationLevel@1000,AssuranceLevel@1000,VowLevel@1000,OffenseLevel@1000,MarchLevel@1000,RendLevel@1000,DineLevel@1000,TitanLevel@1000",
                                                "Enables/Disables the Wallet showing up in shop");
            ConfigMoneyScrapEnabled = configFile.Bind("Scrap Options",
                                                "Scrap | Enabled",
                                                true,
                                                "Enables/Disables the Money showing up in the Factory");
            ClearUnusedEntries(configFile);
            Plugin.Logger.LogInfo("Setting up config for CodeRebirth plugin...");
        }
        private void ClearUnusedEntries(ConfigFile configFile) {
            // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
            PropertyInfo orphanedEntriesProp = configFile.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(configFile, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            configFile.Save(); // Save the config file to save these changes
        }
    }
}