using CodeRebirth.Util;
using CodeRebirth.Util.AssetLoading;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.EnemyStuff;

public class EnemyHandler : ContentHandler<EnemyHandler> {
    public class ButterflyAssets(string bundleName) : AssetBundleLoader<ButterflyAssets>(bundleName) {
        [LoadFromBundle("ButterflyObj.asset")]
        public EnemyType ButterflyEnemyType { get; private set; }

        [LoadFromBundle("CutieFlyTN.asset")]
        public TerminalNode ButterflyTerminalNode { get; private set; }

        [LoadFromBundle("CutieFlyTK.asset")]
        public TerminalKeyword ButterflyTerminalKeyword { get; private set; }
    }

    public class SnailCatAssets(string bundleName) : AssetBundleLoader<SnailCatAssets>(bundleName) {
        [LoadFromBundle("SnailCatObj.asset")]
        public EnemyType SnailCatEnemyType { get; private set; }

        [LoadFromBundle("SnailCatTN.asset")]
        public TerminalNode SnailCatTerminalNode { get; private set; }

        [LoadFromBundle("SnailCatTK.asset")]
        public TerminalKeyword SnailCatTerminalKeyword { get; private set; }
    }

    public class ScrapMasterAssets(string bundleName) : AssetBundleLoader<ScrapMasterAssets>(bundleName) {
        [LoadFromBundle("ScrapMasterObj.asset")]
        public EnemyType ScrapMasterEnemyType { get; private set; }

        [LoadFromBundle("ScrapMasterTN.asset")]
        public TerminalNode ScrapMasterTerminalNode { get; private set; }

        [LoadFromBundle("ScrapMasterTK.asset")]
        public TerminalKeyword ScrapMasterTerminalKeyword { get; private set; }
    }

    public class PjonkGooseAssets(string bundleName) : AssetBundleLoader<PjonkGooseAssets>(bundleName) {
        [LoadFromBundle("PjonkGooseObj.asset")]
        public EnemyType PjonkGooseEnemyType { get; private set; }

        [LoadFromBundle("PjonkGooseTN.asset")]
        public TerminalNode PjonkGooseTerminalNode { get; private set; }

        [LoadFromBundle("PjonkGooseTK.asset")]
        public TerminalKeyword PjonkGooseTerminalKeyword { get; private set; }
        
        [LoadFromBundle("PjonkEggObj.asset")]
        public Item GoldenEggItem { get; private set; }
    }

    public ButterflyAssets Butterfly { get; private set; }
    public SnailCatAssets SnailCat { get; private set; }
    public ScrapMasterAssets ScrapMaster { get; private set; }
    public PjonkGooseAssets PjonkGoose { get; private set; }

    public EnemyHandler() {
        Butterfly = new ButterflyAssets("coderebirthasset");
        SnailCat = new SnailCatAssets("coderebirthasset");
        ScrapMaster = new ScrapMasterAssets("coderebirthasset");
        PjonkGoose = new PjonkGooseAssets("coderebirthasset");

        Plugin.samplePrefabs.Add("GoldenEgg", PjonkGoose.GoldenEggItem);
        // Plugin.samplePrefabs.Add("Grape", Assets.GrapeItem);

        RegisterEnemyWithConfig(true, "All:9999", PjonkGoose.PjonkGooseEnemyType, PjonkGoose.PjonkGooseTerminalNode, PjonkGoose.PjonkGooseTerminalKeyword, 3, 1);
        RegisterEnemyWithConfig(Plugin.ModConfig.ConfigCutieFlyEnabled.Value, Plugin.ModConfig.ConfigCutieFlySpawnWeights.Value, Butterfly.ButterflyEnemyType, Butterfly.ButterflyTerminalNode, Butterfly.ButterflyTerminalKeyword, Plugin.ModConfig.ConfigCutieFlyPowerLevel.Value, Plugin.ModConfig.ConfigCutieFlyMaxSpawnCount.Value);
        RegisterEnemyWithConfig(Plugin.ModConfig.ConfigSnailCatEnabled.Value, Plugin.ModConfig.ConfigSnailCatSpawnWeights.Value, SnailCat.SnailCatEnemyType, SnailCat.SnailCatTerminalNode, SnailCat.SnailCatTerminalKeyword, Plugin.ModConfig.ConfigSnailCatPowerLevel.Value, Plugin.ModConfig.ConfigSnailCatMaxSpawnCount.Value);
    }
}