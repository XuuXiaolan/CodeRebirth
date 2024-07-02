﻿using System.Collections.Generic;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.ItemStuff;

public class ItemHandler : ContentHandler<ItemHandler> {
    public class WalletAssets(string bundleName) : AssetBundleLoader<WalletAssets>(bundleName) {
        [LoadFromBundle("WalletNewObj.asset")]
        public Item WalletItemNew { get; private set; }

        [LoadFromBundle("WalletOldObj.asset")]
        public Item WalletItemOld { get; private set; }

        [LoadFromBundle("wTerminalNode.asset")]
        public TerminalNode WalletTerminalNode { get; private set; }
    }

    public class HoverboardAssets(string bundleName) : AssetBundleLoader<HoverboardAssets>(bundleName) {
        [LoadFromBundle("HoverboardObj.asset")]
        public Item HoverboardItem { get; private set; }

        [LoadFromBundle("HoverboardTerminalNode.asset")]
        public TerminalNode HoverboardTerminalNode { get; private set; }
    }

    public class EpicAxeAssets(string bundleName) : AssetBundleLoader<EpicAxeAssets>(bundleName) {
        [LoadFromBundle("EpicAxeObj.asset")]
        public Item EpicAxeItem { get; private set; }
    }

    public class SnowGlobeAssets(string bundleName) : AssetBundleLoader<SnowGlobeAssets>(bundleName) {
        [LoadFromBundle("SnowGlobeObj.asset")]
        public Item SnowGlobeItem { get; private set; }
    }

    public WalletAssets Wallet { get; private set; }
    public HoverboardAssets Hoverboard { get; private set; }
    public EpicAxeAssets EpicAxe { get; private set; }
    public SnowGlobeAssets SnowGlobe { get; private set; }

    public ItemHandler() {
        Wallet = new WalletAssets("walletassets");
        Hoverboard = new HoverboardAssets("hoverboardassets");
        EpicAxe = new EpicAxeAssets("epicaxeassets");
        SnowGlobe = new SnowGlobeAssets("snowglobeassets");

        RegisterShopItemWithConfig(Plugin.ModConfig.ConfigHoverboardEnabled.Value, false, Hoverboard.HoverboardItem, Hoverboard.HoverboardTerminalNode, Plugin.ModConfig.ConfigHoverboardCost.Value, "");
        if (Plugin.ModConfig.ConfigWalletMode.Value) RegisterShopItemWithConfig(Plugin.ModConfig.ConfigWalletEnabled.Value, false, Wallet.WalletItemOld, Wallet.WalletTerminalNode, Plugin.ModConfig.ConfigWalletCost.Value, "");
        else RegisterShopItemWithConfig(Plugin.ModConfig.ConfigWalletEnabled.Value, true, Wallet.WalletItemNew, Wallet.WalletTerminalNode, Plugin.ModConfig.ConfigWalletCost.Value, "");
        RegisterScrapWithConfig(Plugin.ModConfig.ConfigEpicAxeScrapEnabled.Value, Plugin.ModConfig.ConfigEpicAxeScrapSpawnWeights.Value, EpicAxe.EpicAxeItem);
        RegisterScrapWithConfig(Plugin.ModConfig.ConfigSnowGlobeEnabled.Value, Plugin.ModConfig.ConfigSnowGlobeSpawnWeights.Value, SnowGlobe.SnowGlobeItem);
    }
}