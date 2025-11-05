using CodeRebirth.src.Content.Moons;
using Dusk;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class EnemyHandler : ContentHandler<EnemyHandler>
{
    public class SnailCatAssets(DuskMod mod, string filePath) : AssetBundleLoader<SnailCatAssets>(mod, filePath)
    {
    }

    public class CarnivorousPlantAssets(DuskMod mod, string filePath) : AssetBundleLoader<CarnivorousPlantAssets>(mod, filePath)
    {
    }

    public class RedwoodTitanAssets(DuskMod mod, string filePath) : AssetBundleLoader<RedwoodTitanAssets>(mod, filePath)
    {
    }

    public class DuckSongAssets(DuskMod mod, string filePath) : AssetBundleLoader<DuckSongAssets>(mod, filePath)
    {
        [LoadFromBundle("DuckHolder.prefab")]
        public GameObject DuckUIPrefab { get; private set; } = null!;
    }

    public class ManorLordAssets(DuskMod mod, string filePath) : AssetBundleLoader<ManorLordAssets>(mod, filePath)
    {
        [LoadFromBundle("PuppeteerPuppet.prefab")]
        public GameObject PuppeteerPuppetPrefab { get; private set; } = null!;
    }

    public class MistressAssets(DuskMod mod, string filePath) : AssetBundleLoader<MistressAssets>(mod, filePath)
    {
        [LoadFromBundle("GuillotinePrefab.prefab")]
        public GameObject GuillotinePrefab { get; private set; } = null!;
    }

    public class JanitorAssets(DuskMod mod, string filePath) : AssetBundleLoader<JanitorAssets>(mod, filePath)
    {
    }

    public class TransporterAssets(DuskMod mod, string filePath) : AssetBundleLoader<TransporterAssets>(mod, filePath)
    {
    }

    public class MonarchAssets(DuskMod mod, string filePath) : AssetBundleLoader<MonarchAssets>(mod, filePath)
    {
    }

    public class PandoraAssets(DuskMod mod, string filePath) : AssetBundleLoader<PandoraAssets>(mod, filePath)
    {
    }

    public class NancyAssets(DuskMod mod, string filePath) : AssetBundleLoader<NancyAssets>(mod, filePath)
    {
    }

    public class DriftwoodMenaceAssets(DuskMod mod, string filePath) : AssetBundleLoader<DriftwoodMenaceAssets>(mod, filePath)
    {
    }

    public class CuteaAssets(DuskMod mod, string filePath) : AssetBundleLoader<CuteaAssets>(mod, filePath)
    {
    }

    public class PeaceKeeperAssets(DuskMod mod, string filePath) : AssetBundleLoader<PeaceKeeperAssets>(mod, filePath)
    {
    }

    public class RabbitMagicianAssets(DuskMod mod, string filePath) : AssetBundleLoader<RabbitMagicianAssets>(mod, filePath)
    {
    }

    public class CactusBudlingAssets(DuskMod mod, string filePath) : AssetBundleLoader<CactusBudlingAssets>(mod, filePath)
    {
    }

    public CactusBudlingAssets? CactusBudling = null;
    public RabbitMagicianAssets? RabbitMagician = null;
    public PeaceKeeperAssets? PeaceKeeper = null;
    public CuteaAssets? Cutea = null;
    public NancyAssets? Nancy = null;
    public DriftwoodMenaceAssets? DriftwoodMenace = null;
    public PandoraAssets? Pandora = null;
    public MonarchAssets? Monarch = null;
    public MistressAssets? Mistress = null;
    public TransporterAssets? Transporter = null;
    public JanitorAssets? Janitor = null;
    public ManorLordAssets? ManorLord = null;
    public DuckSongAssets? DuckSong = null;
    public SnailCatAssets? SnailCat = null;
    public CarnivorousPlantAssets? CarnivorousPlant = null;
    public RedwoodTitanAssets? RedwoodTitan = null;

    public EnemyHandler(DuskMod mod) : base(mod)
    {
        RegisterContent("cactusbudlingassets", out CactusBudling);

        RegisterContent("rabbitmagicianassets", out RabbitMagician);

        RegisterContent("peacekeeperassets", out PeaceKeeper);

        RegisterContent("manorlordassets", out ManorLord);

        RegisterContent("janitorassets", out Janitor);

        // RegisterContent("pandoraassets", out Pandora);

        RegisterContent("driftwoodmenaceassets", out DriftwoodMenace);

        RegisterContent("nancyassets", out Nancy);

        RegisterContent("monarchassets", out Monarch);

        RegisterContent("mistressassets", out Mistress);

        RegisterContent("redwoodtitanassets", out RedwoodTitan);

        RegisterContent("carnivorousplantassets", out CarnivorousPlant);

        RegisterContent("snailcatassets", out SnailCat);

        RegisterContent("ducksongassets", out DuckSong);

        RegisterContent("transporterassets", out Transporter);
    }
}