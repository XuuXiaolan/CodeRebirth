using CodeRebirth.Misc;
using CodeRebirth.ScrapStuff;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Extras;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.MapStuff;

public class MapObjectHandler : ContentHandler<MapObjectHandler> {
	public class MapObjectAssets(string bundleName) : AssetBundleLoader<MapObjectAssets>(bundleName) {
		[LoadFromBundle("MoneyObj.asset")]
		public Item MoneyItem { get; private set; }
	}

	public MapObjectAssets Assets { get; private set; }
	
	public MapObjectHandler() {
		Assets = new MapObjectAssets("coderebirthasset");

		Assets.MoneyItem.spawnPrefab.GetComponent<Money>().SetScrapValue(-1);
		
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = Assets.MoneyItem.spawnPrefab;
		if (Plugin.ModConfig.ConfigWalletEnabled.Value) MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Plugin.ModConfig.ConfigMoneyAbundance.Value)));
	}
}