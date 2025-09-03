using Dawn.Dusk;
using UnityEngine;

namespace CodeRebirth.src.Content.Weathers;
public class WeatherHandler : ContentHandler<WeatherHandler>
{
    public class MeteoriteAssets(DuskMod mod, string filePath) : AssetBundleLoader<MeteoriteAssets>(mod, filePath)
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

    public class TornadoAssets(DuskMod mod, string filePath) : AssetBundleLoader<TornadoAssets>(mod, filePath)
    {
    }

    public class NightShiftAssets(DuskMod mod, string filePath) : AssetBundleLoader<NightShiftAssets>(mod, filePath)
    {
    }

    public class GodRaysAssets(DuskMod mod, string filePath) : AssetBundleLoader<GodRaysAssets>(mod, filePath)
    {
        [LoadFromBundle("GodRayWeather.prefab")]
        public GameObject GodRayPermanentEffectPrefab { get; private set; } = null!;
    }

    public NightShiftAssets? NightShift = null;
    public MeteoriteAssets? Meteorite = null;
    public TornadoAssets? Tornado = null;
    public GodRaysAssets? GodRays = null;

    public WeatherHandler(DuskMod mod) : base(mod)
    {
        RegisterContent("meteorshowerassets", out Meteorite);

        RegisterContent("tornadoassets", out Tornado);

        RegisterContent("nightshiftassets", out NightShift, Plugin.ModConfig.ConfigOxydeEnabled.Value);
    }
}