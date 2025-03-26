﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class ItemHandler : ContentHandler<ItemHandler>
{
    public class HoverboardAssets(string bundleName) : AssetBundleLoader<HoverboardAssets>(bundleName)
    {
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
    }

    public class ZortAssets(string bundleName) : AssetBundleLoader<ZortAssets>(bundleName)
    {
    }

    public class XuAndRigoAssets(string bundleName) : AssetBundleLoader<XuAndRigoAssets>(bundleName)
    {
        /*[LoadFromBundle("SmallRigoPrefab.prefab")]
        public GameObject SmallRigoPrefab { get; private set; } = null!;*/
    }

    public XuAndRigoAssets? XuAndRigo { get; private set; } = null;
    public ZortAssets? Zort { get; private set; } = null;
    public HoverboardAssets? Hoverboard { get; private set; } = null;
    public EpicAxeAssets? EpicAxe { get; private set; } = null;
    public SnowGlobeAssets? SnowGlobe { get; private set; } = null;
    public NaturesMaceAssets? NaturesMace { get; private set; } = null;
    public IcyHammerAssets? IcyHammer { get; private set; } = null;
    public SpikyMaceAssets? SpikyMace { get; private set; } = null;

    public ItemHandler()
    {

        XuAndRigo = LoadAndRegisterAssets<XuAndRigoAssets>("xuandrigoassets");

        Zort = LoadAndRegisterAssets<ZortAssets>("zortassets");

        Hoverboard = LoadAndRegisterAssets<HoverboardAssets>("hoverboardassets");

        SnowGlobe = LoadAndRegisterAssets<SnowGlobeAssets>("snowglobeassets");

        /*if (Plugin.ModConfig.ConfigEpicAxeScrapEnabled.Value)
        {
            EpicAxe = new EpicAxeAssets("epicaxeassets");
            RegisterShopItemWithConfig(false, true, EpicAxe.EpicAxeItem, null, 0, Plugin.ModConfig.ConfigEpicAxeScrapSpawnWeights.Value, Plugin.ModConfig.ConfigEpicAxeWorth.Value);
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
        }*/
    }
}