﻿﻿using System;
using System.Collections.Generic;
using CodeRebirth.src.Patches;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util.AssetLoading;
using LethalLib.Extras;
using LethalLib.Modules;
using UnityEngine;
using CodeRebirth.src.Util;

namespace CodeRebirth.src.Content.Maps;
public class MapObjectHandler : ContentHandler<MapObjectHandler>
{
	public class MoneyAssets(string bundleName) : AssetBundleLoader<MoneyAssets>(bundleName)
	{
		[LoadFromBundle("MoneyObj.asset")]
		public Item MoneyItem { get; private set; } = null!;
	}

	public class CrateAssets(string bundleName) : AssetBundleLoader<CrateAssets>(bundleName)
	{
		[LoadFromBundle("Wooden Crate")]
		public GameObject WoodenCratePrefab { get; private set; } = null!;
		
		[LoadFromBundle("Metal Crate")]
		public GameObject MetalCratePrefab { get; private set; } = null!;
	}

	public class FloraAssets(string bundleName) : AssetBundleLoader<FloraAssets>(bundleName)
	{
		[LoadFromBundle("AllFlora.prefab")]
		public GameObject AllFloraPrefab { get; private set; } = null!;
	}

	public class BiomeAssets(string bundleName) : AssetBundleLoader<BiomeAssets>(bundleName)
	{
		[LoadFromBundle("BiomeSpreader.prefab")]
		public GameObject BiomePrefab { get; private set; } = null!;
	}

	public class BearTrapAssets(string bundleName) : AssetBundleLoader<BearTrapAssets>(bundleName)
	{
		[LoadFromBundle("GrassBearTrap.prefab")]
		public GameObject GrassMatPrefab { get; private set; } = null!;
		[LoadFromBundle("GravelBearTrap.prefab")]
		public GameObject GravelMatPrefab { get; private set; } = null!;
		[LoadFromBundle("SnowBearTrap.prefab")]
		public GameObject SnowMatPrefab { get; private set; } = null!;
	}

	public class GlowingGemAssets(string bundleName) : AssetBundleLoader<GlowingGemAssets>(bundleName)
	{
		[LoadFromBundle("LaserTurret.prefab")]
		public GameObject LaserTurretPrefab { get; private set; } = null!;
	}

	public class IndustrialFanAssets(string bundleName) : AssetBundleLoader<IndustrialFanAssets>(bundleName)
	{
		[LoadFromBundle("FanTrapAnimated.prefab")]
		public GameObject IndustrialFanPrefab { get; private set; } = null!;
	}

	public class FlashTurretAssets(string bundleName) : AssetBundleLoader<FlashTurretAssets>(bundleName)
	{
		[LoadFromBundle("FlashTurretUpdated.prefab")]
		public GameObject FlashTurretPrefab { get; private set; } = null!;
	}

	public class TeslaShockAssets(string bundleName) : AssetBundleLoader<TeslaShockAssets>(bundleName)
	{
		[LoadFromBundle("BugZapper.prefab")]
		public GameObject TeslaShockPrefab { get; private set; } = null!;

		[LoadFromBundle("ChainLightning.prefab")]
		public GameObject ChainLightningPrefab { get; private set; } = null!;
	}

	/*public class AirControlUnitAssets(string bundleName) : AssetBundleLoader<AirControlUnitAssets>(bundleName)
	{
		[LoadFromBundle("AirControlUnit.prefab")]
		public GameObject AirControlUnitPrefab { get; private set; } = null!;

		[LoadFromBundle("AirControlUnitProjectile.prefab")]
		public GameObject ProjectilePrefab { get; private set; } = null!;
	}*/

	public class FunctionalMicrowaveAssets(string bundleName) : AssetBundleLoader<FunctionalMicrowaveAssets>(bundleName)
	{
		[LoadFromBundle("FunctionalMicrowave.prefab")]
		public GameObject FunctionalMicrowavePrefab { get; private set; } = null!;
	}

