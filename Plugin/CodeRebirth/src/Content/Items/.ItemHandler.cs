﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class ItemHandler : ContentHandler<ItemHandler>
{
    public class WalletAssets(string bundleName) : AssetBundleLoader<WalletAssets>(bundleName)
    {
        [LoadFromBundle("WalletNewObj.asset")]
        public Item WalletItemNew { get; private set; } = null!;

        [LoadFromBundle("WalletOldObj.asset")]
        public Item WalletItemOld { get; private set; } = null!;

        [LoadFromBundle("wTerminalNode.asset")]
        public TerminalNode WalletTerminalNode { get; private set; } = null!;
    }

    public class HoverboardAssets(string bundleName) : AssetBundleLoader<HoverboardAssets>(bundleName)
    {
        [LoadFromBundle("HoverboardObj.asset")]
        public Item HoverboardItem { get; private set; } = null!;

        [LoadFromBundle("HoverboardTerminalNode.asset")]
        public TerminalNode HoverboardTerminalNode { get; private set; } = null!;
    }

    public class EpicAxeAssets(string bundleName) : AssetBundleLoader<EpicAxeAssets>(bundleName)
    {
        [LoadFromBundle("EpicAxeObj.asset")]
        public Item EpicAxeItem { get; private set; } = null!;
    }

    public class NaturesMaceAssets(string bundleName) : AssetBundleLoader<NaturesMaceAssets>(bundleName)
    {
        [LoadFromBundle("NaturesMaceObj.asset")]
        public Item NatureMaceItem { get; private set; } = null!;
    }

    public class IcyHammerAssets(string bundleName) : AssetBundleLoader<IcyHammerAssets>(bundleName)
    {
        [LoadFromBundle("IcyHammerObj.asset")]
        public Item IcyHammerItem { get; private set; } = null!;
    }

    public class SpikyMaceAssets(string bundleName) : AssetBundleLoader<SpikyMaceAssets>(bundleName)
    {
        [LoadFromBundle("SpikyMaceObj.asset")]
        public Item SpikyMaceItem { get; private set; } = null!;
    }

    public class SnowGlobeAssets(string bundleName) : AssetBundleLoader<SnowGlobeAssets>(bundleName)
    {
        [LoadFromBundle("SnowGlobeObj.asset")]
        public Item SnowGlobeItem { get; private set; } = null!;
    }

    public class PjonkTurkeyAssets(string bundleName) : AssetBundleLoader<PjonkTurkeyAssets>(bundleName)
    {
        [LoadFromBundle("PjonkTurkeyObj.asset")]
        public Item PjonkTurkeyItem { get; private set; } = null!;
    }

    public class ZortAssets(string bundleName) : AssetBundleLoader<ZortAssets>(bundleName)
    {
        [LoadFromBundle("AccordionObj.asset")]
        public Item AccordionItem { get; private set; } = null!;

        [LoadFromBundle("RecorderObj.asset")]
        public Item RecorderItem { get; private set; } = null!;

        [LoadFromBundle("GuitarObj.asset")]
        public Item GuitarItem { get; private set; } = null!;

        [LoadFromBundle("ViolinObj.asset")]
        public Item ViolinItem { get; private set; } = null!;
    }

    public ZortAssets Zort { get; private set; } = null!;
    public PjonkTurkeyAssets PjonkTurkey { get; private set; } = null!;
    public WalletAssets Wallet { get; private set; } = null!;
    public HoverboardAssets Hoverboard { get; private set; } = null!;
    public EpicAxeAssets EpicAxe { get; private set; } = null!;
    public SnowGlobeAssets SnowGlobe { get; private set; } = null!;
    public NaturesMaceAssets NaturesMace { get; private set; } = null!;
    public IcyHammerAssets IcyHammer { get; private set; } = null!;
    public SpikyMaceAssets SpikyMace { get; private set; } = null!;

    public ItemHandler()
    {
        if (Plugin.ModConfig.ConfigZortAddonsEnabled.Value)
        {
            Zort = new ZortAssets("zortassets");
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigZortGuitarSpawnWeights.Value, Zort.GuitarItem, -1, -1);
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigZortViolinSpawnWeights.Value, Zort.ViolinItem, -1, -1);
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigZortRecorderSpawnWeights.Value, Zort.RecorderItem, -1, -1);
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigZortAccordionSpawnWeights.Value, Zort.AccordionItem, -1, -1);
        }

        if (Plugin.ModConfig.ConfigWalletEnabled.Value)
        {
            Wallet = new WalletAssets("walletassets");
            if (Plugin.ModConfig.ConfigWalletMode.Value) RegisterShopItemWithConfig(false, Wallet.WalletItemOld, Wallet.WalletTerminalNode, Plugin.ModConfig.ConfigWalletCost.Value, "", -1, -1);
            else RegisterShopItemWithConfig(true, Wallet.WalletItemNew, Wallet.WalletTerminalNode, Plugin.ModConfig.ConfigWalletCost.Value, "", -1, -1);
        }

        if (Plugin.ModConfig.ConfigHoverboardEnabled.Value)
        {
            Hoverboard = new HoverboardAssets("hoverboardassets");
            RegisterShopItemWithConfig(false, Hoverboard.HoverboardItem, Hoverboard.HoverboardTerminalNode, Plugin.ModConfig.ConfigHoverboardCost.Value, "", -1, -1);
        }

        if (Plugin.ModConfig.ConfigEpicAxeScrapEnabled.Value)
        {
            EpicAxe = new EpicAxeAssets("epicaxeassets");
            int[] scrapValues = ChangeItemValues(Plugin.ModConfig.ConfigEpicAxeWorth.Value);
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigEpicAxeScrapSpawnWeights.Value, EpicAxe.EpicAxeItem, scrapValues[0], scrapValues[1]);
        }

        if (Plugin.ModConfig.ConfigSnowGlobeEnabled.Value)
        {
            SnowGlobe = new SnowGlobeAssets("snowglobeassets");
            int[] scrapValues = ChangeItemValues(Plugin.ModConfig.ConfigSnowGlobeWorth.Value);
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigSnowGlobeSpawnWeights.Value, SnowGlobe.SnowGlobeItem, scrapValues[0], scrapValues[1]);
        }

        if (Plugin.ModConfig.ConfigNaturesMaceScrapEnabled.Value)
        {
            NaturesMace = new NaturesMaceAssets("naturesmaceassets");
            int[] scrapValues = ChangeItemValues(Plugin.ModConfig.ConfigNaturesMaceWorth.Value);
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigNaturesMaceScrapSpawnWeights.Value, NaturesMace.NatureMaceItem, scrapValues[0], scrapValues[1]);
        }

        if (Plugin.ModConfig.ConfigIcyHammerScrapEnabled.Value)
        {
            IcyHammer = new IcyHammerAssets("icyhammerassets");
            int[] scrapValues = ChangeItemValues(Plugin.ModConfig.ConfigIcyHammerWorth.Value);
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigIcyHammerScrapSpawnWeights.Value, IcyHammer.IcyHammerItem, scrapValues[0], scrapValues[1]);
        }

        if (Plugin.ModConfig.ConfigSpikyMaceScrapEnabled.Value)
        {
            SpikyMace = new SpikyMaceAssets("spikymaceassets");
            int[] scrapValues = ChangeItemValues(Plugin.ModConfig.ConfigSpikyMaceWorth.Value);
            RegisterScrapWithConfig(Plugin.ModConfig.ConfigSpikyMaceScrapSpawnWeights.Value, SpikyMace.SpikyMaceItem, scrapValues[0], scrapValues[1]);
        }

        if (Plugin.ModConfig.ConfigPjonkTurkeyEnabled.Value)
        {
            PjonkTurkey = new PjonkTurkeyAssets("pjonkturkeyassets");
            Plugin.samplePrefabs.Add("Pjonk Turkey", PjonkTurkey.PjonkTurkeyItem);
            RegisterScrapWithConfig("All:0", PjonkTurkey.PjonkTurkeyItem, -1, -1);
        }
    }
}