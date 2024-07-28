﻿using System.Collections.Generic;
using System.Linq;
using CodeRebirth.ScrapStuff;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Extras;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.MapStuff;

public class MapObjectHandler : ContentHandler<MapObjectHandler> {
	public class MoneyAssets(string bundleName) : AssetBundleLoader<MoneyAssets>(bundleName) {
		[LoadFromBundle("MoneyObj.asset")]
		public Item MoneyItem { get; private set; } = null!;
	}

	public class CrateAssets(string bundleName) : AssetBundleLoader<CrateAssets>(bundleName) {
		[LoadFromBundle("Crate")]
		public GameObject ItemCratePrefab { get; private set; } = null!;
		
		[LoadFromBundle("Metal Crate")]
		public GameObject MetalCratePrefab { get; private set; } = null!;
	}

	public class FloraAssets(string bundleName) : AssetBundleLoader<FloraAssets>(bundleName) {
		[LoadFromBundle("AllFlora.prefab")]
		public GameObject Flora { get; private set; } = null!;
	}

	public class DevilDealAssets(string bundleName) : AssetBundleLoader<DevilDealAssets>(bundleName) {
		[LoadFromBundle("Devil.prefab")]
		public GameObject DevilPrefab { get; private set; } = null!;

		[LoadFromBundle("DevilChair.prefab")]
		public GameObject DevilChairPrefab { get; private set; } = null!;

		[LoadFromBundle("DevilTable.prefab")]
		public GameObject DevilTablePrefab { get; private set; } = null!;

		[LoadFromBundle("playerChair.prefab")]
		public GameObject PlayerChairPrefab { get; private set; } = null!;
	}

	public MoneyAssets Money { get; private set; } = null!;
	public CrateAssets Crate { get; private set; } = null!;
	public FloraAssets Flora { get; private set; } = null!;
	public DevilDealAssets DevilDeal { get; private set; } = null!;
	public static Dictionary<string, GameObject> DevilDealPrefabs = new Dictionary<string, GameObject>();

    public MapObjectHandler() {
		
		if (Plugin.ModConfig.ConfigItemCrateEnabled.Value)
			Crate = new CrateAssets("crateassets");

		if (Plugin.ModConfig.ConfigFloraEnabled.Value) RegisterOutsideFlora();

		if (Plugin.ModConfig.ConfigMoneyEnabled.Value) RegisterInsideMoney();

		if (false) RegisterDevilDeal();
	}

	public void RegisterInsideMoney() {
		Money = new MoneyAssets("moneyassets");
		Money.MoneyItem.spawnPrefab.GetComponent<Money>().SetScrapValue(-1);
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = Money.MoneyItem.spawnPrefab;
		if (Plugin.ModConfig.ConfigWalletEnabled.Value) {
			MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => 
				new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigMoneyAbundance.Value, 0, 1000)))
			);
		}
	}

	public void RegisterOutsideFlora() {
		Flora = new FloraAssets("floraassets");
		var floraStuff = Flora.Flora.GetComponent<Flora>();
		foreach (var flora in floraStuff.BluntspearB) {
			RegisterOutsideObjectWithConfig(["Grass"], 1, flora, new AnimationCurve(new Keyframe(0, 20), new Keyframe(1, 100)), Plugin.ModConfig.ConfigFloraSpawnPlaces.Value);
		}
		foreach (var flora in floraStuff.PeacockPlant) {
			RegisterOutsideObjectWithConfig(["Grass"], 1, flora, new AnimationCurve(new Keyframe(0, 20), new Keyframe(1, 100)), Plugin.ModConfig.ConfigFloraSpawnPlaces.Value);
		}
		foreach (var flora in floraStuff.BluntspearA) {
			RegisterOutsideObjectWithConfig(["Grass"], 1, flora, new AnimationCurve(new Keyframe(0, 20), new Keyframe(1, 100)), Plugin.ModConfig.ConfigFloraSpawnPlaces.Value);
		}
		foreach (var flora in floraStuff.Staright) {
			RegisterOutsideObjectWithConfig(["Grass"], 1, flora, new AnimationCurve(new Keyframe(0, 20), new Keyframe(1, 100)), Plugin.ModConfig.ConfigFloraSpawnPlaces.Value);
		}
		foreach (var flora in floraStuff.SteelSprigs) {
			RegisterOutsideObjectWithConfig(["Grass"], 1, flora, new AnimationCurve(new Keyframe(0, 20), new Keyframe(1, 100)), Plugin.ModConfig.ConfigFloraSpawnPlaces.Value);
		}
		foreach (var flora in floraStuff.Misc) {
			RegisterOutsideObjectWithConfig(["Grass"], 1, flora, new AnimationCurve(new Keyframe(0, 20), new Keyframe(1, 100)), Plugin.ModConfig.ConfigFloraSpawnPlaces.Value);
		}
	}
	public void RegisterDevilDeal() {
		DevilDeal = new DevilDealAssets("devildealassets");
		DevilDealPrefabs.Add("Devil", DevilDeal.DevilPrefab);
		DevilDealPrefabs.Add("DevilChair", DevilDeal.DevilChairPrefab);
		DevilDealPrefabs.Add("DevilTable", DevilDeal.DevilTablePrefab);
		DevilDealPrefabs.Add("PlayerChair", DevilDeal.PlayerChairPrefab);
	}
}