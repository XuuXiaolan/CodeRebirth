﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;
using WeatherRegistry;

namespace CodeRebirth.src.Content.Weathers;
public class WeatherHandler : ContentHandler<WeatherHandler>
{
    public class MeteoriteAssets(string bundleName) : AssetBundleLoader<MeteoriteAssets>(bundleName)
    {
        [LoadFromBundle("BetterCrater.prefab")]
        public GameObject CraterPrefab { get; private set; } = null!;

        [LoadFromBundle("BigExplosion.prefab")]
        public GameObject ExplosionPrefab { get; private set; } = null!;

        [LoadFromBundle("SapphireMeteoriteObj")]
        public Item SapphireMeteoriteItem { get; private set; } = null!;

        [LoadFromBundle("EmeraldMeteoriteObj")]
        public Item EmeraldMeteoriteItem { get; private set; } = null!;

        [LoadFromBundle("RubyMeteoriteObj")]
        public Item RubyMeteoriteItem { get; private set; } = null!;

        [LoadFromBundle("NewMeteor.prefab")]
        public GameObject MeteorPrefab { get; private set; } = null!;
        
        [LoadFromBundle("Meteor.prefab")]
        public GameObject FloatingMeteorPrefab { get; private set; } = null!;
        
        [LoadFromBundle("MeteorContainer.prefab")]
        public GameObject MeteorEffectPrefab { get; private set; } = null!;
        
        [LoadFromBundle("MeteorShowerWeather.prefab")]
        public GameObject MeteorPermanentEffectPrefab { get; private set; } = null!;
    }

    public class TornadoAssets(string bundleName) : AssetBundleLoader<TornadoAssets>(bundleName)
    {
        [LoadFromBundle("TornadoObj.asset")]
        public EnemyType TornadoObj { get; private set; } = null!;

        [LoadFromBundle("TornadoContainer.prefab")]
        public GameObject TornadoEffectPrefab { get; private set; } = null!;

        [LoadFromBundle("TornadoWeather.prefab")]
        public GameObject TornadoPermanentEffectPrefab { get; private set; } = null!;

        [LoadFromBundle("HurricaneObj.asset")]
        public EnemyType HurricaneObj { get; private set; } = null!;

        [LoadFromBundle("HurricaneContainer.prefab")]
        public GameObject HurricaneEffectPrefab { get; private set; } = null!;

        [LoadFromBundle("HurricaneWeather.prefab")]
        public GameObject HurricanePermanentEffectPrefab { get; private set; } = null!;

        [LoadFromBundle("FireStormObj.asset")]
        public EnemyType FireStormObj { get; private set; } = null!;

        [LoadFromBundle("FireStormContainer.prefab")]
        public GameObject FireStormEffectPrefab { get; private set; } = null!;

        [LoadFromBundle("FireStormWeather.prefab")]
        public GameObject FireStormPermanentEffectPrefab { get; private set; } = null!;
    }

    public class GodRaysAssets(string bundleName) : AssetBundleLoader<GodRaysAssets>(bundleName)
    { 
        [LoadFromBundle("GodRayWeather.prefab")]
        public GameObject GodRayPermanentEffectPrefab { get; private set; } = null!;
    }

    public MeteoriteAssets Meteorite { get; private set; } = null!;
    public TornadoAssets Tornado { get; private set; } = null!;
    public GodRaysAssets GodRays { get; private set; } = null!;
    public Weather MeteorShowerWeather { get; private set; } = null!;
    public Weather TornadoWeather { get; private set; } = null!;
    public Weather HurricaneWeather { get; private set; } = null!;
    public Weather FireStormWeather { get; private set; } = null!;
    public Weather GodRaysWeather { get; private set; } = null!;

    public WeatherHandler()
    {
        if (Plugin.ModConfig.ConfigMeteorShowerEnabled.Value) RegisterMeteorShower();
        if (Plugin.ModConfig.ConfigTornadosEnabled.Value) RegisterTornadoWeather();
        if (false) RegisterGodRaysWeather();
    }

    private void RegisterGodRaysWeather()
    {
        GodRays = new GodRaysAssets("godrayassets");
                
        GameObject effectPermanentObject = GameObject.Instantiate(GodRays.GodRayPermanentEffectPrefab);
        effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectPermanentObject);

        ImprovedWeatherEffect godRayEffect = new(null, effectPermanentObject)
        {
            SunAnimatorBool = "",
        };

