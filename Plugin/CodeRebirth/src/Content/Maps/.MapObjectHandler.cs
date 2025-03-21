﻿using System;
using System.Collections.Generic;
using CodeRebirth.src.Patches;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;
using CodeRebirth.src.Util;
using LethalLib.Modules;
using CodeRebirth.src.MiscScripts;
using LethalLib.Extras;
using System.Linq;

namespace CodeRebirth.src.Content.Maps;
public class MapObjectHandler : ContentHandler<MapObjectHandler>
{
	public class CompactorTobyAssets(string bundleName) : AssetBundleLoader<CompactorTobyAssets>(bundleName)
	{
		[LoadFromBundle("CompactorToby.prefab")]
		public GameObject CompactorTobyPrefab { get; private set; } = null!;

		[LoadFromBundle("SallyCubesObj.asset")]
		public Item SallyCubesScrap { get; private set; } = null!;

		[LoadFromBundle("FlatBodyScrapObj.asset")]
		public Item FlatDeadPlayerScrap { get; private set; } = null!;
	}

	public class ShredderSarahAssets(string bundleName) : AssetBundleLoader<ShredderSarahAssets>(bundleName)
    {
        [LoadFromBundle("ShreddingSarah.prefab")]
		public GameObject ShreddingSarahPrefab { get; private set; } = null!;

        [LoadFromBundle("DeadBodyScrapObj.asset")]
        public Item DeadPlayerScrap { get; private set; } = null!;

		[LoadFromBundle("ShreddedScrapObj.asset")]
		public Item ShreddedScrap { get; private set; } = null!;
    }

	public class CrateAssets(string bundleName) : AssetBundleLoader<CrateAssets>(bundleName)
	{
		[LoadFromBundle("Wooden Crate")]
		public GameObject WoodenCratePrefab { get; private set; } = null!;
		
		[LoadFromBundle("Metal Crate")]
		public GameObject MetalCratePrefab { get; private set; } = null!;

		[LoadFromBundle("Mimic Wooden Crate")]
		public GameObject MimicWoodenCratePrefab { get; private set; } = null!;
		
		[LoadFromBundle("Mimic Metal Crate")]
		public GameObject MimicMetalCratePrefab { get; private set; } = null!;
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
		[LoadFromBundle("BoomBearTrap.prefab")]
		public GameObject BoomTrapPrefab { get; private set; } = null!;
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

	public class AirControlUnitAssets(string bundleName) : AssetBundleLoader<AirControlUnitAssets>(bundleName)
	{
		[LoadFromBundle("AirControlUnit.prefab")]
		public GameObject AirControlUnitPrefab { get; private set; } = null!;

		[LoadFromBundle("AirControlUnitProjectile.prefab")]
		public GameObject ProjectilePrefab { get; private set; } = null!;
	}

	public class FunctionalMicrowaveAssets(string bundleName) : AssetBundleLoader<FunctionalMicrowaveAssets>(bundleName)
	{
		[LoadFromBundle("FunctionalMicrowave.prefab")]
		public GameObject FunctionalMicrowavePrefab { get; private set; } = null!;

		[LoadFromBundle("MicrowaveForkItem.asset")]
		public Item ForkItem { get; private set; } = null!;

		[LoadFromBundle("MicrowaveSporkItem.asset")]
		public Item SporkItem { get; private set; } = null!;

		[LoadFromBundle("MicrowaveCharredBabyItem.asset")]
		public Item CharredBabyItem { get; private set; } = null!;
	}

	public class MerchantAssets(string bundleName) : AssetBundleLoader<MerchantAssets>(bundleName)
	{
		[LoadFromBundle("MoneyPrefab.prefab")]
		public GameObject MoneyPrefab { get; private set; } = null!;

		[LoadFromBundle("PiggyBankUnlockable.asset")]
		public UnlockableItemDef PiggyBankUnlockable { get; private set; } = null!;
	}

	public class GunslingerGregAssets(string bundleName) : AssetBundleLoader<GunslingerGregAssets>(bundleName)
	{
		[LoadFromBundle("GregNotSAM.prefab")]
		public GameObject GunslingerGregPrefab { get; private set; } = null!;

		[LoadFromBundle("GregMissile.prefab")]
		public GameObject MissilePrefab { get; private set; } = null!;
	}

