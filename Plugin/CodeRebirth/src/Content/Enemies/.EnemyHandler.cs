﻿using CodeRebirth.src.Util;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;

namespace CodeRebirth.src.Content.Enemies;

public class EnemyHandler : ContentHandler<EnemyHandler>
{
    public class ButterflyAssets(string bundleName) : AssetBundleLoader<ButterflyAssets>(bundleName)
    {
        [LoadFromBundle("ButterflyObj.asset")]
        public EnemyType ButterflyEnemyType { get; private set; } = null!;

        [LoadFromBundle("CutieFlyTN.asset")]
        public TerminalNode ButterflyTerminalNode { get; private set; } = null!;

        [LoadFromBundle("CutieFlyTK.asset")]
        public TerminalKeyword ButterflyTerminalKeyword { get; private set; } = null!;
    }

    public class SnailCatAssets(string bundleName) : AssetBundleLoader<SnailCatAssets>(bundleName)
    {
        [LoadFromBundle("SnailCatObj.asset")]
        public EnemyType SnailCatEnemyType { get; private set; } = null!;

        [LoadFromBundle("SnailCatTN.asset")]
        public TerminalNode SnailCatTerminalNode { get; private set; } = null!;

        [LoadFromBundle("SnailCatTK.asset")]
        public TerminalKeyword SnailCatTerminalKeyword { get; private set; } = null!;
    }

    public class CarnivorousPlantAssets(string bundleName ) : AssetBundleLoader<CarnivorousPlantAssets>(bundleName)
    {
        [LoadFromBundle("CarnivorousPlantObj.asset")]
        public EnemyType CarnivorousPlantEnemyType { get; private set; } = null!;

        [LoadFromBundle("CarnivorousPlantTN")]
        public TerminalNode CarnivorousPlantTerminalNode { get; private set; } = null!;

        [LoadFromBundle("CarnivorousPlantTK")]
        public TerminalKeyword CarnivorousPlantTerminalKeyword { get; private set; } = null!;
    }

    public class RedwoodTitanAssets(string bundleName) : AssetBundleLoader<RedwoodTitanAssets>(bundleName)
    {
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

    public class DuckSongAssets(string bundleName) : AssetBundleLoader<DuckSongAssets>(bundleName)
    {
        [LoadFromBundle("DuckObj.asset")]
        public EnemyType DuckSongEnemyType { get; private set; } = null!;

        [LoadFromBundle("GrapeObj.asset")]
        public Item GrapeItem { get; private set; } = null!;

        [LoadFromBundle("LemonadePitcherObj.asset")]
        public Item LemonadePitcherItem { get; private set; } = null!;

        [LoadFromBundle("DuckTN.asset")]
        public TerminalNode DuckSongTerminalNode { get; private set; } = null!;

        [LoadFromBundle("DuckTK.asset")]
        public TerminalKeyword DuckSongTerminalKeyword { get; private set; } = null!;

        [LoadFromBundle("DuckHolder.prefab")]
        public GameObject DuckUIPrefab { get; private set; } = null!;
    }

    public class PokemonEnemyAssets(string bundleName) : AssetBundleLoader<PokemonEnemyAssets>(bundleName)
    {
        [LoadFromBundle("FlareonObj.asset")]
        public EnemyType FlareonEnemyType { get; private set; } = null!;

        [LoadFromBundle("MechaSylveonObj.asset")]
        public EnemyType MechaSylveonEnemyType { get; private set; } = null!;

        [LoadFromBundle("GlaceonObj.asset")]
        public EnemyType GlaceonEnemyType { get; private set; } = null!;

        [LoadFromBundle("VaporeonObj.asset")]
        public EnemyType VaporeonEnemyType { get; private set; } = null!;

        [LoadFromBundle("JolteonObj.asset")]
        public EnemyType JolteonEnemyType { get; private set; } = null!;

        [LoadFromBundle("UmbreonObj.asset")]
        public EnemyType UmbreonEnemyType { get; private set; } = null!;

        [LoadFromBundle("EspeonObj.asset")]
        public EnemyType EspeonEnemyType { get; private set; } = null!;

        [LoadFromBundle("LeafeonObj.asset")]
        public EnemyType LeafeonEnemyType { get; private set; } = null!;