        GodRaysWeather = new Weather("When Day Breaks", godRayEffect)
        {
            DefaultWeight = 50,
            DefaultLevelFilters = ["Gordion"],
            LevelFilteringOption = FilteringOption.Exclude,
            Color = UnityEngine.Color.red,
        };
        WeatherManager.RegisterWeather(GodRaysWeather);
    }

    private void RegisterTornadoWeather()
    {
        Tornado = new TornadoAssets("tornadoassets");
        RegisterRegularTornado();
        // RegisterHurricane();
        // RegisterFireStorm();
    }

    private void RegisterRegularTornado()
    {
        GameObject effectObject = GameObject.Instantiate(Tornado.TornadoEffectPrefab);
        effectObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectObject);

        GameObject effectPermanentObject = GameObject.Instantiate(Tornado.TornadoPermanentEffectPrefab);
        effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectPermanentObject);

        ImprovedWeatherEffect tornadoEffect = new(effectObject, effectPermanentObject)
        {
            SunAnimatorBool = "overcast",
        };

        TornadoWeather = new Weather("Tornado", tornadoEffect)
        {
            DefaultWeight = 10,
            DefaultLevelFilters = new string[] { "Gordion", "Galetry" },
            LevelFilteringOption = FilteringOption.Exclude,
            Color = new Color(0.6f, 0.6f, 0.6f, 1f),
        };
        RegisterEnemyWithConfig("All:0", Tornado.TornadoObj, null, null, 1, 10);
        WeatherManager.RegisterWeather(TornadoWeather);
    }

    private void RegisterHurricane()
    {
        GameObject effectObject = GameObject.Instantiate(Tornado.HurricaneEffectPrefab);
        effectObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectObject);

        GameObject effectPermanentObject = GameObject.Instantiate(Tornado.HurricanePermanentEffectPrefab);
        effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectPermanentObject);

        ImprovedWeatherEffect HurricaneEffect = new(effectObject, effectPermanentObject)
        {
            SunAnimatorBool = "overcast",
        };

        HurricaneWeather = new Weather("Hurricane", HurricaneEffect)
        {
            DefaultWeight = 10,
            DefaultLevelFilters = new string[] { "Gordion", "Galetry" },
            LevelFilteringOption = FilteringOption.Exclude,
            Color = new Color(0f, 0f, 0.75f, 1f),
        };
        RegisterEnemyWithConfig("All:0", Tornado.HurricaneObj, null, null, 1, 10);
        WeatherManager.RegisterWeather(HurricaneWeather);
    }

    private void RegisterFireStorm()
    {
        GameObject effectObject = GameObject.Instantiate(Tornado.FireStormEffectPrefab);
        effectObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectObject);

        GameObject effectPermanentObject = GameObject.Instantiate(Tornado.FireStormPermanentEffectPrefab);
        effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectPermanentObject);

        ImprovedWeatherEffect FireStormEffect = new(effectObject, effectPermanentObject)
        {
            SunAnimatorBool = "eclipse",
        };

        FireStormWeather = new Weather("Fire Storm", FireStormEffect)
        {
            DefaultWeight = 10,
            DefaultLevelFilters = new string[] { "Gordion", "Galetry" },
            LevelFilteringOption = FilteringOption.Exclude,
            Color = UnityEngine.Color.red,
        };
        RegisterEnemyWithConfig("All:0", Tornado.FireStormObj, null, null, 1, 10);
        WeatherManager.RegisterWeather(FireStormWeather);
    }

    private void RegisterMeteorShower()
    {
        Meteorite = new MeteoriteAssets("meteorshowerassets");
        
        RegisterShopItemWithConfig(false, true, Meteorite.SapphireMeteoriteItem, null, 0, "", Plugin.ModConfig.ConfigSapphireWorth.Value);
        RegisterShopItemWithConfig(false, true, Meteorite.EmeraldMeteoriteItem, null, 0, "", Plugin.ModConfig.ConfigEmeraldWorth.Value);
        RegisterShopItemWithConfig(false, true, Meteorite.RubyMeteoriteItem, null, 0, "", Plugin.ModConfig.ConfigRubyWorth.Value);

        GameObject effectObject = GameObject.Instantiate(Meteorite.MeteorEffectPrefab);
        effectObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectObject);

        GameObject effectPermanentObject = GameObject.Instantiate(Meteorite.MeteorPermanentEffectPrefab);
        effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectPermanentObject);

        ImprovedWeatherEffect meteorEffect = new(effectObject, effectPermanentObject)
        {
            SunAnimatorBool = "eclipse",
        };

        MeteorShowerWeather = new Weather("Meteor Shower", meteorEffect)
        {
            DefaultWeight = 30,
            DefaultLevelFilters = new string[] { "Gordion", "Galetry" },
            ScrapAmountMultiplier = 1.2f,
            ScrapValueMultiplier = 1.2f,
            LevelFilteringOption = FilteringOption.Exclude,
            Color = new Color(0.5f, 0f, 0f, 1f),

        };

        WeatherManager.RegisterWeather(MeteorShowerWeather);
    }
}