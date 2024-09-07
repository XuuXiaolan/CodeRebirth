﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;

namespace CodeRebirth.src.Content.Enemies;

public class EnemyHandler : ContentHandler<EnemyHandler> {
    public class ButterflyAssets(string bundleName) : AssetBundleLoader<ButterflyAssets>(bundleName) {
        [LoadFromBundle("ButterflyObj.asset")]
        public EnemyType ButterflyEnemyType { get; private set; } = null!;

        [LoadFromBundle("CutieFlyTN.asset")]
        public TerminalNode ButterflyTerminalNode { get; private set; } = null!;

        [LoadFromBundle("CutieFlyTK.asset")]
        public TerminalKeyword ButterflyTerminalKeyword { get; private set; } = null!;
    }

    public class SnailCatAssets(string bundleName) : AssetBundleLoader<SnailCatAssets>(bundleName) {
        [LoadFromBundle("SnailCatObj.asset")]
        public EnemyType SnailCatEnemyType { get; private set; } = null!;

        [LoadFromBundle("SnailCatTN.asset")]
        public TerminalNode SnailCatTerminalNode { get; private set; } = null!;

        [LoadFromBundle("SnailCatTK.asset")]
        public TerminalKeyword SnailCatTerminalKeyword { get; private set; } = null!;
    }

    /*public class ScrapMasterAssets(string bundleName) : AssetBundleLoader<ScrapMasterAssets>(bundleName) {
        [LoadFromBundle("ScrapMasterObj.asset")]
        public EnemyType ScrapMasterEnemyType { get; private set; }

        [LoadFromBundle("ScrapMasterTN.asset")]
        public TerminalNode ScrapMasterTerminalNode { get; private set; }

        [LoadFromBundle("ScrapMasterTK.asset")]
        public TerminalKeyword ScrapMasterTerminalKeyword { get; private set; }
    }*/

    public class PjonkGooseAssets(string bundleName) : AssetBundleLoader<PjonkGooseAssets>(bundleName) {
        [LoadFromBundle("PjonkGooseObj.asset")]
        public EnemyType PjonkGooseEnemyType { get; private set; } = null!;

        [LoadFromBundle("PjonkGooseTN.asset")]
        public TerminalNode PjonkGooseTerminalNode { get; private set; } = null!;

        [LoadFromBundle("PjonkGooseTK.asset")]
        public TerminalKeyword PjonkGooseTerminalKeyword { get; private set; } = null!;
        
        [LoadFromBundle("PjonkEggObj.asset")]
        public Item GoldenEggItem { get; private set; } = null!;
    }

    public class CarnivorousPlantAssets(string bundleName ) : AssetBundleLoader<CarnivorousPlantAssets>(bundleName) {
        [LoadFromBundle("CarnivorousPlantObj.asset")]
        public EnemyType CarnivorousPlantEnemyType { get; private set; } = null!;

        [LoadFromBundle("CarnivorousPlantTN")]
        public TerminalNode CarnivorousPlantTerminalNode { get; private set; } = null!;

        [LoadFromBundle("CarnivorousPlantTK")]
        public TerminalKeyword CarnivorousPlantTerminalKeyword { get; private set; } = null!;
    }

    public class RedwoodTitanAssets(string bundleName) : AssetBundleLoader<RedwoodTitanAssets>(bundleName) {
        [LoadFromBundle("RedwoodTitanObj.asset")]
        public EnemyType RedwoodTitanEnemyType { get; private set; } = null!;

        [LoadFromBundle("RedwoodTitanTN.asset")]
        public TerminalNode RedwoodTitanTerminalNode { get; private set; } = null!;

        [LoadFromBundle("RedwoodTitanTK.asset")]
        public TerminalKeyword RedwoodTitanTerminalKeyword { get; private set; } = null!;
        
        /*[LoadFromBundle("RedwoodHeart.asset")]
        public Item RedwoodHeart { get; private set; } = null!;

        [LoadFromBundle("RedwoodWhistle.asset")]
        public Item RedwoodWhistle { get; private set; } = null!;*/
    }

    public class ReadyJPAssets(string bundleName) : AssetBundleLoader<ReadyJPAssets>(bundleName) {
        [LoadFromBundle("ReadyJPObj.asset")]
        public EnemyType ReadyJPEnemyType { get; private set; } = null!;

