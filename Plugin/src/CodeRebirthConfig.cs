
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace CodeRebirth.src;
public class CodeRebirthConfig
{
    #region Enables/Disables
    public ConfigEntry<bool> ConfigSeamineTinkEnabled { get; private set; }
    public ConfigEntry<bool> ConfigShockwaveBotEnabled { get; private set; }
    public ConfigEntry<bool> ConfigDangerousFloraEnabled { get; private set; }
    public ConfigEntry<bool> ConfigFarmingEnabled { get; private set; }
    public ConfigEntry<bool> ConfigBiomesEnabled { get; private set; }
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
    #endregion
    #region Spawn Weights
    public ConfigEntry<string> ConfigWoodenSeedSpawnWeights { get; private set; }
    public ConfigEntry<float> ConfigBiomesSpawnChance { get; private set; }
    public ConfigEntry<string> ConfigCarnivorousSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigNaturesMaceScrapSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigIcyHammerScrapSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigSpikyMaceScrapSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigRedwoodSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigSnailCatSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigCutieFlySpawnWeights { get; private set; }
    public ConfigEntry<int> ConfigMoneyAbundance { get; private set; }
    public ConfigEntry<string> ConfigEpicAxeScrapSpawnWeights { get; private set; }
    public ConfigEntry<int> ConfigMetalCrateAbundance { get; private set; }
    public ConfigEntry<int> ConfigWoodenCrateAbundance { get; private set; }
    public ConfigEntry<string> ConfigSnowGlobeSpawnWeights { get; private set; }
    #endregion
    #region Enemy Specific
    public ConfigEntry<float> ConfigCarnivorousPowerLevel { get; private set; }
    public ConfigEntry<int> ConfigCarnivorousMaxSpawnCount { get; private set; }
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
    #endregion
    #region Weather Specific
    public ConfigEntry<float> ConfigMeteorShowerTimeToLeave { get; private set; }
    public ConfigEntry<float> ConfigTornadoInsideBeforeThrow { get; private set; }
    public ConfigEntry<float> ConfigTornadoPullStrength { get; private set; }
    public ConfigEntry<bool> ConfigTornadoYeetSFX { get; private set; }
    public ConfigEntry<string> ConfigTornadoCanFlyYouAwayWeatherTypes { get; private set; }
    public ConfigEntry<float> ConfigTornadoSpeed { get; private set; }
    public ConfigEntry<float> ConfigMeteorSpeed { get; private set; }
    public ConfigEntry<int> ConfigMinMeteorSpawnCount { get; private set; }
    public ConfigEntry<int> ConfigMaxMeteorSpawnCount { get; private set; }
    public ConfigEntry<string> ConfigTornadoMoonWeatherTypes { get; private set; }
    public ConfigEntry<float> ConfigTornadoInShipVolume { get; private set; }
    public ConfigEntry<float> ConfigTornadoDefaultVolume { get; private set; }
    public ConfigEntry<float> ConfigMeteorShowerMeteoriteSpawnChance { get; private set; }
    public ConfigEntry<float> ConfigMeteorShowerInShipVolume { get; private set; }
    public ConfigEntry<bool> ConfigMeteorHitShip { get; private set; }
    public ConfigEntry<float> ConfigMeteorsDefaultVolume { get; private set; }
    #endregion
    #region Misc
    public ConfigEntry<bool> ConfigShockwaveBotAutomatic { get; private set; }
    public ConfigEntry<float> ConfigShockwaveBotPropellerVolume { get; private set; }
    public ConfigEntry<bool> ConfigShockwaveHoldsFourItems { get; private set; }
    public ConfigEntry<int> ConfigPlantPotPrice { get; private set; }
    public ConfigEntry<int> ConfigShockwaveCharges { get; private set; }
    public ConfigEntry<string> ConfigShockwaveBotEnemyBlacklist { get; private set; }
    public ConfigEntry<int> ConfigMetalHitNumber { get; private set; }
    public ConfigEntry<float> ConfigWoodenOpenTimer { get; private set; }
    public ConfigEntry<int> ConfigCrateNumberToSpawn { get; private set; }
    public ConfigEntry<string> ConfigWoodenCratesBlacklist { get; private set; }
    public ConfigEntry<string> ConfigMetalCratesBlacklist { get; private set; }
    public ConfigEntry<bool> ConfigShovelCratesOnly { get; private set; }
    public ConfigEntry<int> ConfigSeamineTinkCost { get; private set; }
    public ConfigEntry<int> ConfigShockwaveBotCost { get; private set; }
    public ConfigEntry<bool> ConfigCanBreakTrees { get; private set; }
    public ConfigEntry<bool> ConfigAllowPowerLevelChangesFromWeather { get; private set; }
    public ConfigEntry<bool> ConfigEnableExtendedLogging { get; private set; }
    public ConfigEntry<string> ConfigFloraExcludeSpawnPlaces { get; private set; }
    public ConfigEntry<int> ConfigFloraMaxAbundance { get; private set; }
    public ConfigEntry<int> ConfigFloraMinAbundance { get; private set; }
    public ConfigEntry<string> ConfigFloraGrassSpawnPlaces { get; private set; }
    public ConfigEntry<string> ConfigFloraDesertSpawnPlaces { get; private set; }
    public ConfigEntry<string> ConfigFloraSnowSpawnPlaces { get; private set; }
    public ConfigEntry<float> ConfigCritChance { get; private set; }
    public ConfigEntry<bool> ConfigWalletMode { get; private set; }
    public ConfigEntry<int> ConfigHoverboardCost { get; private set; }
    public ConfigEntry<int> ConfigWalletCost { get; private set; }
    public ConfigEntry<int> ConfigMinCoinValue { get; private set; }
    public ConfigEntry<int> ConfigMaxCoinValue { get; private set; }
    #endregion 
    #region Worth
    public ConfigEntry<string> ConfigTomatoValue { get; private set; }
    public ConfigEntry<string> ConfigGoldenTomatoValue { get; private set; }
    public ConfigEntry<string> ConfigNaturesMaceWorth { get; private set; }
    public ConfigEntry<string> ConfigIcyHammerWorth { get; private set; }
    public ConfigEntry<string> ConfigSpikyMaceWorth { get; private set; }
    public ConfigEntry<string> ConfigEpicAxeWorth { get; private set; }
    public ConfigEntry<string> ConfigSnowGlobeWorth { get; private set; }
    public ConfigEntry<string> ConfigSapphireWorth { get; private set; }
    public ConfigEntry<string> ConfigRubyWorth { get; private set; }
    public ConfigEntry<string> ConfigEmeraldWorth { get; private set; }
    #endregion
    #region Debug
    #endregion
    public CodeRebirthConfig(ConfigFile configFile)
    {
        configFile.SaveOnConfigSet = false;
        #region General
        ConfigEnableExtendedLogging = configFile.Bind("General",
                                            "Enable Extended Logging",
                                            false,
                                            "Whether extended logging is enabled.");
        ConfigAllowPowerLevelChangesFromWeather = configFile.Bind("General",
                                            "Allow Power Level Changes From Weather",
                                            true,
                                            "Whether power level changes from CodeRebirth weathers are allowed.");
        #endregion
        #region Shockwave Gal
        ConfigShockwaveBotEnabled = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Enabled",
                                            true,
                                            "Whether the Shockwave Gal is enabled.");
        ConfigShockwaveBotCost = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Cost",
                                            999,
                                            "Cost of the Shockwave Gal.");
        ConfigShockwaveCharges = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Charges",
                                            10,
                                            "How many charges the Shockwave Gal has.");
        ConfigShockwaveHoldsFourItems = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Holds Four Items",
                                            false,
                                            "Whether the Shockwave Gal holds four items regardless of singleplayer or multiplayer.");
        ConfigShockwaveBotEnemyBlacklist = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Enemy Blacklist",
                                            "Centipede, Red Locust Bees, Docile Locust Bees, Manticoil, CutieFly, SnailCat, Tornado, RadMech, Earth Leviathan, Puffer, Jester, Blob, Girl, Spring, Clay Surgeon",
                                            "Comma separated list of enemies that the Shockwave Gal will not target (immortal enemies should be counted by default, just not in config).");
        ConfigShockwaveBotPropellerVolume = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Propeller Volume",
                                            0.75f,
                                            new ConfigDescription(
                                                "Volume of the Shockwave Gal's propeller.",
                                                new AcceptableValueRange<float>(0, 1f)
                                            ));
        ConfigShockwaveBotAutomatic = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Automatic Behaviour",
                                            false,
                                            "Whether the Shockwave Gal will automatically wake up and choose the nearest player as the owner.");
        #endregion
        #region Seamine Gal
        /*ConfigSeamineTinkEnabled = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Enabled",
                                            true,
                                            "Whether the Seamine Gal is enabled.");
        ConfigSeamineTinkCost = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Cost",
                                            999,
                                            "Cost of the Seamine Tink.");*/
        #endregion
        #region Biomes
        ConfigBiomesEnabled = configFile.Bind("Biome Options",
                                            "Biomes | Enabled",
                                            false,
                                            "Whether Biomes are enabled.");
        ConfigBiomesSpawnChance = configFile.Bind("Biome Options",
                                            "Biomes | Spawn Chance",
                                            0.5f,
                                            "Biomes spawn chance.");
        #endregion
        #region DangerousFlora
        ConfigDangerousFloraEnabled = configFile.Bind("Flora Options",
                                            "Dangerous Flora | Enabled",
                                            true,
                                            "Whether dangerous Flora is enabled.");
        ConfigCarnivorousSpawnWeights = configFile.Bind("Flora Options",
                                            "Dangerous Flora | Carnivorous Spawn Weights",
                                            "Custom:20,Vanilla:20",
                                            "Carnivorous Plant spawn weights e.g. `Custom:20,Vanilla:20`.");
        ConfigCarnivorousPowerLevel = configFile.Bind("Flora Options",
                                            "Dangerous Flora | Carnivorous Power Levels",
                                            0.5f,
                                            "Carnivorous Plant power level.");
        ConfigCarnivorousMaxSpawnCount = configFile.Bind("Flora Options",
                                            "Dangerous Flora | Carnivorous Max Count",
                                            6,
                                            "Carnivorous Plant max count.");
        #endregion
        #region Flora
        ConfigFloraEnabled = configFile.Bind("Flora Options",
                                            "Flora | Enabled",
                                            true,
                                            "Whether Flora is enabled.");
        ConfigFloraMaxAbundance = configFile.Bind("Flora Options",
                                            "Flora | Max Abundance",
                                            60,
                                            "How many plants can get added at most.");
        ConfigFloraMinAbundance = configFile.Bind("Flora Options",
                                            "Flora | Min Abundance",
                                            30,
                                            "How many plants can get added at least.");
        ConfigFloraGrassSpawnPlaces = configFile.Bind("Flora Options",
                                            "Flora | Grass Spawn Places",
                                            "Vow,Adamance,March,Custom,",
                                            "Flora spawn places e.g. `Custom,Vanilla,Experimentation,Assurance,Gloom`.");
        ConfigFloraDesertSpawnPlaces = configFile.Bind("Flora Options",
                                            "Flora | Desert Spawn Places",
                                            "Assurance,Offense,Custom,",
                                            "Flora spawn places e.g. `Custom,Vanilla,Experimentation,Assurance,Gloom`.");
        ConfigFloraSnowSpawnPlaces = configFile.Bind("Flora Options",
                                            "Flora | Snow Spawn Places",
                                            "Dine,Rend,Titan,Custom,",
                                            "Flora spawn places e.g. `Custom,Vanilla,Experimentation,Assurance,Gloom`.");
        ConfigFloraExcludeSpawnPlaces = configFile.Bind("Flora Options",
                                            "Flora | Exclude Spawn Places",
                                            "Infernis",
                                            "Flora EXLUDE spawn places e.g. `Experimentation,Assurance,Gloom` (only takes moon names).");
        #endregion
        #region Farming
        ConfigFarmingEnabled = configFile.Bind("Farming Options",
                                            "Farming | Enabled",
                                            true,
                                            "Whether Farming is enabled.");
        ConfigPlantPotPrice = configFile.Bind("Farming Options",
                                            "Farming | Plant Pot Price",
                                            696,
                                            "Price of the Plant Pot.");
        ConfigWoodenSeedSpawnWeights = configFile.Bind("Farming Options",
                                            "Farming | Wooden Seed Spawn Weights",
                                            "",
                                            "Weights of the Wooden Seed spawn moons e.g. `Custom:10,Vanilla:10,Experimentation:50,Assurance:30,Gloom:20` (recommended empty).");
        ConfigTomatoValue = configFile.Bind("Farming Options",
                                            "Farming | Tomato Value",
                                            "-1,-1",
                                            "Min,Max value of the Tomato, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        ConfigGoldenTomatoValue = configFile.Bind("Farming Options",
                                            "Farming | Golden Tomato Value",
                                            "-1,-1",
                                            "Min,Max value of the Golden Tomato, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        #endregion
        #region Tornado
        ConfigTornadosEnabled = configFile.Bind("Tornado Options",
                                            "Tornados | Enabled",
                                            true,
                                            "Enables/Disables the Tornados from popping up into moons.");
        ConfigTornadoMoonWeatherTypes = configFile.Bind("Tornado Options",
                                            "Tornados | Moon Types",
                                            "Smoky: All, | Fire: All, | Water: All, | Electric: All, | Windy: All, | Blood: All, |",
                                            "Moons that the fire type can pop into (All, or specify per moon type like so: Experimentation,Artifice,etc).");
        ConfigTornadoCanFlyYouAwayWeatherTypes = configFile.Bind("Tornado Options",
                                            "Tornado | Can Fly You Away Weather Types",
                                            "All",
                                            "Tornado weather types that can fly you away (All, or specify per weather type like so: Fire,Electric,etc except water!!).");
        ConfigTornadoSpeed = configFile.Bind("Tornado Options",
                                            "Tornados | Speed",
                                            7f,
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
        ConfigTornadoInsideBeforeThrow = configFile.Bind("Tornado Options",
                                            "Tornados | Inside Before Throw",
                                            10f,
                                            new ConfigDescription(
                                                "Timer of being inside tornado before you get flung the hell out (50 if you never wanna be thrown).",
                                                new AcceptableValueRange<float>(1f, 50f)
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
        ConfigCanBreakTrees = configFile.Bind("Weapon Options",
                                            "Weapons | Can Break Trees",
                                            true,
                                            "Enables/Disables breaking trees in the game for code rebirth weapons.");
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
        ConfigNaturesMaceWorth = configFile.Bind("NatureMace Options",
                                            "Natures Mace | Worth",
                                            "-1,-1",
                                            "Min and Max value of the NaturesMace, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        ConfigNaturesMaceScrapSpawnWeights = configFile.Bind("NatureMace Options",
                                            "Natures Mace | Scrap Spawn Weights",
                                            "Custom:15,Vanilla:15",
                                            "Natures Mace scrap spawn weights.");
        ConfigIcyHammerScrapEnabled = configFile.Bind("Icy Hammer Options",
                                            "Icy Hammer | Scrap Enabled",
                                            true,
                                            "Whether Icy Hammer scrap is enabled.");
        ConfigIcyHammerWorth = configFile.Bind("Icy Hammer Options",
                                            "Icy Hammer | Worth",
                                            "-1,-1",
                                            "Min and Max value of the IcyHammer, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        ConfigIcyHammerScrapSpawnWeights = configFile.Bind("Icy Hammer Options",
                                            "Icy Hammer | Scrap Spawn Weights",
                                            "Custom:15,Vanilla:15",
                                            "Icy Hammer scrap spawn weights.");
        ConfigSpikyMaceScrapEnabled = configFile.Bind("Spiky Mace Options",
                                            "Spiky Mace | Scrap Enabled",
                                            true,
                                            "Whether Spiky Mace scrap is enabled.");
        ConfigSpikyMaceWorth = configFile.Bind("Spiky Mace Options",
                                            "Spiky Mace | Worth",
                                            "-1,-1",
                                            "Min and Max value of the SpikyMace, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        ConfigSpikyMaceScrapSpawnWeights = configFile.Bind("Spiky Mace Options",
                                            "Spiky Mace | Scrap Spawn Weights",
                                            "Custom:15,Vanilla:15",
                                            "Spiky Mace scrap spawn weights.");
        ConfigEpicAxeScrapEnabled = configFile.Bind("EpicAxe Options",
                                            "Epic Axe Scrap | Enabled",
                                            true,
                                            "Enables/Disables the Epic Axe from showing up in the Factory.");
        ConfigEpicAxeWorth = configFile.Bind("EpicAxe Options",
                                            "Epic Axe Scrap | Worth",
                                            "-1,-1",
                                            "Min and Max value of the EpicAxe, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        ConfigEpicAxeScrapSpawnWeights = configFile.Bind("EpicAxe Options",
                                            "Epic Axe Scrap | Spawn Weights",
                                            "Custom:15,Vanilla:15",
                                            "Spawn Weight of the epic axe in moons.");
        #endregion
        #region Redwood
        ConfigRedwoodCanEatOldBirds = configFile.Bind("Redwood Options",
                                            "Redwood | Can Eat Old Birds",
                                            true,
                                            "Whether redwood can eat old birds.");
        ConfigRedwoodMaxSpawnCount = configFile.Bind("Redwood Options",
                                            "Redwood | Max Spawn Count",
                                            1,
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
                                            10f,
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
                                            ));
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
                                                new AcceptableValueRange<int>(0, 999)
                                            ));
        ConfigMinMeteorSpawnCount = configFile.Bind("MeteorShower Options",
                                            "Meteors | Min Spawn Count",
                                            1,
                                            new ConfigDescription(
                                                "Minimum number of meteors to spawn at once every spawn cycle.",
                                                new AcceptableValueRange<int>(0, 999)
                                            ));
        ConfigMeteorSpeed = configFile.Bind("MeteorShower Options",
                                            "Meteors | Speed",
                                            50f,
                                            new ConfigDescription(
                                                "Speed of meteors.",
                                                new AcceptableValueRange<float>(0, 1000f)
                                            ));
        ConfigMeteorShowerTimeToLeave = configFile.Bind("MeteorShower Options",
                                            "MeteorShower | Time To Leave",
                                            1f,
                                            new ConfigDescription(
                                                "Normalised time it takes for the meteor shower to leave the moon, 1 being at 12PM~.",
                                                new AcceptableValueRange<float>(0, 1f)
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
        ConfigMeteorsDefaultVolume = configFile.Bind("MeteorShower Options",
                                            "Meteors | Default Volume",
                                            0.2f,
                                            new ConfigDescription(
                                                "Default Volume of Meteors (between 0 and 1).",
                                                new AcceptableValueRange<float>(0, 1f)
                                            ));
        ConfigMeteorShowerInShipVolume = configFile.Bind("MeteorShower Options",
                                            "MeteorShower | Meteor Volume",
                                            0.5f,
                                            new ConfigDescription(
                                                "Multiplier of the meteors volume for when the player is in the ship and the ship door is closed.", 
                                                new AcceptableValueRange<float>(0, 1f)
                                            ));
        ConfigSapphireWorth = configFile.Bind("MeteorShower Options",
                                            "MeteorShower | Sapphire Worth",
                                            "-1,-1",
                                            "Min and Max value of the Sapphire, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        ConfigRubyWorth = configFile.Bind("MeteorShower Options",
                                            "MeteorShower | Ruby Worth",
                                            "-1,-1",
                                            "Min and Max value of the Ruby, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        ConfigEmeraldWorth = configFile.Bind("MeteorShower Options",
                                            "MeteorShower | Emerald Worth",
                                            "-1,-1",
                                            "Min and Max value of the Emerald, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        ConfigWesleyModeEnabled = configFile.Bind("MeteorShower Options",
                                            "MeteorShower | Wesley Mode",
                                            false,
                                            "Enables/Disables the Wesley Mode (this is a meme, not recommended lol).");
        #endregion
        #region ModCompat
        #endregion
        #region CutieFly
        ConfigCutieFlyEnabled = configFile.Bind("CutieFly Options",
                                            "CutieFly Enemy | Enabled",
                                            true,
                                            "Enables/Disables the CutieFly enemy");
        ConfigCutieFlySpawnWeights = configFile.Bind("CutieFly Options",
                                            "CutieFly Enemy | Spawn Weights",
                                            "Custom:50,Vanilla:50",
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
                                            "Custom:50,Vanilla:50",
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
        ConfigMinCoinValue = configFile.Bind("Money Options",
                                            "Money Scrap | Min Value",
                                            5,
                                            "Min value of Money in the level (can be negative).");
        ConfigMaxCoinValue = configFile.Bind("Money Options",
                                            "Money Scrap | Max Value",
                                            25,
                                            "Max value of Money in the level (has to be higher than min value (or same)).");
        #endregion
        #region SnowGlobe
        ConfigSnowGlobeEnabled = configFile.Bind("SnowGlobe Options",
                                            "Snow Globe | Enabled",
                                            true,
                                            "Enables/Disables the Snow Globe from spawning.");
        ConfigSnowGlobeWorth = configFile.Bind("SnowGlobe Options",
                                            "Snow Globe | Worth",
                                            "-1,-1",
                                            "Min and Max value of the SnowGlobe, leave at -1 for both defaults to not mess with base values, values are NOT multiplied by 0.4.");
        ConfigSnowGlobeSpawnWeights = configFile.Bind("SnowGlobe Options",
                                            "Snow Globe | Spawn Weights",
                                            "Custom:50,Vanilla:50",
                                            "Spawn Weight of the Snow Globe in moons.");
        ConfigSnowGlobeMusic = configFile.Bind("SnowGlobe Options",
                                            "Snow Globe | Music",
                                            true,
                                            "Enables/Disables the music in the snow globe.");
        #endregion
        #region ItemCrate
        ConfigItemCrateEnabled = configFile.Bind("Crate Options",
                                            "Crate | Enabled",
                                            true,
                                            "Enables/Disables the Item Crate from spawning.");
        ConfigMetalCrateAbundance = configFile.Bind("Crate Options",
                                            "Crate | Metal Abundance",
                                            3,
                                            "Abundance of Metal Crates that spawn outside (between 0 and your number).");
        ConfigCrateNumberToSpawn = configFile.Bind("Crate Options",
                                            "Crate | Metal Number To Spawn",
                                            3,
                                            "Number of items that spawn inside a crate (between 0 and your number).");
        ConfigWoodenCrateAbundance = configFile.Bind("Crate Options",
                                            "Crate | Wooden Abundance",
                                            3,
                                            "Abundance of Wooden Crates that spawn outside (between 0 and your number).");
        ConfigMetalHitNumber = configFile.Bind("Crate Options",
                                            "Crate | Metal Hit Number",
                                            4,
                                            "Hits to open metal crate");
        ConfigWoodenOpenTimer = configFile.Bind("Crate Options",
                                            "Crate | Wooden Open Timer",
                                            30f,
                                            "Timer to open wooden crate");
        ConfigWoodenCratesBlacklist = configFile.Bind("Crate Options",
                                            "Crate | Wooden Blacklist",
                                            "",
                                            "Blacklist of Items that can spawn from wooden crates (comma separated, recommend leaving empty).");
        ConfigMetalCratesBlacklist = configFile.Bind("Crate Options",
                                            "Crate | Metal Blacklist",
                                            "",
                                            "Blacklist of Items that can spawn from metal crates (comma separated, recommend leaving empty).");
        ConfigShovelCratesOnly = configFile.Bind("Crate Options",
                                            "Crate | Shovel Crates Only",
                                            true,
                                            "Only Shovels can hit Crates.");
        #endregion
        configFile.SaveOnConfigSet = true;
        ClearUnusedEntries(configFile);
    }

    private void ClearUnusedEntries(ConfigFile configFile)
    {
        // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
        PropertyInfo orphanedEntriesProp = configFile.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
        var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(configFile, null);
        orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
        configFile.Save(); // Save the config file to save these changes
    }
}