using CodeRebirth.Misc;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Modules;
using UnityEngine;
using WeatherRegistry;

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
		[LoadFromBundle("Meteor.prefab")]
		public GameObject TornadoPrefab { get; private set; }
		[LoadFromBundle("MeteorContainer.prefab")]
		public GameObject TornadoEffectPrefab { get; private set; }
		
		[LoadFromBundle("MeteorShowerWeather.prefab")]
		public GameObject TornadoPermanentEffectPrefab { get; private set; }
	}

	public WeatherAssets Assets { get; private set; }
	public Weather MeteorShowerWeather { get; private set; }
	public Weather TornadoesWeather { get; private set; }
	
	public WeatherHandler() {
		Assets = new WeatherAssets("coderebirthasset");

		RegisterMeteorShower();
		// RegisterTornadoWeather();
	}

	void RegisterTornadoWeather() {
		GameObject effectObject = GameObject.Instantiate(Assets.TornadoEffectPrefab);
		effectObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectObject);
		
		GameObject effectPermanentObject = GameObject.Instantiate(Assets.TornadoPermanentEffectPrefab);
		effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectPermanentObject);

		ImprovedWeatherEffect tornadoEffect = new(effectObject, effectPermanentObject){
			SunAnimatorBool = "eclipse",
		};
		
		TornadoesWeather = new Weather("Tornadoes", tornadoEffect) {};

		// Weathers.RegisterWeather("Tornados", TornadosWeather, Levels.LevelTypes.All, 0, 0);
	}

	void RegisterMeteorShower() {
		Plugin.samplePrefabs.Add("Meteorite", Assets.MeteoriteItem);
		
		GameObject effectObject = GameObject.Instantiate(Assets.MeteorEffectPrefab);
		effectObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectObject);
		
		GameObject effectPermanentObject = GameObject.Instantiate(Assets.MeteorPermanentEffectPrefab);
		effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectPermanentObject);

		ImprovedWeatherEffect meteorEffect = new(effectObject, effectPermanentObject){
			SunAnimatorBool = "eclipse",
		};

		MeteorShowerWeather = new Weather("Meteor Shower", meteorEffect) {};
		// Weathers.RegisterWeather("Meteor Shower", MeteorShowerWeather, Levels.LevelTypes.All, 0, 0);
	}
}