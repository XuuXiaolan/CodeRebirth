using System.Collections.Generic;
using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.ItemStuff;

public class ItemHandler : ContentHandler<ItemHandler> {
	public class ItemAssets : AssetBundleLoader<ItemAssets> {
		[LoadFromBundle("WalletObj.asset")]
		public Item WalletItem { get; private set; }
		
		[LoadFromBundle("wTerminalNode.asset")]
		public TerminalNode WalletTerminalNode { get; private set; }
		[LoadFromBundle("HoverboardObj.asset")]
		public Item HoverboardItem { get; private set; }

		[LoadFromBundle("EpicAxeObj.asset")]
		public Item EpicAxeItem { get; private set; }

        [LoadFromBundle("SnowGlobeObj.asset")]
        public Item SnowGlobeItem { get; private set; }

        public ItemAssets(string bundleName) : base(bundleName) {
		}
	}

	public ItemAssets Assets { get; private set; }
    
	public ItemHandler() {
		Assets = new ItemAssets("coderebirthasset");
        
		RegisterScrapWithConfig(Plugin.ModConfig.ConfigHoverboardEnabled.Value, Plugin.ModConfig.ConfigHoverboardSpawnWeights.Value, Assets.HoverboardItem);
		RegisterShopItemWithConfig(Plugin.ModConfig.ConfigWalletEnabled.Value, false, Assets.WalletItem, Assets.WalletTerminalNode, Plugin.ModConfig.ConfigWalletCost.Value, "");
		RegisterScrapWithConfig(Plugin.ModConfig.ConfigEpicAxeScrapEnabled.Value, Plugin.ModConfig.ConfigEpicAxeScrapSpawnWeights.Value, Assets.EpicAxeItem);
		RegisterScrapWithConfig(Plugin.ModConfig.ConfigSnowGlobeEnabled.Value, Plugin.ModConfig.ConfigSnowGlobeSpawnWeights.Value, Assets.SnowGlobeItem);
	}
}