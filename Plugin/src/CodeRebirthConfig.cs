
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
        public ConfigEntry<int> ConfigTornadoWeatherType { get; private set; }
        public ConfigEntry<float> ConfigMeteorShowerMeteoriteSpawnChance { get; private set; }
        public ConfigEntry<float> ConfigMeteorShowerInShipVolume { get; private set; }
        public ConfigEntry<bool> ConfigMeteorHitShip { get; private set; }
        public ConfigEntry<float> ConfigMeteorsDefaultVolume { get; private set; }
        // Misc
        public ConfigEntry<int> ConfigHoverboardCost { get; private set; }
        public ConfigEntry<int> ConfigWalletCost { get; private set; }
        public ConfigEntry<int> ConfigAverageCoinValue { get; private set; }
        public CodeRebirthConfig(ConfigFile configFile) {
            ConfigTornadoWeatherType = configFile.Bind("Weather Options",
                                                "Weather | Tornado Type",
                                                0,
                                                "Type of tornado to spawn. 0 = Random, 1 = Fire, 2 = Blood, 3 = Wind, 4 = Smoke, 5 = Water");
            ConfigMeteorShowerMeteoriteSpawnChance = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Meteorite Spawn Chance",
                                                1f,
                                                "Chance of spawning a meteorite when a meteor is spawned (0 to 100 decimals included).");
            ConfigMeteorShowerInShipVolume = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Meteor Volume",
                                                1f,
                                                "Multiplier of the meteors' volume for when the player is in the ship and the ship door is closed.");
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
            ConfigMeteorShowerEnabled = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Enabled",
                                                true,
                                                "Enables/Disables the MeteorShower from popping up into moons.");
            ConfigTornadosEnabled = configFile.Bind("Tornados Options",
                                                "Tornados | Enabled",
                                                true,
                                                "Enables/Disables the Tornados from popping up into moons.");
            ConfigCutieFlyMaxSpawnCount = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Max Spawn Count",
                                                5,
                                                "How many CutieFlies can spawn at once.");
            ConfigSnailCatMaxSpawnCount = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Max Spawn Count",
                                                5,
                                                "How many SnailCats can spawn at once.");
            ConfigCutieFlyPowerLevel = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Power Level",
                                                1.0f,
                                                "Power level of the CutieFly enemy.");
            ConfigSnailCatPowerLevel = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Power Level",
                                                1.0f,
                                                "Power level of the SnailCat enemy.");
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
                                                "Spawn Weight of the Snow Globe in moons.");
            ConfigItemCrateEnabled = configFile.Bind("Crate Options",
                                                "Item Crate | Enabled",
                                                true,
                                                "Enables/Disables the Item Crate from spawning.");
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