using CodeRebirthLib;
using CodeRebirthLib.AssetManagement;
using CodeRebirthLib.ContentManagement;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class ItemHandler : ContentHandler<ItemHandler>
{
    public class HoverboardAssets(CRMod mod, string filePath) : AssetBundleLoader<HoverboardAssets>(mod, filePath)
    {
    }

    public class BrrreakerAssets(CRMod mod, string filePath) : AssetBundleLoader<BrrreakerAssets>(mod, filePath)
    {
    }

    public class TurbulenceAssets(CRMod mod, string filePath) : AssetBundleLoader<TurbulenceAssets>(mod, filePath)
    {
    }

    public class MarrowSplitterAssets(CRMod mod, string filePath) : AssetBundleLoader<MarrowSplitterAssets>(mod, filePath)
    {
    }

    public class SwatterAssets(CRMod mod, string filePath) : AssetBundleLoader<SwatterAssets>(mod, filePath)
    {
    }

    public class TomaHopAssets(CRMod mod, string filePath) : AssetBundleLoader<TomaHopAssets>(mod, filePath)
    {
    }

    public class SnowGlobeAssets(CRMod mod, string filePath) : AssetBundleLoader<SnowGlobeAssets>(mod, filePath)
    {
    }

    public class ZortAssets(CRMod mod, string filePath) : AssetBundleLoader<ZortAssets>(mod, filePath)
    {
    }

    public class XuAndRigoAssets(CRMod mod, string filePath) : AssetBundleLoader<XuAndRigoAssets>(mod, filePath)
    {
        [LoadFromBundle("RodFollower.prefab")]
        public GameObject SmallRigoPrefab { get; private set; } = null!;
    }

    public class MoonUnlockerAssets(CRMod mod, string filePath) : AssetBundleLoader<MoonUnlockerAssets>(mod, filePath)
    {
    }

    public class OxydeLoreAssets(CRMod mod, string filePath) : AssetBundleLoader<OxydeLoreAssets>(mod, filePath)
    {
    }

    public MoonUnlockerAssets? MoonUnlocker { get; private set; } = null;
    public OxydeLoreAssets? OxydeLore { get; private set; } = null;
    public XuAndRigoAssets? XuAndRigo { get; private set; } = null;
    public ZortAssets? Zort { get; private set; } = null;
    public HoverboardAssets? Hoverboard { get; private set; } = null;
    public SnowGlobeAssets? SnowGlobe { get; private set; } = null;
    public BrrreakerAssets? Brrreaker { get; private set; } = null;
    public TurbulenceAssets? Turbulence { get; private set; } = null;
    public MarrowSplitterAssets? MarrowSplitter { get; private set; } = null;
    public SwatterAssets? Swatter { get; private set; } = null;
    public TomaHopAssets? TomaHop { get; private set; } = null;

    public ItemHandler(CRMod mod) : base(mod)
    {
        if (TryLoadContentBundle("oxydeloreassets", out OxydeLoreAssets? oxydeLoreAssets))
        {
            OxydeLore = oxydeLoreAssets;
            LoadAllContent(oxydeLoreAssets!);
        }

        if (TryLoadContentBundle("moonunlockerassets", out MoonUnlockerAssets? moonUnlockerAssets))
        {
            MoonUnlocker = moonUnlockerAssets;
            LoadAllContent(moonUnlockerAssets!);
        }

        if (TryLoadContentBundle("xuandrigoassets", out XuAndRigoAssets? xuAndRigoAssets))
        {
            XuAndRigo = xuAndRigoAssets;
            LoadAllContent(xuAndRigoAssets!);
        }

        if (TryLoadContentBundle("zortassets", out ZortAssets? zortAssets))
        {
            Zort = zortAssets;
            LoadAllContent(zortAssets!);
        }

        if (TryLoadContentBundle("hoverboardassets", out HoverboardAssets? hoverboardAssets))
        {
            Hoverboard = hoverboardAssets;
            LoadAllContent(hoverboardAssets!);
        }

        if (TryLoadContentBundle("snowglobeassets", out SnowGlobeAssets? snowGlobeAssets))
        {
            SnowGlobe = snowGlobeAssets;
            LoadAllContent(snowGlobeAssets!);
        }

        if (TryLoadContentBundle("mountaineerassets", out BrrreakerAssets? brrreakerAssets))
        {
            Brrreaker = brrreakerAssets;
            LoadAllContent(brrreakerAssets!);
        }

        if (TryLoadContentBundle("turbulenceassets", out TurbulenceAssets? turbulenceAssets))
        {
            Turbulence = turbulenceAssets;
            LoadAllContent(turbulenceAssets!);
        }

        if (TryLoadContentBundle("marrowsplitterassets", out MarrowSplitterAssets? marrowSplitterAssets))
        {
            MarrowSplitter = marrowSplitterAssets;
            LoadAllContent(marrowSplitterAssets!);
        }

        if (TryLoadContentBundle("swatterassets", out SwatterAssets? swatterAssets))
        {
            Swatter = swatterAssets;
            LoadAllContent(swatterAssets!);
        }

        if (TryLoadContentBundle("tomahopassets", out TomaHopAssets? tomaHopAssets))
        {
            TomaHop = tomaHopAssets;
            LoadAllContent(tomaHopAssets!);
        }
    }
}