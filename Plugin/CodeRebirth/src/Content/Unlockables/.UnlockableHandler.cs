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

    public UnlockableHandler(CRMod mod) : base(mod)
    {
        if (TryLoadContentBundle("shockwavebotassets", out ShockwaveBotAssets? shockwaveBotAssets))
        {
            ShockwaveBot = shockwaveBotAssets;
            LoadAllContent(shockwaveBotAssets!);
        }

        if (TryLoadContentBundle("plantpotassets", out PlantPotAssets? plantPotAssets))
        {
            PlantPot = plantPotAssets;
            LoadAllContent(plantPotAssets!);
        }

        if (TryLoadContentBundle("terminalbotassets", out TerminalBotAssets? terminalBotAssets))
        {
            TerminalBot = terminalBotAssets;
            LoadAllContent(terminalBotAssets!);
        }

        if (TryLoadContentBundle("cruisergalassets", out CruiserGalAssets? cruiserGalAssets))
        {
            CruiserGal = cruiserGalAssets;
            LoadAllContent(cruiserGalAssets!);
        }

        if (TryLoadContentBundle("scp999galassets", out SCP999Assets? scp999Assets))
        {
            SCP999 = scp999Assets;
            LoadAllContent(scp999Assets!);
        }

        if (TryLoadContentBundle("fishdispenserassets", out Fishdispenserassets? shrimpDispenserAssets))
        {
            ShrimpDispenser = shrimpDispenserAssets;
            LoadAllContent(shrimpDispenserAssets!);
        }

        if (TryLoadContentBundle("seaminetinkassets", out SeamineTinkAssets? seamineTinkAssets))
        {
            SeamineTink = seamineTinkAssets;
            LoadAllContent(seamineTinkAssets!);
        }

        if (TryLoadContentBundle("cleanerdronegalassets", out CleanerDroneGalAssets? cleanerDroneGalAssets))
        {
            CleanerDroneGal = cleanerDroneGalAssets;
            LoadAllContent(cleanerDroneGalAssets!);
        }

        if (TryLoadContentBundle("acunitgalassets", out ACUnitGalAssets? acUnitGalAssets))
        {
            ACUnitGal = acUnitGalAssets;
            LoadAllContent(acUnitGalAssets!);
        }

        if (TryLoadContentBundle("beartrapgalassets", out BearTrapGalAssets? bearTrapGalAssets))
        {
            BearTrapGal = bearTrapGalAssets;
            LoadAllContent(bearTrapGalAssets!);
        }

        if (TryLoadContentBundle("friendassets", out FriendAssets? friendAssets))
        {
            Friend = friendAssets;
            LoadAllContent(friendAssets!);
        }

        if (TryLoadContentBundle("bellcrabgalassets", out BellCrabAssets? bellCrabAssets))
        {
            BellCrab = bellCrabAssets;
            LoadAllContent(bellCrabAssets!);
        }
    }
}