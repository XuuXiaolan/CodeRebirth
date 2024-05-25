using CodeRebirth.Misc;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Modules;
using UnityEngine;
using WeatherAPI;

namespace CodeRebirth.WeatherStuff;

public class WeatherHandler : ContentHandler<WeatherHandler> {
	public class WeatherAssets(string bundleName) : AssetBundleLoader<WeatherAssets>(bundleName) {
		[LoadFromBundle("Meteor.prefab")]
		public GameObject MeteorPrefab { get; private set; }
		
		[LoadFromBundle("BetterCrater.prefab")]
		public GameObject CraterPrefab { get; private set; }
		
		[LoadFromBundle("BigExplosion.prefab")]
		public GameObject ExplosionPrefab { get; private set; }
		
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
		Plugin.samplePrefabs.Add("Meteorite", Assets.MeteoriteItem);
		

		GameObject effectObject = GameObject.Instantiate(Assets.MeteorEffectPrefab);
		effectObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectObject);
		
		GameObject effectPermanentObject = GameObject.Instantiate(Assets.MeteorPermanentEffectPrefab);
		effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
		GameObject.DontDestroyOnLoad(effectPermanentObject);
		
		WeatherApiEffect weatherEffect = new WeatherApiEffect(effectObject, effectPermanentObject){
			name = "MeteorShower",
			SunAnimatorBool = "eclipse",
			DefaultVariable1= 0,
			DefaultVariable2= 0,
		};

		Weather MeteorShowerWeather = new("Meteor Shower", weatherEffect){
			Color = new Color(0.5f, 0.5f, 0.5f),
			DefaultWeight = 60,
			ScrapAmountMultiplier = 2,
			ScrapValueMultiplier = 1,
		};
	}
}