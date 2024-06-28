using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using CodeRebirth.Misc;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Modules;
using UnityEngine;
using WeatherRegistry;

namespace CodeRebirth.WeatherStuff;

public class WeatherHandler : ContentHandler<WeatherHandler> {
    public class MeteoriteAssets(string bundleName) : AssetBundleLoader<MeteoriteAssets>(bundleName) {
        [LoadFromBundle("BetterCrater.prefab")]
        public GameObject CraterPrefab { get; private set; }
        [LoadFromBundle("BigExplosion.prefab")]
        public GameObject ExplosionPrefab { get; private set; }

    
        [LoadFromBundle("MeteoriteObj")]
        public Item MeteoriteItem { get; private set; }
        
        [LoadFromBundle("WesleyCubeMeteor.prefab")]
        public GameObject WesleyModePrefab { get; private set; }
        
        [LoadFromBundle("Meteor.prefab")]
        public GameObject MeteorPrefab { get; private set; }
        
        [LoadFromBundle("MeteorContainer.prefab")]
        public GameObject MeteorEffectPrefab { get; private set; }
        
        [LoadFromBundle("MeteorShowerWeather.prefab")]
        public GameObject MeteorPermanentEffectPrefab { get; private set; }
    }

    public class TornadoAssets(string bundleName) : AssetBundleLoader<TornadoAssets>(bundleName) {
        [LoadFromBundle("TornadoMain.prefab")]
        public GameObject TornadoPrefab { get; private set; }
        
        [LoadFromBundle("TornadoContainer.prefab")]
        public GameObject TornadoEffectPrefab { get; private set; }
        
        [LoadFromBundle("TornadoWeather.prefab")]
        public GameObject TornadoPermanentEffectPrefab { get; private set; }
    }

    public MeteoriteAssets Meteorite { get; private set; }
    public TornadoAssets Tornado { get; private set; }
    public Weather MeteorShowerWeather { get; private set; }
    public Weather TornadoesWeather { get; private set; }

    public WeatherHandler() {
        Meteorite = new MeteoriteAssets("coderebirthasset");
        Tornado = new TornadoAssets("coderebirthasset");

        if (Plugin.ModConfig.ConfigMeteorShowerEnabled.Value) RegisterMeteorShower();
        if (Plugin.ModConfig.ConfigTornadosEnabled.Value) RegisterTornadoWeather();
    }

    private void RegisterTornadoWeather() {
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
            DefaultLevelFilters = new[] { "Gordion" },
            LevelFilteringOption = FilteringOption.Exclude,
            Color = UnityEngine.Color.gray,
        };

        WeatherRegistry.WeatherManager.RegisterWeather(TornadoesWeather);
    }

    private void RegisterMeteorShower() {
        Plugin.samplePrefabs.Add("Meteorite", Meteorite.MeteoriteItem);

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
            DefaultLevelFilters = new[] { "Gordion" },
            LevelFilteringOption = FilteringOption.Exclude,
            Color = new Color(0.5f, 0f, 0f, 1f),
        };

        WeatherRegistry.WeatherManager.RegisterWeather(MeteorShowerWeather);
    }
}