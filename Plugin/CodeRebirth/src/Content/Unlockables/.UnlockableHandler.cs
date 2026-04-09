using Dusk;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;

public class UnlockableHandler : ContentHandler<UnlockableHandler>
{
    public class ShockwaveBotAssets(DuskMod mod, string filePath) : AssetBundleLoader<ShockwaveBotAssets>(mod, filePath)
    {
        [LoadFromBundle("LaserShockBlast.prefab")]
        public GameObject LaserShockBlast { get; private set; } = null!;
    }

    public class SeamineTinkAssets(DuskMod mod, string filePath) : AssetBundleLoader<SeamineTinkAssets>(mod, filePath)
    {
    }

    public class TerminalBotAssets(DuskMod mod, string filePath) : AssetBundleLoader<TerminalBotAssets>(mod, filePath)
    {
    }

    public class SCP999Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP999Assets>(mod, filePath)
    {
    }

    public class Fishdispenserassets(DuskMod mod, string filePath) : AssetBundleLoader<Fishdispenserassets>(mod, filePath)
    {
    }

    public class FriendAssets(DuskMod mod, string filePath) : AssetBundleLoader<FriendAssets>(mod, filePath)
    {
    }

    public class CruiserGalAssets(DuskMod mod, string filePath) : AssetBundleLoader<CruiserGalAssets>(mod, filePath)
    {
    }

    public class CodeRebirthPlatinumAssets(DuskMod mod, string filePath) : AssetBundleLoader<CodeRebirthPlatinumAssets>(mod, filePath)
    {
    }

    public class TimmyBassAssets(DuskMod mod, string filePath) : AssetBundleLoader<TimmyBassAssets>(mod, filePath)
    {
    }

    public class GalsDecorAssets(DuskMod mod, string filePath) : AssetBundleLoader<GalsDecorAssets>(mod, filePath)
    {
    }

    public FriendAssets? Friend = null;
    public Fishdispenserassets? ShrimpDispenser = null;
    public SCP999Assets? SCP999 = null;
    public SeamineTinkAssets? SeamineTink = null;
    public TerminalBotAssets? TerminalBot = null;
    public ShockwaveBotAssets? ShockwaveBot = null;
    public CruiserGalAssets? CruiserGal = null;
    public CodeRebirthPlatinumAssets? CodeRebirthPlatinum = null;
    public TimmyBassAssets? TimmyBass = null;
    public GalsDecorAssets? GalsDecor = null;

    public UnlockableHandler(DuskMod mod) : base(mod)
    {
        RegisterContent("coderebirthplatinumassets", out CodeRebirthPlatinum);

        RegisterContent("shockwavebotassets", out ShockwaveBot);

        RegisterContent("galsdecorassets", out GalsDecor);

        RegisterContent("terminalbotassets", out TerminalBot);

        RegisterContent("cruisergalassets", out CruiserGal);

        RegisterContent("scp999galassets", out SCP999);

        RegisterContent("fishdispenserassets", out ShrimpDispenser);

        RegisterContent("seaminetinkassets", out SeamineTink);

        RegisterContent("friendassets", out Friend);

        RegisterContent("timmybassassets", out TimmyBass);
    }
}