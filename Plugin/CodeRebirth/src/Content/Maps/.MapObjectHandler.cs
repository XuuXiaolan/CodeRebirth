using System;
using System.Collections.Generic;
using CodeRebirth.src.Patches;
using UnityEngine;
using LethalLib.Modules;
using CodeRebirthLib.ContentManagement;
using CodeRebirthLib;
using CodeRebirthLib.AssetManagement;

namespace CodeRebirth.src.Content.Maps;
public class MapObjectHandler : ContentHandler<MapObjectHandler>
{
    public class CompactorTobyAssets(CRMod mod, string filePath) : AssetBundleLoader<CompactorTobyAssets>(mod, filePath)
    {
    }

    public class ShredderSarahAssets(CRMod mod, string filePath) : AssetBundleLoader<ShredderSarahAssets>(mod, filePath)
    {
    }

    public class CrateAssets(CRMod mod, string filePath) : AssetBundleLoader<CrateAssets>(mod, filePath)
    {
    }

    public class FloraAssets(CRMod mod, string filePath) : AssetBundleLoader<FloraAssets>(mod, filePath)
    {
        [LoadFromBundle("AllFlora.prefab")]
        public GameObject AllFloraPrefab { get; private set; } = null!;
    }

    public class BiomeAssets(CRMod mod, string filePath) : AssetBundleLoader<BiomeAssets>(mod, filePath)
    {
    }

    public class BearTrapAssets(CRMod mod, string filePath) : AssetBundleLoader<BearTrapAssets>(mod, filePath)
    {
    }

    public class GlowingGemAssets(CRMod mod, string filePath) : AssetBundleLoader<GlowingGemAssets>(mod, filePath)
    {
    }

    public class IndustrialFanAssets(CRMod mod, string filePath) : AssetBundleLoader<IndustrialFanAssets>(mod, filePath)
    {
    }

    public class FlashTurretAssets(CRMod mod, string filePath) : AssetBundleLoader<FlashTurretAssets>(mod, filePath)
    {
        [LoadFromBundle("FlashTurretUpdated.prefab")]
        public GameObject FlashTurretPrefab { get; private set; } = null!;
    }

    public class TeslaShockAssets(CRMod mod, string filePath) : AssetBundleLoader<TeslaShockAssets>(mod, filePath)
    {
        [LoadFromBundle("ChainLightning.prefab")]
        public GameObject ChainLightningPrefab { get; private set; } = null!;
    }

    public class AirControlUnitAssets(CRMod mod, string filePath) : AssetBundleLoader<AirControlUnitAssets>(mod, filePath)
    {
        [LoadFromBundle("AirControlUnitProjectile.prefab")]
        public GameObject ProjectilePrefab { get; private set; } = null!;
    }

    public class FunctionalMicrowaveAssets(CRMod mod, string filePath) : AssetBundleLoader<FunctionalMicrowaveAssets>(mod, filePath)
    {
    }

    public class MerchantAssets(CRMod mod, string filePath) : AssetBundleLoader<MerchantAssets>(mod, filePath)
    {
    }

    public class GunslingerGregAssets(CRMod mod, string filePath) : AssetBundleLoader<GunslingerGregAssets>(mod, filePath)
    {
        [LoadFromBundle("GregMissile.prefab")]
        public GameObject MissilePrefab { get; private set; } = null!;

        [LoadFromBundle("DebrisExplosionEffect.prefab")]
        public GameObject OldBirdExplosionPrefab { get; private set; } = null!;
    }

    public class OxydeCrashShipAssets(CRMod mod, string filePath) : AssetBundleLoader<OxydeCrashShipAssets>(mod, filePath)
    {
    }

    public class AutonomousCraneAssets(CRMod mod, string filePath) : AssetBundleLoader<AutonomousCraneAssets>(mod, filePath)
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

    public MapObjectHandler(CRMod mod) : base(mod)
    {
        if (TryLoadContentBundle("oxydecrashshipassets", out OxydeCrashShipAssets? oxydecrashshipassets))
        {
            OxydeCrashShip = oxydecrashshipassets;
            LoadAllContent(oxydecrashshipassets!);
        }

        if (TryLoadContentBundle("compactortobyassets", out CompactorTobyAssets? compactortobyassets))
        {
            CompactorToby = compactortobyassets;
            LoadAllContent(compactortobyassets!);
        }

        if (TryLoadContentBundle("gunslingergregassets", out GunslingerGregAssets? gunslingergregassets))
        {
            GunslingerGreg = gunslingergregassets;
            LoadAllContent(gunslingergregassets!);
        }

        if (TryLoadContentBundle("shreddersarahassets", out ShredderSarahAssets? shreddersarahassets))
        {
            ShredderSarah = shreddersarahassets;
            LoadAllContent(shreddersarahassets!);
        }

        if (TryLoadContentBundle("merchantassets", out MerchantAssets? merchantassets))
        {
            Merchant = merchantassets;
            LoadAllContent(merchantassets!);
        }

        if (TryLoadContentBundle("crateassets", out CrateAssets? crateassets))
        {
            Crate = crateassets;
            LoadAllContent(crateassets!);
        }

        if (TryLoadContentBundle("biomeassets", out BiomeAssets? biomeassets))
        {
            Biome = biomeassets;
            LoadAllContent(biomeassets!);
        }

        if (TryLoadContentBundle("teslashockassets", out TeslaShockAssets? teslashockassets))
        {
            TeslaShock = teslashockassets;
            LoadAllContent(teslashockassets!);
        }

        if (TryLoadContentBundle("beartrapassets", out BearTrapAssets? beartrapassets))
        {
            BearTrap = beartrapassets;
            LoadAllContent(beartrapassets!);
        }

        if (TryLoadContentBundle("functionalmicrowaveassets", out FunctionalMicrowaveAssets? functionalmicrowaveassets))
        {
            FunctionalMicrowave = functionalmicrowaveassets;
            LoadAllContent(functionalmicrowaveassets!);
        }

        if (TryLoadContentBundle("autonomouscraneassets", out AutonomousCraneAssets? autonomouscraneassets))
        {
            AutonomousCrane = autonomouscraneassets;
            LoadAllContent(autonomouscraneassets!);
        }

        if (TryLoadContentBundle("glowinggemassets", out GlowingGemAssets? glowinggemassets))
        {
            GlowingGem = glowinggemassets;
            LoadAllContent(glowinggemassets!);
        }

        if (TryLoadContentBundle("industrialfanassets", out IndustrialFanAssets? industrialfanassets))
        {
            IndustrialFan = industrialfanassets;
            LoadAllContent(industrialfanassets!);
        }

        Plugin.ModConfig.ConfigFloraEnabled = Plugin.configFile.Bind("Flora Options",
                                            "Flora | Enabled",
                                            true,
                                            "Whether Flora is enabled.");

        if (Plugin.ModConfig.ConfigFloraEnabled.Value)
            RegisterOutsideFlora(mod);
    }

    public void RegisterOutsideFlora(CRMod mod)
    {
        Flora = new FloraAssets(mod, "floraassets");
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