using CodeRebirth.src.Content.Moons;
using Dusk;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class ItemHandler : ContentHandler<ItemHandler>
{
    public class HoverboardAssets(DuskMod mod, string filePath) : AssetBundleLoader<HoverboardAssets>(mod, filePath)
    {
    }

    public class BrrreakerAssets(DuskMod mod, string filePath) : AssetBundleLoader<BrrreakerAssets>(mod, filePath)
    {
    }

    public class TurbulenceAssets(DuskMod mod, string filePath) : AssetBundleLoader<TurbulenceAssets>(mod, filePath)
    {
    }

    public class MarrowSplitterAssets(DuskMod mod, string filePath) : AssetBundleLoader<MarrowSplitterAssets>(mod, filePath)
    {
    }

    public class SwatterAssets(DuskMod mod, string filePath) : AssetBundleLoader<SwatterAssets>(mod, filePath)
    {
    }

    public class TomaHopAssets(DuskMod mod, string filePath) : AssetBundleLoader<TomaHopAssets>(mod, filePath)
    {
    }

    public class SnowGlobeAssets(DuskMod mod, string filePath) : AssetBundleLoader<SnowGlobeAssets>(mod, filePath)
    {
    }

    public class ZortAssets(DuskMod mod, string filePath) : AssetBundleLoader<ZortAssets>(mod, filePath)
    {
    }

    public class XuAndRigoAssets(DuskMod mod, string filePath) : AssetBundleLoader<XuAndRigoAssets>(mod, filePath)
    {
        [LoadFromBundle("RodFollower.prefab")]
        public GameObject SmallRigoPrefab { get; private set; } = null!;
    }

    public class MoonUnlockerAssets(DuskMod mod, string filePath) : AssetBundleLoader<MoonUnlockerAssets>(mod, filePath)
    {
    }

    public class OxydeLoreAssets(DuskMod mod, string filePath) : AssetBundleLoader<OxydeLoreAssets>(mod, filePath)
    {
    }

    public class CodeRebirthPlushiesAssets(DuskMod mod, string filePath) : AssetBundleLoader<CodeRebirthPlushiesAssets>(mod, filePath)
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
    public CodeRebirthPlushiesAssets? CRPlushies = null;

    public ItemHandler(DuskMod mod) : base(mod)
    {
        RegisterContent("oxydeloreassets", out OxydeLore, MoonHandler.Instance.Oxyde != null);

        RegisterContent("moonunlockerassets", out MoonUnlocker, MoonHandler.Instance.Oxyde != null);

        RegisterContent("xuandrigoassets", out XuAndRigo, MoonHandler.Instance.Oxyde != null);

        RegisterContent("zortassets", out Zort);

        RegisterContent("hoverboardassets", out Hoverboard);

        RegisterContent("snowglobeassets", out SnowGlobe);

        RegisterContent("mountaineerassets", out Brrreaker);

        RegisterContent("turbulenceassets", out Turbulence);

        RegisterContent("marrowsplitterassets", out MarrowSplitter);

        // RegisterContent("swatterassets", out Swatter);

        RegisterContent("tomahopassets", out TomaHop);

        RegisterContent("coderebirthplushiesassets", out CRPlushies);
    }
}