using System.Collections.Generic;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

public class EnemyHandler : ContentHandler<EnemyHandler>
{
    public class SnailCatAssets(string bundleName) : AssetBundleLoader<SnailCatAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("SnailCatEnemyObj.asset")]
        public CREnemyDefinition SnailCatEnemyDefinition { get; private set; } = null!;

        [LoadFromBundle("SnailCatItemObj.asset")]
        public CRItemDefinition SnailCatItemDefinition { get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [SnailCatEnemyDefinition];

        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class CarnivorousPlantAssets(string bundleName ) : AssetBundleLoader<CarnivorousPlantAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("CarnivorousPlantEnemyDefinition.asset")]
        public CREnemyDefinition FirstEnemyDefinition { get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [FirstEnemyDefinition];

        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class RedwoodTitanAssets(string bundleName) : AssetBundleLoader<RedwoodTitanAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("RedwoodEnemyDefinition.asset")]
        public CREnemyDefinition FirstEnemyDefinition { get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [FirstEnemyDefinition];
    
        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class DuckSongAssets(string bundleName) : AssetBundleLoader<DuckSongAssets>(bundleName), IEnemyAssets, IItemAssets
    {
        [LoadFromBundle("DuckSongEnemyDefinition.asset")]
        public CREnemyDefinition DuckSongEnemyDefinition { get; private set; } = null!;

        [LoadFromBundle("GrapeItemDefinition.asset")]
        public CRItemDefinition GrapeItemDefinition { get; private set; } = null!;

        [LoadFromBundle("LemonadePitcherItemDefinition.asset")]
        public CRItemDefinition LemonadePitcherItemDefinition { get; private set; } = null!;

        [LoadFromBundle("DuckHolder.prefab")]
        public GameObject DuckUIPrefab { get; private set; } = null!;

        public IReadOnlyList<CRItemDefinition> ItemDefinitions =>
            [GrapeItemDefinition, LemonadePitcherItemDefinition];

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [DuckSongEnemyDefinition];

        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class ManorLordAssets(string bundleName) : AssetBundleLoader<ManorLordAssets>(bundleName), IEnemyAssets, IItemAssets
    {
        [LoadFromBundle("ManorLordEnemyDefinition.asset")]
        public CREnemyDefinition ManorLordEnemyDefinition { get; private set; } = null!;

        [LoadFromBundle("PuppetItemDefinition.asset")]
        public CRItemDefinition PuppetItemDefinition { get; private set; } = null!;

        [LoadFromBundle("PinNeedleItemDefinition.asset")]
        public CRItemDefinition PinNeedleItemDefinition { get; private set; } = null!;

        [LoadFromBundle("PuppeteerPuppet.prefab")]
        public GameObject PuppeteerPuppetPrefab { get; private set; } = null!;

        public IReadOnlyList<CRItemDefinition> ItemDefinitions =>
            [PinNeedleItemDefinition, PuppetItemDefinition];

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [ManorLordEnemyDefinition];
    
        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class MistressAssets(string bundleName) : AssetBundleLoader<MistressAssets>(bundleName), IEnemyAssets, IItemAssets
    {
        [LoadFromBundle("MistressEnemyDefinition.asset")]
        public CREnemyDefinition MistressEnemyDefinition { get; private set; } = null!;

        [LoadFromBundle("LeChoppedHeadItemDefinition.asset")]
        public CRItemDefinition LeChoppedHeadItemDefinition { get; private set; } = null!;

        [LoadFromBundle("GuillotinePrefab.prefab")]
        public GameObject GuillotinePrefab { get; private set; } = null!;

        public IReadOnlyList<CRItemDefinition> ItemDefinitions =>
            [LeChoppedHeadItemDefinition];

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [MistressEnemyDefinition];
    
        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class JanitorAssets(string bundleName) : AssetBundleLoader<JanitorAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("JanitorEnemyDefinition.asset")]
        public CREnemyDefinition JanitorEnemyDefinition { get; private set; } = null!;

        [LoadFromBundle("JanitorTrash.prefab")]
        public GameObject TrashCanPrefab { get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [JanitorEnemyDefinition];
    
        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class TransporterAssets(string bundleName) : AssetBundleLoader<TransporterAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("TransporterEnemyDefinition.asset")]
        public CREnemyDefinition TransporterEnemyDefinition { get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [TransporterEnemyDefinition];
    
        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class MonarchAssets(string bundleName) : AssetBundleLoader<MonarchAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("MonarchEnemyDefinition.asset")]
        public CREnemyDefinition MonarchEnemyDefinition { get; private set; } = null!;

        [LoadFromBundle("CutieflyEnemyDefinition.asset")]
        public CREnemyDefinition CutieflyEnemyDefinition{ get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [MonarchEnemyDefinition, CutieflyEnemyDefinition];

        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class PandoraAssets(string bundleName) : AssetBundleLoader<PandoraAssets>(bundleName)
    {
        [LoadFromBundle("PandoraObj.asset")]
        public EnemyType PandoraEnemyType { get; private set; } = null!;

        public AssetBundleData? AssetBundleData { get; private set; } = null;
    }

    public class NancyAssets(string bundleName) : AssetBundleLoader<NancyAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("NancyEnemyDefinition.asset")]
        public CREnemyDefinition NancyEnemyDefinition { get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [NancyEnemyDefinition];
    
        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public class DriftwoodMenaceAssets(string bundleName) : AssetBundleLoader<DriftwoodMenaceAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("DriftwoodEnemyDefinition.asset")]
        public CREnemyDefinition DriftwoodEnemyDefinition { get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            [DriftwoodEnemyDefinition];

        public AssetBundleData? AssetBundleData { get; set; } = null;
    }

    public NancyAssets? Nancy { get; private set; }
    public DriftwoodMenaceAssets? DriftwoodMenace { get; private set; }
    public PandoraAssets? Pandora { get; private set; }
    public MonarchAssets? Monarch { get; private set; }
    public MistressAssets? Mistress { get; private set; }
    public TransporterAssets? Transporter { get; private set; }
    public JanitorAssets? Janitor { get; private set; }
    public ManorLordAssets? ManorLord { get; private set; }
    public DuckSongAssets? DuckSong { get; private set; }
    public SnailCatAssets? SnailCat { get; private set; }
    public CarnivorousPlantAssets? CarnivorousPlant { get; private set; }
    public RedwoodTitanAssets? RedwoodTitan { get; private set; }

    public EnemyHandler()
    {

#if DEBUG
        Pandora = new PandoraAssets("pandoraassets");
        RegisterEnemyWithConfig("", Pandora.PandoraEnemyType, null, null, 3, 0);
#endif

        DriftwoodMenace = LoadAndRegisterAssets<DriftwoodMenaceAssets>("driftwoodmenaceassets");
        RegisterEnemyAssets(DriftwoodMenace);

        Nancy = LoadAndRegisterAssets<NancyAssets>("nancyassets");
        RegisterEnemyAssets(Nancy);

        Monarch = LoadAndRegisterAssets<MonarchAssets>("monarchassets");
        RegisterEnemyAssets(Monarch);

        Mistress = LoadAndRegisterAssets<MistressAssets>("mistressassets");
        RegisterEnemyAssets(Mistress);
        RegisterItemAssets(Mistress);

        RedwoodTitan = LoadAndRegisterAssets<RedwoodTitanAssets>("redwoodtitanassets");
        RegisterEnemyAssets(RedwoodTitan);

        CarnivorousPlant = LoadAndRegisterAssets<CarnivorousPlantAssets>("carnivorousplantassets");
        RegisterEnemyAssets(CarnivorousPlant);

        SnailCat = LoadAndRegisterAssets<SnailCatAssets>("snailcatassets");
        RegisterEnemyAssets(SnailCat);

        DuckSong = LoadAndRegisterAssets<DuckSongAssets>("ducksongassets");
        RegisterEnemyAssets(DuckSong);
        RegisterItemAssets(DuckSong);
        if (DuckSong != null)
        {
            Plugin.samplePrefabs.Add(DuckSong.GrapeItemDefinition.item.itemName, DuckSong.GrapeItemDefinition.item);
            Plugin.samplePrefabs.Add(DuckSong.LemonadePitcherItemDefinition.item.itemName, DuckSong.LemonadePitcherItemDefinition.item);
        }

        Transporter = LoadAndRegisterAssets<TransporterAssets>("transporterassets");
        RegisterEnemyAssets(Transporter);

        ManorLord = LoadAndRegisterAssets<ManorLordAssets>("manorlordassets");
        RegisterEnemyAssets(ManorLord);
        RegisterItemAssets(ManorLord);

        Janitor = LoadAndRegisterAssets<JanitorAssets>("janitorassets");
        RegisterEnemyAssets(Janitor);
        if (Janitor != null)
        {
            RegisterInsideMapObjectWithConfig(Janitor.TrashCanPrefab, "Vanilla - 0.00,5.00 ; 0.11,6.49 ; 0.22,6.58 ; 0.33,6.40 ; 0.44,8.22 ; 0.56,9.55 ; 0.67,10.02 ; 0.78,10.01 ; 0.89,9.88 ; 1.00,10.00 | Custom - 0.00,5.00 ; 0.11,6.49 ; 0.22,6.58 ; 0.33,6.40 ; 0.44,8.22 ; 0.56,9.55 ; 0.67,10.02 ; 0.78,10.01 ; 0.89,9.88 ; 1.00,10.00");
        }
    }
}