﻿using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

public class EnemyHandler : ContentHandler<EnemyHandler>
{
    public class SnailCatAssets(string bundleName) : AssetBundleLoader<SnailCatAssets>(bundleName)
    {
        [LoadFromBundle("SnailCatEnemyObj.asset")]
        public EnemyType? SnailCatEnemyType { get; private set; } = null;

        [LoadFromBundle("SnailCatItemObj.asset")]
        public Item? SnailCatItem { get; private set; } = null;
    }

    public class CarnivorousPlantAssets(string bundleName ) : AssetBundleLoader<CarnivorousPlantAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("CarnivorousPlantEnemyDefinition.asset")]
        public CREnemyDefinition FirstEnemyDefinition { get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            new List<CREnemyDefinition> { FirstEnemyDefinition };
    }

    public class RedwoodTitanAssets(string bundleName) : AssetBundleLoader<RedwoodTitanAssets>(bundleName), IEnemyAssets
    {
        [LoadFromBundle("RedwoodEnemyDefinition.asset")]
        public CREnemyDefinition FirstEnemyDefinition { get; private set; } = null!;

        public IReadOnlyList<CREnemyDefinition> EnemyDefinitions =>
            new List<CREnemyDefinition> { FirstEnemyDefinition };
    }

    public class DuckSongAssets(string bundleName) : AssetBundleLoader<DuckSongAssets>(bundleName)
    {
        [LoadFromBundle("DuckObj.asset")]
        public EnemyType? DuckSongEnemyType { get; private set; } = null;

        [LoadFromBundle("GrapeObj.asset")]
        public Item GrapeItem { get; private set; } = null!;

        [LoadFromBundle("LemonadePitcherObj.asset")]
        public Item LemonadePitcherItem { get; private set; } = null!;

        [LoadFromBundle("DuckHolder.prefab")]
        public GameObject DuckUIPrefab { get; private set; } = null!;
    }

    public class ManorLordAssets(string bundleName) : AssetBundleLoader<ManorLordAssets>(bundleName)
    {
        [LoadFromBundle("ManorLordObj.asset")]
        public EnemyType ManorLordEnemyType { get; private set; } = null!;

        /*[LoadFromBundle("ManorLordTN.asset")]
        public TerminalNode ManorLordTerminalNode { get; private set; } = null!;

        [LoadFromBundle("ManorLordTK.asset")]
        public TerminalKeyword ManorLordTerminalKeyword { get; private set; } = null!;*/

        [LoadFromBundle("PuppeteerPuppet.prefab")]
        public GameObject PuppeteerPuppetPrefab { get; private set; } = null!;

        [LoadFromBundle("PuppetScrapObj.asset")]
        public Item PuppetItem { get; private set; } = null!;

        [LoadFromBundle("PinNeedleObj.asset")]
        public Item PinNeedleItem { get; private set; } = null!;
    }

    public class MistressAssets(string bundleName) : AssetBundleLoader<MistressAssets>(bundleName)
    {
        [LoadFromBundle("MistressObj.asset")]
        public EnemyType MistressEnemyType { get; private set; } = null!;

        [LoadFromBundle("LeChoppedHeadObj.asset")]
        public Item ChoppedTalkingHead { get; private set; } = null!;

        [LoadFromBundle("GuillotinePrefab.prefab")]
        public GameObject GuillotinePrefab { get; private set; } = null!;
    }

    public class JanitorAssets(string bundleName) : AssetBundleLoader<JanitorAssets>(bundleName)
    {
        [LoadFromBundle("JanitorObj.asset")]
        public EnemyType JanitorEnemyType { get; private set; } = null!;

        [LoadFromBundle("JanitorTrash.prefab")]
        public GameObject TrashCanPrefab { get; private set; } = null!;
    }

    public class TransporterAssets(string bundleName) : AssetBundleLoader<TransporterAssets>(bundleName)
    {
        [LoadFromBundle("TransporterObj.asset")]
        public EnemyType TransporterEnemyType { get; private set; } = null!;
    }

    public class MonarchAssets(string bundleName) : AssetBundleLoader<MonarchAssets>(bundleName)
    {
        [LoadFromBundle("MonarchObj.asset")]
        public EnemyType MonarchEnemyType { get; private set; } = null!;

        [LoadFromBundle("CutieflyObj.asset")]
        public EnemyType CutieflyEnemyType { get; private set; } = null!;
    }

    public class PandoraAssets(string bundleName) : AssetBundleLoader<PandoraAssets>(bundleName)
    {
        [LoadFromBundle("PandoraObj.asset")]
        public EnemyType PandoraEnemyType { get; private set; } = null!;
    }

    public class NancyAssets(string bundleName) : AssetBundleLoader<NancyAssets>(bundleName)
    {
        [LoadFromBundle("NancyObj.asset")]
        public EnemyType NancyEnemyType { get; private set; } = null!;
    }

    public class DriftwoodMenaceAssets(string bundleName) : AssetBundleLoader<DriftwoodMenaceAssets>(bundleName)
    {
        [LoadFromBundle("DriftwoodMenaceObj.asset")]
        public EnemyType DriftwoodMenaceEnemyType { get; private set; } = null!;
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

        if (Plugin.ModConfig.ConfigDriftwoodMenaceEnabled.Value)
        {
            DriftwoodMenace = new DriftwoodMenaceAssets("driftwoodmenaceassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigDriftwoodMenaceSpawnWeights.Value, DriftwoodMenace.DriftwoodMenaceEnemyType, null, null, Plugin.ModConfig.ConfigDriftwoodMenacePowerLevel.Value, Plugin.ModConfig.ConfigDriftwoodMenaceMaxSpawnCount.Value);
        }

        if (Plugin.ModConfig.ConfigNancyEnabled.Value)
        {
            Nancy = new NancyAssets("nancyassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigNancySpawnWeights.Value, Nancy.NancyEnemyType, null, null, Plugin.ModConfig.ConfigNancyPowerLevel.Value, Plugin.ModConfig.ConfigNancyMaxSpawnCount.Value);
        }

        if (Plugin.ModConfig.ConfigMonarchEnabled.Value)
        {
            Monarch = new MonarchAssets("monarchassets");
            RegisterEnemyWithConfig("", Monarch.MonarchEnemyType, null, null, 3, 0);
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigCutieFlySpawnWeights.Value, Monarch.CutieflyEnemyType, null, null, Plugin.ModConfig.ConfigCutieFlyPowerLevel.Value, Plugin.ModConfig.ConfigCutieFlyMaxSpawnCount.Value);
        }

        if (Plugin.ModConfig.ConfigMistressEnabled.Value)
        {
            Mistress = new MistressAssets("mistressassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigMistressSpawnWeights.Value, Mistress.MistressEnemyType, null, null, Plugin.ModConfig.ConfigMistressPowerLevel.Value, Plugin.ModConfig.ConfigMistressMaxSpawnCount.Value);
            RegisterScrapWithConfig("", Mistress.ChoppedTalkingHead, -1, -1);
        }

        RedwoodTitan = LoadAndTryRegisterEnemy<RedwoodTitanAssets>("redwoodtitanassets", ["Redwood Titan"], ["Vanilla:20,Custom:20"], [2f], [3]);

        CarnivorousPlant = LoadAndTryRegisterEnemy<CarnivorousPlantAssets>("carnivorousplantassets", ["Carnivorous Plant"], ["Vanilla:20,Custom:20"], [2f], [3]);

        if (Plugin.ModConfig.ConfigSnailCatEnabled.Value)
        {
            SnailCat = new SnailCatAssets("snailcatassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigSnailCatSpawnWeights.Value, SnailCat.SnailCatEnemyType, null, null, Plugin.ModConfig.ConfigSnailCatPowerLevel.Value, Plugin.ModConfig.ConfigSnailCatMaxSpawnCount.Value);
        }

        if (Plugin.ModConfig.ConfigDuckSongEnabled.Value)
        {
            DuckSong = new DuckSongAssets("ducksongassets");
            RegisterScrapWithConfig("", DuckSong.GrapeItem, -1, -1);
            RegisterScrapWithConfig("", DuckSong.LemonadePitcherItem, -1, -1);
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigDuckSongSpawnWeights.Value, DuckSong.DuckSongEnemyType, null, null, Plugin.ModConfig.ConfigDuckSongPowerLevel.Value, Plugin.ModConfig.ConfigDuckSongMaxSpawnCount.Value);
            Plugin.samplePrefabs.Add(DuckSong.GrapeItem.itemName, DuckSong.GrapeItem);
            Plugin.samplePrefabs.Add(DuckSong.LemonadePitcherItem.itemName, DuckSong.LemonadePitcherItem);
        } // configurable quest time, max amount of ducks to spawn.

        if (Plugin.ModConfig.ConfigManorLordEnabled.Value)
        {
            ManorLord = new ManorLordAssets("manorlordassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigManorLordSpawnWeights.Value, ManorLord.ManorLordEnemyType, null, null, Plugin.ModConfig.ConfigManorLordPowerLevel.Value, Plugin.ModConfig.ConfigManorLordMaxSpawnCount.Value);
            RegisterScrapWithConfig("", ManorLord.PuppetItem, -1, -1);
            RegisterScrapWithConfig("", ManorLord.PinNeedleItem, -1, -1);
            Plugin.samplePrefabs.Add(ManorLord.PinNeedleItem.itemName, ManorLord.PinNeedleItem);
            Plugin.samplePrefabs.Add(ManorLord.PuppetItem.itemName, ManorLord.PuppetItem);
        }

        if (Plugin.ModConfig.ConfigJanitorEnabled.Value)
        {
            Janitor = new JanitorAssets("janitorassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigJanitorSpawnWeights.Value, Janitor.JanitorEnemyType, null, null, Plugin.ModConfig.ConfigJanitorPowerLevel.Value, Plugin.ModConfig.ConfigJanitorMaxSpawnCount.Value);
            RegisterInsideMapObjectWithConfig(Janitor.TrashCanPrefab, "Vanilla - 0.00,5.00 ; 0.11,6.49 ; 0.22,6.58 ; 0.33,6.40 ; 0.44,8.22 ; 0.56,9.55 ; 0.67,10.02 ; 0.78,10.01 ; 0.89,9.88 ; 1.00,10.00 | Custom - 0.00,5.00 ; 0.11,6.49 ; 0.22,6.58 ; 0.33,6.40 ; 0.44,8.22 ; 0.56,9.55 ; 0.67,10.02 ; 0.78,10.01 ; 0.89,9.88 ; 1.00,10.00");
        }

        if (Plugin.ModConfig.ConfigTransporterEnabled.Value)
        {
            Transporter = new TransporterAssets("transporterassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigTransporterSpawnWeights.Value, Transporter.TransporterEnemyType, null, null, Plugin.ModConfig.ConfigTransporterPowerLevel.Value, Plugin.ModConfig.ConfigTransporterMaxSpawnCount.Value);
        }
    }
}