﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;
public class EnemyHandler : ContentHandler<EnemyHandler>
{
    public class SnailCatAssets(string bundleName) : AssetBundleLoader<SnailCatAssets>(bundleName)
    {
    }

    public class CarnivorousPlantAssets(string bundleName) : AssetBundleLoader<CarnivorousPlantAssets>(bundleName)
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

    public class PandoraAssets(string bundleName) : AssetBundleLoader<PandoraAssets>(bundleName)
    {
    }

    public class NancyAssets(string bundleName) : AssetBundleLoader<NancyAssets>(bundleName)
    {
    }

    public class DriftwoodMenaceAssets(string bundleName) : AssetBundleLoader<DriftwoodMenaceAssets>(bundleName)
    {
    }

    public class CuteaAssets(string bundleName) : AssetBundleLoader<CuteaAssets>(bundleName)
    {
    }

    public class PeaceKeeperAssets(string bundleName) : AssetBundleLoader<PeaceKeeperAssets>(bundleName)
    {
    }

    public class RabbitMagicianAssets(string bundleName) : AssetBundleLoader<RabbitMagicianAssets>(bundleName)
    {
    }

    public class CactusBudlingAssets(string bundleName) : AssetBundleLoader<CactusBudlingAssets>(bundleName)
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

    public EnemyHandler()
    {
        CactusBudling = LoadAndRegisterAssets<CactusBudlingAssets>("cactusbudlingassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RabbitMagician = LoadAndRegisterAssets<RabbitMagicianAssets>("rabbitmagicianassets");

        PeaceKeeper = LoadAndRegisterAssets<PeaceKeeperAssets>("peacekeeperassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        Pandora = LoadAndRegisterAssets<PandoraAssets>("pandoraassets");

        Cutea = LoadAndRegisterAssets<CuteaAssets>("cuteaassets");

        DriftwoodMenace = LoadAndRegisterAssets<DriftwoodMenaceAssets>("driftwoodmenaceassets");

        Nancy = LoadAndRegisterAssets<NancyAssets>("nancyassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        Monarch = LoadAndRegisterAssets<MonarchAssets>("monarchassets");

        Mistress = LoadAndRegisterAssets<MistressAssets>("mistressassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        RedwoodTitan = LoadAndRegisterAssets<RedwoodTitanAssets>("redwoodtitanassets");

        CarnivorousPlant = LoadAndRegisterAssets<CarnivorousPlantAssets>("carnivorousplantassets");

        SnailCat = LoadAndRegisterAssets<SnailCatAssets>("snailcatassets");

        DuckSong = LoadAndRegisterAssets<DuckSongAssets>("ducksongassets");

        Transporter = LoadAndRegisterAssets<TransporterAssets>("transporterassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        ManorLord = LoadAndRegisterAssets<ManorLordAssets>("manorlordassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);

        Janitor = LoadAndRegisterAssets<JanitorAssets>("janitorassets", Plugin.ModConfig.ConfigOxydeEnabled.Value);
    }
}