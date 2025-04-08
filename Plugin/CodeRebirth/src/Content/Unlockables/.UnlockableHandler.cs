using CodeRebirth.src.Util.AssetLoading;
using CodeRebirth.src.Util;
using LethalLib.Extras;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class UnlockableHandler : ContentHandler<UnlockableHandler>
{
    public class ShockwaveBotAssets(string bundleName) : AssetBundleLoader<ShockwaveBotAssets>(bundleName)
    {
        [LoadFromBundle("LaserShockBlast.prefab")]
        public GameObject LasetShockBlast { get; private set; } = null!;

        [LoadFromBundle("ShockWaveDrone.prefab")]
        public GameObject ShockWaveDronePrefab { get; private set; } = null!;
    }

    public class PlantPotAssets(string bundleName) : AssetBundleLoader<PlantPotAssets>(bundleName)
    {
    }

    public class SeamineTinkAssets(string bundleName) : AssetBundleLoader<SeamineTinkAssets>(bundleName)
    {
        [LoadFromBundle("SeamineGal.prefab")]
        public GameObject SeamineGalPrefab { get; private set; } = null!;
    }

    public class TerminalBotAssets(string bundleName) : AssetBundleLoader<TerminalBotAssets>(bundleName)
    {
        [LoadFromBundle("TerminalGalDaisy.prefab")]
        public GameObject TerminalGalPrefab { get; private set; } = null!;
    }

    public class BellCrabAssets(string bundleName) : AssetBundleLoader<BellCrabAssets>(bundleName)
    {
    }

    public class ACUnitGalAssets(string bundleName) : AssetBundleLoader<ACUnitGalAssets>(bundleName)
    {
    }

    public class BearTrapGalAssets(string bundleName) : AssetBundleLoader<BearTrapGalAssets>(bundleName)
    {
    }

    public class SCP999Assets(string bundleName) : AssetBundleLoader<SCP999Assets>(bundleName)
    {
    }

    public class Fishdispenserassets(string bundleName) : AssetBundleLoader<Fishdispenserassets>(bundleName)
    {
    }

    public class FriendAssets(string bundleName) : AssetBundleLoader<FriendAssets>(bundleName)
    {
        [LoadFromBundle("GlitchedPlushieUnlockable.asset")]
        public UnlockableItemDef GlitchedPlushieUnlockable { get; private set; } = null!;
    }

    public class CleanerDroneGalAssets(string bundleName) : AssetBundleLoader<CleanerDroneGalAssets>(bundleName)
    {
        [LoadFromBundle("JaneFogUnlockable.asset")]
        public UnlockableItemDef CleanerDroneGalUnlockable { get; private set; } = null!;
    }

    public class CruiserGalAssets(string bundleName) : AssetBundleLoader<CruiserGalAssets>(bundleName)
    {
        [LoadFromBundle("CruiserGal.prefab")]
        public GameObject CruiserGalPrefab { get; private set; } = null!;
    }

    public FriendAssets? Friend { get; private set; } = null;
    public Fishdispenserassets? ShrimpDispenser { get; private set; } = null;
    public SCP999Assets? SCP999 { get; private set; } = null;
    public BellCrabAssets? BellCrab { get; private set; } = null;
    public SeamineTinkAssets? SeamineTink { get; private set; } = null;
    public TerminalBotAssets? TerminalBot { get; private set; } = null;
    public PlantPotAssets? PlantPot { get; private set; } = null;
    public ShockwaveBotAssets? ShockwaveBot { get; private set; } = null;
    public ACUnitGalAssets? ACUnitGal { get; private set; } = null;
    public BearTrapGalAssets? BearTrapGal { get; private set; } = null;
    public CleanerDroneGalAssets? CleanerDroneGal { get; private set; } = null;
    public CruiserGalAssets? CruiserGal { get; private set; } = null;

    public UnlockableHandler()
    {

        ShockwaveBot = LoadAndRegisterAssets<ShockwaveBotAssets>("shockwavebotassets");

        PlantPot = LoadAndRegisterAssets<PlantPotAssets>("plantpotassets");

        TerminalBot = LoadAndRegisterAssets<TerminalBotAssets>("terminalbotassets");

        CruiserGal = LoadAndRegisterAssets<CruiserGalAssets>("cruisergalassets");

        SCP999 = LoadAndRegisterAssets<SCP999Assets>("scp999galassets");

        ShrimpDispenser = LoadAndRegisterAssets<Fishdispenserassets>("fishdispenserassets");

        SeamineTink = LoadAndRegisterAssets<SeamineTinkAssets>("seaminetinkassets");

        CleanerDroneGal = LoadAndRegisterAssets<CleanerDroneGalAssets>("cleanerdronegalassets");

        ACUnitGal = LoadAndRegisterAssets<ACUnitGalAssets>("acunitgalassets");

        BearTrapGal = LoadAndRegisterAssets<BearTrapGalAssets>("beartrapgalassets");

        Friend = LoadAndRegisterAssets<FriendAssets>("friendassets");

        BellCrab = LoadAndRegisterAssets<BellCrabAssets>("bellcrabgalassets");
    }
}