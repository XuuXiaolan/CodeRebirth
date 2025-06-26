using LethalLib.Extras;
using UnityEngine;
using CodeRebirthLib.ContentManagement;
using CodeRebirthLib.AssetManagement;
using CodeRebirthLib;

namespace CodeRebirth.src.Content.Unlockables;
public class UnlockableHandler : ContentHandler<UnlockableHandler>
{
    public class ShockwaveBotAssets(CRMod mod, string filePath) : AssetBundleLoader<ShockwaveBotAssets>(mod, filePath)
    {
        [LoadFromBundle("LaserShockBlast.prefab")]
        public GameObject LasetShockBlast { get; private set; } = null!;

        [LoadFromBundle("ShockWaveDrone.prefab")]
        public GameObject ShockWaveDronePrefab { get; private set; } = null!;
    }

    public class PlantPotAssets(CRMod mod, string filePath) : AssetBundleLoader<PlantPotAssets>(mod, filePath)
    {
    }

    public class SeamineTinkAssets(CRMod mod, string filePath) : AssetBundleLoader<SeamineTinkAssets>(mod, filePath)
    {
        [LoadFromBundle("SeamineGal.prefab")]
        public GameObject SeamineGalPrefab { get; private set; } = null!;
    }

    public class TerminalBotAssets(CRMod mod, string filePath) : AssetBundleLoader<TerminalBotAssets>(mod, filePath)
    {
        [LoadFromBundle("TerminalGalDaisy.prefab")]
        public GameObject TerminalGalPrefab { get; private set; } = null!;
    }

    public class BellCrabAssets(CRMod mod, string filePath) : AssetBundleLoader<BellCrabAssets>(mod, filePath)
    {
    }

    public class ACUnitGalAssets(CRMod mod, string filePath) : AssetBundleLoader<ACUnitGalAssets>(mod, filePath)
    {
    }

    public class BearTrapGalAssets(CRMod mod, string filePath) : AssetBundleLoader<BearTrapGalAssets>(mod, filePath)
    {
    }

    public class SCP999Assets(CRMod mod, string filePath) : AssetBundleLoader<SCP999Assets>(mod, filePath)
    {
    }

    public class Fishdispenserassets(CRMod mod, string filePath) : AssetBundleLoader<Fishdispenserassets>(mod, filePath)
    {
    }

    public class FriendAssets(CRMod mod, string filePath) : AssetBundleLoader<FriendAssets>(mod, filePath)
    {
        [LoadFromBundle("GlitchedPlushieUnlockable.asset")]
        public UnlockableItemDef GlitchedPlushieUnlockable { get; private set; } = null!;
    }

    public class CleanerDroneGalAssets(CRMod mod, string filePath) : AssetBundleLoader<CleanerDroneGalAssets>(mod, filePath)
    {
        [LoadFromBundle("JaneFogUnlockable.asset")]
        public UnlockableItemDef CleanerDroneGalUnlockable { get; private set; } = null!;
    }

    public class CruiserGalAssets(CRMod mod, string filePath) : AssetBundleLoader<CruiserGalAssets>(mod, filePath)
    {
        [LoadFromBundle("CruiserGal.prefab")]
        public GameObject CruiserGalPrefab { get; private set; } = null!;
    }

    public FriendAssets? Friend = null;
    public Fishdispenserassets? ShrimpDispenser = null;
    public SCP999Assets? SCP999 = null;
    public BellCrabAssets? BellCrab = null;
    public SeamineTinkAssets? SeamineTink = null;
    public TerminalBotAssets? TerminalBot = null;
    public PlantPotAssets? PlantPot = null;
    public ShockwaveBotAssets? ShockwaveBot = null;
    public ACUnitGalAssets? ACUnitGal = null;
    public BearTrapGalAssets? BearTrapGal = null;
    public CleanerDroneGalAssets? CleanerDroneGal = null;
    public CruiserGalAssets? CruiserGal = null;

    public UnlockableHandler(CRMod mod) : base(mod)
    {
        RegisterContent("shockwavebotassets", out ShockwaveBot);

        // RegisterContent("plantpotassets", out PlantPot);

        RegisterContent("terminalbotassets", out TerminalBot);

        RegisterContent("cruisergalassets", out CruiserGal);

        RegisterContent("scp999galassets", out SCP999);

        RegisterContent("fishdispenserassets", out ShrimpDispenser);

        RegisterContent("seaminetinkassets", out SeamineTink);

        RegisterContent("cleanerdronegalassets", out CleanerDroneGal);

        RegisterContent("acunitgalassets", out ACUnitGal);

        RegisterContent("beartrapgalassets", out BearTrapGal);

        RegisterContent("friendassets", out Friend);

        RegisterContent("bellcrabgalassets", out BellCrab);
    }
}