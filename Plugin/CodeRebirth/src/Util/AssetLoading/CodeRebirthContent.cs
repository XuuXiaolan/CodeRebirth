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
    public List<EnemyData> enemies;
    public List<ItemData> items;
}

[Serializable]
public abstract class EntityData
{
    public string entityName; // Common name property for both enemies and items
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