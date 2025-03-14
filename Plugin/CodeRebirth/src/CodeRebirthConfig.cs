﻿using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace CodeRebirth.src;
public class CodeRebirthConfig
{
    #region Enables/Disables
    public ConfigEntry<bool> ConfigDriftwoodMenaceEnabled { get; private set; }
    public ConfigEntry<bool> ConfigNancyEnabled { get; private set; }
    public ConfigEntry<bool> ConfigUnlockAllGals { get; private set; }
    public ConfigEntry<bool> ConfigOxydeEnabled { get; private set; }
    public ConfigEntry<bool> ConfigMerchantEnabled { get; private set; }
    public ConfigEntry<bool> ConfigCruiserGalEnabled { get; private set; }
    public ConfigEntry<bool> ConfigCleanerDroneGalEnabled { get; private set; }
    public ConfigEntry<bool> ConfigDisableTrashCans { get; private set; }
    public ConfigEntry<bool> ConfigJanitorEnabled { get; private set; }
    public ConfigEntry<bool> ConfigManorLordEnabled { get; private set; }
    public ConfigEntry<bool> ConfigMistressEnabled { get; private set; }
    public ConfigEntry<bool> ConfigTransporterEnabled { get; private set; }
    public ConfigEntry<bool> ConfigZortModelReplacementEnabled { get; private set; }
    public ConfigEntry<bool> ConfigZortAddonsEnabled { get; private set; }
    public ConfigEntry<bool> ConfigBearTrapGalEnabled { get; private set; }
    public ConfigEntry<bool> ConfigACUnitGalEnabled { get; private set; }
    public ConfigEntry<bool> ConfigSuspiciousActivityEnabled { get; private set; }
    public ConfigEntry<bool> ConfigTerminalBotEnabled { get; private set; }
    public ConfigEntry<bool> ConfigFriendStuffEnabled { get; private set; }
    public ConfigEntry<bool> ConfigShrimpDispenserEnabled { get; private set; }
    public ConfigEntry<bool> Config999GalEnabled { get; private set; }
    public ConfigEntry<bool> ConfigRemoveInteriorFog { get; private set; }
    public ConfigEntry<bool> ConfigDontTargetFarEnemies { get; private set; }
    public ConfigEntry<bool> ConfigHazardsDeleteBodies { get; private set; }
    public ConfigEntry<bool> ConfigOnlyOwnerDisablesGal { get; private set; }
    public ConfigEntry<bool> ConfigShockwaveGalPlayerModelEnabled { get; private set; }
    public ConfigEntry<bool> ConfigSeamineTinkPlayerModelEnabled { get; private set; }
    public ConfigEntry<bool> ConfigFirstLaunchPopup { get; private set; }
    public ConfigEntry<bool> ConfigFunctionalMicrowaveEnabled { get; private set; }
    public ConfigEntry<bool> ConfigInsideBearTrapEnabled { get; private set; }
    public ConfigEntry<bool> ConfigBearTrapEnabled { get; private set; }
    public ConfigEntry<bool> ConfigLaserTurretEnabled { get; private set; }
    public ConfigEntry<bool> ConfigFlashTurretEnabled { get; private set; }
    public ConfigEntry<bool> ConfigIndustrialFanEnabled { get; private set; }
    public ConfigEntry<bool> ConfigTeslaShockEnabled { get; private set; }
    public ConfigEntry<bool> ConfigAirControlUnitEnabled { get; private set; }
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
    public ConfigEntry<bool> ConfigHoverboardEnabled { get; private set; }
    public ConfigEntry<bool> ConfigMeteorShowerEnabled { get; private set; }
    public ConfigEntry<bool> ConfigTornadosEnabled { get; private set; }
    public ConfigEntry<bool> ConfigEpicAxeScrapEnabled { get; private set; }
    public ConfigEntry<bool> ConfigMonarchEnabled { get; private set; }
    public ConfigEntry<bool> ConfigSnailCatEnabled { get; private set; }
    public ConfigEntry<bool> ConfigItemCrateEnabled { get; private set; }
    public ConfigEntry<bool> ConfigSnowGlobeEnabled { get; private set; }
    public ConfigEntry<bool> ConfigNaturesMaceScrapEnabled { get; private set; }
    public ConfigEntry<bool> ConfigIcyHammerScrapEnabled { get; private set; }
    public ConfigEntry<bool> ConfigSpikyMaceScrapEnabled { get; private set; }
    public ConfigEntry<bool> ConfigBellCrabGalEnabled { get; private set; }
    public ConfigEntry<bool> ConfigDuckSongEnabled { get; private set; }
    #endregion
    #region Spawn Weights
    public ConfigEntry<string> ConfigDuckSongSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigWoodenSeedSpawnWeights { get; private set; }
    public ConfigEntry<float> ConfigBiomesSpawnChance { get; private set; }
    public ConfigEntry<string> ConfigCarnivorousSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigNaturesMaceScrapSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigIcyHammerScrapSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigSpikyMaceScrapSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigRedwoodSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigSnailCatSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigCutieFlySpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigEpicAxeScrapSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigBearTrapSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigMetalCrateSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigWoodenCrateSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigLaserTurretCurveSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigSnowGlobeSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigFlashTurretCurveSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigIndustrialFanCurveSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigTeslaShockCurveSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigAirControlUnitSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigFunctionalMicrowaveCurveSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigBearTrapInsideSpawnWeight { get; private set; }
    public ConfigEntry<string> ConfigZortGuitarSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigZortViolinSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigZortRecorderSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigZortAccordionSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigManorLordSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigMistressSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigTransporterSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigJanitorSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigDriftwoodMenaceSpawnWeights { get; private set; }
    public ConfigEntry<string> ConfigNancySpawnWeights { get; private set; }
    #endregion
    #region Enemy Specific
    public ConfigEntry<float> ConfigDriftwoodMenacePowerLevel { get; private set; }
    public ConfigEntry<int> ConfigDriftwoodMenaceMaxSpawnCount { get; private set; }
    public ConfigEntry<float> ConfigNancyPowerLevel { get; private set; }
    public ConfigEntry<int> ConfigNancyMaxSpawnCount { get; private set; }
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
    public ConfigEntry<int> ConfigCutieFlyMaxSpawnCount { get; private set; }
    public ConfigEntry<int> ConfigSnailCatMaxSpawnCount { get; private set; }
    public ConfigEntry<float> ConfigCutieFlyPowerLevel { get; private set; }
    public ConfigEntry<float> ConfigSnailCatPowerLevel { get; private set; }
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
    public ConfigEntry<int> ConfigPiggyBankCost { get; private set; }
    public ConfigEntry<bool> ConfigBearTrapsPopTires { get; private set; }
    public ConfigEntry<bool> ConfigOnlyOwnerSeesScanEffects { get; private set; }
    public ConfigEntry<int> ConfigSeamineTinkCharges { get; private set; }
    public ConfigEntry<bool> ConfigOnlyOwnerSeesScanEffectsTerminalGal { get; private set; }
    public ConfigEntry<float> ConfigTerminalBotFlyingVolume { get; private set; }
    public ConfigEntry<bool> ConfigCruiserGalAutomatic { get; private set; }
    public ConfigEntry<bool> ConfigTerminalBotAutomatic { get; private set; }
    public ConfigEntry<int> ConfigTerminalBotCost { get; private set; }
    public ConfigEntry<int> ConfigWoodenSeedTreeSpawnChance { get; private set; }
    public ConfigEntry<bool> ConfigWoodenCrateIsWhitelist { get; private set; }
    public ConfigEntry<float> ConfigMetalCrateValueMultiplier { get; private set; }
    public ConfigEntry<bool> ConfigShockwaveBotAutomatic { get; private set; }
    public ConfigEntry<float> ConfigShockwaveBotPropellerVolume { get; private set; }
    public ConfigEntry<bool> ConfigShockwaveHoldsFourItems { get; private set; }
    public ConfigEntry<int> ConfigPlantPotPrice { get; private set; }
    public ConfigEntry<int> ConfigShockwaveCharges { get; private set; }
    public ConfigEntry<string> ConfigShockwaveBotEnemyBlacklist { get; private set; }
    public ConfigEntry<int> ConfigWoodenCrateHealth { get; private set; }
    public ConfigEntry<float> ConfigMetalHoldTimer { get; private set; }
    public ConfigEntry<int> ConfigCrateNumberToSpawn { get; private set; }
    public ConfigEntry<string> ConfigWoodenCratesBlacklist { get; private set; }
    public ConfigEntry<string> ConfigMetalCratesBlacklist { get; private set; }
    public ConfigEntry<bool> ConfigShovelCratesOnly { get; private set; }
    public ConfigEntry<int> ConfigSeamineTinkCost { get; private set; }
    public ConfigEntry<int> ConfigShockwaveBotCost { get; private set; }
    public ConfigEntry<int> ConfigGlitchedPlushieCost { get; private set; }
    public ConfigEntry<bool> ConfigCanBreakTrees { get; private set; }
    public ConfigEntry<bool> ConfigAllowPowerLevelChangesFromWeather { get; private set; }
    public ConfigEntry<bool> ConfigExtendedLogging { get; private set; }
    public ConfigEntry<string> ConfigFloraExcludeSpawnPlaces { get; private set; }
    public ConfigEntry<int> ConfigFloraMaxAbundance { get; private set; }
    public ConfigEntry<int> ConfigFloraMinAbundance { get; private set; }
    public ConfigEntry<string> ConfigFloraGrassSpawnPlaces { get; private set; }
    public ConfigEntry<string> ConfigFloraDesertSpawnPlaces { get; private set; }
    public ConfigEntry<string> ConfigFloraSnowSpawnPlaces { get; private set; }
    public ConfigEntry<float> ConfigCritChance { get; private set; }
    public ConfigEntry<int> ConfigHoverboardCost { get; private set; }
    public ConfigEntry<int> ConfigWalletCost { get; private set; }
    public ConfigEntry<string> ConfigSeamineTinkEnemyBlacklist { get; private set; }
    public ConfigEntry<float> ConfigSeamineTinkRidingBruceVolume { get; private set; }
    public ConfigEntry<bool> ConfigSeamineTinkAutomatic { get; private set; }
    public ConfigEntry<float> ConfigMicrowaveVolume { get; private set; }
    public ConfigEntry<float> ConfigBearTrapVolume { get; private set; }
    public ConfigEntry<float> ConfigLaserTurretVolume { get; private set; }
    public ConfigEntry<float> ConfigFlashTurretVolume { get; private set; }
    public ConfigEntry<float> ConfigTeslaShockVolume { get; private set; }
    public ConfigEntry<float> ConfigACUVolume { get; private set; }
    public ConfigEntry<int> ConfigBellCrabGalCost { get; private set; }
    public ConfigEntry<int> Config999GalCost { get; private set; }
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
    public ConfigEntry<int> ConfigShrimpDispenserCost { get; private set; }
    public ConfigEntry<float> ConfigAirControlUnitKnockbackPower { get; private set; }
    public ConfigEntry<int> ConfigAirControlUnitDamage { get; private set; }
    public ConfigEntry<int> ConfigDuckSongPowerLevel { get; private set; }
    public ConfigEntry<int> ConfigDuckSongMaxSpawnCount { get; private set; }
    public ConfigEntry<float> ConfigDuckSongTimer { get; private set; }
    public ConfigEntry<int> ConfigBearTrapGalCost { get; private set; }
    public ConfigEntry<int> ConfigACUnitGalCost { get; private set; }
    public ConfigEntry<int> ConfigManorLordPowerLevel { get; private set; }
    public ConfigEntry<int> ConfigManorLordMaxSpawnCount { get; private set; }
    public ConfigEntry<int> ConfigMistressPowerLevel { get; private set; }
    public ConfigEntry<int> ConfigMistressMaxSpawnCount { get; private set; }
    public ConfigEntry<int> ConfigTransporterPowerLevel { get; private set; }
    public ConfigEntry<int> ConfigTransporterMaxSpawnCount { get; private set; }
    public ConfigEntry<int> ConfigJanitorPowerLevel { get; private set; }
    public ConfigEntry<int> ConfigJanitorMaxSpawnCount { get; private set; }
    public ConfigEntry<int> ConfigCleanerDroneGalCost { get; private set; }
    public ConfigEntry<int> ConfigCruiserGalCost { get; private set; }
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
    public ConfigEntry<bool> ConfigDebugMode { get; private set; }

