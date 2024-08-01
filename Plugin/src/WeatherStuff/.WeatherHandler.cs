﻿using System.Runtime.CompilerServices;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using UnityEngine;
using WeatherRegistry;

namespace CodeRebirth.WeatherStuff;

public class WeatherHandler : ContentHandler<WeatherHandler> {
    public class MeteoriteAssets(string bundleName) : AssetBundleLoader<MeteoriteAssets>(bundleName) {
        [LoadFromBundle("BetterCrater.prefab")]
        public GameObject CraterPrefab { get; private set; } = null!;

        [LoadFromBundle("BigExplosion.prefab")]
        public GameObject ExplosionPrefab { get; private set; } = null!;

        [LoadFromBundle("MeteoriteObj")]
        public Item MeteoriteItem { get; private set; } = null!;

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

    public MeteoriteAssets Meteorite { get; private set; } = null!;
    public TornadoAssets Tornado { get; private set; } = null!;
    public Weather MeteorShowerWeather { get; private set; } = null!;
    public Weather TornadoesWeather { get; private set; } = null!;

    public WeatherHandler() {
        if (Plugin.ModConfig.ConfigMeteorShowerEnabled.Value) RegisterMeteorShower();
        if (Plugin.ModConfig.ConfigTornadosEnabled.Value) RegisterTornadoWeather();
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
            DefaultWeight = Plugin.ModConfig.ConfigTornadoWeatherWeight.Value,
            ScrapAmountMultiplier = Plugin.ModConfig.ConfigTornadoScrapAmountMultiplier.Value,
            ScrapValueMultiplier = Plugin.ModConfig.ConfigTornadoScrapValueMultiplier.Value,
            DefaultLevelFilters = new string[] { "Gordion" },
            LevelFilteringOption = FilteringOption.Exclude,
            Color = UnityEngine.Color.gray,
        };
        RegisterEnemyWithConfig("All:0", Tornado.TornadoObj, Tornado.TornadoTerminalNode, Tornado.TornadoTerminalKeyword, 1, 10);
        WeatherManager.RegisterWeather(TornadoesWeather);
    }

    private void RegisterMeteorShower() {
        Meteorite = new MeteoriteAssets("meteorshowerassets");
        Plugin.samplePrefabs.Add("Meteorite", Meteorite.MeteoriteItem);
        RegisterScrapWithConfig("All:0", Meteorite.MeteoriteItem);

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
            DefaultWeight = Plugin.ModConfig.ConfigMeteorWeatherWeight.Value,
            ScrapAmountMultiplier = Plugin.ModConfig.ConfigMeteorScrapAmountMultiplier.Value,
            ScrapValueMultiplier = Plugin.ModConfig.ConfigMeteorScrapValueMultiplier.Value,
            DefaultLevelFilters = new string[] { "Gordion" },
            LevelFilteringOption = FilteringOption.Exclude,
            Color = new Color(0.5f, 0f, 0f, 1f),

        };

        WeatherManager.RegisterWeather(MeteorShowerWeather);
    }
}