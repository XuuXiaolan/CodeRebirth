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
    }
}