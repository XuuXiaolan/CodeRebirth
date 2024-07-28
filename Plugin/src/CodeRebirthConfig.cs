
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace CodeRebirth.Configs {
    public class CodeRebirthConfig {
        // Enables/Disables
        public ConfigEntry<bool> ConfigFloraEnabled { get; private set; }
        public ConfigEntry<bool> ConfigRedwoodHeartEnabled { get; private set; }
        public ConfigEntry<bool> ConfigRedwoodEnabled { get; private set; }
        public ConfigEntry<bool> ConfigSnowGlobeMusic { get; private set; }
        public ConfigEntry<bool> ConfigAllowCrits { get; private set; }
        public ConfigEntry<bool> ConfigWesleyModeEnabled { get; private set; }
        public ConfigEntry<bool> ConfigHoverboardEnabled { get; private set; }
        public ConfigEntry<bool> ConfigMeteorShowerEnabled { get; private set; }
        public ConfigEntry<bool> ConfigTornadosEnabled { get; private set; }
        public ConfigEntry<bool> ConfigWalletEnabled { get; private set; }
        public ConfigEntry<bool> ConfigEpicAxeScrapEnabled { get; private set; }
        public ConfigEntry<bool> ConfigCutieFlyEnabled { get; private set; }
        public ConfigEntry<bool> ConfigSnailCatEnabled { get; private set; }
        public ConfigEntry<bool> ConfigItemCrateEnabled { get; private set; }
        public ConfigEntry<bool> ConfigSnowGlobeEnabled { get; private set; }
        public ConfigEntry<bool> ConfigMoneyEnabled { get; private set; }
        public ConfigEntry<bool> ConfigNaturesMaceScrapEnabled { get; private set; }
        public ConfigEntry<bool> ConfigIcyHammerScrapEnabled { get; private set; }
        public ConfigEntry<bool> ConfigSpikyMaceScrapEnabled { get; private set; }
        // Spawn Weights
        public ConfigEntry<string> ConfigNaturesMaceScrapSpawnWeights { get; private set; }
        public ConfigEntry<string> ConfigIcyHammerScrapSpawnWeights { get; private set; }
        public ConfigEntry<string> ConfigSpikyMaceScrapSpawnWeights { get; private set; }
        public ConfigEntry<string> ConfigRedwoodSpawnWeights { get; private set; }
        public ConfigEntry<string> ConfigSnailCatSpawnWeights { get; private set; }
        public ConfigEntry<string> ConfigCutieFlySpawnWeights { get; private set; }
        public ConfigEntry<int> ConfigMoneyAbundance { get; private set; }
        public ConfigEntry<string> ConfigEpicAxeScrapSpawnWeights { get; private set; }
        public ConfigEntry<int> ConfigCrateAbundance { get; private set; }
        public ConfigEntry<string> ConfigSnowGlobeSpawnWeights { get; private set; }
        // Enemy Specific
        public ConfigEntry<float> ConfigRedwoodNormalVolume { get; private set; }
        public ConfigEntry<float> ConfigRedwoodPowerLevel { get; private set; }
        public ConfigEntry<int> ConfigRedwoodMaxSpawnCount { get; private set; }
        public ConfigEntry<float> ConfigRedwoodInShipVolume { get; private set; }
        public ConfigEntry<float> ConfigRedwoodSpeed { get; private set; }
        public ConfigEntry<float> ConfigRedwoodShipPadding { get; private set; }
        public ConfigEntry<float> ConfigRedwoodEyesight { get; private set; }
        public ConfigEntry<bool> ConfigRedwoodCanEatOldBirds { get; private set; }
        public ConfigEntry<float> ConfigCutieFlyFlapWingVolume { get; private set; }
        public ConfigEntry<int> ConfigCutieFlyMaxSpawnCount { get; private set; }
        public ConfigEntry<int> ConfigSnailCatMaxSpawnCount { get; private set; }
        public ConfigEntry<float> ConfigCutieFlyPowerLevel { get; private set; }
        public ConfigEntry<float> ConfigSnailCatPowerLevel { get; private set; }

        // Weather Specific
        public ConfigEntry<float> ConfigTornadoPullStrength { get; private set; }
        public ConfigEntry<bool> ConfigTornadoYeetSFX { get; private set; }
        public ConfigEntry<string> ConfigTornadoCanFlyYouAwayWeatherTypes { get; private set; }
        public ConfigEntry<float> ConfigTornadoSpeed { get; private set; }
        public ConfigEntry<float> ConfigMeteorSpeed { get; private set; }
        public ConfigEntry<int> ConfigMinMeteorSpawnCount { get; private set; }
        public ConfigEntry<int> ConfigMaxMeteorSpawnCount { get; private set; }
        public ConfigEntry<string> ConfigTornadoWeatherTypes { get; private set; }
        public ConfigEntry<float> ConfigTornadoInShipVolume { get; private set; }
        public ConfigEntry<float> ConfigTornadoDefaultVolume { get; private set; }
        public ConfigEntry<float> ConfigMeteorShowerMeteoriteSpawnChance { get; private set; }
        public ConfigEntry<float> ConfigMeteorShowerInShipVolume { get; private set; }
        public ConfigEntry<bool> ConfigMeteorHitShip { get; private set; }
        public ConfigEntry<float> ConfigMeteorsDefaultVolume { get; private set; }
        // Misc
        public ConfigEntry<string> ConfigFloraSpawnPlaces { get; private set; }
        public ConfigEntry<float> ConfigCritChance { get; private set; }
        public ConfigEntry<bool> ConfigEnableImperiumDebugs { get; private set; }
        public ConfigEntry<bool> ConfigWalletMode { get; private set; }
        public ConfigEntry<int> ConfigHoverboardCost { get; private set; }
        public ConfigEntry<int> ConfigWalletCost { get; private set; }
        public ConfigEntry<int> ConfigAverageCoinValue { get; private set; }
        public CodeRebirthConfig(ConfigFile configFile) {
			configFile.SaveOnConfigSet = false;

            #region Flora
            ConfigFloraEnabled = configFile.Bind("Flora Options",
                                                "Flora | Enabled",
                                                true,
                                                "Whether Flora is enabled.");
            ConfigFloraSpawnPlaces = configFile.Bind("Flora Options",
                                                "Flora | Spawn Places",
                                                "Vow,Adamance,March,Modded",
                                                "Flora spawn places e.g. `All,Experimentation,Assurance,Gloom`, can only spawn on top of grass tagged ground!!");
            #endregion
            #region Tornado
            ConfigTornadosEnabled = configFile.Bind("Tornado Options",
                                                "Tornados | Enabled",
                                                true,
                                                "Enables/Disables the Tornados from popping up into moons.");
            ConfigTornadoWeatherTypes = configFile.Bind("Tornado Options",
                                                "Tornados | Enabled Types",
                                                "random",
                                                new ConfigDescription("Types of tornados that are allowed to spawn", new AcceptableValueList<string>("fire", "blood", "windy", "smoke", "water", "electric", "random"))
                                                );
            ConfigTornadoCanFlyYouAwayWeatherTypes = configFile.Bind("Tornado Options",
                                                "Tornado | Can Fly You Away Weather Types",
                                                "All",
                                                "Tornado weather types that can fly you away (All, or specify per weather type like so: Fire,Electric,etc except water!!).");
            ConfigTornadoSpeed = configFile.Bind("Tornado Options",
                                                "Tornados | Speed",
                                                5f,
                                                new ConfigDescription(
                                                    "Speed of tornados.",
                                                    new AcceptableValueRange<float>(0, 100f)
                                                ));
            ConfigTornadoPullStrength = configFile.Bind("Tornado Options",
                                                "Tornados | Pull Strength",
                                                20f,
                                                new ConfigDescription(
                                                    "Pull strength of tornados.",
                                                    new AcceptableValueRange<float>(0f, 100f)
                                                ));
            ConfigTornadoDefaultVolume = configFile.Bind("Tornado Options",
                                                "Tornados | Default Volume",
                                                1f,
                                                new ConfigDescription(
                                                    "Default volume of tornados.",
                                                    new AcceptableValueRange<float>(0, 1f)
                                                ));
            ConfigTornadoInShipVolume = configFile.Bind("Tornado Options",
                                                "Tornados | Volume in Ship",
                                                1f,
                                                new ConfigDescription(
                                                    "Volume of tornados in the ship.",
                                                    new AcceptableValueRange<float>(0, 1f)
                                                ));
            ConfigTornadoYeetSFX = configFile.Bind("Tornado Options",
                                                "Tornados | Yeet SFX",
                                                false,
                                                "Tornado Yeet SFX");
            #endregion
            #region Weapons
            ConfigAllowCrits = configFile.Bind("Weapon Options",
                                                "Weapons | Crits",
                                                true,
                                                "Enables/Disables crits in the game for code rebirth weapons.");
            ConfigCritChance = configFile.Bind("Weapon Options",
                                                "Weapons | Crit Chance",
                                                25f,
                                                new ConfigDescription(
                                                    "Chance of crits in the game for code rebirth weapons.",
                                                    new AcceptableValueRange<float>(0, 100f)
                                                ));
            ConfigNaturesMaceScrapEnabled = configFile.Bind("NatureMace Options",
                                                "Natures Mace | Scrap Enabled",
                                                true,
                                                "Whether Natures Mace scrap is enabled.");
            ConfigNaturesMaceScrapSpawnWeights = configFile.Bind("NatureMace Options",
                                                "Natures Mace | Scrap Spawn Weights",
                                                "Modded:15,Vanilla:15",
                                                "Natures Mace scrap spawn weights.");
            ConfigIcyHammerScrapEnabled = configFile.Bind("Icy Hammer Options",
                                                "Icy Hammer | Scrap Enabled",
                                                true,
                                                "Whether Icy Hammer scrap is enabled.");
            ConfigIcyHammerScrapSpawnWeights = configFile.Bind("Icy Hammer Options",
                                                "Icy Hammer | Scrap Spawn Weights",
                                                "Modded:15,Vanilla:15",
                                                "Icy Hammer scrap spawn weights.");
            ConfigSpikyMaceScrapEnabled = configFile.Bind("Spiky Mace Options",
                                                "Spiky Mace | Scrap Enabled",
                                                true,
                                                "Whether Spiky Mace scrap is enabled.");
            ConfigSpikyMaceScrapSpawnWeights = configFile.Bind("Spiky Mace Options",
                                                "Spiky Mace | Scrap Spawn Weights",
                                                "Modded:15,Vanilla:15",
                                                "Spiky Mace scrap spawn weights.");
            ConfigEpicAxeScrapEnabled = configFile.Bind("EpicAxe Options",
                                                "Epic Axe Scrap | Enabled",
                                                true,
                                                "Enables/Disables the Epic Axe from showing up in the Factory.");
            ConfigEpicAxeScrapSpawnWeights = configFile.Bind("EpicAxe Options",
                                                "Epic Axe Scrap | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "Spawn Weight of the epic axe in moons.");
            #endregion
            #region Redwood
            /*ConfigRedwoodCanEatOldBirds = configFile.Bind("Redwood Options",
                                                "Redwood | Can Eat Old Birds",
                                                true,
                                                "Whether redwood can eat old birds.");
            ConfigRedwoodMaxSpawnCount = configFile.Bind("Redwood Options",
                                                "Redwood | Max Spawn Count",
                                                5,
                                                new ConfigDescription(
                                                    "Redwood max spawn count.",
                                                    new AcceptableValueRange<int>(0, 99)
                                                ));
            ConfigRedwoodPowerLevel = configFile.Bind("Redwood Options",
                                                "Redwood | Power Level",
                                                3f,
                                                new ConfigDescription(
                                                    "Redwood power level.",
                                                    new AcceptableValueRange<float>(0, 999f)
                                                ));
            ConfigRedwoodShipPadding = configFile.Bind("Redwood Options",
                                                "Redwood | Ship Padding",
                                                15f,
                                                new ConfigDescription(
                                                    "How far away the redwood usually stays from the ship.",
                                                    new AcceptableValueRange<float>(0, 999f)
                                                ));
            ConfigRedwoodSpawnWeights = configFile.Bind("Redwood Options",
                                                "Redwood | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "Redwood spawn weights.");
            ConfigRedwoodSpeed = configFile.Bind("Redwood Options",
                                                "Redwood | Speed",
                                                5f,
                                                new ConfigDescription(
                                                    "Redwood speed.",
                                                    new AcceptableValueRange<float>(0, 999f)
                                                ));
            ConfigRedwoodEnabled = configFile.Bind("Redwood Options",
                                                "Redwood | Enabled",
                                                true,
                                                "Whether redwood is enabled.");
            ConfigRedwoodEyesight = configFile.Bind("Redwood Options",
                                                "Redwood | Eyesight",
                                                40f,
                                                new ConfigDescription(
                                                    "Redwood eyesight.",
                                                    new AcceptableValueRange<float>(0, 999f)
                                                ));
            ConfigRedwoodNormalVolume = configFile.Bind("Redwood Options",
                                                "Redwood | Normal Volume",
                                                1f,
                                                new ConfigDescription(
                                                    "Redwood Normal volume.",
                                                    new AcceptableValueRange<float>(0, 1f)
                                                ));
            ConfigRedwoodHeartEnabled = configFile.Bind("Redwood Options",
                                                "Redwood | Heart Enabled",
                                                true,
                                                "Whether redwood heart is enabled.");
            ConfigRedwoodInShipVolume = configFile.Bind("Redwood Options",
                                                "Redwood | In Ship Volume",
                                                0.75f,
                                                new ConfigDescription(
                                                    "Redwood in ship volume.",
                                                    new AcceptableValueRange<float>(0, 1f)
                                                ));*/
            #endregion
            #region Meteors
            ConfigMeteorShowerEnabled = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Enabled",
                                                true,
                                                "Enables/Disables the MeteorShower from popping up into moons.");
            ConfigMaxMeteorSpawnCount = configFile.Bind("MeteorShower Options",
                                                "Meteors | Max Spawn Count",
                                                3,
                                                new ConfigDescription(
                                                    "Maximum number of meteors to spawn at once every spawn cycle.",
                                                    new AcceptableValueRange<int>(0, 100)
                                                ));
            ConfigMinMeteorSpawnCount = configFile.Bind("MeteorShower Options",
                                                "Meteors | Min Spawn Count",
                                                1,
                                                new ConfigDescription(
                                                    "Minimum number of meteors to spawn at once every spawn cycle.",
                                                    new AcceptableValueRange<int>(0, 100)
                                                ));
            ConfigMeteorSpeed = configFile.Bind("MeteorShower Options",
                                                "Meteors | Speed",
                                                50f,
                                                new ConfigDescription(
                                                    "Speed of meteors.",
                                                    new AcceptableValueRange<float>(0, 1000f)
                                                ));
            ConfigMeteorHitShip = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Meteor Strikes Ship",
                                                true,
                                                "Allows striking the ship with a meteor.");
            ConfigMeteorShowerMeteoriteSpawnChance = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Meteorite Spawn Chance",
                                                2.5f,
												new ConfigDescription(
													"Chance of spawning a meteorite when a meteor is spawned (0 to 100 decimals included).",
													new AcceptableValueRange<float>(0, 100f)
												));
            ConfigWesleyModeEnabled = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Wesley Mode",
                                                false,
                                                "Enables/Disables the Wesley Mode (this is a meme, not recommended lol).");
            ConfigMeteorsDefaultVolume = configFile.Bind("MeteorShower Options",
                                                "Meteors | Default Volume",
                                                0.25f,
												new ConfigDescription(
													"Default Volume of Meteors (between 0 and 1).",
													new AcceptableValueRange<float>(0, 1f)
												));
            ConfigMeteorShowerInShipVolume = configFile.Bind("MeteorShower Options",
                                                "MeteorShower | Meteor Volume",
                                                1f,
												new ConfigDescription(
													"Multiplier of the meteors volume for when the player is in the ship and the ship door is closed.", 
													new AcceptableValueRange<float>(0, 1f)
												));
            #endregion
            #region ModCompat
            ConfigEnableImperiumDebugs = configFile.Bind("Imperium Compatibility", 
                                                        "Enable Imperium Debugs",
                                                        false,
                                                        "Enables the debugs I made using the imperium api for stuff like tornados. (currently no work)");
            #endregion
            #region CutieFly
            ConfigCutieFlyEnabled = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Enabled",
                                                true,
                                                "Enables/Disables the CutieFly enemy");
            ConfigCutieFlySpawnWeights = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "SpawnWeight of the CutieFly in moons.");
            ConfigCutieFlyMaxSpawnCount = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Max Spawn Count",
                                                5,
                                                "How many CutieFlies can spawn at once.");
            ConfigCutieFlyPowerLevel = configFile.Bind("CutieFly Options",
                                                "CutieFly Enemy | Power Level",
                                                1.0f,
                                                "Power level of the CutieFly enemy.");
            ConfigCutieFlyFlapWingVolume = configFile.Bind("CutieFly Options",
                                                "Cutie Fly | Flap Wing Volume",
                                                0.75f,
                                                new ConfigDescription(
                                                    "Volume of flapping wings.",
                                                    new AcceptableValueRange<float>(0, 1f)
                                                ));
            #endregion
            #region SnailCat
            ConfigSnailCatEnabled = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Enabled",
                                                true,
                                                "Enables/Disables the SnailCat enemy");
            ConfigSnailCatSpawnWeights = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "SpawnWeight of the SnailCat in moons.");
            ConfigSnailCatMaxSpawnCount = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Max Spawn Count",
                                                5,
                                                "How many SnailCats can spawn at once.");
            ConfigSnailCatPowerLevel = configFile.Bind("SnailCat Options",
                                                "SnailCat Enemy | Power Level",
                                                1.0f,
                                                "Power level of the SnailCat enemy.");
            #endregion
            #region Hoverboard
            ConfigHoverboardEnabled = configFile.Bind("Hoverboard Options",
                                                "Hoverboard | Enabled",
                                                true,
                                                "Enables/Disables the Hoverboard from spawning.");
            ConfigHoverboardCost = configFile.Bind("Hoverboard Options",
                                                "Hoverboard | Cost",
                                                500,
                                                "Cost of Hoverboard.");
            #endregion
            #region Wallet
            ConfigWalletEnabled = configFile.Bind("Wallet Options",
                                                "Wallet Item | Enabled",
                                                true,
                                                "Enables/Disables the Wallet from showing up in shop.");
            ConfigWalletMode = configFile.Bind("Wallet Options",
                                                "Wallet | Mode",
                                                true,
                                                "true for old system (item mode), false for newer system (non-held mode).");
            ConfigWalletCost = configFile.Bind("Wallet Options",
                                                "Wallet Item | Cost",
                                                250,
                                                "Cost of Wallet");
            #endregion
            #region Money
            ConfigMoneyEnabled = configFile.Bind("Money Options",
                                                "Money | Enabled",
                                                true,
                                                "Enables/Disables the Money from spawning.");
            ConfigMoneyAbundance = configFile.Bind("Money Options",
                                                "Money Scrap | Abundance",
                                                10,
                                                "Overall Abundance of Money in the level.");
            ConfigAverageCoinValue = configFile.Bind("Money Options",
                                                "Money Scrap | Average Value",
                                                15,
                                                "Average value of Money in the level. (so 5 and 25 are lower and upper limits here).");
            #endregion
            #region SnowGlobe
            ConfigSnowGlobeEnabled = configFile.Bind("SnowGlobe Options",
                                                "Snow Globe | Enabled",
                                                true,
                                                "Enables/Disables the Snow Globe from spawning.");
            ConfigSnowGlobeSpawnWeights = configFile.Bind("SnowGlobe Options",
                                                "Snow Globe | Spawn Weights",
                                                "Modded:50,Vanilla:50",
                                                "Spawn Weight of the Snow Globe in moons.");
            ConfigSnowGlobeMusic = configFile.Bind("SnowGlobe Options",
                                                "Snow Globe | Music",
                                                true,
                                                "Enables/Disables the music in the snow globe.");
            #endregion
            #region ItemCrate
            ConfigItemCrateEnabled = configFile.Bind("Crate Options",
                                                "Item Crate | Enabled",
                                                true,
                                                "Enables/Disables the Item Crate from spawning.");
            ConfigCrateAbundance = configFile.Bind("Crate Options",
                                                "Crate | Abundance",
                                                3,
                                                "Abundance of crates that spawn outside (between 0 and your number).");
            #endregion
			configFile.SaveOnConfigSet = true;
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