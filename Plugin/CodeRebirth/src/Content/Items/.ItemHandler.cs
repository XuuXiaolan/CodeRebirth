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

    public MoonUnlockerAssets? MoonUnlocker = null;
    public OxydeLoreAssets? OxydeLore = null;
    public XuAndRigoAssets? XuAndRigo = null;
    public ZortAssets? Zort = null;
    public HoverboardAssets? Hoverboard = null;
    public SnowGlobeAssets? SnowGlobe = null;
    public BrrreakerAssets? Brrreaker = null;
    public TurbulenceAssets? Turbulence = null;
    public MarrowSplitterAssets? MarrowSplitter = null;
    public SwatterAssets? Swatter = null;
    public TomaHopAssets? TomaHop = null;

    public ItemHandler(CRMod mod) : base(mod)
    {
        RegisterContent("oxydeloreassets", out OxydeLore, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("moonunlockerassets", out MoonUnlocker, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("xuandrigoassets", out XuAndRigo, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("zortassets", out Zort);

        RegisterContent("hoverboardassets", out Hoverboard);

        RegisterContent("snowglobeassets", out SnowGlobe);

        RegisterContent("mountaineerassets", out Brrreaker);

        RegisterContent("turbulenceassets", out Turbulence);

        RegisterContent("marrowsplitterassets", out MarrowSplitter);

        // RegisterContent("swatterassets", out Swatter);

        RegisterContent("tomahopassets", out TomaHop);
    }
}