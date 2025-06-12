using System;
using System.Collections.Generic;
using CodeRebirth.src.Patches;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;
using CodeRebirth.src.Util;
using CodeRebirth.src.MiscScripts;
using LethalLib.Modules;

namespace CodeRebirth.src.Content.Maps;
public class MapObjectHandler : ContentHandler<MapObjectHandler>
{
    public class CompactorTobyAssets(string bundleName) : AssetBundleLoader<CompactorTobyAssets>(bundleName)
    {
    }

    public class ShredderSarahAssets(string bundleName) : AssetBundleLoader<ShredderSarahAssets>(bundleName)
    {
    }

    public class CrateAssets(string bundleName) : AssetBundleLoader<CrateAssets>(bundleName)
    {
    }

    public class FloraAssets(string bundleName) : AssetBundleLoader<FloraAssets>(bundleName)
    {
        [LoadFromBundle("AllFlora.prefab")]
        public GameObject AllFloraPrefab { get; private set; } = null!;
    }

    public class BiomeAssets(string bundleName) : AssetBundleLoader<BiomeAssets>(bundleName)
    {
    }

    public class BearTrapAssets(string bundleName) : AssetBundleLoader<BearTrapAssets>(bundleName)
    {
    }

    public class GlowingGemAssets(string bundleName) : AssetBundleLoader<GlowingGemAssets>(bundleName)
    {
    }

    public class IndustrialFanAssets(string bundleName) : AssetBundleLoader<IndustrialFanAssets>(bundleName)
    {
    }

    public class FlashTurretAssets(string bundleName) : AssetBundleLoader<FlashTurretAssets>(bundleName)
    {
        [LoadFromBundle("FlashTurretUpdated.prefab")]
        public GameObject FlashTurretPrefab { get; private set; } = null!;
    }

    public class TeslaShockAssets(string bundleName) : AssetBundleLoader<TeslaShockAssets>(bundleName)
    {
        [LoadFromBundle("ChainLightning.prefab")]
        public GameObject ChainLightningPrefab { get; private set; } = null!;
    }

    public class AirControlUnitAssets(string bundleName) : AssetBundleLoader<AirControlUnitAssets>(bundleName)
    {
        [LoadFromBundle("AirControlUnitProjectile.prefab")]
        public GameObject ProjectilePrefab { get; private set; } = null!;
    }

    public class FunctionalMicrowaveAssets(string bundleName) : AssetBundleLoader<FunctionalMicrowaveAssets>(bundleName)
    {
    }

    public class MerchantAssets(string bundleName) : AssetBundleLoader<MerchantAssets>(bundleName)
    {
    }

    public class GunslingerGregAssets(string bundleName) : AssetBundleLoader<GunslingerGregAssets>(bundleName)
    {
        [LoadFromBundle("GregMissile.prefab")]
        public GameObject MissilePrefab { get; private set; } = null!;

        [LoadFromBundle("DebrisExplosionEffect.prefab")]
        public GameObject OldBirdExplosionPrefab { get; private set; } = null!;
    }

    public class OxydeCrashShipAssets(string bundleName) : AssetBundleLoader<OxydeCrashShipAssets>(bundleName)
    {
    }

    public class AutonomousCraneAssets(string bundleName) : AssetBundleLoader<AutonomousCraneAssets>(bundleName)
    {
    }

