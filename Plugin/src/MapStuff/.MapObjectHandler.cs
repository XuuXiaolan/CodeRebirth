﻿using System;
using System.Collections.Generic;
using CodeRebirth.Misc;
using CodeRebirth.ScrapStuff;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Extras;
using LethalLib.Modules;
using Unity.Mathematics;
using UnityEngine;

namespace CodeRebirth.MapStuff;

public class MapObjectHandler : ContentHandler<MapObjectHandler> {
	public class MoneyAssets(string bundleName) : AssetBundleLoader<MoneyAssets>(bundleName) {
		[LoadFromBundle("MoneyObj.asset")]
		public Item MoneyItem { get; private set; }
	}

	public class CrateAssets(string bundleName) : AssetBundleLoader<CrateAssets>(bundleName) {
		[LoadFromBundle("Crate")]
		public GameObject ItemCratePrefab { get; private set; }
	}

	public class DevilDealAssets(string bundleName) : AssetBundleLoader<DevilDealAssets>(bundleName) {
		[LoadFromBundle("Devil.prefab")]
		public GameObject DevilPrefab { get; private set; }

		[LoadFromBundle("DevilChair.prefab")]
		public GameObject DevilChairPrefab { get; private set; }

		[LoadFromBundle("DevilTable.prefab")]
		public GameObject DevilTablePrefab { get; private set; }

		[LoadFromBundle("playerChair.prefab")]
		public GameObject PlayerChairPrefab { get; private set; }
	}

	public MoneyAssets Money { get; private set; }
	public CrateAssets Crate { get; private set; }
	public DevilDealAssets DevilDeal { get; private set; }
	public static Dictionary<string, GameObject> DevilDealPrefabs = new Dictionary<string, GameObject>();

    public MapObjectHandler() {
		
		if(Plugin.ModConfig.ConfigItemCrateEnabled.Value)
			Crate = new CrateAssets("crateassets");

		if (Plugin.ModConfig.ConfigMoneyEnabled.Value) RegisterInsideMoney();

		if (true) RegisterDevilDeal();
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

	public void RegisterDevilDeal() {
		DevilDeal = new DevilDealAssets("devildealassets");
		DevilDealPrefabs.Add("Devil", DevilDeal.DevilPrefab);
		DevilDealPrefabs.Add("DevilChair", DevilDeal.DevilChairPrefab);
		DevilDealPrefabs.Add("DevilTable", DevilDeal.DevilTablePrefab);
		DevilDealPrefabs.Add("PlayerChair", DevilDeal.PlayerChairPrefab);
	}
}