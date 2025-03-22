﻿using System;
using System.Collections.Generic;
using CodeRebirth.src.Patches;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;
using CodeRebirth.src.Util;
using CodeRebirth.src.MiscScripts;
using System.Linq;

namespace CodeRebirth.src.Content.Maps;
public class MapObjectHandler : ContentHandler<MapObjectHandler>
{
	public class CompactorTobyAssets(string bundleName) : AssetBundleLoader<CompactorTobyAssets>(bundleName)
	{
	}

	public class ShredderSarahAssets(string bundleName) : AssetBundleLoader<ShredderSarahAssets>(bundleName)
    {
    }

	public class CrateAssets(string bundleName) : AssetBundleLoader<CrateAssets>(bundleName)
	{
	}

	public class FloraAssets(string bundleName) : AssetBundleLoader<FloraAssets>(bundleName)
	{
		[LoadFromBundle("AllFlora.prefab")]
		public GameObject AllFloraPrefab { get; private set; } = null!;
	}

	public class BiomeAssets(string bundleName) : AssetBundleLoader<BiomeAssets>(bundleName)
	{
	}

	public class BearTrapAssets(string bundleName) : AssetBundleLoader<BearTrapAssets>(bundleName)
	{
	}

	public class GlowingGemAssets(string bundleName) : AssetBundleLoader<GlowingGemAssets>(bundleName)
	{
	}

	public class IndustrialFanAssets(string bundleName) : AssetBundleLoader<IndustrialFanAssets>(bundleName)
	{
	}

	public class FlashTurretAssets(string bundleName) : AssetBundleLoader<FlashTurretAssets>(bundleName)
	{
		[LoadFromBundle("FlashTurretUpdated.prefab")]
		public GameObject FlashTurretPrefab { get; private set; } = null!;
	}

	public class TeslaShockAssets(string bundleName) : AssetBundleLoader<TeslaShockAssets>(bundleName)
	{
		[LoadFromBundle("ChainLightning.prefab")]
		public GameObject ChainLightningPrefab { get; private set; } = null!;
	}

	public class AirControlUnitAssets(string bundleName) : AssetBundleLoader<AirControlUnitAssets>(bundleName)
	{
		[LoadFromBundle("AirControlUnitProjectile.prefab")]
		public GameObject ProjectilePrefab { get; private set; } = null!;
	}

	public class FunctionalMicrowaveAssets(string bundleName) : AssetBundleLoader<FunctionalMicrowaveAssets>(bundleName)
	{
	}

	public class MerchantAssets(string bundleName) : AssetBundleLoader<MerchantAssets>(bundleName)
	{
	}

	public class GunslingerGregAssets(string bundleName) : AssetBundleLoader<GunslingerGregAssets>(bundleName)
	{
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

    public MapObjectHandler()
	{
		CompactorToby = LoadAndRegisterAssets<CompactorTobyAssets>("compactortobyassets");
		if (CompactorToby != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.CompactorToby] = CompactorToby.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("compactor")).First().gameObject;;
		}

		GunslingerGreg = LoadAndRegisterAssets<GunslingerGregAssets>("gunslingergregassets");
		if (GunslingerGreg != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.GunslingerGreg] = GunslingerGreg.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("greg")).First().gameObject;
		}

		ShredderSarah = LoadAndRegisterAssets<ShredderSarahAssets>("shreddersarahassets");
		if (ShredderSarah != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.ShredderSarah] = ShredderSarah.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("shredder")).First().gameObject;
		}

		Merchant = LoadAndRegisterAssets<MerchantAssets>("merchantassets");
		if (Merchant != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.Merchant] = Merchant.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("merchant")).First().gameObject;
		}

		Crate = LoadAndRegisterAssets<CrateAssets>("crateassets");
		if (Crate != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.MimicWoodenCrate] = Crate.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("mimic wooden")).First().gameObject;
			prefabMapping[SpawnSyncedCRObject.CRObjectType.WoodenCrate] = Crate.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("normal wooden")).First().gameObject;
			prefabMapping[SpawnSyncedCRObject.CRObjectType.MimicMetalCrate] = Crate.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("mimic metal")).First().gameObject;
			prefabMapping[SpawnSyncedCRObject.CRObjectType.MetalCrate] = Crate.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("normal metal")).First().gameObject;
		}

		if (Plugin.ModConfig.ConfigFloraEnabled.Value)
			RegisterOutsideFlora();

		Biome = LoadAndRegisterAssets<BiomeAssets>("biomeassets");

		TeslaShock = LoadAndRegisterAssets<TeslaShockAssets>("teslashockassets");
		if (TeslaShock != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.BugZapper] = TeslaShock.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("zapper")).First().gameObject;
		}

		BearTrap = LoadAndRegisterAssets<BearTrapAssets>("beartrapassets");
		if (BearTrap != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.BearTrap] = BearTrap.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("bear")).First().gameObject;
			prefabMapping[SpawnSyncedCRObject.CRObjectType.BoomTrap] = BearTrap.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("boom")).First().gameObject;
			// if (Plugin.ModConfig.ConfigInsideBearTrapEnabled.Value) RegisterInsideBearTraps();
		}

		FunctionalMicrowave = LoadAndRegisterAssets<FunctionalMicrowaveAssets>("functionalmicrowaveassets");
		if (FunctionalMicrowave != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.FunctionalMicrowave] = FunctionalMicrowave.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("microwave")).First().gameObject;
		}

		FlashTurret = LoadAndRegisterAssets<FlashTurretAssets>("flashturretassets");
		if (FlashTurret != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.FlashTurret] = FlashTurret.FlashTurretPrefab;
		}

		IndustrialFan = LoadAndRegisterAssets<IndustrialFanAssets>("industrialfanassets");
		if (FlashTurret != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.IndustrialFan] = IndustrialFan.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("fan")).First().gameObject;
		}

		GlowingGem = LoadAndRegisterAssets<GlowingGemAssets>("glowinggemassets");
		if (GlowingGem != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.LaserTurret] = GlowingGem.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("laser")).First().gameObject;
		}

		AirControlUnit = LoadAndRegisterAssets<AirControlUnitAssets>("aircontrolunitassets");
		if (AirControlUnit != null)
		{
			prefabMapping[SpawnSyncedCRObject.CRObjectType.AirControlUnit] = AirControlUnit.MapObjectDefinitions.Where(x => x.GetGameObjectOnName("air")).First().gameObject;
		}
	}

    public GameObject? GetPrefabFor(SpawnSyncedCRObject.CRObjectType objectType)
    {
        prefabMapping.TryGetValue(objectType, out var prefab);
        return prefab;
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