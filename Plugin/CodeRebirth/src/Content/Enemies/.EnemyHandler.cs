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

    public EnemyHandler(CRMod mod) : base(mod)
    {
        RegisterContent("cactusbudlingassets", out CactusBudling, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("rabbitmagicianassets", out RabbitMagician);

        RegisterContent("peacekeeperassets", out PeaceKeeper, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("manorlordassets", out ManorLord, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("janitorassets", out Janitor, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("pandoraassets", out Pandora);

        RegisterContent("driftwoodmenaceassets", out DriftwoodMenace);

        RegisterContent("nancyassets", out Nancy, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("monarchassets", out Monarch);

        RegisterContent("mistressassets", out Mistress, Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RegisterContent("redwoodtitanassets", out RedwoodTitan);

        RegisterContent("carnivorousplantassets", out CarnivorousPlant);

        RegisterContent("snailcatassets", out SnailCat);

        RegisterContent("ducksongassets", out DuckSong);

        RegisterContent("transporterassets", out Transporter, Plugin.ModConfig.ConfigOxydeEnabled.Value);
    }
}