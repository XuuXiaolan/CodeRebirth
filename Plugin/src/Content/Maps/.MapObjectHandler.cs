﻿using System;
using System.Collections.Generic;
using CodeRebirth.src.Patches;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util.AssetLoading;
using LethalLib.Extras;
using LethalLib.Modules;
using UnityEngine;
using CodeRebirth.src.Util;

namespace CodeRebirth.src.Content.Maps;
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
		public GameObject AllFloraPrefab { get; private set; } = null!;
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

	public class BiomeAssets(string bundleName) : AssetBundleLoader<BiomeAssets>(bundleName) {
		[LoadFromBundle("BiomeSpreader.prefab")]
		public GameObject BiomePrefab { get; private set; } = null!;
	}

	public MoneyAssets Money { get; private set; } = null!;
	public CrateAssets Crate { get; private set; } = null!;
	public FloraAssets Flora { get; private set; } = null!;
	public DevilDealAssets DevilDeal { get; private set; } = null!;
	public BiomeAssets Biome { get; private set; } = null!;

	public static Dictionary<string, GameObject> DevilDealPrefabs = new Dictionary<string, GameObject>();

    public MapObjectHandler() {
		
		if (Plugin.ModConfig.ConfigItemCrateEnabled.Value)
			Crate = new CrateAssets("crateassets");

		if (Plugin.ModConfig.ConfigFloraEnabled.Value) RegisterOutsideFlora();

		if (Plugin.ModConfig.ConfigMoneyEnabled.Value) RegisterInsideMoney();

		if (Plugin.ModConfig.ConfigBiomesEnabled.Value)
			Biome = new BiomeAssets("biomeassets");

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
		Flora floraStuff = Flora.AllFloraPrefab.GetComponent<Flora>();
		
		string[] grassMoonList = MapObjectConfigParsing(Plugin.ModConfig.ConfigFloraGrassSpawnPlaces.Value);
		string[] desertMoonList = MapObjectConfigParsing(Plugin.ModConfig.ConfigFloraDesertSpawnPlaces.Value);
		string[] snowMoonList = MapObjectConfigParsing(Plugin.ModConfig.ConfigFloraSnowSpawnPlaces.Value);
		string[] dangerMoonList = MapObjectConfigParsing(Plugin.ModConfig.ConfigFloraDangerSpawnPlaces.Value);
		string[] moonBlackList = MapObjectConfigParsing(Plugin.ModConfig.ConfigFloraExcludeSpawnPlaces.Value);

		foreach (var flora in floraStuff.Desert) {
			RegisterFlora(flora, new AnimationCurve(new Keyframe(0, Math.Clamp(Plugin.ModConfig.ConfigFloraMinAbundance.Value, 0, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), new Keyframe(1, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), desertMoonList, FloraTag.Desert, moonBlackList);
		}

		foreach (var flora in floraStuff.Snowy) {
			RegisterFlora(flora, new AnimationCurve(new Keyframe(0, Math.Clamp(Plugin.ModConfig.ConfigFloraMinAbundance.Value, 0, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), new Keyframe(1, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), snowMoonList, FloraTag.Snow, moonBlackList);
		}

		foreach (var flora in floraStuff.Grass) {
			RegisterFlora(flora, new AnimationCurve(new Keyframe(0, Math.Clamp(Plugin.ModConfig.ConfigFloraMinAbundance.Value, 0, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), new Keyframe(1, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), grassMoonList, FloraTag.Grass, moonBlackList);
		}

		/*foreach (var flora in floraStuff.DangerousSpecies) {
			RegisterFlora(flora, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 3)), dangerMoonList, FloraTag.Dangerous, moonBlackList);
		}*/
	}

	public void RegisterFlora(GameObject prefab, AnimationCurve curve, string[] moonWhiteList, FloraTag tag, string[] moonBlackList) {
		RoundManagerPatch.spawnableFlora.Add(new SpawnableFlora() {
			prefab = prefab,
			moonsWhiteList = moonWhiteList,
			spawnCurve = curve,
			blacklistedTags = ["Metal", "Wood", "Concrete", "Puddle", "Aluminum", "Catwalk", "Bush", "Rock", "MoldSpore"],
			floraTag = tag,
			moonsBlackList = moonBlackList
		});
	}
	
	public void RegisterDevilDeal() {
		DevilDeal = new DevilDealAssets("devildealassets");
		DevilDealPrefabs.Add("Devil", DevilDeal.DevilPrefab);
		DevilDealPrefabs.Add("DevilChair", DevilDeal.DevilChairPrefab);
		DevilDealPrefabs.Add("DevilTable", DevilDeal.DevilTablePrefab);
		DevilDealPrefabs.Add("PlayerChair", DevilDeal.PlayerChairPrefab);
	}
}