	public MoneyAssets Money { get; private set; } = null!;
	public CrateAssets Crate { get; private set; } = null!;
	public FloraAssets Flora { get; private set; } = null!;
	public BiomeAssets Biome { get; private set; } = null!;
	public BearTrapAssets BearTrap { get; private set; } = null!;
	public GlowingGemAssets GlowingGem { get; private set; } = null!;
	public IndustrialFanAssets IndustrialFan { get; private set; } = null!;
	public FlashTurretAssets FlashTurret { get; private set; } = null!;
	public TeslaShockAssets TeslaShock { get; private set; } = null!;
	//public AirControlUnitAssets AirControlUnit { get; private set; } = null!;
	public FunctionalMicrowaveAssets FunctionalMicrowave { get; private set; } = null!;

	public static List<GameObject> hazardPrefabs = new List<GameObject>();

    public MapObjectHandler()
	{
		if (Plugin.ModConfig.ConfigItemCrateEnabled.Value)
			Crate = new CrateAssets("crateassets");

		if (Plugin.ModConfig.ConfigFloraEnabled.Value)
			RegisterOutsideFlora();

		if (Plugin.ModConfig.ConfigMoneyEnabled.Value)
			RegisterInsideMoney();

		if (Plugin.ModConfig.ConfigBiomesEnabled.Value)
			Biome = new BiomeAssets("biomeassets");

		if (Plugin.ModConfig.ConfigBearTrapEnabled.Value)
		{
			BearTrap = new BearTrapAssets("beartrapassets");
			hazardPrefabs.Add(MapObjectHandler.Instance.BearTrap.GrassMatPrefab);			
		}

		if (Plugin.ModConfig.ConfigLaserTurretEnabled.Value)
			RegisterLaserTurret();
		
		if (Plugin.ModConfig.ConfigIndustrialFanEnabled.Value)
			RegisterIndustrialFan();

		if (Plugin.ModConfig.ConfigFlashTurretEnabled.Value)
			RegisterFlashTurret();

		if (Plugin.ModConfig.ConfigTeslaShockEnabled.Value)
			RegisterTeslaShock();

		if (Plugin.ModConfig.ConfigFunctionalMicrowaveEnabled.Value)
			RegisterFunctionalMicrowave();

		/*if (Plugin.ModConfig.ConfigAirControlUnitEnabled.Value)
		{
			AirControlUnit = new AirControlUnitAssets("aircontrolunitassets");
			hazardPrefabs.Add(MapObjectHandler.Instance.AirControlUnit.AirControlUnitPrefab);
		}*/
	}

