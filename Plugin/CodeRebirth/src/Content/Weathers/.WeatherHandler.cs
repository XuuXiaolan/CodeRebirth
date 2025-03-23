﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using CullFactory;
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

        [LoadFromBundle("NewMeteor.prefab")]
        public GameObject MeteorPrefab { get; private set; } = null!;
        
        [LoadFromBundle("Meteor.prefab")]
        public GameObject FloatingMeteorPrefab { get; private set; } = null!;
    }

    public class TornadoAssets(string bundleName) : AssetBundleLoader<TornadoAssets>(bundleName)
    {
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

    public MeteoriteAssets? Meteorite { get; private set; } = null;
    public TornadoAssets? Tornado { get; private set; } = null;
    public GodRaysAssets? GodRays { get; private set; } = null;

    public WeatherHandler()
    {
        Meteorite = LoadAndRegisterAssets<MeteoriteAssets>("meteorshowerassets");
        Tornado = LoadAndRegisterAssets<TornadoAssets>("tornadoassets");
    }
}