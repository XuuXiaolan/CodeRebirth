namespace CodeRebirth.src.Util.AssetLoading;

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CodeRebirthContent", menuName = "CodeRebirth/CodeRebirthContent", order = 0)]
public class CodeRebirthContent : ScriptableObject
{
    public List<AssetBundleData> assetBundles;
}

[Serializable]
public class AssetBundleData
{
    public string assetBundleName;
    public string configName;
    public List<WeatherData> weathers;
    public List<EnemyData> enemies;
    public List<ItemData> items;
    public List<MapObjectData> mapObjects;
    public List<UnlockableData> unlockables;
}

[Serializable]
public abstract class EntityData
{
    public string entityName;
}

[Serializable]
public class WeatherData : EntityData
{
    public int spawnWeight;
    public float scrapMultiplier;
    public float scrapValueMultiplier;
    public bool isExclude;
    public bool createExcludeConfig;
    public string excludeOrIncludeList;
}

[Serializable]
public class EnemyData : EntityData
{
    public string spawnWeights;
    public float powerLevel;
    public int maxSpawnCount;
}

[Serializable]
public class ItemData : EntityData
{
    public string spawnWeights;
    public bool generateSpawnWeightsConfig;
    public bool isScrap;
    public bool generateScrapConfig;
    public bool isShopItem;
    public bool generateShopItemConfig;
    public int cost;
}

[Serializable]
public class MapObjectData : EntityData
{
    public bool isInsideHazard;
    public bool createInsideHazardConfig;
    public string defaultInsideCurveSpawnWeights;
    public bool createInsideCurveSpawnWeightsConfig;
    public bool isOutsideHazard;
    public bool createOutsideHazardConfig;
    public string defaultOutsideCurveSpawnWeights;
    public bool createOutsideCurveSpawnWeightsConfig;
}

[Serializable]
public class UnlockableData : EntityData
{
    public int cost;
    public bool isShipUpgrade;
    public bool isDecor;
    public bool isProgressive;
    public bool createProgressiveConfig;
}