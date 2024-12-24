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
		
		[LoadFromBundle("ShockWaveDrone.prefab")]
		public GameObject ShockWaveDronePrefab { get; private set; } = null!;
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

		[LoadFromBundle("SeamineGal.prefab")]
		public GameObject SeamineGalPrefab { get; private set; } = null!;
	}

	public class TerminalBotAssets(string bundleName) : AssetBundleLoader<TerminalBotAssets>(bundleName)
	{
		[LoadFromBundle("TerminalBotUnlockable.asset")]
		public UnlockableItemDef TerminalBotUnlockable { get; private set; } = null!;

		[LoadFromBundle("TerminalGalPrefab.prefab")]
		public GameObject TerminalGalPrefab { get; private set; } = null!;
	}

	public class BellCrabAssets(string bundleName) : AssetBundleLoader<BellCrabAssets>(bundleName)
	{
		[LoadFromBundle("BellCrabUnlockable.asset")]
		public UnlockableItemDef BellCrabUnlockable { get; private set; } = null!;
	}

	public class SCP999Assets(string bundleName) : AssetBundleLoader<SCP999Assets>(bundleName)
	{
		[LoadFromBundle("SCP999GalUnlockable.asset")]
		public UnlockableItemDef SCP999Unlockable { get; private set; } = null!;
	}

	public class Fishdispenserassets(string bundleName) : AssetBundleLoader<Fishdispenserassets>(bundleName)
	{
		[LoadFromBundle("ShrimpDispenserUnlockable.asset")]
		public UnlockableItemDef ShrimpDispenserUnlockable { get; private set; } = null!;

		[LoadFromBundle("ShrimpWeaponObj.asset")]
		public Item ShrimpWeapon { get; private set; } = null!;
	}

    public class FriendAssets(string bundleName) : AssetBundleLoader<FriendAssets>(bundleName)
    {
        [LoadFromBundle("GlitchedPlushieUnlockable.asset")]
        public UnlockableItemDef GlitchedPlushieUnlockable { get; private set; } = null!;
    }

    public FriendAssets Friend { get; private set; } = null!;
	public Fishdispenserassets ShrimpDispenser { get; private set; } = null!;
	public SCP999Assets SCP999 { get; private set; } = null!;
	public BellCrabAssets BellCrab { get; private set; } = null!;
	public SeamineTinkAssets SeamineTink { get; private set; } = null!;
	public TerminalBotAssets TerminalBot { get; private set; } = null!;
	public PlantPotAssets PlantPot { get; private set; } = null!;
	public ShockwaveBotAssets ShockwaveBot { get; private set; } = null!;

    public UnlockableHandler()
	{
		if (Plugin.ModConfig.ConfigShockwaveBotEnabled.Value) RegisterShockWaveGal();
		if (Plugin.ModConfig.ConfigFarmingEnabled.Value) RegisterPlantPot();
		if (Plugin.ModConfig.ConfigSeamineTinkEnabled.Value) RegisterSeamineTink();
		if (Plugin.ModConfig.ConfigTerminalBotEnabled.Value) RegisterTerminalBot();
		if (Plugin.ModConfig.ConfigBellCrabGalEnabled.Value) RegisterBellCrab();
		if (Plugin.ModConfig.Config999GalEnabled.Value) Register999Gal();
		if (Plugin.ModConfig.ConfigShrimpDispenserEnabled.Value) RegisterShrimpDispenser();
        if (Plugin.ModConfig.ConfigFriendStuffEnabled.Value) RegisterFriendStuff();
	}

	private void RegisterFriendStuff()
	{
		Friend = new FriendAssets("friendassets");
		LethalLib.Modules.Unlockables.RegisterUnlockable(Friend.GlitchedPlushieUnlockable, Plugin.ModConfig.ConfigGlitchedPlushieCost.Value, StoreType.Decor);
	}

	private void RegisterShrimpDispenser()
	{
		ShrimpDispenser = new Fishdispenserassets("fishdispenserassets");
		LethalLib.Modules.Unlockables.RegisterUnlockable(ShrimpDispenser.ShrimpDispenserUnlockable, Plugin.ModConfig.ConfigShrimpDispenserCost.Value, StoreType.ShipUpgrade);

		RegisterScrapWithConfig("", ShrimpDispenser.ShrimpWeapon, 0, 0);
		Plugin.samplePrefabs.Add("Shrimp Weapon", ShrimpDispenser.ShrimpWeapon);
	}

	private void Register999Gal()
	{
		SCP999 = new SCP999Assets("scp999galassets");
		LethalLib.Modules.Unlockables.RegisterUnlockable(SCP999.SCP999Unlockable, Plugin.ModConfig.Config999GalCost.Value, StoreType.ShipUpgrade);
	}

	private void RegisterBellCrab()
	{
		BellCrab = new BellCrabAssets("bellcrabgalassets");
		LethalLib.Modules.Unlockables.RegisterUnlockable(BellCrab.BellCrabUnlockable, Plugin.ModConfig.ConfigBellCrabGalCost.Value, StoreType.Decor);
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

	private void RegisterTerminalBot()
	{
		TerminalBot = new TerminalBotAssets("terminalbotassets");
		LethalLib.Modules.Unlockables.RegisterUnlockable(TerminalBot.TerminalBotUnlockable, Plugin.ModConfig.ConfigTerminalBotCost.Value, StoreType.ShipUpgrade);
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