	public GunslingerGregAssets? GunslingerGreg { get; private set; } = null;
	public CompactorTobyAssets? CompactorToby { get; private set; } = null;
	public ShredderSarahAssets? ShredderSarah { get; private set; } = null;
	public MerchantAssets? Merchant { get; private set; } = null;
	public CrateAssets? Crate { get; private set; } = null;
	public FloraAssets? Flora { get; private set; } = null;
	public BiomeAssets? Biome { get; private set; } = null;
	public BearTrapAssets? BearTrap { get; private set; } = null;
	public GlowingGemAssets? GlowingGem { get; private set; } = null;
	public IndustrialFanAssets? IndustrialFan { get; private set; } = null;
	public FlashTurretAssets? FlashTurret { get; private set; } = null;
	public TeslaShockAssets? TeslaShock { get; private set; } = null;
	public AirControlUnitAssets? AirControlUnit { get; private set; } = null;
	public FunctionalMicrowaveAssets? FunctionalMicrowave { get; private set; } = null;

    public Dictionary<SpawnSyncedCRObject.CRObjectType, GameObject> prefabMapping = new();
	public static List<GameObject> hazardPrefabs = new List<GameObject>();

    public MapObjectHandler()
	{
		if (Plugin.ModConfig.ConfigOxydeEnabled.Value)
		{
			CompactorToby = new CompactorTobyAssets("compactortobyassets");
			prefabMapping[SpawnSyncedCRObject.CRObjectType.CompactorToby] = CompactorToby.CompactorTobyPrefab;
			RegisterScrapWithConfig("", CompactorToby.FlatDeadPlayerScrap);
			RegisterScrapWithConfig("", CompactorToby.SallyCubesScrap);
		}

		if (Plugin.ModConfig.ConfigGunslingerGregEnabled.Value)
		{
			GunslingerGreg = new GunslingerGregAssets("gunslingergregassets");
			prefabMapping[SpawnSyncedCRObject.CRObjectType.GunslingerGreg] = GunslingerGreg.GunslingerGregPrefab;
		}

		if (Plugin.ModConfig.ConfigOxydeEnabled.Value)
		{
			ShredderSarah = new ShredderSarahAssets("shreddersarahassets");
			prefabMapping[SpawnSyncedCRObject.CRObjectType.ShredderSarah] = ShredderSarah.ShreddingSarahPrefab;
			RegisterScrapWithConfig("", ShredderSarah.ShreddedScrap);
			RegisterScrapWithConfig("", ShredderSarah.DeadPlayerScrap);
		}

		Merchant = LoadAndRegisterAssets<MerchantAssets>("merchantassets");
		if (Merchant != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.Merchant] = Merchant.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("merchant")).First().gameObject;
			LethalLib.Modules.Unlockables.RegisterUnlockable(Merchant.PiggyBankUnlockable, Plugin.ModConfig.ConfigPiggyBankCost.Value, StoreType.ShipUpgrade);
		}

		if (Plugin.ModConfig.ConfigItemCrateEnabled.Value)
		{
			Crate = new CrateAssets("crateassets");
			prefabMapping[SpawnSyncedCRObject.CRObjectType.MimicWoodenCrate] = Crate.MimicWoodenCratePrefab;
			prefabMapping[SpawnSyncedCRObject.CRObjectType.WoodenCrate] = Crate.WoodenCratePrefab;
			prefabMapping[SpawnSyncedCRObject.CRObjectType.MimicMetalCrate] = Crate.MimicMetalCratePrefab;
			prefabMapping[SpawnSyncedCRObject.CRObjectType.MetalCrate] = Crate.MetalCratePrefab;
		}

		if (Plugin.ModConfig.ConfigFloraEnabled.Value)
			RegisterOutsideFlora();

		if (Plugin.ModConfig.ConfigBiomesEnabled.Value)
			Biome = new BiomeAssets("biomeassets");

		if (Plugin.ModConfig.ConfigTeslaShockEnabled.Value)
			RegisterTeslaShock();

		if (Plugin.ModConfig.ConfigBearTrapEnabled.Value)
		{
			BearTrap = new BearTrapAssets("beartrapassets");
			prefabMapping[SpawnSyncedCRObject.CRObjectType.BearTrap] = BearTrap.GrassMatPrefab;
			prefabMapping[SpawnSyncedCRObject.CRObjectType.BoomTrap] = BearTrap.BoomTrapPrefab;
			hazardPrefabs.Add(MapObjectHandler.Instance.BearTrap.GrassMatPrefab);
			if (Plugin.ModConfig.ConfigInsideBearTrapEnabled.Value) RegisterInsideBearTraps();
		}

		if (Plugin.ModConfig.ConfigLaserTurretEnabled.Value)
			RegisterLaserTurret();
		
		if (Plugin.ModConfig.ConfigIndustrialFanEnabled.Value)
			RegisterIndustrialFan();

