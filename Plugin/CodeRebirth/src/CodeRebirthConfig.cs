using BepInEx.Configuration;

namespace CodeRebirth.src;
public class CodeRebirthConfig
{
    #region Enables/Disables
    public ConfigEntry<bool> ConfigOxydeEnabledFromStart { get; private set; }
    public ConfigEntry<bool> ConfigOxydeEnabled { get; private set; }
    public ConfigEntry<bool> ConfigDisableTrashCans { get; private set; }
    public ConfigEntry<bool> ConfigRemoveInteriorFog { get; private set; }
    public ConfigEntry<bool> ConfigDontTargetFarEnemies { get; private set; }
    public ConfigEntry<bool> ConfigHazardsDeleteBodies { get; private set; }
    public ConfigEntry<bool> ConfigOnlyOwnerDisablesGal { get; private set; }
    public ConfigEntry<bool> ConfigFloraEnabled { get; set; }
    public ConfigEntry<bool> ConfigRedwoodHeartEnabled { get; private set; }
    public ConfigEntry<bool> ConfigSnowGlobeMusic { get; private set; }
    #endregion
    #region Spawn Weights
    #endregion
    #region Enemy Specific
    public ConfigEntry<float> ConfigRedwoodNormalVolume { get; private set; }
    public ConfigEntry<float> ConfigRedwoodInShipVolume { get; private set; }
    public ConfigEntry<float> ConfigRedwoodSpeed { get; private set; }
    public ConfigEntry<float> ConfigRedwoodShipPadding { get; private set; }
    public ConfigEntry<float> ConfigRedwoodEyesight { get; private set; }
    public ConfigEntry<bool> ConfigRedwoodCanEatOldBirds { get; private set; }
    #endregion
    #region Weather Specific
    public ConfigEntry<float> ConfigMeteorShowerTimeToLeave { get; private set; }
    public ConfigEntry<float> ConfigTornadoInsideBeforeThrow { get; private set; }
    public ConfigEntry<float> ConfigTornadoPullStrength { get; private set; }
    public ConfigEntry<float> ConfigTornadoSpeed { get; private set; }
    public ConfigEntry<float> ConfigMeteorSpeed { get; private set; }
    public ConfigEntry<int> ConfigMinMeteorSpawnCount { get; private set; }
    public ConfigEntry<int> ConfigMaxMeteorSpawnCount { get; private set; }
    public ConfigEntry<float> ConfigTornadoInShipVolume { get; private set; }
    public ConfigEntry<float> ConfigTornadoDefaultVolume { get; private set; }
    public ConfigEntry<float> ConfigMeteorShowerMeteoriteSpawnChance { get; private set; }
    public ConfigEntry<float> ConfigMeteorShowerInShipVolume { get; private set; }
    public ConfigEntry<bool> ConfigMeteorHitShip { get; private set; }
    public ConfigEntry<float> ConfigMeteorsDefaultVolume { get; private set; }
    #endregion
    #region Misc
    public ConfigEntry<float> ConfigTerminalScanFrequency { get; private set; }
    public ConfigEntry<float> ConfigTerminalScanRange { get; private set; }
    public ConfigEntry<float> ConfigSeamineScanFrequency { get; private set; }
    public ConfigEntry<float> ConfigSeamineScanRange { get; private set; }
    public ConfigEntry<bool> ConfigBearTrapsPopTires { get; private set; }
    public ConfigEntry<bool> ConfigOnlyOwnerSeesScanEffects { get; private set; }
    public ConfigEntry<int> ConfigSeamineTinkCharges { get; private set; }
    public ConfigEntry<bool> ConfigOnlyOwnerSeesScanEffectsTerminalGal { get; private set; }
    public ConfigEntry<float> ConfigTerminalBotFlyingVolume { get; private set; }
    public ConfigEntry<bool> ConfigCruiserGalAutomatic { get; private set; }
    public ConfigEntry<bool> ConfigTerminalBotAutomatic { get; private set; }
    public ConfigEntry<int> ConfigWoodenSeedTreeSpawnChance { get; private set; }
    public ConfigEntry<bool> ConfigWoodenCrateIsWhitelist { get; private set; }
    public ConfigEntry<bool> ConfigShockwaveBotAutomatic { get; private set; }
    public ConfigEntry<float> ConfigShockwaveBotPropellerVolume { get; private set; }
    public ConfigEntry<bool> ConfigShockwaveHoldsFourItems { get; private set; }
    public ConfigEntry<int> ConfigShockwaveCharges { get; private set; }
    public ConfigEntry<int> ConfigWoodenCrateHealth { get; private set; }
    public ConfigEntry<float> ConfigMetalHoldTimer { get; private set; }
    public ConfigEntry<int> ConfigCrateNumberToSpawn { get; private set; }
    public ConfigEntry<string> ConfigWoodenCratesBlacklist { get; private set; }
    public ConfigEntry<string> ConfigMetalCratesBlacklist { get; private set; }
    public ConfigEntry<bool> ConfigShovelCratesOnly { get; private set; }
    public ConfigEntry<bool> ConfigAllowPowerLevelChangesFromWeather { get; private set; }
    public ConfigEntry<bool> ConfigExtendedLogging { get; set; }
    public ConfigEntry<string> ConfigFloraGrassCurveSpawnWeight { get; set; }
    public ConfigEntry<string> ConfigFloraDesertCurveSpawnWeight { get; set; }
    public ConfigEntry<string> ConfigFloraSnowCurveSpawnWeight { get; set; }
    public ConfigEntry<float> ConfigSeamineTinkRidingBruceVolume { get; private set; }
    public ConfigEntry<bool> ConfigSeamineTinkAutomatic { get; private set; }
    public ConfigEntry<float> ConfigMicrowaveVolume { get; private set; }
    public ConfigEntry<float> ConfigBearTrapVolume { get; private set; }
    public ConfigEntry<float> ConfigLaserTurretVolume { get; private set; }
    public ConfigEntry<float> ConfigFlashTurretVolume { get; private set; }
    public ConfigEntry<float> ConfigBugZapperVolume { get; private set; }
    public ConfigEntry<float> ConfigACUVolume { get; private set; }
    public ConfigEntry<float> Config999GalHealCooldown { get; private set; }
    public ConfigEntry<int> Config999GalHealAmount { get; private set; }
    public ConfigEntry<float> Config999GalHealSpeed { get; private set; }
    public ConfigEntry<bool> Config999GalHealOnlyInteractedPlayer { get; private set; }
    public ConfigEntry<bool> Config999GalReviveNearbyDeadPlayers { get; private set; }
    public ConfigEntry<int> Config999GalHealTotalAmount { get; private set; }
    public ConfigEntry<int> Config999GalReviveCharges { get; private set; }
    public ConfigEntry<bool> Config999GalCompanyMoonRecharge { get; private set; }
    public ConfigEntry<float> Config999GalFailureChance { get; private set; }
    public ConfigEntry<bool> Config999GalScaleHealAndReviveWithPlayerCount { get; private set; }
    public ConfigEntry<float> ConfigAirControlUnitKnockbackPower { get; private set; }
    public ConfigEntry<int> ConfigAirControlUnitDamage { get; private set; }
    public ConfigEntry<bool> ConfigCleanUnusedConfigs { get; private set; }
    #endregion 
    #region Worth
    #endregion
    public ConfigEntry<bool> ConfigDebugMode { get; private set; }

