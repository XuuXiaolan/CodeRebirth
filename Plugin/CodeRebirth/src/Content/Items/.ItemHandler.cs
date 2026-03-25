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
        /*[LoadFromBundle("745LogCommandDetails.asset")]
        public LogCommandDetails SevenFourFiveLogCommandDetails { get; private set; } = null!;

        [LoadFromBundle("906LogCommandDetails.asset")]
        public LogCommandDetails NineZeroSixLogCommandDetails { get; private set; } = null!;

        [LoadFromBundle("ArchiveLogCommandDetails.asset")]
        public LogCommandDetails ArchiveLogCommandDetails { get; private set; } = null!;*/
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
    public TomaHopAssets? TomaHop = null;
    public CodeRebirthPlushiesAssets? CRPlushies = null;

    public ItemHandler(DuskMod mod) : base(mod)
    {
        RegisterContent("oxydeloreassets", out OxydeLore);
        /*if (OxydeLore != null)
        {
            DawnLib.DefineTerminalCommand(OxydeLore.SevenFourFiveLogCommandDetails.NamespacedKey, OxydeLore.SevenFourFiveLogCommandDetails.CommandBasicInformation, builder =>
            {
                builder.SetKeywords([OxydeLore.SevenFourFiveLogCommandDetails.MainKeyword]);
                builder.DefineInputCommand(inputCommandBuilder =>
                {
                    inputCommandBuilder.SetResultDisplayText(OxydeLore.SevenFourFiveLogCommandDetails.ResultDisplayText);
                });
            });

            DawnLib.DefineTerminalCommand(OxydeLore.NineZeroSixLogCommandDetails.NamespacedKey, OxydeLore.NineZeroSixLogCommandDetails.CommandBasicInformation, builder =>
            {
                builder.SetKeywords([OxydeLore.NineZeroSixLogCommandDetails.MainKeyword]);
                builder.DefineInputCommand(inputCommandBuilder =>
                {
                    inputCommandBuilder.SetResultDisplayText(OxydeLore.NineZeroSixLogCommandDetails.ResultDisplayText);
                });
            });

            DawnLib.DefineTerminalCommand(OxydeLore.ArchiveLogCommandDetails.NamespacedKey, OxydeLore.ArchiveLogCommandDetails.CommandBasicInformation, builder =>
            {
                builder.SetKeywords([OxydeLore.ArchiveLogCommandDetails.MainKeyword]);
                builder.DefineInputCommand(inputCommandBuilder =>
                {
                    inputCommandBuilder.SetResultDisplayText(OxydeLore.ArchiveLogCommandDetails.ResultDisplayText);
                });
            });
        }*/

        RegisterContent("moonunlockerassets", out MoonUnlocker);

        RegisterContent("xuandrigoassets", out XuAndRigo);

        RegisterContent("zortassets", out Zort);

        RegisterContent("hoverboardassets", out Hoverboard);

        RegisterContent("snowglobeassets", out SnowGlobe);

        RegisterContent("mountaineerassets", out Brrreaker);

        RegisterContent("turbulenceassets", out Turbulence);

        RegisterContent("marrowsplitterassets", out MarrowSplitter);

        RegisterContent("tomahopassets", out TomaHop);

        RegisterContent("coderebirthplushiesassets", out CRPlushies);
    }
}