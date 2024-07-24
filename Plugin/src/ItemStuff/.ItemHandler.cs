﻿using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;

namespace CodeRebirth.ItemStuff;

public class ItemHandler : ContentHandler<ItemHandler> {
    public class WalletAssets(string bundleName) : AssetBundleLoader<WalletAssets>(bundleName) {
        [LoadFromBundle("WalletNewObj.asset")]
        public Item WalletItemNew { get; private set; } = null!;

        [LoadFromBundle("WalletOldObj.asset")]
        public Item WalletItemOld { get; private set; } = null!;

        [LoadFromBundle("wTerminalNode.asset")]
        public TerminalNode WalletTerminalNode { get; private set; } = null!;
    }

    public class HoverboardAssets(string bundleName) : AssetBundleLoader<HoverboardAssets>(bundleName) {
        [LoadFromBundle("HoverboardObj.asset")]
        public Item HoverboardItem { get; private set; } = null!;

        [LoadFromBundle("HoverboardTerminalNode.asset")]
        public TerminalNode HoverboardTerminalNode { get; private set; } = null!;
    }

    public class EpicAxeAssets(string bundleName) : AssetBundleLoader<EpicAxeAssets>(bundleName) {
        [LoadFromBundle("EpicAxeObj.asset")]
        public Item EpicAxeItem { get; private set; } = null!;
    }

    public class NaturesMaceAssets(string bundleName) : AssetBundleLoader<NaturesMaceAssets>(bundleName) {
        [LoadFromBundle("NaturesMaceObj.asset")]
        public Item NatureMaceItem { get; private set; } = null!;
    }

    public class IcyHammerAssets(string bundleName) : AssetBundleLoader<IcyHammerAssets>(bundleName) {
        [LoadFromBundle("IcyHammerObj.asset")]
        public Item IcyHammerItem { get; private set; } = null!;
    }

    public class SpikyMaceAssets(string bundleName) : AssetBundleLoader<SpikyMaceAssets>(bundleName) {
        [LoadFromBundle("SpikyMaceObj.asset")]
        public Item SpikyMaceItem { get; private set; } = null!;
    }

    public class SnowGlobeAssets(string bundleName) : AssetBundleLoader<SnowGlobeAssets>(bundleName) {
        [LoadFromBundle("SnowGlobeObj.asset")]
        public Item SnowGlobeItem { get; private set; } = null!;
    }

    public WalletAssets Wallet { get; private set; } = null!;
    public HoverboardAssets Hoverboard { get; private set; } = null!;
    public EpicAxeAssets EpicAxe { get; private set; } = null!;
    public SnowGlobeAssets SnowGlobe { get; private set; } = null!;
    public NaturesMaceAssets NaturesMace { get; private set; } = null!;
    public IcyHammerAssets IcyHammer { get; private set; } = null!;
    public SpikyMaceAssets SpikyMace { get; private set; } = null!;

    public ItemHandler() {

        if (Plugin.ModConfig.ConfigWalletEnabled.Value) {
            Wallet = new WalletAssets("walletassets");
            if (Plugin.ModConfig.ConfigWalletMode.Value) RegisterShopItemWithConfig(false, Wallet.WalletItemOld, Wallet.WalletTerminalNode, Plugin.ModConfig.ConfigWalletCost.Value, "");
            else RegisterShopItemWithConfig(true, Wallet.WalletItemNew, Wallet.WalletTerminalNode, Plugin.ModConfig.ConfigWalletCost.Value, "");
        }

        if (Plugin.ModConfig.ConfigHoverboardEnabled.Value) {
            Hoverboard = new HoverboardAssets("hoverboardassets");
            RegisterShopItemWithConfig(false, Hoverboard.HoverboardItem, Hoverboard.HoverboardTerminalNode, Plugin.ModConfig.ConfigHoverboardCost.Value, "");
        }

        if (Plugin.ModConfig.ConfigEpicAxeScrapEnabled.Value) {
            EpicAxe = new EpicAxeAssets("epicaxeassets");
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigEpicAxeScrapSpawnWeights.Value, EpicAxe.EpicAxeItem);
        }

        if (Plugin.ModConfig.ConfigSnowGlobeEnabled.Value) {
            SnowGlobe = new SnowGlobeAssets("snowglobeassets");
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigSnowGlobeSpawnWeights.Value, SnowGlobe.SnowGlobeItem);
        }

        if (Plugin.ModConfig.ConfigNaturesMaceScrapEnabled.Value) {
            NaturesMace = new NaturesMaceAssets("naturesmaceassets");
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigNaturesMaceScrapSpawnWeights.Value, NaturesMace.NatureMaceItem);
        }

        if (Plugin.ModConfig.ConfigIcyHammerScrapEnabled.Value) {
            IcyHammer = new IcyHammerAssets("icyhammerassets");
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigIcyHammerScrapSpawnWeights.Value, IcyHammer.IcyHammerItem);
        }

        if (Plugin.ModConfig.ConfigSpikyMaceScrapEnabled.Value) {
            SpikyMace = new SpikyMaceAssets("spikymaceassets");
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigSpikyMaceScrapSpawnWeights.Value, SpikyMace.SpikyMaceItem);
        }
    }
}