using CodeRebirthLib;
using CodeRebirthLib.AssetManagement;
using CodeRebirthLib.ContentManagement;
using UnityEngine;

namespace CodeRebirth.src.Content.Weathers;
public class WeatherHandler : ContentHandler<WeatherHandler>
{
    public class MeteoriteAssets(CRMod mod, string filePath) : AssetBundleLoader<MeteoriteAssets>(mod, filePath)
    {
        [LoadFromBundle("BetterCrater.prefab")]
        public GameObject CraterPrefab { get; private set; } = null!;

        [LoadFromBundle("BigExplosion.prefab")]
        public GameObject ExplosionPrefab { get; private set; } = null!;

        [LoadFromBundle("NewMeteor.prefab")]
        public GameObject MeteorPrefab { get; private set; } = null!;

        [LoadFromBundle("Meteor.prefab")]
        public GameObject FloatingMeteorPrefab { get; private set; } = null!;
    }

    public class TornadoAssets(CRMod mod, string filePath) : AssetBundleLoader<TornadoAssets>(mod, filePath)
    {
    }

    public class NightShiftAssets(CRMod mod, string filePath) : AssetBundleLoader<NightShiftAssets>(mod, filePath)
    {
    }

    public class GodRaysAssets(CRMod mod, string filePath) : AssetBundleLoader<GodRaysAssets>(mod, filePath)
    {
        [LoadFromBundle("GodRayWeather.prefab")]
        public GameObject GodRayPermanentEffectPrefab { get; private set; } = null!;
    }

    public NightShiftAssets? NightShift = null;
    public MeteoriteAssets? Meteorite = null;
    public TornadoAssets? Tornado = null;
    public GodRaysAssets? GodRays = null;

    public WeatherHandler(CRMod mod) : base(mod)
    {
        RegisterContent("meteorshowerassets", out Meteorite);

        RegisterContent("tornadoassets", out Tornado);

        RegisterContent("nightshiftassets", out NightShift, Plugin.ModConfig.ConfigOxydeEnabled.Value);
    }
}