    #endregion
    public CodeRebirthConfig(ConfigFile configFile)
    {
        configFile.SaveOnConfigSet = false;

        #region Debug
        ConfigDebugMode = configFile.Bind("Debug Options",
                                            "Debug Mode | Hazard Spawning Enabled",
                                            false,
                                            "Whether debug mode is enabled (for hazard spawning stuff).");
        ConfigExtendedLogging = configFile.Bind("Debug Options",
                                            "Debug Mode | Extended Logging",
                                            false,
                                            "Whether ExtendedLogging is enabled.");
        ConfigRemoveInteriorFog = configFile.Bind("Debug Options",
                                            "Debug Mode | Remove Interior Fog",
                                            false,
                                            "Whether zeekerss' horrible interior fog is removed.");
        ConfigUnlockAllGals = configFile.Bind("Debug Options",
                                            "Debug Mode | Unlock All Gals",
                                            false,
                                            "Whether all Gal unlockables are unlocked by default.");
        #endregion
        #region Oxyde
        ConfigOxydeEnabled = configFile.Bind("??? Options",
                                            "??? | Enabled",
                                            true,
                                            "Whether the ??? is enabled, keep in mind enabling this option enables the following parts of this mod automatically.\nThis includes but is not limited to the following: Janitor, Transporter, All the hazards, Wallet+Coins, Merchant.");
        #endregion
        #region Driftwood Menace
        ConfigDriftwoodMenaceEnabled = configFile.Bind("Driftwood Menace Options",
                                            "Driftwood Menace | Enabled",
                                            true,
                                            "Whether the Driftwood Menace is enabled.");
        ConfigDriftwoodMenaceSpawnWeights = configFile.Bind("Driftwood Menace Options",
                                            "Driftwood Menace | Spawn Weights",
                                            "Vanilla:20,Custom:20",
                                            "The spawn weights for the Driftwood Menace.");
        ConfigDriftwoodMenacePowerLevel = configFile.Bind("Driftwood Menace Options",
                                            "Driftwood Menace | Power Level",
                                            2f,
                                            "The power level of the Driftwood Menace.");
        ConfigDriftwoodMenaceMaxSpawnCount = configFile.Bind("Driftwood Menace Options",
                                            "Driftwood Menace | Max Spawn Count",
                                            3,
                                            "The max spawn count of the Driftwood Menace.");
        #endregion
        #region Nancy
        ConfigNancyEnabled = configFile.Bind("Nancy Options",
                                            "Nancy | Enabled",
                                            true,
                                            "Whether the Nancy is enabled.");
        ConfigNancySpawnWeights = configFile.Bind("Nancy Options",
                                            "Nancy | Spawn Weights",
                                            "Vanilla:20,Custom:20",
                                            "The spawn weights for the Nancy.");
        ConfigNancyPowerLevel = configFile.Bind("Nancy Options",
                                            "Nancy | Power Level",
                                            2f,
                                            "The power level of the Nancy.");
        ConfigNancyMaxSpawnCount = configFile.Bind("Nancy Options",
                                            "Nancy | Max Spawn Count",
                                            1,
                                            "The max spawn count of the Nancy.");
        #endregion
        #region Merchant
        ConfigMerchantEnabled = configFile.Bind("Merchant Options",
                                            "Merchant | Enabled",
                                            true,
                                            "Whether the Merchant is enabled.");
        ConfigWalletCost = configFile.Bind("Money Options",
                                            "Wallet Item | Cost",
                                            150,
                                            "Cost of Wallet");
        ConfigPiggyBankCost = configFile.Bind("Money Options",
                                            "PiggyBank | Cost",
                                            250,
                                            "Cost of the Piggy Bank Unlockable");
        #endregion
        #region Transporter
        ConfigTransporterEnabled = configFile.Bind("Transporter Options",
                                            "Transporter | Enabled",
                                            true,
                                            "Whether the Transporter is enabled.");
        ConfigTransporterSpawnWeights = configFile.Bind("Transporter Options",
                                            "Transporter | Spawn Weights",
                                            "Vanilla:20,Custom:20 ",
                                            "The spawn weights for the Transporter.");
        ConfigTransporterPowerLevel = configFile.Bind("Transporter Options",
                                            "Transporter | Power Level",
                                            2,
                                            "The power level of the Transporter.");
        ConfigTransporterMaxSpawnCount = configFile.Bind("Transporter Options",
                                            "Transporter | Max Spawn Count",
                                            1,
                                            "The max spawn count of the Transporter.");
        #endregion
        #region Cleaner Drone Gal
        ConfigCleanerDroneGalEnabled = configFile.Bind("Cleaner Drone Gal Options",
                                            "Cleaner Drone Gal | Enabled",
                                            true,
                                            "Whether the Cleaner Drone Gal is enabled.");
        ConfigCleanerDroneGalCost = configFile.Bind("Cleaner Drone Gal Options",
                                            "Cleaner Drone Gal | Cost",
                                            420,
                                            "The cost of the Cleaner Drone Gal.");
        #endregion

        #region Janitor
        ConfigJanitorEnabled = configFile.Bind("Janitor Options",
                                            "Janitor | Enabled",
                                            true,
                                            "Whether the Janitor is enabled.");
        ConfigJanitorSpawnWeights = configFile.Bind("Janitor Options",
                                            "Janitor | Spawn Weights",
                                            "Experimentation:20,Assurance:20,Vow:10,Offense:20,March:10,Adamance:5,Rend:10,Dine:10,Titan:60,Artifice:80,Embrion:90,Modded:20",
                                            "The spawn weights for the Janitor.");
        ConfigJanitorPowerLevel = configFile.Bind("Janitor Options",
                                            "Janitor | Power Level",
                                            2,
                                            "The power level of the Janitor.");
        ConfigJanitorMaxSpawnCount = configFile.Bind("Janitor Options",
                                            "Janitor | Max Spawn Count",
                                            3,
                                            "The max spawn count of the Janitor.");
        ConfigDisableTrashCans = configFile.Bind("Janitor Options",
                                            "Janitor | Disable Trash Cans",
                                            false,
                                            "Whether trash cans are disabled.");
        #endregion
        #region Mistress
        ConfigMistressEnabled = configFile.Bind("Mistress Options",
                                            "Mistress | Enabled",
                                            true,
                                            "Whether the Mistress is enabled.");
        ConfigMistressSpawnWeights = configFile.Bind("Mistress Options",
                                            "Mistress | Spawn Weights",
                                            "Experimentation:2,Assurance:5,Vow:5,Offense:10,March:10,Adamance:10,Rend:50,Dine:80,Titan:50,Artifice:60,Embrion:10,Modded:10 ",
                                            "The spawn weights for the Mistress.");
        ConfigMistressPowerLevel = configFile.Bind("Mistress Options",
                                            "Mistress | Power Level",
                                            2,
                                            "The power level of the Mistress.");
        ConfigMistressMaxSpawnCount = configFile.Bind("Mistress Options",
                                            "Mistress | Max Spawn Count",
                                            1,
                                            "The max spawn count of the Mistress.");
        #endregion
        #region Lord Of The Manor
        ConfigManorLordEnabled = configFile.Bind("Manor Lord Options",
                                            "Manor Lord | Enabled",
                                            true,
                                            "Whether the Manor Lord is enabled.");
        ConfigManorLordSpawnWeights = configFile.Bind("Manor Lord Options",
                                            "Manor Lord | Spawn Weights",
                                            "Experimentation:2,Assurance:5,Vow:5,Offense:10,March:10,Adamance:10,Rend:50,Dine:80,Titan:20,Artifice:60,Embrion:10,Modded:10 ",
                                            "The spawn weights for the Manor Lord.");
        ConfigManorLordPowerLevel = configFile.Bind("Manor Lord Options",
                                            "Manor Lord | Power Level",
                                            2,
                                            "The power level of the Manor Lord.");
        ConfigManorLordMaxSpawnCount = configFile.Bind("Manor Lord Options",
                                            "Manor Lord | Max Spawn Count",
                                            3,
                                            "The max spawn count of the Manor Lord.");
        #endregion
        #region Zort Stuff
        ConfigZortAddonsEnabled = configFile.Bind("Zort Options",
                                            "Zort Addons | Enabled",
                                            true,
                                            "Whether Zort addons are enabled (from the game zort, try the game out it's amazing with 4 people).");
        ConfigZortGuitarSpawnWeights = configFile.Bind("Zort Options",
                                            "Zort Guitar | Spawn Weights",
                                            "Vanilla:10,Custom:10",
                                            "The spawn weights for the Zort guitar.");
        ConfigZortViolinSpawnWeights = configFile.Bind("Zort Options",
                                            "Zort Violin | Spawn Weights",
                                            "Vanilla:10,Custom:10",
                                            "The spawn weights for the Zort violin.");
        ConfigZortRecorderSpawnWeights = configFile.Bind("Zort Options",
                                            "Zort Recorder | Spawn Weights",
                                            "Vanilla:10,Custom:10",
                                            "The spawn weights for the Zort recorder.");
        ConfigZortAccordionSpawnWeights = configFile.Bind("Zort Options",
                                            "Zort Accordion | Spawn Weights",
                                            "Vanilla:10,Custom:10",
                                            "The spawn weights for the Zort accordion.");
        ConfigZortModelReplacementEnabled = configFile.Bind("Zort Options",
                                            "Zort Model Replacement | Enabled",
                                            true,
                                            "Whether the Zort model replacement is enabled.");
        #endregion
        #region BearTrap Gal
        ConfigBearTrapGalEnabled = configFile.Bind("BearTrapGal Options",
                                            "BearTrapGal | Enabled",
                                            true,
                                            "Whether the BearTrapGal is enabled.");
        ConfigBearTrapGalCost = configFile.Bind("BearTrapGal Options",
                                            "BearTrapGal | Cost",
                                            200,
                                            "The cost of the BearTrapGal.");
        #endregion
        #region ACUnit Gal
        ConfigACUnitGalEnabled = configFile.Bind("ACUnitGal Options",
                                            "ACUnitGal | Enabled",
                                            true,
                                            "Whether the ACUnitGal is enabled.");
        ConfigACUnitGalCost = configFile.Bind("ACUnitGal Options",
                                            "ACUnitGal | Cost",
                                            200,
                                            "The cost of the ACUnitGal.");
        #endregion
        #region Friend Stuff
        ConfigFriendStuffEnabled = configFile.Bind("FriendStuff Options",
                                            "Friend Stuff | Enabled",
                                            true,
                                            "Whether friend stuff is enabled, unrelated to this mod, just stuff for my friends.");
        ConfigGlitchedPlushieCost = configFile.Bind("FriendStuff Options",
                                            "Friend Stuff | Glitched Plushie SpawnWeights",
                                            69,
                                            "The MoonName - SpawnWeight for the Glitched Plushie.");
        #endregion
        #region Duck Song
        ConfigDuckSongEnabled = configFile.Bind("DuckSong Options",
                                            "Duck Song | Enabled",
                                            true,
                                            "Whether the Duck Song is enabled.");
        ConfigDuckSongSpawnWeights = configFile.Bind("DuckSong Options",
                                            "Duck Song | SpawnWeights",
                                            "Vanilla:20,Custom:20",
                                            "The MoonName - SpawnWeight for the Duck.");
        ConfigDuckSongPowerLevel = configFile.Bind("DuckSong Options",
                                            "Duck Song | Power Level",
                                            3,
                                            "The Power Level of the Duck Song enemy.");
        ConfigDuckSongMaxSpawnCount = configFile.Bind("DuckSong Options",
                                            "Duck Song | Max Spawn Count",
                                            2,
                                            "The Max Spawn Count of the Duck Song enemy.");
        ConfigDuckSongTimer = configFile.Bind("DuckSong Options",
                                            "Duck Song | Timer",
                                            120f,
                                            "The Quest Timer of the Duck Song enemy.");
        #endregion
        #region Shrimp Dispenser
        ConfigShrimpDispenserEnabled = configFile.Bind("ShrimpDispenser Options",
                                            "Shrimp Dispenser | Enabled",
                                            true,
                                            "Whether the Shrimp Dispenser is enabled.");
        ConfigShrimpDispenserCost = configFile.Bind("ShrimpDispenser Options",
                                            "Shrimp Dispenser | Cost",
                                            150,
                                            "The cost of the Shrimp Dispenser.");
        #endregion
        #region Functional Microwave
        ConfigFunctionalMicrowaveEnabled = configFile.Bind("FunctionalMicrowave Options",
                                            "Functional Microwave | Enabled",
                                            true,
                                            "Whether the Functional Microwave is enabled.");
        ConfigFunctionalMicrowaveCurveSpawnWeight = configFile.Bind("FunctionalMicrowave Options",
                                            "Functional Microwave | SpawnWeight Curve",
                                            "Vanilla - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00 | Custom - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00",
                                            "The MoonName - CurveSpawnWeight for the hazard.");
        ConfigMicrowaveVolume = configFile.Bind("FunctionalMicrowave Options",
                                            "Functional Microwave | Volume",
                                            1f,
                                            "The volume of the Functional Microwave.");
        #endregion
        #region Bear Trap
        ConfigBearTrapEnabled = configFile.Bind("BearTrap Options",
                                            "Bear Trap | Enabled",
                                            true,
                                            "Whether the bear trap is enabled.");
        ConfigInsideBearTrapEnabled = configFile.Bind("BearTrap Options",
                                            "Bear Trap | Interior Spawn",
                                            false,
                                            "Whether the bear traps can spawn in the interior.");
        ConfigBearTrapsPopTires = configFile.Bind("BearTrap Options",
                                            "Bear Trap | Pop Tires",
                                            true,
                                            "Whether the bear trap can pop tires.");
        ConfigBearTrapSpawnWeight = configFile.Bind("BearTrap Options",
                                            "Bear Trap | OUTSIDE Spawn Abundance",
                                            "Vanilla:10,Custom:10",
                                            "The MoonName:Number Spawn Abundance (where it will spawn between 0 and 10) of bear trap clusters to spawn per round (clusters means that theres 1 primary bear trap that spawns more (0 to 5) around it).");
        ConfigBearTrapInsideSpawnWeight = configFile.Bind("BearTrap Options",
                                            "Bear Trap | INSIDE Spawn Weight",
                                            "Vanilla - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00 | Custom - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00",
                                            "The MoonName - CurveSpawnWeight for the INSIDE BearTrap.");
        ConfigBearTrapVolume = configFile.Bind("BearTrap Options",
                                            "Bear Trap | Volume",
                                            1f,
                                            "The volume of the Bear Trap.");
        #endregion
        #region Laser Turret
        ConfigLaserTurretEnabled = configFile.Bind("LaserTurret Options",
                                            "Laser Turret | Enabled",
                                            true,
                                            "Whether the Laser Turret is enabled.");
        ConfigLaserTurretCurveSpawnWeight = configFile.Bind("LaserTurret Options",
                                            "Laser Turret | SpawnWeight Curve",
                                            "Vanilla - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00 | Custom - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00",
                                            "The MoonName - CurveSpawnWeight for the LaserTurret.");
        ConfigLaserTurretVolume = configFile.Bind("LaserTurret Options",
                                            "Laser Turret | Volume",
                                            1f,
                                            "The volume of the Laser Turret.");
        #endregion
        #region Flash Turret
        ConfigFlashTurretEnabled = configFile.Bind("FlashTurret Options",
                                            "Flash Turret | Enabled",
                                            true,
                                            "Whether the flash turret is enabled.");
        ConfigFlashTurretCurveSpawnWeight = configFile.Bind("FlashTurret Options",
                                            "Flash Turret | SpawnWeight Curve",
                                            "Vanilla - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00 | Custom - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00 ",
                                            "The MoonName - CurveSpawnWeight for the FlashTurret.");
        ConfigFlashTurretVolume = configFile.Bind("FlashTurret Options",
                                            "Flash Turret | Volume",
                                            1f,
                                            "The volume of the Flash Turret.");
        #endregion
        #region Industrial Fan
        ConfigIndustrialFanEnabled = configFile.Bind("IndustrialFan Options",
                                            "Industrial Fan | Enabled",
                                            true,
                                            "Whether the industrial fan is enabled.");
        ConfigIndustrialFanCurveSpawnWeight = configFile.Bind("IndustrialFan Options",
                                            "Industrial Fan | SpawnWeight Curve",
                                            "Vanilla - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00 | Custom - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00",
                                            "The MoonName - CurveSpawnWeight for the IndustrialFan.");
        #endregion
        #region Tesla Shock
        ConfigTeslaShockEnabled = configFile.Bind("TeslaShock Options",
                                            "Tesla Shock | Enabled",
                                            true,
                                            "Whether the tesla shock is enabled.");
        ConfigTeslaShockCurveSpawnWeight = configFile.Bind("TeslaShock Options",
                                            "Tesla Shock | SpawnWeight Curve",
                                            "Vanilla - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00 | Custom - 0.00,0.00 ; 0.11,0.14 ; 0.22,0.29 ; 0.33,0.43 ; 0.44,0.55 ; 0.56,0.63 ; 0.67,0.71 ; 0.78,0.87 ; 0.89,1.16 ; 1.00,8.00",
                                            "The MoonName - CurveSpawnWeight for the TeslaShock.");
        ConfigTeslaShockVolume = configFile.Bind("TeslaShock Options",
                                            "Tesla Shock | Volume",
                                            1f,
                                            "The volume of the Tesla Shock.");

        #endregion
        #region Air Control Unit
        ConfigAirControlUnitEnabled = configFile.Bind("AirControlUnit Options",
                                            "Air Control Unit | Enabled",
                                            true,
                                            "Whether the air control unit is enabled.");
        ConfigAirControlUnitSpawnWeight = configFile.Bind("AirControlUnit Options",
                                            "Air Control Unit | SpawnWeight",
                                            "Vanilla:2,Custom:2,Tіtan:0,Titan:0,Olympus:2",
                                            "The MoonName:CurveSpawnWeight for the AirControlUnit.");
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
        #endregion
        #region Cruiser Gal
        ConfigCruiserGalEnabled = configFile.Bind("Cruiser Options",
                                            "Cruiser Gal | Enabled",
                                            true,
                                            "Whether the Cruiser Gal is enabled.");
        ConfigCruiserGalCost = configFile.Bind("Cruiser Options",
                                            "Cruiser Gal | Cost",
                                            2500,
                                            "Cost of the Cruiser Gal.");
        ConfigCruiserGalAutomatic = configFile.Bind("Cruiser Options",
                                            "Cruiser Gal | Automatic Behaviour",
                                            false,
                                            "Whether the Cruiser Gal will automatically wake up and choose the nearest player as the owner.");
        #endregion
        #region Shockwave Gal
        ConfigShockwaveBotEnabled = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Enabled",
                                            true,
                                            "Whether the Shockwave Gal is enabled.");
        ConfigShockwaveBotCost = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Cost",
                                            2000,
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
                                            "Pikmin, Centipede, Red Locust Bees, Docile Locust Bees, Manticoil, SnailCat, Tornado, RadMech, Earth Leviathan, Puffer, Jester, Blob, Girl, Spring, Clay Surgeon",
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
        ConfigShockwaveGalPlayerModelEnabled = configFile.Bind("Shockwave Options",
                                            "Shockwave Gal | Player Model",
                                            true,
                                            "Whether the Shockwave Gal player model version is available for use (Requires MoreSuits and ModelReplacementAPI to be installed).");
        #endregion
        #region Seamine Gal
        ConfigSeamineTinkEnabled = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Enabled",
                                            true,
                                            "Whether the Seamine Gal is enabled.");
        ConfigSeamineTinkCost = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Cost",
                                            1500,
                                            "Cost of the Seamine Gal.");
        ConfigSeamineTinkCharges = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Charges",
                                            3,
                                            "How many charges the Seamine Gal has.");
        ConfigSeamineTinkEnemyBlacklist = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Enemy Blacklist",
                                            "Pikmin, Docile Locust Bees, Manticoil, Nemo, Horse, Shisha, Tornado, Scary",
                                            "Comma separated list of enemies that the Seamine Gal will not target (keep in mind she targets ALL enemies).");
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
        ConfigSeamineTinkPlayerModelEnabled = configFile.Bind("Seamine Options",
                                            "Seamine Gal | Player Model",
                                            true,
                                            "Whether the Seamine Gal player model version is available for use (Requires MoreSuits and ModelReplacementAPI to be installed).");
        #endregion
        #region Terminal Gal
        ConfigTerminalBotEnabled = configFile.Bind("Terminal Options",
                                            "Terminal Gal | Enabled",
                                            true,
                                            "Whether the Terminal Gal is enabled.");
        ConfigTerminalBotCost = configFile.Bind("Terminal Options",
                                            "Terminal Gal | Cost",
                                            2000,
                                            "Cost of the Terminal Gal.");
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
        #region Bell Crab Gal
        ConfigBellCrabGalEnabled = configFile.Bind("Bell Crab Options",
                                            "Bell Crab Gal | Enabled",
                                            true,
                                            "Whether the Bell Crab Gal is enabled.");
        ConfigBellCrabGalCost = configFile.Bind("Bell Crab Options",
                                            "Bell Crab Gal | Cost",
                                            250,
                                            "Cost of the Bell Crab Gal.");
        #endregion
        #region SCP 999 Gal
        Config999GalEnabled = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Enabled",
                                            true,
                                            "Whether the SCP 999 Gal is enabled.");
        Config999GalCost = configFile.Bind("SCP 999 Gal Options",
                                            "SCP 999 Gal | Cost",
                                            1999,
                                            "Cost of the SCP 999 Gal.");
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
                                            "Vanilla,Custom,",
                                            "Flora spawn places e.g. `Custom,Vanilla,Experimentation,Assurance,Gloom`.");
        ConfigFloraDesertSpawnPlaces = configFile.Bind("Flora Options",
                                            "Flora | Desert Spawn Places",
                                            "Vanilla,Custom,",
                                            "Flora spawn places e.g. `Custom,Vanilla,Experimentation,Assurance,Gloom`.");
        ConfigFloraSnowSpawnPlaces = configFile.Bind("Flora Options",
                                            "Flora | Snow Spawn Places",
                                            "Vanilla,Custom,",
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
        ConfigWoodenSeedTreeSpawnChance = configFile.Bind("Farming Options",
                                            "Farming | Wooden Seed Tree Spawn Chance",
                                            2,
                                            "Chance of the wooden seed to spawn from a broken tree");
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
                                            "Experimentation:1,Assurance:10,Vow:90,Offense:20,March:90,Adamance:60,Rend:5,Dine:10,Titan:2,Artifice:30,Embrion:10,Modded:50",
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
                                            30f,
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
        #endregion
        #region ModCompat
        ConfigFirstLaunchPopup = configFile.Bind("ModCompat Options",
                                            "First Launch Popup",
                                            true,
                                            "Enables/Disables the first launch popup for the host about the ShockwaveGalModelReplacement mod.");
        #endregion
        #region Monarch+Cutiefly
        ConfigMonarchEnabled = configFile.Bind("Monarch Options",
                                            "Monarch Enemy | Enabled",
                                            true,
                                            "Enables/Disables the Monarch");
        ConfigCutieFlySpawnWeights = configFile.Bind("Monarch Options",
                                            "Cutiefly Enemy | Spawn Weights",
                                            "Custom:10,Vanilla:10",
                                            "SpawnWeight of the CutieFly in moons.");
        ConfigCutieFlyPowerLevel = configFile.Bind("Monarch Options",
                                            "Cutiefly Enemy | Power Level",
                                            2.5f,
                                            "Power level of the CutieFly enemy.");
        ConfigCutieFlyMaxSpawnCount = configFile.Bind("Monarch Options",
                                            "Cutiefly Enemy | Max Spawn Count",
                                            5,
                                            "How many CutieFly can spawn in a moon.");
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
        ConfigSuspiciousActivityEnabled = configFile.Bind("Crate Options",
                                            "Crate | Suspicious Activity Enabled",
                                            true,
                                            "Enables/Disables the Suspicious Activity from happening.");
        ConfigMetalCrateSpawnWeight = configFile.Bind("Crate Options",
                                            "Crate | Metal SpawnWeight",
                                            "Vanilla:3,Custom:3",
                                            "MoonName:SpawnWeight of Metal Crates that spawn outside (between 0 and your number).");
        ConfigCrateNumberToSpawn = configFile.Bind("Crate Options",
                                            "Crate | Scrap Number To Spawn",
                                            3,
                                            "Number of items that spawn inside a crate (between 0 and your number).");
        ConfigWoodenCrateSpawnWeight = configFile.Bind("Crate Options",
                                            "Crate | Wooden SpawnWeight",
                                            "Vanilla:3,Custom:3",
                                            "MoonName:SpawnWeight of Wooden Crates that spawn outside (between 0 and your number).");
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
        ConfigMetalCrateValueMultiplier = configFile.Bind("Crate Options",
                                            "Crate | Metal Value Multiplier",
                                            1.4f,
                                            "Value Multiplier for Metal Crates.");
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