    public OxydeCrashShipAssets? OxydeCrashShip { get; private set; } = null;
    public GunslingerGregAssets? GunslingerGreg { get; private set; } = null;
    public CompactorTobyAssets? CompactorToby { get; private set; } = null;
    public ShredderSarahAssets? ShredderSarah { get; private set; } = null;
    public MerchantAssets? Merchant { get; private set; } = null;
    public CrateAssets? Crate { get; private set; } = null;
    public FloraAssets? Flora { get; private set; } = null;
    public BiomeAssets? Biome { get; private set; } = null;
    public BearTrapAssets? BearTrap { get; private set; } = null;
    public GlowingGemAssets? GlowingGem { get; private set; } = null;
    public IndustrialFanAssets? IndustrialFan { get; private set; } = null;
    public FlashTurretAssets? FlashTurret { get; private set; } = null;
    public TeslaShockAssets? TeslaShock { get; private set; } = null;
    public AirControlUnitAssets? AirControlUnit { get; private set; } = null;
    public FunctionalMicrowaveAssets? FunctionalMicrowave { get; private set; } = null;
    public AutonomousCraneAssets? AutonomousCrane { get; private set; } = null;

    public Dictionary<SpawnSyncedCRObject.CRObjectType, GameObject> prefabMapping = new();

