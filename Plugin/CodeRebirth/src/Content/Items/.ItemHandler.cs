﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class ItemHandler : ContentHandler<ItemHandler>
{
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

    public class XuAndRigoAssets(string bundleName) : AssetBundleLoader<XuAndRigoAssets>(bundleName)
    {
        [LoadFromBundle("GoldRigoObj.asset")]
        public Item GoldRigoItem { get; private set; } = null!;

        [LoadFromBundle("SmallRigoPrefab.prefab")]
        public GameObject SmallRigoPrefab { get; private set; } = null!;

        [LoadFromBundle("XuObj.asset")]
        public Item XuItem { get; private set; } = null!;
    }

    public XuAndRigoAssets XuAndRigo { get; private set; } = null!;
    public ZortAssets Zort { get; private set; } = null!;
    public HoverboardAssets Hoverboard { get; private set; } = null!;
    public EpicAxeAssets EpicAxe { get; private set; } = null!;
    public SnowGlobeAssets SnowGlobe { get; private set; } = null!;
    public NaturesMaceAssets NaturesMace { get; private set; } = null!;
    public IcyHammerAssets IcyHammer { get; private set; } = null!;
    public SpikyMaceAssets SpikyMace { get; private set; } = null!;

    public ItemHandler()
    {
        /*if (Plugin.ModConfig.ConfigXuAndRigoEnabled.Value)
        {
            XuAndRigo = new XuAndRigoAssets("xuandrigoassets");
            RegisterScrapWithConfig("", XuAndRigo.GoldRigoItem, -1, -1);
            RegisterScrapWithConfig("", XuAndRigo.XuItem, -1, -1);
        }*/

        if (Plugin.ModConfig.ConfigZortAddonsEnabled.Value)
        {
            Zort = new ZortAssets("zortassets");
            RegisterShopItemWithConfig(false, true, Zort.GuitarItem, null, 0, Plugin.ModConfig.ConfigZortGuitarSpawnWeights.Value, "-1,-1");
            RegisterShopItemWithConfig(false, true, Zort.ViolinItem, null, 0, Plugin.ModConfig.ConfigZortViolinSpawnWeights.Value, "-1,-1");
            RegisterShopItemWithConfig(false, true, Zort.RecorderItem, null, 0, Plugin.ModConfig.ConfigZortRecorderSpawnWeights.Value, "-1,-1");
            RegisterShopItemWithConfig(false, true, Zort.AccordionItem, null, 0, Plugin.ModConfig.ConfigZortAccordionSpawnWeights.Value, "-1,-1");
        }

        if (Plugin.ModConfig.ConfigHoverboardEnabled.Value)
        {
            Hoverboard = new HoverboardAssets("hoverboardassets");
            RegisterShopItemWithConfig(true, false, Hoverboard.HoverboardItem, Hoverboard.HoverboardTerminalNode, Plugin.ModConfig.ConfigHoverboardCost.Value, "", "-1,-1");
        }

        if (Plugin.ModConfig.ConfigEpicAxeScrapEnabled.Value)
        {
            EpicAxe = new EpicAxeAssets("epicaxeassets");
            RegisterShopItemWithConfig(false, true, EpicAxe.EpicAxeItem, null, 0, Plugin.ModConfig.ConfigEpicAxeScrapSpawnWeights.Value, Plugin.ModConfig.ConfigEpicAxeWorth.Value);
        }

        if (Plugin.ModConfig.ConfigSnowGlobeEnabled.Value)
        {
            SnowGlobe = new SnowGlobeAssets("snowglobeassets");
            RegisterShopItemWithConfig(false, true, SnowGlobe.SnowGlobeItem, null, 0, Plugin.ModConfig.ConfigSnowGlobeSpawnWeights.Value, Plugin.ModConfig.ConfigSnowGlobeWorth.Value);
        }

        if (Plugin.ModConfig.ConfigNaturesMaceScrapEnabled.Value)
        {
            NaturesMace = new NaturesMaceAssets("naturesmaceassets");
            RegisterShopItemWithConfig(false, true, NaturesMace.NatureMaceItem, null, 0, Plugin.ModConfig.ConfigNaturesMaceScrapSpawnWeights.Value, Plugin.ModConfig.ConfigNaturesMaceWorth.Value);
        }

        if (Plugin.ModConfig.ConfigIcyHammerScrapEnabled.Value)
        {
            IcyHammer = new IcyHammerAssets("icyhammerassets");
            RegisterShopItemWithConfig(false, true, IcyHammer.IcyHammerItem, null, 0, Plugin.ModConfig.ConfigIcyHammerScrapSpawnWeights.Value, Plugin.ModConfig.ConfigIcyHammerWorth.Value);
        }

        if (Plugin.ModConfig.ConfigSpikyMaceScrapEnabled.Value)
        {
            SpikyMace = new SpikyMaceAssets("spikymaceassets");
            RegisterShopItemWithConfig(false, true, SpikyMace.SpikyMaceItem, null, 0, Plugin.ModConfig.ConfigSpikyMaceScrapSpawnWeights.Value, Plugin.ModConfig.ConfigSpikyMaceWorth.Value);
        }
    }
}