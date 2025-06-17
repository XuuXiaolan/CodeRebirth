using CodeRebirthLib;
using CodeRebirthLib.AssetManagement;
using CodeRebirthLib.ContentManagement;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class EnemyHandler : ContentHandler<EnemyHandler>
{
    public class SnailCatAssets(CRMod mod, string filePath) : AssetBundleLoader<SnailCatAssets>(mod, filePath)
    {
    }

    public class CarnivorousPlantAssets(CRMod mod, string filePath) : AssetBundleLoader<CarnivorousPlantAssets>(mod, filePath)
    {
    }

    public class RedwoodTitanAssets(CRMod mod, string filePath) : AssetBundleLoader<RedwoodTitanAssets>(mod, filePath)
    {
    }

    public class DuckSongAssets(CRMod mod, string filePath) : AssetBundleLoader<DuckSongAssets>(mod, filePath)
    {
        [LoadFromBundle("DuckHolder.prefab")]
        public GameObject DuckUIPrefab { get; private set; } = null!;
    }

    public class ManorLordAssets(CRMod mod, string filePath) : AssetBundleLoader<ManorLordAssets>(mod, filePath)
    {
        [LoadFromBundle("PuppeteerPuppet.prefab")]
        public GameObject PuppeteerPuppetPrefab { get; private set; } = null!;
    }

    public class MistressAssets(CRMod mod, string filePath) : AssetBundleLoader<MistressAssets>(mod, filePath)
    {
        [LoadFromBundle("GuillotinePrefab.prefab")]
        public GameObject GuillotinePrefab { get; private set; } = null!;
    }

    public class JanitorAssets(CRMod mod, string filePath) : AssetBundleLoader<JanitorAssets>(mod, filePath)
    {
    }

    public class TransporterAssets(CRMod mod, string filePath) : AssetBundleLoader<TransporterAssets>(mod, filePath)
    {
    }

    public class MonarchAssets(CRMod mod, string filePath) : AssetBundleLoader<MonarchAssets>(mod, filePath)
    {
    }

    public class PandoraAssets(CRMod mod, string filePath) : AssetBundleLoader<PandoraAssets>(mod, filePath)
    {
    }

    public class NancyAssets(CRMod mod, string filePath) : AssetBundleLoader<NancyAssets>(mod, filePath)
    {
    }

    public class DriftwoodMenaceAssets(CRMod mod, string filePath) : AssetBundleLoader<DriftwoodMenaceAssets>(mod, filePath)
    {
    }

    public class CuteaAssets(CRMod mod, string filePath) : AssetBundleLoader<CuteaAssets>(mod, filePath)
    {
    }

    public class PeaceKeeperAssets(CRMod mod, string filePath) : AssetBundleLoader<PeaceKeeperAssets>(mod, filePath)
    {
    }

    public class RabbitMagicianAssets(CRMod mod, string filePath) : AssetBundleLoader<RabbitMagicianAssets>(mod, filePath)
    {
    }

    public class CactusBudlingAssets(CRMod mod, string filePath) : AssetBundleLoader<CactusBudlingAssets>(mod, filePath)
    {
    }

    public CactusBudlingAssets? CactusBudling { get; private set; }
    public RabbitMagicianAssets? RabbitMagician { get; private set; }
    public PeaceKeeperAssets? PeaceKeeper { get; private set; }
    public CuteaAssets? Cutea { get; private set; }
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

    public EnemyHandler(CRMod mod) : base(mod)
    {
        if (TryLoadContentBundle("cactusbudlingassets", out CactusBudlingAssets? cactusBudlingAssets))
        {
            CactusBudling = cactusBudlingAssets;
            LoadAllContent(cactusBudlingAssets!); // todo: needs override
        }

        if (TryLoadContentBundle("rabbitmagicianassets", out RabbitMagicianAssets? rabbitMagicianAssets))
        {
            RabbitMagician = rabbitMagicianAssets;
            LoadAllContent(rabbitMagicianAssets!);
        }

        if (TryLoadContentBundle("peacekeeperassets", out PeaceKeeperAssets? peaceKeeperAssets))
        {
            PeaceKeeper = peaceKeeperAssets;
            LoadAllContent(peaceKeeperAssets!);
        }

        if (TryLoadContentBundle("manorlordassets", out ManorLordAssets? manorLordAssets))
        {
            ManorLord = manorLordAssets;
            LoadAllContent(manorLordAssets!);
        }

        if (TryLoadContentBundle("janitorassets", out JanitorAssets? janitorAssets))
        {
            Janitor = janitorAssets;
            LoadAllContent(janitorAssets!);
        }

        if (TryLoadContentBundle("pandoraassets", out PandoraAssets? pandoraAssets))
        {
            Pandora = pandoraAssets;
            LoadAllContent(pandoraAssets!);
        }

        if (TryLoadContentBundle("driftwoodmenaceassets", out DriftwoodMenaceAssets? driftwoodMenaceAssets))
        {
            DriftwoodMenace = driftwoodMenaceAssets;
            LoadAllContent(driftwoodMenaceAssets!);
        }

        if (TryLoadContentBundle("nancyassets", out NancyAssets? nancyAssets))
        {
            Nancy = nancyAssets;
            LoadAllContent(nancyAssets!);
        }

        if (TryLoadContentBundle("monarchassets", out MonarchAssets? monarchAssets))
        {
            Monarch = monarchAssets;
            LoadAllContent(monarchAssets!);
        }

        if (TryLoadContentBundle("mistressassets", out MistressAssets? mistressAssets))
        {
            Mistress = mistressAssets;
            LoadAllContent(mistressAssets!);
        }

        if (TryLoadContentBundle("redwoodtitanassets", out RedwoodTitanAssets? redwoodTitanAssets))
        {
            RedwoodTitan = redwoodTitanAssets;
            LoadAllContent(redwoodTitanAssets!);
        }

        if (TryLoadContentBundle("carnivorousplantassets", out CarnivorousPlantAssets? carnivorousPlantAssets))
        {
            CarnivorousPlant = carnivorousPlantAssets;
            LoadAllContent(carnivorousPlantAssets!);
        }

        if (TryLoadContentBundle("snailcatassets", out SnailCatAssets? snailCatAssets))
        {
            SnailCat = snailCatAssets;
            LoadAllContent(snailCatAssets!);
        }

        if (TryLoadContentBundle("ducksongassets", out DuckSongAssets? duckSongAssets))
        {
            DuckSong = duckSongAssets;
            LoadAllContent(duckSongAssets!);
        }

        if (TryLoadContentBundle("transporterassets", out TransporterAssets? transporterAssets))
        {
            Transporter = transporterAssets;
            LoadAllContent(transporterAssets!);
        }
    }
}