using CodeRebirth.src.Patches;
using UnityEngine;
using Dawn;
using Dawn.Dusk;

namespace CodeRebirth.src.Content.Maps;
public class MapObjectHandler : ContentHandler<MapObjectHandler>
{
    public class CompactorTobyAssets(DuskMod mod, string filePath) : AssetBundleLoader<CompactorTobyAssets>(mod, filePath)
    {
    }

    public class ShredderSarahAssets(DuskMod mod, string filePath) : AssetBundleLoader<ShredderSarahAssets>(mod, filePath)
    {
    }

    public class CrateAssets(DuskMod mod, string filePath) : AssetBundleLoader<CrateAssets>(mod, filePath)
    {
    }

    public class FloraAssets(DuskMod mod, string filePath) : AssetBundleLoader<FloraAssets>(mod, filePath)
    {
        [LoadFromBundle("AllFlora.prefab")]
        public GameObject AllFloraPrefab { get; private set; } = null!;
    }

    public class BiomeAssets(DuskMod mod, string filePath) : AssetBundleLoader<BiomeAssets>(mod, filePath)
    {
    }

    public class BearTrapAssets(DuskMod mod, string filePath) : AssetBundleLoader<BearTrapAssets>(mod, filePath)
    {
    }

    public class GlowingGemAssets(DuskMod mod, string filePath) : AssetBundleLoader<GlowingGemAssets>(mod, filePath)
    {
    }

    public class IndustrialFanAssets(DuskMod mod, string filePath) : AssetBundleLoader<IndustrialFanAssets>(mod, filePath)
    {
    }

    public class FlashTurretAssets(DuskMod mod, string filePath) : AssetBundleLoader<FlashTurretAssets>(mod, filePath)
    {
        [LoadFromBundle("FlashTurretUpdated.prefab")]
        public GameObject FlashTurretPrefab { get; private set; } = null!;
    }

    public class TeslaShockAssets(DuskMod mod, string filePath) : AssetBundleLoader<TeslaShockAssets>(mod, filePath)
    {
        [LoadFromBundle("ChainLightning.prefab")]
        public GameObject ChainLightningPrefab { get; private set; } = null!;
    }

    public class AirControlUnitAssets(DuskMod mod, string filePath) : AssetBundleLoader<AirControlUnitAssets>(mod, filePath)
    {
        [LoadFromBundle("AirControlUnitProjectile.prefab")]
        public GameObject ProjectilePrefab { get; private set; } = null!;
    }

    public class FunctionalMicrowaveAssets(DuskMod mod, string filePath) : AssetBundleLoader<FunctionalMicrowaveAssets>(mod, filePath)
    {
    }

    public class MerchantAssets(DuskMod mod, string filePath) : AssetBundleLoader<MerchantAssets>(mod, filePath)
    {
        [LoadFromBundle("GuardsmanProjectile.prefab")]
        public GameObject ProjectilePrefab { get; private set; } = null!;
    }

    public class GunslingerGregAssets(DuskMod mod, string filePath) : AssetBundleLoader<GunslingerGregAssets>(mod, filePath)
    {
        [LoadFromBundle("GregMissile.prefab")]
        public GameObject MissilePrefab { get; private set; } = null!;

        [LoadFromBundle("DebrisExplosionEffect.prefab")]
        public GameObject OldBirdExplosionPrefab { get; private set; } = null!;
    }

    public class OxydeCrashShipAssets(DuskMod mod, string filePath) : AssetBundleLoader<OxydeCrashShipAssets>(mod, filePath)
    {
    }

    public class AutonomousCraneAssets(DuskMod mod, string filePath) : AssetBundleLoader<AutonomousCraneAssets>(mod, filePath)
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

    public MapObjectHandler(DuskMod mod) : base(mod)
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

    public void RegisterOutsideFlora(DuskMod mod)
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
        try
        {
            MapObjectSpawnMechanics floraMapObjectSpawnMechanics = new MapObjectSpawnMechanics(configString);

            RoundManagerPatch.spawnableFlora.Add(new SpawnableFlora()
            {
                prefab = prefab,
                floraTag = tag,
                spawnCurveFunction = floraMapObjectSpawnMechanics.CurveFunction,
            });
        }
        catch (MalformedAnimationCurveConfigException exception)
        {
            exception.LogNicely(Plugin.Logger);
        }
    }
}