		if (Plugin.ModConfig.ConfigFlashTurretEnabled.Value)
			RegisterFlashTurret();

		if (Plugin.ModConfig.ConfigFunctionalMicrowaveEnabled.Value)
			RegisterFunctionalMicrowave();

		if (Plugin.ModConfig.ConfigAirControlUnitEnabled.Value)
		{
			AirControlUnit = new AirControlUnitAssets("aircontrolunitassets");
			prefabMapping[SpawnSyncedCRObject.CRObjectType.AirControlUnit] = AirControlUnit.AirControlUnitPrefab;
			hazardPrefabs.Add(MapObjectHandler.Instance.AirControlUnit.AirControlUnitPrefab);
		}
	}

    public GameObject? GetPrefabFor(SpawnSyncedCRObject.CRObjectType objectType)
    {
        prefabMapping.TryGetValue(objectType, out var prefab);
        return prefab;
    }

	public void RegisterInsideBearTraps()
	{
		RegisterInsideMapObjectWithConfig(BearTrap.GrassMatPrefab, Plugin.ModConfig.ConfigBearTrapInsideSpawnWeight.Value);
	}

	public void RegisterFunctionalMicrowave()
	{
		FunctionalMicrowave = new FunctionalMicrowaveAssets("functionalmicrowaveassets");
		prefabMapping[SpawnSyncedCRObject.CRObjectType.FunctionalMicrowave] = FunctionalMicrowave.FunctionalMicrowavePrefab;
		RegisterScrapWithConfig("", FunctionalMicrowave.SporkItem);
		RegisterScrapWithConfig("", FunctionalMicrowave.ForkItem);
		RegisterScrapWithConfig("", FunctionalMicrowave.CharredBabyItem);
		Plugin.samplePrefabs.Add("MicrowaveSpork", FunctionalMicrowave.SporkItem);
		Plugin.samplePrefabs.Add("MicrowaveFork", FunctionalMicrowave.ForkItem);
		Plugin.samplePrefabs.Add("MicrowaveCharredBaby", FunctionalMicrowave.CharredBabyItem);
		RegisterInsideMapObjectWithConfig(FunctionalMicrowave.FunctionalMicrowavePrefab, Plugin.ModConfig.ConfigFunctionalMicrowaveCurveSpawnWeight.Value);
	}

	public void RegisterTeslaShock()
	{
		TeslaShock = new TeslaShockAssets("teslashockassets");
		prefabMapping[SpawnSyncedCRObject.CRObjectType.BugZapper] = TeslaShock.TeslaShockPrefab;
		RegisterInsideMapObjectWithConfig(TeslaShock.TeslaShockPrefab, Plugin.ModConfig.ConfigTeslaShockCurveSpawnWeight.Value);
	}

	public void RegisterFlashTurret()
	{
		FlashTurret = new FlashTurretAssets("flashturretassets");
		prefabMapping[SpawnSyncedCRObject.CRObjectType.FlashTurret] = FlashTurret.FlashTurretPrefab;
		RegisterInsideMapObjectWithConfig(FlashTurret.FlashTurretPrefab, Plugin.ModConfig.ConfigFlashTurretCurveSpawnWeight.Value);
	}

	public void RegisterIndustrialFan()
	{
		IndustrialFan = new IndustrialFanAssets("industrialfanassets");
		prefabMapping[SpawnSyncedCRObject.CRObjectType.IndustrialFan] = IndustrialFan.IndustrialFanPrefab;
		RegisterInsideMapObjectWithConfig(IndustrialFan.IndustrialFanPrefab, Plugin.ModConfig.ConfigIndustrialFanCurveSpawnWeight.Value);
	}

	public void RegisterLaserTurret()
	{
		GlowingGem = new GlowingGemAssets("glowinggemassets");
		prefabMapping[SpawnSyncedCRObject.CRObjectType.LaserTurret] = GlowingGem.LaserTurretPrefab;
		RegisterInsideMapObjectWithConfig(GlowingGem.LaserTurretPrefab, Plugin.ModConfig.ConfigLaserTurretCurveSpawnWeight.Value);
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
		RoundManagerPatch.spawnableFlora.Add(new SpawnableFlora()
		{
			prefab = prefab,
			moonsWhiteList = moonWhiteList,
			spawnCurve = curve,
			blacklistedTags = ["Metal", "Wood", "Concrete", "Puddle", "Aluminum", "Catwalk", "Bush", "Rock", "MoldSpore", "Untagged"],
			floraTag = tag,
			moonsBlackList = moonBlackList
		});
	}
}