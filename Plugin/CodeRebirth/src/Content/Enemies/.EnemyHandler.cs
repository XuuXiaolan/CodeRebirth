using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

public class EnemyHandler : ContentHandler<EnemyHandler>
{
    public class SnailCatAssets(string bundleName) : AssetBundleLoader<SnailCatAssets>(bundleName)
    {
    }

    public class CarnivorousPlantAssets(string bundleName ) : AssetBundleLoader<CarnivorousPlantAssets>(bundleName)
    {
    }

    public class RedwoodTitanAssets(string bundleName) : AssetBundleLoader<RedwoodTitanAssets>(bundleName)
    {
    }

    public class DuckSongAssets(string bundleName) : AssetBundleLoader<DuckSongAssets>(bundleName)
    {
        [LoadFromBundle("DuckHolder.prefab")]
        public GameObject DuckUIPrefab { get; private set; } = null!;
    }

    public class ManorLordAssets(string bundleName) : AssetBundleLoader<ManorLordAssets>(bundleName)
    {
        [LoadFromBundle("PuppeteerPuppet.prefab")]
        public GameObject PuppeteerPuppetPrefab { get; private set; } = null!;
    }

    public class MistressAssets(string bundleName) : AssetBundleLoader<MistressAssets>(bundleName)
    {
        [LoadFromBundle("GuillotinePrefab.prefab")]
        public GameObject GuillotinePrefab { get; private set; } = null!;
    }

    public class JanitorAssets(string bundleName) : AssetBundleLoader<JanitorAssets>(bundleName)
    {
    }

    public class TransporterAssets(string bundleName) : AssetBundleLoader<TransporterAssets>(bundleName)
    {
    }

    public class MonarchAssets(string bundleName) : AssetBundleLoader<MonarchAssets>(bundleName)
    {
    }

    /*public class PandoraAssets(string bundleName) : AssetBundleLoader<PandoraAssets>(bundleName)
    {
        [LoadFromBundle("PandoraObj.asset")]
        public EnemyType PandoraEnemyType { get; private set; } = null!;
    }*/

    public class NancyAssets(string bundleName) : AssetBundleLoader<NancyAssets>(bundleName)
    {
    }

    public class DriftwoodMenaceAssets(string bundleName) : AssetBundleLoader<DriftwoodMenaceAssets>(bundleName)
    {
    }

    public NancyAssets? Nancy { get; private set; }
    public DriftwoodMenaceAssets? DriftwoodMenace { get; private set; }
    // public PandoraAssets? Pandora { get; private set; }
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

/*#if DEBUG
        Pandora = new PandoraAssets("pandoraassets");
        RegisterEnemyWithConfig("", Pandora.PandoraEnemyType, null, null, 3, 0);
#endif*/

        DriftwoodMenace = LoadAndRegisterAssets<DriftwoodMenaceAssets>("driftwoodmenaceassets");

        Nancy = LoadAndRegisterAssets<NancyAssets>("nancyassets");

        Monarch = LoadAndRegisterAssets<MonarchAssets>("monarchassets");

        Mistress = LoadAndRegisterAssets<MistressAssets>("mistressassets");

        RedwoodTitan = LoadAndRegisterAssets<RedwoodTitanAssets>("redwoodtitanassets");

        CarnivorousPlant = LoadAndRegisterAssets<CarnivorousPlantAssets>("carnivorousplantassets");

        SnailCat = LoadAndRegisterAssets<SnailCatAssets>("snailcatassets");

        DuckSong = LoadAndRegisterAssets<DuckSongAssets>("ducksongassets");
        if (DuckSong != null)
        {
            foreach (var itemDefinition in DuckSong.ItemDefinitions)
            {
                Plugin.samplePrefabs.Add(itemDefinition.item.itemName, itemDefinition.item);
            }
        }

        Transporter = LoadAndRegisterAssets<TransporterAssets>("transporterassets");

        ManorLord = LoadAndRegisterAssets<ManorLordAssets>("manorlordassets");

        Janitor = LoadAndRegisterAssets<JanitorAssets>("janitorassets");
    }
}