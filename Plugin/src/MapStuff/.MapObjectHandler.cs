using System;
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
	public class MapObjectAssets(string bundleName) : AssetBundleLoader<MapObjectAssets>(bundleName) {
		[LoadFromBundle("MoneyObj.asset")]
		public Item MoneyItem { get; private set; }
		
		[LoadFromBundle("Crate")]
		public GameObject ItemCratePrefab { get; private set; }
	}

	public MapObjectAssets Assets { get; private set; }
	
	public MapObjectHandler() {
		Assets = new MapObjectAssets("coderebirthasset");

		if (Plugin.ModConfig.ConfigMoneyEnabled.Value) RegisterInsideMoney();
		// if (Plugin.ModConfig.ConfigItemCrateEnabled.Value) RegisterOutsideCrates();
	}
	public void RegisterOutsideCrates() {
		SpawnableOutsideObjectDef outsideObjDefBug = ScriptableObject.CreateInstance<SpawnableOutsideObjectDef>();
		outsideObjDefBug.spawnableMapObject = new SpawnableOutsideObjectWithRarity();
		outsideObjDefBug.spawnableMapObject.spawnableObject = ScriptableObject.CreateInstance<SpawnableOutsideObject>();
		outsideObjDefBug.spawnableMapObject.spawnableObject.prefabToSpawn = Assets.ItemCratePrefab;
		MapObjects.RegisterOutsideObject(outsideObjDefBug, Levels.LevelTypes.All, (level) => new AnimationCurve(new Keyframe(0, 5), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigCrateAbundance.Value, 0, 1000))));
	}
	public void RegisterInsideMoney() {
		Assets.MoneyItem.spawnPrefab.GetComponent<Money>().SetScrapValue(-1);
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = Assets.MoneyItem.spawnPrefab;
		if (Plugin.ModConfig.ConfigWalletEnabled.Value) MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigMoneyAbundance.Value, 0, 1000))));
	}
}