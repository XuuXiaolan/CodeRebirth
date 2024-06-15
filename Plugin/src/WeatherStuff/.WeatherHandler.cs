using CodeRebirth.Misc;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.WeatherStuff;

public class WeatherHandler : ContentHandler<WeatherHandler> {
	public class WeatherAssets(string bundleName) : AssetBundleLoader<WeatherAssets>(bundleName) {
		
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
		[LoadFromBundle("TornadoMain.prefab")]
		public GameObject TornadoPrefab { get; private set; }
		[LoadFromBundle("TornadoContainer.prefab")]
		public GameObject TornadoEffectPrefab { get; private set; }
		
		[LoadFromBundle("TornadoWeather.prefab")]
		public GameObject TornadoPermanentEffectPrefab { get; private set; }
	}

	public WeatherAssets Assets { get; private set; }
	public WeatherEffect MeteorShowerWeather { get; private set; }
	public WeatherEffect TornadosWeather { get; private set; }
	
	public WeatherHandler() {
		Assets = new WeatherAssets("coderebirthasset");

		RegisterMeteorShower();
		RegisterTornadoWeather();
	}

	void RegisterTornadoWeather() {
		GameObject effectObject = GameObject.Instantiate(Assets.TornadoEffectPrefab);
		effectObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectObject);
		
		GameObject effectPermanentObject = GameObject.Instantiate(Assets.TornadoPermanentEffectPrefab);
		effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectPermanentObject);
		TornadosWeather = new WeatherEffect() {
			name = "Windy",
			effectObject = effectObject,
			effectPermanentObject = effectPermanentObject,
			lerpPosition = false,
			sunAnimatorBool = "eclipse",
			transitioning = false
			};
		Weathers.RegisterWeather("Windy", TornadosWeather, Levels.LevelTypes.All, 0, 0);
	}
	void RegisterMeteorShower() {
		Plugin.samplePrefabs.Add("Meteorite", Assets.MeteoriteItem);
		

		GameObject effectObject = GameObject.Instantiate(Assets.MeteorEffectPrefab);
		effectObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectObject);
		
		GameObject effectPermanentObject = GameObject.Instantiate(Assets.MeteorPermanentEffectPrefab);
		effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectPermanentObject);
		MeteorShowerWeather = new WeatherEffect() {
			name = "MeteorShower",
			effectObject = effectObject,
			effectPermanentObject = effectPermanentObject,
			lerpPosition = false,
			sunAnimatorBool = "eclipse",
			transitioning = false
			};
		Weathers.RegisterWeather("Meteor Shower", MeteorShowerWeather, Levels.LevelTypes.All, 0, 0);
	}
}