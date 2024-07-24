﻿using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;

namespace CodeRebirth.EnemyStuff;

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

    public class RedwoodGiantAssets(string bundleName) : AssetBundleLoader<RedwoodGiantAssets>(bundleName) {
        [LoadFromBundle("RedwoodGiantObj.asset")]
        public EnemyType RedwoodGiantEnemyType { get; private set; } = null!;

        [LoadFromBundle("RedwoodGiantTN.asset")]
        public TerminalNode RedwoodGiantTerminalNode { get; private set; } = null!;

        [LoadFromBundle("RedwoodGiantTK.asset")]
        public TerminalKeyword RedwoodGiantTerminalKeyword { get; private set; } = null!;
        
        [LoadFromBundle("RedwoodHeart.asset")]
        public Item RedwoodHeart { get; private set; } = null!;

        [LoadFromBundle("RedwoodWhistle.asset")]
        public Item RedwoodWhistle { get; private set; } = null!;
    }
    public ButterflyAssets Butterfly { get; private set; } = null!;
    public SnailCatAssets SnailCat { get; private set; } = null!;
    // public ScrapMasterAssets ScrapMaster { get; private set; }
    public PjonkGooseAssets PjonkGoose { get; private set; } = null!;
    public RedwoodGiantAssets RedwoodGiant { get; private set; } = null!;

    public EnemyHandler() {
        // ScrapMaster = new ScrapMasterAssets("coderebirthasset");
        // Plugin.samplePrefabs.Add("Grape", Assets.GrapeItem);

        if (false) {
            RedwoodGiant = new RedwoodGiantAssets("redwoodgiantassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigRedwoodSpawnWeights.Value, RedwoodGiant.RedwoodGiantEnemyType, RedwoodGiant.RedwoodGiantTerminalNode, RedwoodGiant.RedwoodGiantTerminalKeyword, Plugin.ModConfig.ConfigRedwoodPowerLevel.Value, Plugin.ModConfig.ConfigRedwoodMaxSpawnCount.Value);
            Plugin.samplePrefabs.Add("RedwoodHeart", RedwoodGiant.RedwoodHeart);
        }
        if (Plugin.ModConfig.ConfigCutieFlyEnabled.Value) {
            Butterfly = new ButterflyAssets("cutieflyassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigCutieFlySpawnWeights.Value, Butterfly.ButterflyEnemyType, Butterfly.ButterflyTerminalNode, Butterfly.ButterflyTerminalKeyword, Plugin.ModConfig.ConfigCutieFlyPowerLevel.Value, Plugin.ModConfig.ConfigCutieFlyMaxSpawnCount.Value);
        }

        if (Plugin.ModConfig.ConfigSnailCatEnabled.Value) {
            SnailCat = new SnailCatAssets("snailcatassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigSnailCatSpawnWeights.Value, SnailCat.SnailCatEnemyType, SnailCat.SnailCatTerminalNode, SnailCat.SnailCatTerminalKeyword, Plugin.ModConfig.ConfigSnailCatPowerLevel.Value, Plugin.ModConfig.ConfigSnailCatMaxSpawnCount.Value);
        }

        // TODO: swap out with actual config.
        if (false) {
            PjonkGoose = new PjonkGooseAssets("pjonkgooseassets");
            RegisterEnemyWithConfig("All:500", PjonkGoose.PjonkGooseEnemyType, PjonkGoose.PjonkGooseTerminalNode, PjonkGoose.PjonkGooseTerminalKeyword, 3, 1);
            Plugin.samplePrefabs.Add("GoldenEgg", PjonkGoose.GoldenEggItem);
        }
    }
}