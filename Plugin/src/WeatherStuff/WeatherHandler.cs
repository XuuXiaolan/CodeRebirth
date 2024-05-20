using CodeRebirth.Misc;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.WeatherStuff;

public class WeatherHandler : ContentHandler<WeatherHandler> {
	public class WeatherAssets(string bundleName) : AssetBundleLoader<WeatherAssets>(bundleName) {
		[LoadFromBundle("Meteor.prefab")]
		public GameObject MeteorPrefab { get; private set; }
		
		[LoadFromBundle("BetterCrater.prefab")]
		public GameObject CraterPrefab { get; private set; }
		
		//[LoadFromBundle("BigExplosion.prefab")]
		//public GameObject ExplosionPrefab { get; private set; }
		
		[LoadFromBundle("MeteoriteObj")]
		public Item MeteoriteItem { get; private set; }
		
		[LoadFromBundle("MeteorContainer.prefab")]
		public GameObject MeteorEffectPrefab { get; private set; }
		
		[LoadFromBundle("MeteorShowerWeather.prefab")]
		public GameObject MeteorPermanentEffectPrefab { get; private set; }
	}

	public WeatherAssets Assets { get; private set; }
	public WeatherEffect MeteorShowerWeather { get; private set; }
	
	public WeatherHandler() {
		Assets = new WeatherAssets("coderebirthasset");

		RegisterMeteorShower();
	}

	void RegisterMeteorShower() {
		Assets.MeteoriteItem.spawnPrefab.AddComponent<ScrapValueSyncer>(); // FIXME: this really shouldn't be done here. this should just be on the prefab already lol
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