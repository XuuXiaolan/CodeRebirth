using CodeRebirth.src.Util.AssetLoading;
using CodeRebirth.src.Util;
using LethalLib.Modules;
using LethalLib.Extras;

namespace CodeRebirth.src.Content.Unlockables;
public class UnlockableHandler : ContentHandler<UnlockableHandler> {
	public class ShockwaveBotAssets(string bundleName) : AssetBundleLoader<ShockwaveBotAssets>(bundleName) {
		[LoadFromBundle("ShockwaveBotUnlockable.asset")]
		public UnlockableItemDef ShockWaveBotUnlockable { get; private set; } = null!;
	}

	public class PlantPotAssets(string bundleName) : AssetBundleLoader<PlantPotAssets>(bundleName) { 
		[LoadFromBundle("PlantPotUnlockable.asset")]
		public UnlockableItemDef PlantPotUnlockable { get; private set; } = null!;

		[LoadFromBundle("TomatoObj.asset")]
		public Item Tomato { get; private set; } = null!;

		[LoadFromBundle("GoldenTomatoObj.asset")]
		public Item GoldenTomato { get; private set; } = null!;

		[LoadFromBundle("WoodenSeedObj.asset")]
		public Item Seed { get; private set; } = null!;
	}

	public class SeamineTinkAssets(string bundleName) : AssetBundleLoader<SeamineTinkAssets>(bundleName) {
		[LoadFromBundle("SeamineTinkUnlockable.asset")]
		public UnlockableItemDef SeamineTinkUnlockable { get; private set; } = null!;
	}

	public SeamineTinkAssets SeamineTink { get; private set; } = null!;
	public PlantPotAssets PlantPot { get; private set; } = null!;
	public ShockwaveBotAssets ShockwaveBot { get; private set; } = null!;

    public UnlockableHandler() {
		if (Plugin.ModConfig.ConfigShockwaveBotEnabled.Value) RegisterShockWaveGal();
		if (Plugin.ModConfig.ConfigFarmingEnabled.Value) RegisterPlantPot();
		if (Plugin.ModConfig.ConfigSeamineTinkEnabled.Value) RegisterSeamineTink();
	}

    private void RegisterShockWaveGal() {
        ShockwaveBot = new ShockwaveBotAssets("shockwavebotassets");
        LethalLib.Modules.Unlockables.RegisterUnlockable(ShockwaveBot.ShockWaveBotUnlockable, Plugin.ModConfig.ConfigShockwaveBotCost.Value, StoreType.ShipUpgrade);
    }

	private void RegisterSeamineTink() {
		SeamineTink = new SeamineTinkAssets("seaminetinkassets");
		LethalLib.Modules.Unlockables.RegisterUnlockable(SeamineTink.SeamineTinkUnlockable, Plugin.ModConfig.ConfigSeamineTinkCost.Value, StoreType.ShipUpgrade);
	}

	private void RegisterPlantPot() {
		PlantPot = new PlantPotAssets("plantpotassets");
		RegisterScrapWithConfig("", PlantPot.Seed);
		Plugin.samplePrefabs.Add("Wooden Seed", PlantPot.Seed);
		RegisterScrapWithConfig("", PlantPot.Tomato);
		Plugin.samplePrefabs.Add("Tomato", PlantPot.Tomato);
		RegisterScrapWithConfig("", PlantPot.GoldenTomato);
		Plugin.samplePrefabs.Add("Golden Tomato", PlantPot.GoldenTomato);
		LethalLib.Modules.Unlockables.RegisterUnlockable(PlantPot.PlantPotUnlockable, Plugin.ModConfig.ConfigSeamineTinkCost.Value, StoreType.Decor);
	}
}