        [LoadFromBundle("ChildEeveeObj.asset")]
        public Item ChildEeveeItem { get; private set; } = null!;
    }

    public DuckSongAssets DuckSong { get; private set; } = null!;
    public PokemonEnemyAssets PokemonEnemies { get; private set; } = null!;
    public ButterflyAssets Butterfly { get; private set; } = null!;
    public SnailCatAssets SnailCat { get; private set; } = null!;
    public CarnivorousPlantAssets CarnivorousPlant { get; private set; } = null!;
    public RedwoodTitanAssets RedwoodTitan { get; private set; } = null!;

    public EnemyHandler()
    {

        if (Plugin.ModConfig.ConfigRedwoodEnabled.Value)
        {
            RedwoodTitan = new RedwoodTitanAssets("redwoodtitanassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigRedwoodSpawnWeights.Value, RedwoodTitan.RedwoodTitanEnemyType, RedwoodTitan.RedwoodTitanTerminalNode, RedwoodTitan.RedwoodTitanTerminalKeyword, Plugin.ModConfig.ConfigRedwoodPowerLevel.Value, Plugin.ModConfig.ConfigRedwoodMaxSpawnCount.Value);
            //Plugin.samplePrefabs.Add("RedwoodHeart", RedwoodTitan.RedwoodHeart);
        }

        if (Plugin.ModConfig.ConfigDangerousFloraEnabled.Value)
        {
            CarnivorousPlant = new CarnivorousPlantAssets("carnivorousplantassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigCarnivorousSpawnWeights.Value, CarnivorousPlant.CarnivorousPlantEnemyType, CarnivorousPlant.CarnivorousPlantTerminalNode, CarnivorousPlant.CarnivorousPlantTerminalKeyword, Plugin.ModConfig.ConfigCarnivorousPowerLevel.Value, Plugin.ModConfig.ConfigCarnivorousMaxSpawnCount.Value);
        }

        if (Plugin.ModConfig.ConfigCutieFlyEnabled.Value)
        {
            Butterfly = new ButterflyAssets("cutieflyassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigCutieFlySpawnWeights.Value, Butterfly.ButterflyEnemyType, Butterfly.ButterflyTerminalNode, Butterfly.ButterflyTerminalKeyword, Plugin.ModConfig.ConfigCutieFlyPowerLevel.Value, Plugin.ModConfig.ConfigCutieFlyMaxSpawnCount.Value);
        }

        if (Plugin.ModConfig.ConfigSnailCatEnabled.Value)
        {
            SnailCat = new SnailCatAssets("snailcatassets");
            RegisterEnemyWithConfig(Plugin.ModConfig.ConfigSnailCatSpawnWeights.Value, SnailCat.SnailCatEnemyType, SnailCat.SnailCatTerminalNode, SnailCat.SnailCatTerminalKeyword, Plugin.ModConfig.ConfigSnailCatPowerLevel.Value, Plugin.ModConfig.ConfigSnailCatMaxSpawnCount.Value);
        }

        if (true)
        {
            DuckSong = new DuckSongAssets("ducksongassets");
            RegisterScrapWithConfig("", DuckSong.GrapeItem, -1, -1);
            RegisterScrapWithConfig("", DuckSong.LemonadePitcherItem, -1, -1);
            RegisterEnemyWithConfig("", DuckSong.DuckSongEnemyType, DuckSong.DuckSongTerminalNode, DuckSong.DuckSongTerminalKeyword, 2, 1);
            Plugin.samplePrefabs.Add(DuckSong.GrapeItem.itemName, DuckSong.GrapeItem);
            Plugin.samplePrefabs.Add(DuckSong.LemonadePitcherItem.itemName, DuckSong.LemonadePitcherItem);
        }

        if (false)
        {
            PokemonEnemies = new PokemonEnemyAssets("eeveelutionassets");
            RegisterScrapWithConfig("", PokemonEnemies.ChildEeveeItem, -1, -1);
            RegisterEnemyWithConfig("", PokemonEnemies.UmbreonEnemyType, null, null, 2, 1);
            RegisterEnemyWithConfig("", PokemonEnemies.VaporeonEnemyType, null, null, 2, 1);
        }
    }
}