using CodeRebirth.src.Util.AssetLoading;
using CodeRebirth.src.Util;
using LethalLib.Modules;
using LethalLib.Extras;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class UnlockableHandler : ContentHandler<UnlockableHandler>
{
	public class ShockwaveBotAssets(string bundleName) : AssetBundleLoader<ShockwaveBotAssets>(bundleName)
	{
		[LoadFromBundle("ShockwaveBotUnlockable.asset")]
		public UnlockableItemDef ShockWaveBotUnlockable { get; private set; } = null!;

		[LoadFromBundle("LaserShockBlast.prefab")]
		public GameObject LasetShockBlast { get; private set; } = null!;
	}

	public class PlantPotAssets(string bundleName) : AssetBundleLoader<PlantPotAssets>(bundleName)
	{ 
		[LoadFromBundle("PlantPotUnlockable.asset")]
		public UnlockableItemDef PlantPotUnlockable { get; private set; } = null!;

		[LoadFromBundle("TomatoObj.asset")]
		public Item Tomato { get; private set; } = null!;

		[LoadFromBundle("GoldenTomatoObj.asset")]
		public Item GoldenTomato { get; private set; } = null!;

		[LoadFromBundle("WoodenSeedObj.asset")]
		public Item Seed { get; private set; } = null!;
	}

	public class SeamineTinkAssets(string bundleName) : AssetBundleLoader<SeamineTinkAssets>(bundleName)
	{
		[LoadFromBundle("SeamineTinkUnlockable.asset")]
		public UnlockableItemDef SeamineTinkUnlockable { get; private set; } = null!;
	}

	public SeamineTinkAssets SeamineTink { get; private set; } = null!;
	public PlantPotAssets PlantPot { get; private set; } = null!;
	public ShockwaveBotAssets ShockwaveBot { get; private set; } = null!;

    public UnlockableHandler()
	{
		if (Plugin.ModConfig.ConfigShockwaveBotEnabled.Value) RegisterShockWaveGal();
		if (Plugin.ModConfig.ConfigFarmingEnabled.Value) RegisterPlantPot();
		//if (Plugin.ModConfig.ConfigSeamineTinkEnabled.Value) RegisterSeamineTink();
	}

    private void RegisterShockWaveGal()
	{
        ShockwaveBot = new ShockwaveBotAssets("shockwavebotassets");
        LethalLib.Modules.Unlockables.RegisterUnlockable(ShockwaveBot.ShockWaveBotUnlockable, Plugin.ModConfig.ConfigShockwaveBotCost.Value, StoreType.ShipUpgrade);
    }

	private void RegisterSeamineTink()
	{
		SeamineTink = new SeamineTinkAssets("seaminetinkassets");
		LethalLib.Modules.Unlockables.RegisterUnlockable(SeamineTink.SeamineTinkUnlockable, Plugin.ModConfig.ConfigSeamineTinkCost.Value, StoreType.ShipUpgrade);
	}

	private void RegisterPlantPot()
	{
		PlantPot = new PlantPotAssets("plantpotassets");
		RegisterScrapWithConfig(Plugin.ModConfig.ConfigWoodenSeedSpawnWeights.Value, PlantPot.Seed, -1, -1);
		Plugin.samplePrefabs.Add("Wooden Seed", PlantPot.Seed);

		int[] scrapValues = ChangeItemValues(Plugin.ModConfig.ConfigTomatoValue.Value);
		RegisterScrapWithConfig("", PlantPot.Tomato, scrapValues[0], scrapValues[1]);
		Plugin.samplePrefabs.Add("Tomato", PlantPot.Tomato);

		int[] gScrapValues = ChangeItemValues(Plugin.ModConfig.ConfigGoldenTomatoValue.Value);
		RegisterScrapWithConfig("", PlantPot.GoldenTomato, gScrapValues[0], gScrapValues[1]);
		Plugin.samplePrefabs.Add("Golden Tomato", PlantPot.GoldenTomato);

		LethalLib.Modules.Unlockables.RegisterUnlockable(PlantPot.PlantPotUnlockable, Plugin.ModConfig.ConfigPlantPotPrice.Value, StoreType.ShipUpgrade);
	}
}