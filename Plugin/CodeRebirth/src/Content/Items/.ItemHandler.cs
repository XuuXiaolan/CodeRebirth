﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class ItemHandler : ContentHandler<ItemHandler>
{
    public class HoverboardAssets(string bundleName) : AssetBundleLoader<HoverboardAssets>(bundleName)
    {
    }

    public class BrrreakerAssets(string bundleName) : AssetBundleLoader<BrrreakerAssets>(bundleName)
    {
    }

    public class TurbulenceAssets(string bundleName) : AssetBundleLoader<TurbulenceAssets>(bundleName)
    {
    }

    public class MarrowSplitterAssets(string bundleName) : AssetBundleLoader<MarrowSplitterAssets>(bundleName)
    {
    }

    public class SwatterAssets(string bundleName) : AssetBundleLoader<SwatterAssets>(bundleName)
    {
    }

    public class TomaHopAssets(string bundleName) : AssetBundleLoader<TomaHopAssets>(bundleName)
    {
    }

    public class SnowGlobeAssets(string bundleName) : AssetBundleLoader<SnowGlobeAssets>(bundleName)
    {
    }

    public class ZortAssets(string bundleName) : AssetBundleLoader<ZortAssets>(bundleName)
    {
    }

    public class XuAndRigoAssets(string bundleName) : AssetBundleLoader<XuAndRigoAssets>(bundleName)
    {
        [LoadFromBundle("RodFollower.prefab")]
        public GameObject SmallRigoPrefab { get; private set; } = null!;
    }

    public XuAndRigoAssets? XuAndRigo { get; private set; } = null;
    public ZortAssets? Zort { get; private set; } = null;
    public HoverboardAssets? Hoverboard { get; private set; } = null;
    public SnowGlobeAssets? SnowGlobe { get; private set; } = null;
    public BrrreakerAssets? Brrreaker { get; private set; } = null;
    public TurbulenceAssets? Turbulence { get; private set; } = null;
    public MarrowSplitterAssets? MarrowSplitter { get; private set; } = null;
    public SwatterAssets? Swatter { get; private set; } = null;
    public TomaHopAssets? TomaHop { get; private set; } = null;

    public ItemHandler()
    {

        XuAndRigo = LoadAndRegisterAssets<XuAndRigoAssets>("xuandrigoassets");

        Zort = LoadAndRegisterAssets<ZortAssets>("zortassets");

        Hoverboard = LoadAndRegisterAssets<HoverboardAssets>("hoverboardassets");

        SnowGlobe = LoadAndRegisterAssets<SnowGlobeAssets>("snowglobeassets");

        Brrreaker = LoadAndRegisterAssets<BrrreakerAssets>("mountaineerassets");

        Turbulence = LoadAndRegisterAssets<TurbulenceAssets>("turbulenceassets");

        MarrowSplitter = LoadAndRegisterAssets<MarrowSplitterAssets>("marrowsplitterassets");

        Swatter = LoadAndRegisterAssets<SwatterAssets>("swatterassets");

        TomaHop = LoadAndRegisterAssets<TomaHopAssets>("tomahopassets");
    }
}