    public void InitMainCodeRebirthConfig(ConfigFile configFile)
    {
        #region Debug
        ConfigExtendedLogging = configFile.Bind("Debug Options",
                                            "Debug Mode | Extended Logging",
                                            false,
                                            "Whether ExtendedLogging is enabled.");
        ConfigDebugMode = configFile.Bind("Debug Options",
                                            "Debug Mode | Hazard Spawning Enabled",
                                            false,
                                            "Whether debug mode is enabled (for hazard spawning stuff).");
        ConfigRemoveInteriorFog = configFile.Bind("Debug Options",
                                            "Debug Mode | Remove Interior Fog",
                                            false,
                                            "Whether zeekerss' horrible interior fog is removed.");
        #endregion
        #region Oxyde
        ConfigOxydeEnabledFromStart = configFile.Bind("Oxyde Options",
                                            "Oxyde | Enabled From Start",
                                            true,
                                            "Whether Oxyde is enabled from the very start.");
        ConfigOxydeEnabled = configFile.Bind("Oxyde Options",
                                            "Oxyde | Enabled",
                                            true,
                                            "Whether Oxyde is enabled, keep in mind enabling this option enables the following parts of this mod automatically.\nThis includes but is not limited to the following: Janitor, Transporter, All the hazards, Wallet+Coins, Merchant.");
        #endregion
        #region General
        ConfigAllowPowerLevelChangesFromWeather = configFile.Bind("General",
                                            "Allow Power Level Changes From Weather",
                                            true,
                                            "Whether power level changes from CodeRebirth weathers are allowed.");
        ConfigOnlyOwnerDisablesGal = configFile.Bind("General",
                                            "Gal AI | Owner Power",
                                            false,
                                            "Whether only the current owner of the gal can disable her.");
        ConfigDontTargetFarEnemies = configFile.Bind("General",
                                            "Gal AI | Dont Stray Too Far",
                                            false,
                                            "Whether the Gal AI should stop targetting enemies when she is far from her owner's position.");
        ConfigHazardsDeleteBodies = configFile.Bind("General",
                                            "Hazards | Delete Bodies",
                                            true,
                                            "Whether hazards like IndustrialFan and LaserTurret should delete player bodies.");
        ConfigCleanUnusedConfigs = configFile.Bind("General",
                                            "Clean Unusued Configs",
                                            true,
                                            "Whether CodeRebirth should delete old confing information that are unused.");
        #endregion
        #region Flora
        ConfigFloraGrassCurveSpawnWeight = configFile.Bind("Flora Options",
                                            "Flora | Grass CurveSpawnWeight",
                                            "Vanilla - 0.00,30.00 ; 1.00,60.00 | Custom - 0.00,30.00 ; 1.00,60.00 | Oxyde - 0.00,0.00 ; 1.00,0.00",
                                            "MoonName - CurveSpawnWeight for Grass flora (moon tags also work).");
        ConfigFloraDesertCurveSpawnWeight = configFile.Bind("Flora Options",
                                            "Flora | Desert CurveSpawnWeight",
                                            "Vanilla - 0.00,30.00 ; 1.00,60.00 | Custom - 0.00,30.00 ; 1.00,60.00 | Oxyde - 0.00,0.00 ; 1.00,0.00",
                                            "MoonName - CurveSpawnWeight for Desert flora (moon tags also work).");
        ConfigFloraSnowCurveSpawnWeight = configFile.Bind("Flora Options",
                                            "Flora | Snow CurveSpawnWeight",
                                            "Vanilla - 0.00,30.00 ; 1.00,60.00 | Custom - 0.00,30.00 ; 1.00,60.00 | Oxyde - 0.00,0.00 ; 1.00,0.00",
                                            "MoonName - CurveSpawnWeight for Snowy flora (moon tags also work).");
        #endregion
    }
    
