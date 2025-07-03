using System;
using System.Collections.Generic;
using CodeRebirth.src.Patches;
using UnityEngine;
using LethalLib.Modules;
using CodeRebirthLib.ContentManagement;
using CodeRebirthLib;
using CodeRebirthLib.AssetManagement;
using CodeRebirthLib.ConfigManagement;
using CodeRebirthLib.ContentManagement.MapObjects;

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

    public OxydeCrashShipAssets? OxydeCrashShip = null;
    public GunslingerGregAssets? GunslingerGreg = null;
    public CompactorTobyAssets? CompactorToby = null;
    public ShredderSarahAssets? ShredderSarah = null;
    public MerchantAssets? Merchant = null;
    public CrateAssets? Crate = null;
    public FloraAssets? Flora = null;
    public BiomeAssets? Biome = null;
    public BearTrapAssets? BearTrap = null;
    public GlowingGemAssets? GlowingGem = null;
    public IndustrialFanAssets? IndustrialFan = null;
    public FlashTurretAssets? FlashTurret = null;
    public TeslaShockAssets? TeslaShock = null;
    public AirControlUnitAssets? AirControlUnit = null;
    public FunctionalMicrowaveAssets? FunctionalMicrowave = null;
    public AutonomousCraneAssets? AutonomousCrane = null;

    public MapObjectHandler(CRMod mod) : base(mod)
    {
        RegisterContent("oxydecrashshipassets", out OxydeCrashShip, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("compactortobyassets", out CompactorToby, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("gunslingergregassets", out GunslingerGreg, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("shreddersarahassets", out ShredderSarah, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("merchantassets", out Merchant, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("crateassets", out Crate);

        RegisterContent("aircontrolunitassets", out AirControlUnit);

        RegisterContent("flashturretassets", out FlashTurret);

        // RegisterContent("biomeassets", out Biome);

        // RegisterContent("teslashockassets", out TeslaShock);

        RegisterContent("beartrapassets", out BearTrap);

        RegisterContent("functionalmicrowaveassets", out FunctionalMicrowave);

        RegisterContent("autonomouscraneassets", out AutonomousCrane);

        RegisterContent("glowinggemassets", out GlowingGem);

        RegisterContent("industrialfanassets", out IndustrialFan);

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
        MapObjectSpawnMechanics floraMapObjectSpawnMechanics = new MapObjectSpawnMechanics(configString);

        RoundManagerPatch.spawnableFlora.Add(new SpawnableFlora()
        {
            prefab = prefab,
            floraTag = tag,
            spawnCurveFunction = floraMapObjectSpawnMechanics.CurveFunction,
        });
    }
}