        [LoadFromBundle("ReadyJPTN.asset")]
        public TerminalNode ReadyJPTerminalNode { get; private set; } = null!;

        [LoadFromBundle("ReadyJPTK.asset")]
        public TerminalKeyword ReadyJPTerminalKeyword { get; private set; } = null!;
    }

    public ReadyJPAssets ReadyJP { get; private set; } = null!;
    public ButterflyAssets Butterfly { get; private set; } = null!;
    public SnailCatAssets SnailCat { get; private set; } = null!;
    public CarnivorousPlantAssets CarnivorousPlant { get; private set; } = null!;
    // public ScrapMasterAssets ScrapMaster { get; private set; }
    public PjonkGooseAssets PjonkGoose { get; private set; } = null!;
    public RedwoodTitanAssets RedwoodTitan { get; private set; } = null!;

    public EnemyHandler() {
        // ScrapMaster = new ScrapMasterAssets("coderebirthasset");
        // Plugin.samplePrefabs.Add("Grape", Assets.GrapeItem);

        if (Plugin.ModConfig.ConfigRedwoodEnabled.Value) {
            RedwoodTitan = new RedwoodTitanAssets("redwoodtitanassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigRedwoodSpawnWeights.Value, RedwoodTitan.RedwoodTitanEnemyType, RedwoodTitan.RedwoodTitanTerminalNode, RedwoodTitan.RedwoodTitanTerminalKeyword, Plugin.ModConfig.ConfigRedwoodPowerLevel.Value, Plugin.ModConfig.ConfigRedwoodMaxSpawnCount.Value);
            //Plugin.samplePrefabs.Add("RedwoodHeart", RedwoodTitan.RedwoodHeart);
        }

        if (Plugin.ModConfig.ConfigDangerousFloraEnabled.Value) {
            CarnivorousPlant = new CarnivorousPlantAssets("carnivorousplantassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigCarnivorousSpawnWeights.Value, CarnivorousPlant.CarnivorousPlantEnemyType, CarnivorousPlant.CarnivorousPlantTerminalNode, CarnivorousPlant.CarnivorousPlantTerminalKeyword, Plugin.ModConfig.ConfigCarnivorousPowerLevel.Value, Plugin.ModConfig.ConfigCarnivorousMaxSpawnCount.Value);
        }

        if (Plugin.ModConfig.ConfigCutieFlyEnabled.Value) {
            Butterfly = new ButterflyAssets("cutieflyassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigCutieFlySpawnWeights.Value, Butterfly.ButterflyEnemyType, Butterfly.ButterflyTerminalNode, Butterfly.ButterflyTerminalKeyword, Plugin.ModConfig.ConfigCutieFlyPowerLevel.Value, Plugin.ModConfig.ConfigCutieFlyMaxSpawnCount.Value);
        }

        if (Plugin.ModConfig.ConfigSnailCatEnabled.Value) {
            SnailCat = new SnailCatAssets("snailcatassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigSnailCatSpawnWeights.Value, SnailCat.SnailCatEnemyType, SnailCat.SnailCatTerminalNode, SnailCat.SnailCatTerminalKeyword, Plugin.ModConfig.ConfigSnailCatPowerLevel.Value, Plugin.ModConfig.ConfigSnailCatMaxSpawnCount.Value);
        }

        if (true) {
            ReadyJP = new ReadyJPAssets("readyjpassets");
            RegisterEnemyWithConfig("All:0", ReadyJP.ReadyJPEnemyType, ReadyJP.ReadyJPTerminalNode, ReadyJP.ReadyJPTerminalKeyword, 3, 1);
        }

        /*if (false) {
            PjonkGoose = new PjonkGooseAssets("pjonkgooseassets");
            RegisterEnemyWithConfig("All:500", PjonkGoose.PjonkGooseEnemyType, PjonkGoose.PjonkGooseTerminalNode, PjonkGoose.PjonkGooseTerminalKeyword, 3, 1);
            RegisterScrapWithConfig("All:0", PjonkGoose.GoldenEggItem);
            Plugin.samplePrefabs.Add("GoldenEgg", PjonkGoose.GoldenEggItem);
        }*/
    }
}