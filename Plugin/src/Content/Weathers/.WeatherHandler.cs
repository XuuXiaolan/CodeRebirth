﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;
using WeatherRegistry;

namespace CodeRebirth.src.Content.Weathers;
public class WeatherHandler : ContentHandler<WeatherHandler> {
    public class MeteoriteAssets(string bundleName) : AssetBundleLoader<MeteoriteAssets>(bundleName) {
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

        [LoadFromBundle("Meteor.prefab")]
        public GameObject MeteorPrefab { get; private set; } = null!;
        
        [LoadFromBundle("MeteorContainer.prefab")]
        public GameObject MeteorEffectPrefab { get; private set; } = null!;
        
        [LoadFromBundle("MeteorShowerWeather.prefab")]
        public GameObject MeteorPermanentEffectPrefab { get; private set; } = null!;
    }

    public class TornadoAssets(string bundleName) : AssetBundleLoader<TornadoAssets>(bundleName) {
        [LoadFromBundle("TornadoObj.asset")]
        public EnemyType TornadoObj { get; private set; } = null!;
        
        [LoadFromBundle("TornadoContainer.prefab")]
        public GameObject TornadoEffectPrefab { get; private set; } = null!;
        
        [LoadFromBundle("TornadoWeather.prefab")]
        public GameObject TornadoPermanentEffectPrefab { get; private set; } = null!;
        
        [LoadFromBundle("TornadoTN.asset")]
        public TerminalNode TornadoTerminalNode { get; private set; } = null!;

        [LoadFromBundle("TornadoTK.asset")]
        public TerminalKeyword TornadoTerminalKeyword { get; private set; } = null!;
    }

    public class GodRaysAssets(string bundleName) : AssetBundleLoader<GodRaysAssets>(bundleName) { 
        [LoadFromBundle("GodRayWeather.prefab")]
        public GameObject GodRayPermanentEffectPrefab { get; private set; } = null!;
    }

    public MeteoriteAssets Meteorite { get; private set; } = null!;
    public TornadoAssets Tornado { get; private set; } = null!;
    public GodRaysAssets GodRays { get; private set; } = null!;
    public Weather MeteorShowerWeather { get; private set; } = null!;
    public Weather TornadoesWeather { get; private set; } = null!;
    public Weather GodRaysWeather { get; private set; } = null!;

    public WeatherHandler() {
        if (Plugin.ModConfig.ConfigMeteorShowerEnabled.Value) RegisterMeteorShower();
        if (Plugin.ModConfig.ConfigTornadosEnabled.Value) RegisterTornadoWeather();
        if (true) RegisterGodRaysWeather();
    }

    private void RegisterGodRaysWeather() {
        GodRays = new GodRaysAssets("godraysassets");
                
        GameObject effectPermanentObject = GameObject.Instantiate(GodRays.GodRayPermanentEffectPrefab);
        effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectPermanentObject);

        ImprovedWeatherEffect godRayEffect = new(null, effectPermanentObject) {
            SunAnimatorBool = "",
        };

        GodRaysWeather = new Weather("Red Sun", godRayEffect) {
            DefaultWeight = 50,
            DefaultLevelFilters = ["Gordion"],
            LevelFilteringOption = FilteringOption.Exclude,
            Color = UnityEngine.Color.red,
        };
        WeatherManager.RegisterWeather(GodRaysWeather);
    }

    private void RegisterTornadoWeather() {
        Tornado = new TornadoAssets("tornadoassets");
        GameObject effectObject = GameObject.Instantiate(Tornado.TornadoEffectPrefab);
        effectObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectObject);

        GameObject effectPermanentObject = GameObject.Instantiate(Tornado.TornadoPermanentEffectPrefab);
        effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectPermanentObject);

        ImprovedWeatherEffect tornadoEffect = new(effectObject, effectPermanentObject) {
            SunAnimatorBool = "overcast",
        };

        TornadoesWeather = new Weather("Windy", tornadoEffect) {
            DefaultWeight = 50,
            DefaultLevelFilters = new string[] { "Gordion" },
            LevelFilteringOption = FilteringOption.Exclude,
            Color = UnityEngine.Color.gray,
        };
        RegisterEnemyWithConfig("All:0", Tornado.TornadoObj, Tornado.TornadoTerminalNode, Tornado.TornadoTerminalKeyword, 1, 10);
        WeatherManager.RegisterWeather(TornadoesWeather);
    }

    private void RegisterMeteorShower() {
        Meteorite = new MeteoriteAssets("meteorshowerassets");
        Plugin.samplePrefabs.Add("Sapphire Meteorite", Meteorite.SapphireMeteoriteItem);
        RegisterScrapWithConfig("All:0", Meteorite.SapphireMeteoriteItem);
        Plugin.samplePrefabs.Add("Emerald Meteorite", Meteorite.EmeraldMeteoriteItem);
        RegisterScrapWithConfig("All:0", Meteorite.EmeraldMeteoriteItem);
        Plugin.samplePrefabs.Add("Ruby Meteorite", Meteorite.RubyMeteoriteItem);
        RegisterScrapWithConfig("All:0", Meteorite.RubyMeteoriteItem);

        GameObject effectObject = GameObject.Instantiate(Meteorite.MeteorEffectPrefab);
        effectObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectObject);

        GameObject effectPermanentObject = GameObject.Instantiate(Meteorite.MeteorPermanentEffectPrefab);
        effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
        GameObject.DontDestroyOnLoad(effectPermanentObject);

        ImprovedWeatherEffect meteorEffect = new(effectObject, effectPermanentObject) {
            SunAnimatorBool = "eclipse",
        };

        MeteorShowerWeather = new Weather("Meteor Shower", meteorEffect) {
            DefaultWeight = 50,
            DefaultLevelFilters = new string[] { "Gordion" },
            LevelFilteringOption = FilteringOption.Exclude,
            Color = new Color(0.5f, 0f, 0f, 1f),

        };

        WeatherManager.RegisterWeather(MeteorShowerWeather);
    }
}