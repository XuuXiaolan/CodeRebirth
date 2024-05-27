
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace CodeRebirth.Configs {
    public class CodeRebirthConfig {
        public ConfigEntry<string> ConfigSnailCatSpawnWeights { get; private set; }
        public ConfigEntry<string> ConfigCutieFlySpawnWeights { get; private set; }
        public ConfigEntry<int> ConfigWalletCost { get; private set; }
        public ConfigEntry<bool> ConfigWalletEnabled { get; private set; }
        public ConfigEntry<int> ConfigMoneyAbundance { get; private set; }
        public ConfigEntry<bool> ConfigEpicAxeScrapEnabled { get; private set; }
        public ConfigEntry<string> ConfigEpicAxeScrapSpawnWeights { get; private set; }
        public ConfigEntry<int> ConfigAverageCoinValue { get; private set; }
        public ConfigEntry<float> ConfigMeteorsDefaultVolume { get; private set; }
        public ConfigEntry<string> ConfigMeteorShowerMoonsBlacklist { get; private set; }
        public ConfigEntry<string> ConfigDuckSpawnWeights { get; private set; }
        public CodeRebirthConfig(ConfigFile configFile) {
            ConfigMeteorShowerMoonsBlacklist = configFile.Bind("Weather Options",
                                                "Meteor Shower | Blacklist",
                                                "CompanyBuildingLevel",
                                                "Example: (CompanyBuildingLevelList,OffenseLevel). \nList of moons TO REMOVE the Meteor Shower Weather from (Vanilla moons need Level at the end of their name, but modded do not). \n Remove CompanyBuildingLevel at your own risk.");
            ConfigMeteorsDefaultVolume = configFile.Bind("Weather Options",
                                                "Meteors | Default Volume",
                                                0.5f,
                                                "Default Volume of Meteors (between 0 and 1).");
            ConfigWalletEnabled = configFile.Bind("Shop Options",
                                                "Wallet Item | Enabled",
                                                true,
                                                "Enables/Disables the Wallet from showing up in shop");
            ConfigEpicAxeScrapSpawnWeights = configFile.Bind("Scrap Options",
                                                "Epic Axe Scrap | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "SpawnWeight of the axe in moons");
            ConfigCutieFlySpawnWeights = configFile.Bind("Enemy Options",
                                                "CutieFly Enemy | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "SpawnWeight of the CutieFly in moons");
            ConfigSnailCatSpawnWeights = configFile.Bind("Enemy Options",
                                                "SnailCat Enemy | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "SpawnWeight of the SnailCat in moons");
            ConfigWalletCost = configFile.Bind("Shop Options",
                                                "Wallet Item | Cost",
                                                250,
                                                "Cost of Wallet");
            ConfigEpicAxeScrapEnabled = configFile.Bind("Scrap Options",
                                                "Epic Axe Scrap | Enabled",
                                                true,
                                                "Enables/Disables the Epic Axe from showing up in the Factory");
            ConfigMoneyAbundance = configFile.Bind("Scrap Options",
                                                "Money Scrap | Abundance",
                                                10,
                                                "Overall Abundance of Money in the level.");
            ConfigAverageCoinValue = configFile.Bind("Scrap Options",
                                                "Money Scrap | Average Value",
                                                15,
                                                "Average value of Money in the level. (so 5 and 25 are lower and upper limits here)");
            ConfigDuckSpawnWeights = configFile.Bind("Enemy Options",
                                                "Duck Enemy | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "SpawnWeight of the Duck in moons");
            ClearUnusedEntries(configFile);
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