	public void RegisterFunctionalMicrowave()
	{
		FunctionalMicrowave = new FunctionalMicrowaveAssets("functionalmicrowaveassets");
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = FunctionalMicrowave.FunctionalMicrowavePrefab;
		hazardPrefabs.Add(MapObjectHandler.Instance.FunctionalMicrowave.FunctionalMicrowavePrefab);
		MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => 
			new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigFunctionalMicrowaveAbundance.Value, 0, 1000)))
		);
	}

	public void RegisterTeslaShock()
	{
		TeslaShock = new TeslaShockAssets("teslashockassets");
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = TeslaShock.TeslaShockPrefab;
		MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => 
			new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigTeslaShockAbundance.Value, 0, 1000)))
		);
		hazardPrefabs.Add(MapObjectHandler.Instance.TeslaShock.TeslaShockPrefab);
	}

	public void RegisterFlashTurret()
	{
		FlashTurret = new FlashTurretAssets("flashturretassets");
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = FlashTurret.FlashTurretPrefab;
		MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => 
			new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigFlashTurretAbundance.Value, 0, 1000)))
		);
        hazardPrefabs.Add(MapObjectHandler.Instance.FlashTurret.FlashTurretPrefab);
	}

	public void RegisterIndustrialFan()
	{
		IndustrialFan = new IndustrialFanAssets("industrialfanassets");
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = IndustrialFan.IndustrialFanPrefab;
		MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => 
			new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigIndustrialFanAbundance.Value, 0, 1000)))
		);
		hazardPrefabs.Add(MapObjectHandler.Instance.IndustrialFan.IndustrialFanPrefab);
	}

	public void RegisterLaserTurret()
	{
		GlowingGem = new GlowingGemAssets("glowinggemassets");
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = GlowingGem.LaserTurretPrefab;
		MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => 
			new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigLaserTurretAbundance.Value, 0, 1000)))
		);
        hazardPrefabs.Add(MapObjectHandler.Instance.GlowingGem.LaserTurretPrefab);
	}

	public void RegisterInsideMoney()
	{
		Money = new MoneyAssets("moneyassets");
		Money.MoneyItem.spawnPrefab.GetComponent<Money>().SetScrapValue(-1);
		SpawnableMapObjectDef mapObjDefBug = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
		mapObjDefBug.spawnableMapObject = new SpawnableMapObject();
		mapObjDefBug.spawnableMapObject.prefabToSpawn = Money.MoneyItem.spawnPrefab;
		if (Plugin.ModConfig.ConfigWalletEnabled.Value)
		{
			MapObjects.RegisterMapObject(mapObjDefBug, Levels.LevelTypes.All, (level) => 
				new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(Plugin.ModConfig.ConfigMoneyAbundance.Value, 0, 1000)))
			);
		}
	}

	public void RegisterOutsideFlora()
	{
		Flora = new FloraAssets("floraassets");
		Flora floraStuff = Flora.AllFloraPrefab.GetComponent<Flora>();
		
		string[] grassMoonList = MapObjectConfigParsing(Plugin.ModConfig.ConfigFloraGrassSpawnPlaces.Value);
		string[] desertMoonList = MapObjectConfigParsing(Plugin.ModConfig.ConfigFloraDesertSpawnPlaces.Value);
		string[] snowMoonList = MapObjectConfigParsing(Plugin.ModConfig.ConfigFloraSnowSpawnPlaces.Value);
		string[] moonBlackList = MapObjectConfigParsing(Plugin.ModConfig.ConfigFloraExcludeSpawnPlaces.Value);

		foreach (var flora in floraStuff.Desert)
		{
			RegisterFlora(flora, new AnimationCurve(new Keyframe(0, Math.Clamp(Plugin.ModConfig.ConfigFloraMinAbundance.Value, 0, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), new Keyframe(1, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), desertMoonList, FloraTag.Desert, moonBlackList);
		}

		foreach (var flora in floraStuff.Snowy)
		{
			RegisterFlora(flora, new AnimationCurve(new Keyframe(0, Math.Clamp(Plugin.ModConfig.ConfigFloraMinAbundance.Value, 0, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), new Keyframe(1, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), snowMoonList, FloraTag.Snow, moonBlackList);
		}

		foreach (var flora in floraStuff.Grass)
		{
			RegisterFlora(flora, new AnimationCurve(new Keyframe(0, Math.Clamp(Plugin.ModConfig.ConfigFloraMinAbundance.Value, 0, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), new Keyframe(1, Plugin.ModConfig.ConfigFloraMaxAbundance.Value)), grassMoonList, FloraTag.Grass, moonBlackList);
		}

		/*foreach (var flora in floraStuff.DangerousSpecies) {
			RegisterFlora(flora, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 3)), dangerMoonList, FloraTag.Dangerous, moonBlackList);
		}*/
	}

	public void RegisterFlora(GameObject prefab, AnimationCurve curve, string[] moonWhiteList, FloraTag tag, string[] moonBlackList)
	{
		RoundManagerPatch.spawnableFlora.Add(new SpawnableFlora() {
			prefab = prefab,
			moonsWhiteList = moonWhiteList,
			spawnCurve = curve,
			blacklistedTags = ["Metal", "Wood", "Concrete", "Puddle", "Aluminum", "Catwalk", "Bush", "Rock", "MoldSpore", "Untagged"],
			floraTag = tag,
			moonsBlackList = moonBlackList
		});
	}
}