    public MapObjectHandler()
    {
        OxydeCrashShip = LoadAndRegisterAssets<OxydeCrashShipAssets>("oxydecrashshipassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        CompactorToby = LoadAndRegisterAssets<CompactorTobyAssets>("compactortobyassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        GunslingerGreg = LoadAndRegisterAssets<GunslingerGregAssets>("gunslingergregassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        ShredderSarah = LoadAndRegisterAssets<ShredderSarahAssets>("shreddersarahassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        Merchant = LoadAndRegisterAssets<MerchantAssets>("merchantassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        Crate = LoadAndRegisterAssets<CrateAssets>("crateassets");

        // Biome = LoadAndRegisterAssets<BiomeAssets>("biomeassets");

        TeslaShock = LoadAndRegisterAssets<TeslaShockAssets>("teslashockassets");

        BearTrap = LoadAndRegisterAssets<BearTrapAssets>("beartrapassets");

        FunctionalMicrowave = LoadAndRegisterAssets<FunctionalMicrowaveAssets>("functionalmicrowaveassets");

        FlashTurret = LoadAndRegisterAssets<FlashTurretAssets>("flashturretassets");

        IndustrialFan = LoadAndRegisterAssets<IndustrialFanAssets>("industrialfanassets");

        GlowingGem = LoadAndRegisterAssets<GlowingGemAssets>("glowinggemassets");

        AirControlUnit = LoadAndRegisterAssets<AirControlUnitAssets>("aircontrolunitassets");

        AutonomousCrane = LoadAndRegisterAssets<AutonomousCraneAssets>("autonomouscraneassets");

        Plugin.ModConfig.ConfigFloraEnabled = Plugin.configFile.Bind("Flora Options",
                                            "Flora | Enabled",
                                            true,
                                            "Whether Flora is enabled.");

        if (Plugin.ModConfig.ConfigFloraEnabled.Value)
            RegisterOutsideFlora();
    }

    public GameObject? GetPrefabFor(SpawnSyncedCRObject.CRObjectType objectType)
    {
        prefabMapping.TryGetValue(objectType, out var prefab);
        return prefab;
    }

    public void RegisterOutsideFlora()
    {
        Flora = new FloraAssets("floraassets");
        Flora floraStuff = Flora.AllFloraPrefab.GetComponent<Flora>();

        Plugin.ModConfig.ConfigFloraGrassCurveSpawnWeight = Plugin.configFile.Bind("Flora Options",
                                            "Flora | Grass CurveSpawnWeight",
                                            "Vanilla - 0.00,30.00 ; 1.00,60.00 | Custom - 0.00,30.00 ; 1.00,60.00 | Oxyde - 0.00,0.00 ; 1.00,0.00",
                                            "MoonName - CurveSpawnWeight for Grass flora (moon tags also work).");
        Plugin.ModConfig.ConfigFloraDesertCurveSpawnWeight = Plugin.configFile.Bind("Flora Options",
                                            "Flora | Desert CurveSpawnWeight",
                                            "Vanilla - 0.00,30.00 ; 1.00,60.00 | Custom - 0.00,30.00 ; 1.00,60.00 | Oxyde - 0.00,0.00 ; 1.00,0.00",
                                            "MoonName - CurveSpawnWeight for Desert flora (moon tags also work).");
        Plugin.ModConfig.ConfigFloraSnowCurveSpawnWeight = Plugin.configFile.Bind("Flora Options",
                                            "Flora | Snow CurveSpawnWeight",
                                            "Vanilla - 0.00,30.00 ; 1.00,60.00 | Custom - 0.00,30.00 ; 1.00,60.00 | Oxyde - 0.00,0.00 ; 1.00,0.00",
                                            "MoonName - CurveSpawnWeight for Snowy flora (moon tags also work).");

        foreach (var flora in floraStuff.Desert)
        {
            RegisterFlora(flora, FloraTag.Desert, Plugin.ModConfig.ConfigFloraDesertCurveSpawnWeight.Value);
        }

        foreach (var flora in floraStuff.Snowy)
        {
            RegisterFlora(flora, FloraTag.Snow, Plugin.ModConfig.ConfigFloraSnowCurveSpawnWeight.Value);
        }

        foreach (var flora in floraStuff.Grass)
        {
            RegisterFlora(flora, FloraTag.Grass, Plugin.ModConfig.ConfigFloraGrassCurveSpawnWeight.Value);
        }

        /*foreach (var flora in floraStuff.DangerousSpecies) {
			RegisterFlora(flora, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 3)), dangerMoonList, FloraTag.Dangerous, moonBlackList);
		}*/
    }

    public void RegisterFlora(GameObject prefab, FloraTag tag, string configString)
    {
        (Dictionary<Levels.LevelTypes, string> spawnRateByLevelType, Dictionary<string, string> spawnRateByCustomLevelType) = ConfigParsingWithCurve(configString);

        // Create dictionaries to hold animation curves for each level type
        Dictionary<Levels.LevelTypes, AnimationCurve> curvesByLevelType = new();
        Dictionary<string, AnimationCurve> curvesByCustomLevelType = new();

        bool allCurveExists = false;
        AnimationCurve allAnimationCurve = AnimationCurve.Linear(0, 0, 1, 0);

        bool vanillaCurveExists = false;
        AnimationCurve vanillaAnimationCurve = AnimationCurve.Linear(0, 0, 1, 0);

        bool moddedCurveExists = false;
        AnimationCurve moddedAnimationCurve = AnimationCurve.Linear(0, 0, 1, 0);

        // Populate the animation curves
        foreach (var entry in spawnRateByLevelType)
        {
            Plugin.ExtendedLogging($"Registering flora {prefab.name} for level {entry.Key} with curve {entry.Value}");
            curvesByLevelType[entry.Key] = CreateCurveFromString(entry.Value, prefab.name, entry.Key.ToString());
            if (entry.Key == Levels.LevelTypes.Vanilla)
            {
                vanillaCurveExists = true;
                vanillaAnimationCurve = curvesByLevelType[entry.Key];
            }
            else if (entry.Key == Levels.LevelTypes.Modded)
            {
                moddedCurveExists = true;
                moddedAnimationCurve = curvesByLevelType[entry.Key];
            }
            else if (entry.Key == Levels.LevelTypes.All)
            {
                allCurveExists = true;
                allAnimationCurve = curvesByLevelType[entry.Key];
            }
        }
        foreach (var entry in spawnRateByCustomLevelType)
        {
            curvesByCustomLevelType[entry.Key] = CreateCurveFromString(entry.Value, prefab.name, entry.Key);
        }

        RoundManagerPatch.spawnableFlora.Add(new SpawnableFlora()
        {
            prefab = prefab,
            floraTag = tag,
            spawnCurveFunction =
            level => CurveFunction(level, prefab, curvesByLevelType, curvesByCustomLevelType, vanillaCurveExists, vanillaAnimationCurve, moddedCurveExists, moddedAnimationCurve, allCurveExists, allAnimationCurve)
        });
    }
}