    public void InitCodeRebirthConfig(ConfigFile configFile)
    {
        #region Janitor
        ConfigDisableTrashCans = configFile.Bind("Janitor Options",
                                            "Janitor | Disable Trash Cans",
                                            false,
                                            "Whether trash cans are disabled (this is only visually, trash cans still exist).");
        #endregion
        #region Functional Microwave
        ConfigMicrowaveVolume = configFile.Bind("FunctionalMicrowave Options",
                                            "Functional Microwave | Volume",
                                            1f,
                                            "The volume of the Functional Microwave.");
        #endregion
        #region Bear Trap
        ConfigBearTrapsPopTires = configFile.Bind("BearTrap Options",
                                            "Bear Trap | Pop Tires",
                                            true,
                                            "Whether the bear trap can pop tires.");
        ConfigBearTrapVolume = configFile.Bind("BearTrap Options",
                                            "Bear Trap | Volume",
                                            1f,
                                            "The volume of the Bear Trap.");
        #endregion
        #region Laser Turret
        ConfigLaserTurretVolume = configFile.Bind("LaserTurret Options",
                                            "Laser Turret | Volume",
                                            1f,
                                            "The volume of the Laser Turret.");
        #endregion
        #region Flash Turret
        ConfigFlashTurretVolume = configFile.Bind("FlashTurret Options",
                                            "Flash Turret | Volume",
                                            1f,
                                            "The volume of the Flash Turret.");
        #endregion
        #region Bug Zapper
        ConfigBugZapperVolume = configFile.Bind("BugZapper Options",
                                            "Bug Zapper | Volume",
                                            1f,
                                            "The volume of the Bug Zapper.");

        #endregion
        #region Air Control Unit
        ConfigAirControlUnitDamage = configFile.Bind("AirControlUnit Options",
                                            "Air Control Unit | Damage",
                                            15,
                                            "Damage that the ACUnit deals to a player on hit");
        ConfigAirControlUnitKnockbackPower = configFile.Bind("AirControlUnit Options",
                                            "Air Control Unit | Knockback Power",
                                            250f,
                                            "The knockback power of the ACUnit.");
        ConfigACUVolume = configFile.Bind("AirControlUnit Options",
                                            "Air Control Unit | Volume",
                                            1f,
                                            "The volume of the Air Control Unit.");
        #endregion
        #region Cruiser Gal
        ConfigCruiserGalAutomatic = configFile.Bind("Cruiser Options",
                                            "Cruiser Gal | Automatic Behaviour",
                                            false,
                                            "Whether the Cruiser Gal will automatically wake up and choose the nearest player as the owner.");
        #endregion
        #region Shockwave Gal
        ConfigShockwaveCharges = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Charges",
                                            10,
                                            "How many charges the Shockwave Gal has.");
        ConfigShockwaveHoldsFourItems = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Holds Four Items",
                                            false,
                                            "Whether the Shockwave Gal holds four items regardless of singleplayer or multiplayer.");
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
        ConfigSeamineTinkCharges = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Charges",
                                            3,
                                            "How many charges the Seamine Gal has.");
        ConfigSeamineTinkRidingBruceVolume = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Riding Bruce Volume",
                                            0.75f,
                                            new ConfigDescription(
                                                "Volume of the Seamine Gal's Riding Bruce.",
                                                new AcceptableValueRange<float>(0, 1f)
                                            ));
        ConfigSeamineTinkAutomatic = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Automatic Behaviour",
                                            false,
                                            "Whether the Seamine Gal will automatically wake up and choose the nearest player as the owner.");
        ConfigOnlyOwnerSeesScanEffects = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Only Owner Sees Scan Effects",
                                            false,
                                            "Whether only the owner of the Seamine Gal can see the scan effects.");
        ConfigSeamineScanRange = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Scan Range",
                                            50f,
                                            "Range of the Seamine Gal's scan.");
        ConfigSeamineScanFrequency = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Scan Frequency",
                                            17.5f,
                                            "The average Frequency time of the Seamine Gal's scan in seconds.");
        #endregion
        #region Terminal Gal
        ConfigTerminalBotAutomatic = configFile.Bind("Terminal Options",
                                            "Terminal Gal | Automatic Behaviour",
                                            false,
                                            "Whether the Terminal Gal will automatically wake up and choose the nearest player as the owner.");
        ConfigOnlyOwnerSeesScanEffectsTerminalGal = configFile.Bind("Terminal Options",
                                            "Terminal Gal | Only Owner Sees Scan Effects",
                                            false,
                                            "Whether only the owner of the Terminal Gal can see the scan effects.");
        ConfigTerminalScanRange = configFile.Bind("Terminal Options",
                                            "Terminal Gal | Scan Range",
                                            50f,
                                            "Range of the Terminal Gal's scan.");
        ConfigTerminalScanFrequency = configFile.Bind("Terminal Options",
                                            "Terminal Gal | Scan Frequency",
                                            17.5f,
                                            "The average Frequency time of the Terminal Gal's scan in seconds.");
        ConfigTerminalBotFlyingVolume = configFile.Bind("Terminal Options",
                                            "Terminal Gal | Flying Volume",
                                            0.75f,
                                            "Volume of the Terminal Gal's Flying animation.");
        #endregion
        #region SCP 999 Gal
        Config999GalHealCooldown = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Heal Cooldown",
                                            10f,
                                            "Cooldown between heals by interacting on the gal.");
        Config999GalHealAmount = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Heal Amount",
                                            100,
                                            "Amount healed by interacting on the gal.");
        Config999GalHealSpeed = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Heal Speed",
                                            10f,
                                            "Speed of healing by interacting on the gal (amount of time in seconds it for the gal to finish healing).");
        Config999GalHealTotalAmount = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Healing Capacity",
                                            200,
                                            "How much healing the SCP 999 Gal has per orbit.");
        Config999GalReviveCharges = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Revive Charges",
                                            2,
                                            "How many revive charges the SCP 999 Gal has per orbit.");
        Config999GalCompanyMoonRecharge = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Company Moon Recharge",
                                            true,
                                            "Whether the SCP 999 Gal recharges once per visiting company moon on last day.");
        Config999GalFailureChance = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Failure Chance",
                                            15f,
                                            "Failure chance of the SCP 999 Gal.");
        Config999GalHealOnlyInteractedPlayer = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Heal Only Interacted Player",
                                            false,
                                            "Whether the gal can heal only the player that interacts with her.");
        Config999GalReviveNearbyDeadPlayers = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Revive Nearby Dead Players",
                                            true,
                                            "Whether the gal can revive nearby dead bodies.");
        Config999GalScaleHealAndReviveWithPlayerCount = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Scale Heal and Revive with Player Count",
                                            true,
                                            "Whether the gal scales the heals and revives with player count.");
        #endregion
        #region Farming
        ConfigWoodenSeedTreeSpawnChance = configFile.Bind("Farming Options",
                                            "Farming | Wooden Seed Tree Spawn Chance",
                                            2,
                                            "Chance of the wooden seed to spawn from a broken tree");
        #endregion
        #region Tornado
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
        #endregion
        #region Redwood
        ConfigRedwoodCanEatOldBirds = configFile.Bind("Redwood Options",
                                            "Redwood | Can Eat Old Birds",
                                            true,
                                            "Whether redwood can eat old birds.");
        ConfigRedwoodShipPadding = configFile.Bind("Redwood Options",
                                            "Redwood | Ship Padding",
                                            10f,
                                            new ConfigDescription(
                                                "How far away the redwood usually stays from the ship.",
                                                new AcceptableValueRange<float>(0, 999f)
                                            ));
        ConfigRedwoodSpeed = configFile.Bind("Redwood Options",
                                            "Redwood | Speed",
                                            5f,
                                            new ConfigDescription(
                                                "Redwood speed.",
                                                new AcceptableValueRange<float>(0, 999f)
                                            ));
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
                                            0.85f,
                                            new ConfigDescription(
                                                "Normalised time it takes for the meteor shower to leave the moon, 1 being at 12PM~.",
                                                new AcceptableValueRange<float>(0, 1f)
                                            ));
        ConfigMeteorHitShip = configFile.Bind("MeteorShower Options",
                                            "MeteorShower | Meteor Strikes Ship",
                                            false,
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
                                            0.4f,
                                            new ConfigDescription(
                                                "Default Volume of Meteors (between 0 and 1).",
                                                new AcceptableValueRange<float>(0, 1f)
                                            ));
        ConfigMeteorShowerInShipVolume = configFile.Bind("MeteorShower Options",
                                            "MeteorShower | Meteor Volume",
                                            0.3f,
                                            new ConfigDescription(
                                                "Multiplier of the meteors volume for when the player is in the ship and the ship door is closed.",
                                                new AcceptableValueRange<float>(0, 1f)
                                            ));
        #endregion
        #region SnowGlobe
        ConfigSnowGlobeMusic = configFile.Bind("SnowGlobe Options",
                                            "Snow Globe | Music",
                                            true,
                                            "Enables/Disables the music in the snow globe.");
        #endregion
        #region ItemCrate
        ConfigCrateNumberToSpawn = configFile.Bind("Crate Options",
                                            "Crate | Scrap Number To Spawn",
                                            3,
                                            "Number of items that spawn inside a crate (between 0 and your number).");
        ConfigWoodenCrateHealth = configFile.Bind("Crate Options",
                                            "Crate | Wooden Crate Health",
                                            4,
                                            "Hits to open Wooden crate");
        ConfigMetalHoldTimer = configFile.Bind("Crate Options",
                                            "Crate | Metal Hold Timer",
                                            15f,
                                            "Timer to open Metal crate");
        ConfigWoodenCratesBlacklist = configFile.Bind("Crate Options",
                                            "Crate | Wooden Blacklist",
                                            "",
                                            "Blacklist of Items that can spawn from wooden crates (comma separated, recommend leaving empty, CAN become a whitelist).");
        ConfigWoodenCrateIsWhitelist = configFile.Bind("Crate Options",
                                            "Crate | Wooden Crate Is Whitelist",
                                            false,
                                            "If true, Wooden Crates will spawn using a whitelist which CAN include both shop and non shop items, if false, they will spawn from shop items pool with the blacklist.");
        ConfigMetalCratesBlacklist = configFile.Bind("Crate Options",
                                            "Crate | Metal Blacklist",
                                            "",
                                            "Blacklist of Items that can spawn from metal crates (comma separated, recommend leaving empty).");
        ConfigShovelCratesOnly = configFile.Bind("Crate Options",
                                            "Crate | Shovel Crates Only",
                                            true,
                                            "Only Shovels can hit Crates.");
        #endregion
    }
}