
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace CodeRebirth.Configs {
    public class CodeRebirthConfig {
        // Enables/Disables
        public ConfigEntry<bool> ConfigAllowCrits { get; private set; }
        public ConfigEntry<bool> ConfigWesleyModeEnabled { get; private set; }
        public ConfigEntry<bool> ConfigHoverboardEnabled { get; private set; }
        public ConfigEntry<bool> ConfigMeteorShowerEnabled { get; private set; }
        public ConfigEntry<bool> ConfigTornadosEnabled { get; private set; }
        public ConfigEntry<bool> ConfigWalletEnabled { get; private set; }
        public ConfigEntry<bool> ConfigEpicAxeScrapEnabled { get; private set; }
        public ConfigEntry<bool> ConfigScrapMasterEnabled { get; private set; }
        public ConfigEntry<bool> ConfigCutieFlyEnabled { get; private set; }
        public ConfigEntry<bool> ConfigSnailCatEnabled { get; private set; }
        public ConfigEntry<bool> ConfigItemCrateEnabled { get; private set; }
        public ConfigEntry<bool> ConfigSnowGlobeEnabled { get; private set; }
        public ConfigEntry<bool> ConfigMoneyEnabled { get; private set; }
        // Spawn Weights
        public ConfigEntry<string> ConfigSnailCatSpawnWeights { get; private set; }
        public ConfigEntry<string> ConfigCutieFlySpawnWeights { get; private set; }
        public ConfigEntry<int> ConfigMoneyAbundance { get; private set; }
        public ConfigEntry<string> ConfigEpicAxeScrapSpawnWeights { get; private set; }
        public ConfigEntry<int> ConfigCrateAbundance { get; private set; }
        public ConfigEntry<string> ConfigSnowGlobeSpawnWeights { get; private set; }
        public ConfigEntry<string> ConfigScrapMasterSpawnWeights { get; private set; }
        // Enemy Specific
        public ConfigEntry<int> ConfigCutieFlyMaxSpawnCount { get; private set; }
        public ConfigEntry<int> ConfigSnailCatMaxSpawnCount { get; private set; }
        public ConfigEntry<int> ConfigScrapMasterMaxSpawnCount { get; private set; }
        public ConfigEntry<float> ConfigCutieFlyPowerLevel { get; private set; }
        public ConfigEntry<float> ConfigSnailCatPowerLevel { get; private set; }
        public ConfigEntry<float> ConfigScrapMasterPowerLevel { get; private set; }
        // Weather Specific
        public ConfigEntry<bool> ConfigMeteorHitShip { get; private set; }
        public ConfigEntry<float> ConfigMeteorsDefaultVolume { get; private set; }
        public ConfigEntry<string> ConfigMeteorShowerMoonsBlacklist { get; private set; }
        public ConfigEntry<string> ConfigTornadoType { get; private set; }
        public ConfigEntry<string> ConfigTornadoMoonsBlacklist { get; private set; }
        // Misc
        public ConfigEntry<int> ConfigHoverboardCost { get; private set; }
        public ConfigEntry<int> ConfigWalletCost { get; private set; }
        public ConfigEntry<int> ConfigAverageCoinValue { get; private set; }
        public CodeRebirthConfig(ConfigFile configFile) {
            ConfigMeteorHitShip = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Meteor Strikes Ship",
                                                true,
                                                "Chance of hitting the ship with a meteor.");
            ConfigAllowCrits = configFile.Bind("Weapon Options",
                                                "Weapons | Crits",
                                                true,
                                                "Enables/Disables crits in the game for code rebirth weapons.");
            ConfigWesleyModeEnabled = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Wesley Mode",
                                                false,
                                                "Enables/Disables the Wesley Mode (this is a meme, not recommended lol).");
            ConfigHoverboardEnabled = configFile.Bind("Hoverboard Options",
                                                "Hoverboard | Enabled",
                                                true,
                                                "Enables/Disables the Hoverboard from spawning.");
            ConfigHoverboardCost = configFile.Bind("Hoverboard Options",
                                                "Hoverboard | Cost",
                                                500,
                                                "Cost of Hoverboard.");
            ConfigTornadoType = configFile.Bind("Tornados Options",
                                                "Tornados | Tornado Type",
                                                "Random",
                                                "Type of tornado that could spawn: Random, Fire, Electric, Windy");
            ConfigMeteorShowerEnabled = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Enabled",
                                                true,
                                                "Enables/Disables the MeteorShower from popping up into moons.");
            ConfigTornadosEnabled = configFile.Bind("Tornados Options",
                                                "Tornados | Enabled",
                                                true,
                                                "Enables/Disables the Tornados from popping up into moons.");
            ConfigTornadoMoonsBlacklist = configFile.Bind("Tornados Options",
                                                "Tornados | Moons Blacklist",
                                                "CompanyLevelBuilding",
                                                "Example: (CompanyBuildingLevelList,OffenseLevel). \nList of moons TO REMOVE the Tornados Weather from (Vanilla moons need Level at the end of their name, but modded do not). \n Remove CompanyBuildingLevel at your own risk.");
            ConfigCutieFlyMaxSpawnCount = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Max Spawn Count",
                                                5,
                                                "How many CutieFlies can spawn at once.");
            ConfigSnailCatMaxSpawnCount = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Max Spawn Count",
                                                5,
                                                "How many SnailCats can spawn at once.");
            ConfigScrapMasterMaxSpawnCount = configFile.Bind("ScrapMaster Options",
                                                "Scrap Master | Max Spawn Count",
                                                1,
                                                "How many Scrap Masters can spawn at once.");
            ConfigCutieFlyPowerLevel = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Power Level",
                                                1.0f,
                                                "Power level of the CutieFly enemy.");
            ConfigSnailCatPowerLevel = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Power Level",
                                                1.0f,
                                                "Power level of the SnailCat enemy.");
            ConfigScrapMasterPowerLevel = configFile.Bind("ScrapMaster Options",
                                                "Scrap Master | Power Level",
                                                1.0f,
                                                "Power level of the Scrap Master enemy.");
            ConfigMoneyEnabled = configFile.Bind("Money Options",
                                                "Money | Enabled",
                                                true,
                                                "Enables/Disables the Money from spawning.");
            ConfigSnowGlobeEnabled = configFile.Bind("SnowGlobe Options",
                                                "Snow Globe | Enabled",
                                                true,
                                                "Enables/Disables the Snow Globe from spawning.");
            ConfigSnowGlobeSpawnWeights = configFile.Bind("SnowGlobe Options",
                                                "Snow Globe | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "Spawn Weight of the epic axe in moons.");
            ConfigItemCrateEnabled = configFile.Bind("Crate Options",
                                                "Item Crate | Enabled",
                                                true,
                                                "Enables/Disables the Item Crate from spawning.");
            ConfigScrapMasterEnabled = configFile.Bind("ScrapMaster Options",
                                                "Scrap Master | Enabled",
                                                true,
                                                "Enables/Disables the Scrap Master enemy");
            ConfigCutieFlyEnabled = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Enabled",
                                                true,
                                                "Enables/Disables the CutieFly enemy");
            ConfigSnailCatEnabled = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Enabled",
                                                true,
                                                "Enables/Disables the SnailCat enemy");
            ConfigCrateAbundance = configFile.Bind("Crate Options",
                                                "Crate | Abundance",
                                                3,
                                                "Abundance of crates that spawn outside (between 0 and your number).");
            ConfigMeteorShowerMoonsBlacklist = configFile.Bind("MeteorShower Options",
                                                "Meteor Shower | Blacklist",
                                                "CompanyBuildingLevel",
                                                "Example: (CompanyBuildingLevelList,OffenseLevel). \nList of moons TO REMOVE the Meteor Shower Weather from (Vanilla moons need Level at the end of their name, but modded do not). \n Remove CompanyBuildingLevel at your own risk.");
            ConfigMeteorsDefaultVolume = configFile.Bind("MeteorShower Options",
                                                "Meteors | Default Volume",
                                                0.25f,
                                                "Default Volume of Meteors (between 0 and 1).");
            ConfigWalletEnabled = configFile.Bind("Wallet Options",
                                                "Wallet Item | Enabled",
                                                true,
                                                "Enables/Disables the Wallet from showing up in shop.");
            ConfigEpicAxeScrapSpawnWeights = configFile.Bind("EpicAxe Options",
                                                "Epic Axe Scrap | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "Spawn Weight of the epic axe in moons.");
            ConfigCutieFlySpawnWeights = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "SpawnWeight of the CutieFly in moons.");
            ConfigSnailCatSpawnWeights = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "SpawnWeight of the SnailCat in moons.");
            ConfigWalletCost = configFile.Bind("Wallet Options",
                                                "Wallet Item | Cost",
                                                250,
                                                "Cost of Wallet");
            ConfigEpicAxeScrapEnabled = configFile.Bind("EpicAxe Options",
                                                "Epic Axe Scrap | Enabled",
                                                true,
                                                "Enables/Disables the Epic Axe from showing up in the Factory.");
            ConfigMoneyAbundance = configFile.Bind("Money Options",
                                                "Money Scrap | Abundance",
                                                10,
                                                "Overall Abundance of Money in the level.");
            ConfigAverageCoinValue = configFile.Bind("Money Options",
                                                "Money Scrap | Average Value",
                                                15,
                                                "Average value of Money in the level. (so 5 and 25 are lower and upper limits here).");
            ConfigScrapMasterSpawnWeights = configFile.Bind("ScrapMaster Options",
                                                "Scrap Master Enemy | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "SpawnWeight of the Scrap Master in moons.");
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