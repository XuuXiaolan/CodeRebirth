using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Content.Weathers;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.MiscScripts.ConfigManager;
using CodeRebirth.src.Patches;
using CodeRebirth.src.Util.AssetLoading;
using LethalLevelLoader;
using LethalLib.Extras;
using LethalLib.Modules;
using Unity.Netcode;
using UnityEngine;
using WeatherRegistry;

namespace CodeRebirth.src.Util;

public class ContentHandler<T> where T : ContentHandler<T>
{
    internal static T Instance { get; private set; } = null!;

    internal ContentHandler()
    {
        Instance = (T)this;
    }

    protected void LoadEnemyConfigs(string enemyName, string keyName, string defaultSpawnWeight, string defaultWeatherMultpliers, float defaultPowerLevel, int defaultMaxSpawnCount)
    {
        EnemyConfigManager.LoadConfigForEnemy(
            Plugin.configFile,
            enemyName,
            keyName,
            defaultSpawnWeight,
            defaultWeatherMultpliers,
            defaultPowerLevel,
            defaultMaxSpawnCount
        );
    }

    protected void LoadItemConfigs(string itemName, string keyName, string defaultSpawnWeight, bool createSpawnWeightsConfig, bool isScrapItem, bool createIsScrapItemConfig, bool isShopItem, bool createIsShopItemConfig, int cost)
    {
        ItemConfigManager.LoadConfigForItem(
            Plugin.configFile,
            itemName,
            keyName,
            defaultSpawnWeight,
            createSpawnWeightsConfig,
            isScrapItem,
            createIsScrapItemConfig,
            isShopItem,
            createIsScrapItemConfig,
            cost
        );
    }

    protected void LoadMapObjectConfigs(string mapObjectName, string keyName, bool isInsideHazard, bool createInsideHazardConfig, string defaultInsideCurveSpawnWeights, bool createInsideCurveSpawnWeightsConfig, bool isOutsideHazard, bool createOutsideHazardConfig, string defaultOutsideCurveSpawnWeights, bool createOutsideCurveSpawnWeightsConfig)
    {
        MapObjectConfigManager.LoadConfigForMapObject(
            Plugin.configFile,
            mapObjectName,
            keyName,
            isInsideHazard,
            createInsideHazardConfig,
            defaultInsideCurveSpawnWeights,
            createInsideCurveSpawnWeightsConfig,
            isOutsideHazard,
            createOutsideHazardConfig,
            defaultOutsideCurveSpawnWeights,
            createOutsideCurveSpawnWeightsConfig
        );
    }

    protected void LoadUnlockableConfigs(string unlockableName, string keyName, int cost, bool isShipUpgrade, bool isDecor, bool isProgressive, bool createProgressiveConfig)
    {
        UnlockableConfigManager.LoadConfigForUnlockable(
            Plugin.configFile,
            unlockableName,
            keyName,
            cost,
            isShipUpgrade,
            isDecor,
            isProgressive,
            createProgressiveConfig
        );
    }

    protected void LoadEnabledConfigs(string keyName)
    {
        var config = CRConfigManager.CreateEnabledEntry(Plugin.configFile, keyName, keyName, "Enabled", true, $"Whether {keyName} is enabled.");
        CRConfigManager.CRConfigs[keyName] = config;
    }

    protected TAsset? LoadAndRegisterAssets<TAsset>(string assetBundleName, bool overrideEnabledConfig = false) where TAsset : AssetBundleLoader<TAsset>
    {
        AssetBundleData? assetBundleData = Plugin.Assets.CodeRebirthContent.assetBundles.Where(bundle => bundle.assetBundleName == assetBundleName).FirstOrDefault();
        if (assetBundleData == null)
        {
            Plugin.ExtendedLogging($"Plugin with assetbundle name: {assetBundleName} is not implemented yet!");
            return null;
        }
        LoadEnabledConfigs(assetBundleData.configName);
        bool loadBundle = CRConfigManager.GetEnabledConfigResult(assetBundleData.configName);
        if (!loadBundle && !overrideEnabledConfig) return null;

        // hacky workaround because generic functions can't create instances using new with parameters???
        TAsset assetBundle = (TAsset)Activator.CreateInstance(typeof(TAsset), new object[] { assetBundleName });
        assetBundle.AssetBundleData = assetBundleData;

        // do the loadfrombundle for all CRContentDefinitions

        CRContentDefinition[] definitions = assetBundle.bundle.LoadAllAssets<CRContentDefinition>();
        foreach (CRContentDefinition definition in definitions)
        {
            if (definition is CREnemyDefinition enemyDef)
            {
                // Add to enemy definitions.
                if (enemyDef.enemyType == null)
                    continue;

                Plugin.ExtendedLogging($"EnemyDefinition: {enemyDef.enemyType.enemyName}");
                assetBundle.enemyDefinitions.Add(enemyDef);
            }
            else if (definition is CRItemDefinition itemDef)
            {
                // Add to item definitions.
                if (itemDef.item == null)
                    continue;

                Plugin.ExtendedLogging($"ItemDefinition: {itemDef.item.itemName}");
                assetBundle.itemDefinitions.Add(itemDef);
            }
            else if (definition is CRMapObjectDefinition mapObjectDef)
            {
                // Add to map object definitions.
                if (mapObjectDef.gameObject == null)
                    continue;

                Plugin.ExtendedLogging($"MapObjectDefinition: {mapObjectDef.objectName}");
                assetBundle.mapObjectDefinitions.Add(mapObjectDef);
            }
            else if (definition is CRUnlockableDefinition unlockableDef)
            {
                Plugin.ExtendedLogging($"UnlockableDefinition: {unlockableDef.unlockableItemDef.unlockable.unlockableName}");
                if (unlockableDef.unlockableItemDef == null)
                    continue;

                assetBundle.unlockableDefinitions.Add(unlockableDef);
            }
            else if (definition is CRWeatherDefinition weatherDef)
            {
                Plugin.ExtendedLogging($"WeatherDefinition: {weatherDef.Weather.Name}");
                if (weatherDef.Weather == null)
                    continue;

                assetBundle.weatherDefinitions.Add(weatherDef);
            }
        }

        if (assetBundle.unlockableDefinitions.Count <= 0 && assetBundle.itemDefinitions.Count <= 0 && assetBundle.mapObjectDefinitions.Count <= 0 && assetBundle.enemyDefinitions.Count <= 0 && assetBundle.weatherDefinitions.Count <= 0)
        {
            Plugin.ExtendedLogging($"No definitions found in {assetBundleName}");
        }

        RegisterWeatherAssets(assetBundle);
        RegisterEnemyAssets(assetBundle);
        RegisterMapObjectAssets(assetBundle);
        RegisterUnlockableAssets(assetBundle);
        RegisterItemAssets(assetBundle);
        return assetBundle;
    }

    protected void RegisterWeatherAssets<TAsset>(TAsset? assetBundle) where TAsset : AssetBundleLoader<TAsset>
    {
        if (assetBundle == null || assetBundle.AssetBundleData == null) return;
        Plugin.ExtendedLogging($"Registering weathers for {assetBundle.AssetBundleData.assetBundleName}");
        int definitionIndex = 0;
        assetBundle.weatherDefinitions.Sort((a, b) => a.Weather.Name.CompareTo(b.Weather.Name));
        assetBundle.AssetBundleData.weathers.Sort((a, b) => a.entityName.CompareTo(b.entityName));
        foreach (var CRWeatherDefinition in assetBundle.WeatherDefinitions)
        {
            WeatherData weatherData = assetBundle.AssetBundleData.weathers[definitionIndex];
            Plugin.ExtendedLogging($"WeatherData {weatherData.entityName}");
            Plugin.ExtendedLogging($"WeatherDefinition {CRWeatherDefinition.Weather.Name}");
            foreach (var configDefinition in CRWeatherDefinition.ConfigEntries)
            {
                Plugin.ExtendedLogging($"Registering config {configDefinition.settingName} | {configDefinition.settingDesc} for {CRWeatherDefinition.Weather.Name}");
                ConfigMisc.CreateDynamicGeneralConfig(configDefinition, assetBundle.AssetBundleData.configName);
            }

            GameObject? effectObject = null;
            if (CRWeatherDefinition.Weather.Effect.EffectObject != null)
            {
                effectObject = GameObject.Instantiate(CRWeatherDefinition.Weather.Effect.EffectObject);
                if (effectObject != null)
                {
                    effectObject.hideFlags = HideFlags.HideAndDontSave;
                    GameObject.DontDestroyOnLoad(effectObject);
                }
            }

            GameObject? effectPermanentObject = null;
            if (CRWeatherDefinition.Weather.Effect.WorldObject != null)
            {
                effectPermanentObject = GameObject.Instantiate(CRWeatherDefinition.Weather.Effect.WorldObject);
                if (effectPermanentObject != null)
                {
                    effectPermanentObject.hideFlags = HideFlags.HideAndDontSave;
                    GameObject.DontDestroyOnLoad(effectPermanentObject);
                }
            }

            ImprovedWeatherEffect newImprovedWeatherEffect = new(effectObject, effectPermanentObject)
            {
                SunAnimatorBool = CRWeatherDefinition.Weather.Effect.SunAnimatorBool,
            };

            Weather weather = new Weather($"{CRWeatherDefinition.Weather.Name}", newImprovedWeatherEffect);
            weather.Color = CRWeatherDefinition.Weather.Color;
            weather.Config = new()
            {
                DefaultWeight = new(weatherData.spawnWeight),
                ScrapValueMultiplier = new(weatherData.scrapValueMultiplier),
                ScrapAmountMultiplier = new(weatherData.scrapMultiplier),
                FilteringOption = new(!weatherData.isExclude, weatherData.createExcludeConfig),
                LevelFilters = new(weatherData.excludeOrIncludeList),
            };

            WeatherRegistry.WeatherManager.RegisterWeather(weather);
            definitionIndex++;
        }
        CodeRebirthRegistry.RegisteredCRWeathers.AddRange(assetBundle.WeatherDefinitions);
    }

    protected void RegisterEnemyAssets<TAsset>(TAsset? assetBundle) where TAsset : AssetBundleLoader<TAsset>
    {
        if (assetBundle == null || assetBundle.AssetBundleData == null) return;
        Plugin.ExtendedLogging($"Registering enemies for {assetBundle.AssetBundleData.assetBundleName}");
        int definitionIndex = 0;
        assetBundle.enemyDefinitions.Sort((a, b) => a.enemyType.enemyName.CompareTo(b.enemyType.enemyName));
        assetBundle.AssetBundleData.enemies.Sort((a, b) => a.entityName.CompareTo(b.entityName));
        foreach (var CREnemyDefinition in assetBundle.EnemyDefinitions)
        {
            AssetLoading.EnemyData enemyData = assetBundle.AssetBundleData.enemies[definitionIndex];
            Plugin.ExtendedLogging($"EnemyData {enemyData.entityName}");
            Plugin.ExtendedLogging($"EnemyDefinition {CREnemyDefinition.enemyType.enemyName}");
            LoadEnemyConfigs(CREnemyDefinition.enemyType.enemyName, assetBundle.AssetBundleData.configName, enemyData.spawnWeights, enemyData.weatherMultipliers, enemyData.powerLevel, enemyData.maxSpawnCount);
            var enemyConfig = EnemyConfigManager.GetEnemyConfig(assetBundle.AssetBundleData.configName, CREnemyDefinition.enemyType.enemyName);
            foreach (var configDefinition in CREnemyDefinition.ConfigEntries)
            {
                Plugin.ExtendedLogging($"Registering config {configDefinition.settingName} | {configDefinition.settingDesc} for {CREnemyDefinition.enemyType.enemyName}");
                ConfigMisc.CreateDynamicGeneralConfig(configDefinition, assetBundle.AssetBundleData.configName);
            }

            RegisterEnemyWeatherMultipliers(CREnemyDefinition, enemyConfig.WeatherMultpliers.Value);
            EnemyType enemy = CREnemyDefinition.enemyType;
            enemy.MaxCount = enemyConfig.MaxSpawnCount.Value;
            enemy.PowerLevel = enemyConfig.PowerLevel.Value;
            (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(enemyConfig.SpawnWeights.Value);
            Enemies.RegisterEnemy(enemy, spawnRateByLevelType, spawnRateByCustomLevelType, CREnemyDefinition.terminalNode, CREnemyDefinition.terminalKeyword);
            definitionIndex++;
        }
        CodeRebirthRegistry.RegisteredCREnemies.AddRange(assetBundle.EnemyDefinitions);
    }

    protected void RegisterItemAssets<TAsset>(TAsset? assetBundle) where TAsset : AssetBundleLoader<TAsset>
    {
        if (assetBundle == null || assetBundle.AssetBundleData == null) return;
        Plugin.ExtendedLogging($"Registering items for {assetBundle.AssetBundleData.assetBundleName}");
        assetBundle.itemDefinitions.Sort((a, b) => a.item.itemName.CompareTo(b.item.itemName));
        assetBundle.AssetBundleData.items.Sort((a, b) => a.entityName.CompareTo(b.entityName));
        int definitionIndex = 0;
        foreach (var CRItemDefinition in assetBundle.ItemDefinitions)
        {
            Plugin.samplePrefabs.Add(CRItemDefinition.item.itemName, CRItemDefinition.item);
            ItemData itemData = assetBundle.AssetBundleData.items[definitionIndex];
            Plugin.ExtendedLogging($"ItemData: {assetBundle.AssetBundleData.items[definitionIndex].entityName}");
            Plugin.ExtendedLogging($"CRItemDefinition: {CRItemDefinition.item.itemName}");
            LoadItemConfigs(CRItemDefinition.item.itemName, assetBundle.AssetBundleData.configName, itemData.spawnWeights, itemData.generateSpawnWeightsConfig, itemData.isScrap, itemData.generateScrapConfig, itemData.isShopItem, itemData.generateShopItemConfig, itemData.cost);
            var itemConfig = ItemConfigManager.GetItemConfig(assetBundle.AssetBundleData.configName, CRItemDefinition.item.itemName);
            foreach (var configDefinition in CRItemDefinition.ConfigEntries)
            {
                ConfigMisc.CreateDynamicGeneralConfig(configDefinition, assetBundle.AssetBundleData.configName);
            }

            Item item = CRItemDefinition.item;
            bool isScrap = itemConfig.IsScrapItem?.Value ?? itemData.isScrap;
            bool isShop = itemConfig.IsShopItem?.Value ?? itemData.isShopItem;
            int cost = itemConfig.Cost?.Value ?? itemData.cost;
            string spawnWeights = itemConfig.SpawnWeights?.Value ?? itemData.spawnWeights;
            string value = itemConfig.Value?.Value ?? "-1,-1";
            Plugin.ExtendedLogging($"Registering config for {item.itemName} | {spawnWeights} | {isScrap} | {isShop} | {cost} | {value}");
            RegisterShopItemWithConfig(isShop, isScrap, item, CRItemDefinition.terminalNode, cost, spawnWeights, value);
            definitionIndex++;
        }
        CodeRebirthRegistry.RegisteredCRItems.AddRange(assetBundle.ItemDefinitions);
    }

    protected void RegisterMapObjectAssets<TAsset>(TAsset? assetBundle) where TAsset : AssetBundleLoader<TAsset>
    {
        if (assetBundle == null || assetBundle.AssetBundleData == null) return;
        Plugin.ExtendedLogging($"Registering MapObjects for {assetBundle.AssetBundleData.assetBundleName}");
        int definitionIndex = 0;
        assetBundle.mapObjectDefinitions.Sort((a, b) => a.objectName.CompareTo(b.objectName));
        assetBundle.AssetBundleData.mapObjects.Sort((a, b) => a.entityName.CompareTo(b.entityName));
        foreach (var CRMapObjectDefinition in assetBundle.MapObjectDefinitions)
        {
            MapObjectData mapObjectData = assetBundle.AssetBundleData.mapObjects[definitionIndex];
            Plugin.ExtendedLogging($"MapObjectData: {mapObjectData.entityName}");
            Plugin.ExtendedLogging($"MapObjectDefinition: {CRMapObjectDefinition.objectName}");
            LoadMapObjectConfigs(CRMapObjectDefinition.objectName, assetBundle.AssetBundleData.configName, mapObjectData.isInsideHazard, mapObjectData.createInsideHazardConfig, mapObjectData.defaultInsideCurveSpawnWeights, mapObjectData.createInsideCurveSpawnWeightsConfig, mapObjectData.isOutsideHazard, mapObjectData.createOutsideHazardConfig, mapObjectData.defaultOutsideCurveSpawnWeights, mapObjectData.createOutsideCurveSpawnWeightsConfig);
            var mapObjectConfig = MapObjectConfigManager.GetMapObjectConfig(assetBundle.AssetBundleData.configName, CRMapObjectDefinition.objectName);
            foreach (var configDefinition in CRMapObjectDefinition.ConfigEntries)
            {
                ConfigMisc.CreateDynamicGeneralConfig(configDefinition, assetBundle.AssetBundleData.configName);
            }

            GameObject gameObject = CRMapObjectDefinition.gameObject;
            SpawnSyncedCRObject.CRObjectType CRObjectType = CRMapObjectDefinition.CRObjectType;
            bool alignWithTerrain = CRMapObjectDefinition.alignWithTerrain;
            bool inside = mapObjectConfig.InsideHazard?.Value ?? mapObjectData.isInsideHazard;
            string insideCurveSpawnWeights = mapObjectConfig.InsideCurveSpawnWeights?.Value ?? mapObjectData.defaultInsideCurveSpawnWeights;
            bool outside = mapObjectConfig.OutsideHazard?.Value ?? mapObjectData.isOutsideHazard;
            string outsideCurveSpawnWeightsConfig = mapObjectConfig.OutsideCurveSpawnWeights?.Value ?? mapObjectData.defaultOutsideCurveSpawnWeights;

            RegisterMapObjectWithConfig(gameObject, CRObjectType, inside, insideCurveSpawnWeights, outside, alignWithTerrain, outsideCurveSpawnWeightsConfig);
            definitionIndex++;
        }
        CodeRebirthRegistry.RegisteredCRMapObjects.AddRange(assetBundle.MapObjectDefinitions);
    }

    protected void RegisterUnlockableAssets<TAsset>(TAsset? assetBundle) where TAsset : AssetBundleLoader<TAsset>
    {
        if (assetBundle == null || assetBundle.AssetBundleData == null) return;
        Plugin.ExtendedLogging($"Registering Unlockables for {assetBundle.AssetBundleData.assetBundleName}");
        int definitionIndex = 0;
        assetBundle.unlockableDefinitions.Sort((a, b) => a.unlockableItemDef.unlockable.unlockableName.CompareTo(b.unlockableItemDef.unlockable.unlockableName));
        assetBundle.AssetBundleData.unlockables.Sort((a, b) => a.entityName.CompareTo(b.entityName));
        foreach (var CRUnlockableDefinition in assetBundle.UnlockableDefinitions)
        {
            UnlockableData unlockableData = assetBundle.AssetBundleData.unlockables[definitionIndex];
            Plugin.ExtendedLogging($"UnlockableData: {unlockableData.entityName}");
            Plugin.ExtendedLogging($"UnlockableDefinition: {CRUnlockableDefinition.unlockableItemDef.unlockable.unlockableName}");
            LoadUnlockableConfigs(CRUnlockableDefinition.unlockableItemDef.unlockable.unlockableName, assetBundle.AssetBundleData.configName, unlockableData.cost, unlockableData.isShipUpgrade, unlockableData.isDecor, unlockableData.isProgressive, unlockableData.createProgressiveConfig);
            var unlockableConfig = UnlockableConfigManager.GetUnlockableConfig(assetBundle.AssetBundleData.configName, CRUnlockableDefinition.unlockableItemDef.unlockable.unlockableName);
            foreach (var configDefinition in CRUnlockableDefinition.ConfigEntries)
            {
                ConfigMisc.CreateDynamicGeneralConfig(configDefinition, assetBundle.AssetBundleData.configName);
            }

            UnlockableItemDef unlockableItemDef = CRUnlockableDefinition.unlockableItemDef;
            int cost = unlockableConfig.Cost.Value;
            bool isShipUpgrade = unlockableConfig.IsShipUpgrade.Value;
            bool isDecor = unlockableConfig.IsDecor.Value;
            bool isProgressive = unlockableConfig.IsProgressive?.Value ?? unlockableData.isProgressive;
            RegisterUnlockableWithConfig(CRUnlockableDefinition, cost, isShipUpgrade, isDecor, isProgressive);
            definitionIndex++;
        }
        CodeRebirthRegistry.RegisteredCRUnlockables.AddRange(assetBundle.UnlockableDefinitions);
    }

    protected void RegisterUnlockableWithConfig(CRUnlockableDefinition unlockableDefinition, int cost, bool isShipUpgrade, bool isDecor, bool isProgressive)
    {
        if (isShipUpgrade)
        {
            Unlockables.RegisterUnlockable(unlockableDefinition.unlockableItemDef, cost, StoreType.ShipUpgrade);
        }
        if (isDecor)
        {
            Unlockables.RegisterUnlockable(unlockableDefinition.unlockableItemDef, cost, StoreType.Decor);
        }
        if (isProgressive)
        {
            ProgressiveUnlockables.unlockableIDs.Add(unlockableDefinition.unlockableItemDef.unlockable, false);
            ProgressiveUnlockables.unlockableNames.Add(unlockableDefinition.unlockableItemDef.unlockable.unlockableName);
            if (unlockableDefinition.DenyPurchaseNode == null)
            {
                unlockableDefinition.DenyPurchaseNode = ScriptableObject.CreateInstance<TerminalNode>();
                unlockableDefinition.DenyPurchaseNode.displayText = "Ship Upgrade or Decor is not unlocked";
            }
            ProgressiveUnlockables.rejectionNodes.Add(unlockableDefinition.DenyPurchaseNode);
        }
    }

    protected void RegisterScrapWithConfig(string? configMoonRarity, Item scrap)
    {
        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
        Items.RegisterScrap(scrap, spawnRateByLevelType, spawnRateByCustomLevelType);
    }

    protected void RegisterShopItemWithConfig(bool enabledShopItem, bool enabledScrap, Item item, TerminalNode? terminalNode, int itemCost, string configMoonRarity, string minMaxWorth)
    {
        int[] scrapValues = ChangeItemValues(string.IsNullOrEmpty(minMaxWorth) ? "-1,-1" : minMaxWorth);
        int itemWorthMin = scrapValues[0];
        int itemWorthMax = scrapValues[1];

        if (itemWorthMax != -1 && itemWorthMin != -1)
        {
            if (itemWorthMax < itemWorthMin)
            {
                itemWorthMax = itemWorthMin;
            }
            item.minValue = (int)(itemWorthMin / 0.4f);
            item.maxValue = (int)(itemWorthMax / 0.4f);
        }

        if (enabledShopItem)
        {
            Items.RegisterShopItem(item, null, null, terminalNode, itemCost);
        }
        if (enabledScrap)
        {
            RegisterScrapWithConfig(configMoonRarity, item);
        }
    }

    protected void RegisterMapObjectWithConfig(GameObject prefab, SpawnSyncedCRObject.CRObjectType CRObjectType, bool inside, string insideConfigString, bool outside, bool alignWithTerrain, string outsideConfigString)
    {
        if (inside)
        {
            RegisterInsideMapObjectWithConfig(prefab, insideConfigString);
        }
        if (outside)
        {
            RegisterOutsideMapObjectWithConfig(prefab, alignWithTerrain, outsideConfigString);
        }
        Plugin.ExtendedLogging($"Registered map object: {prefab.name} to outside: {outside} and inside: {inside} to {CRObjectType} with inside config: {insideConfigString} and outside config: {outsideConfigString}");
        MapObjectHandler.Instance.prefabMapping[CRObjectType] = prefab;
    }

    protected void RegisterOutsideMapObjectWithConfig(GameObject prefab, bool _alignWithTerrain, string configString)
    {
        // Create the map object definition
        SpawnableOutsideObjectDef mapObjDef = ScriptableObject.CreateInstance<SpawnableOutsideObjectDef>();
        mapObjDef.spawnableMapObject = new SpawnableOutsideObjectWithRarity
        {
            spawnableObject = ScriptableObject.CreateInstance<SpawnableOutsideObject>()
        };

        mapObjDef.spawnableMapObject.spawnableObject.prefabToSpawn = prefab;
        // Parse the configuration string
        (Dictionary<Levels.LevelTypes, string> spawnRateByLevelType, Dictionary<string, string> spawnRateByCustomLevelType) = ConfigParsingWithCurve(configString);

        // Create dictionaries to hold animation curves for each level type
        Dictionary<Levels.LevelTypes, AnimationCurve> curvesByLevelType = new();
        Dictionary<string, AnimationCurve> curvesByCustomLevelType = new();

        bool allCurveExists = false;
        AnimationCurve allAnimationCurve = AnimationCurve.Linear(0, 0, 1, 0);

        bool vanillaCurveExists = false;
        AnimationCurve vanillaAnimationCurve = AnimationCurve.Linear(0, 0, 1, 0);

        bool moddedCurveExists = false;
        AnimationCurve moddedAnimationCurve = AnimationCurve.Linear(0, 0, 1, 0);

        // Populate the animation curves
        foreach (var entry in spawnRateByLevelType)
        {
            Plugin.ExtendedLogging($"Registering map object {prefab.name} for level {entry.Key} with curve {entry.Value}");
            curvesByLevelType[entry.Key] = CreateCurveFromString(entry.Value, prefab.name, entry.Key.ToString());
            if (entry.Key == Levels.LevelTypes.Vanilla)
            {
                vanillaCurveExists = true;
                vanillaAnimationCurve = curvesByLevelType[entry.Key];
            }
            else if (entry.Key == Levels.LevelTypes.Modded)
            {
                moddedCurveExists = true;
                moddedAnimationCurve = curvesByLevelType[entry.Key];
            }
            else if (entry.Key == Levels.LevelTypes.All)
            {
                allCurveExists = true;
                allAnimationCurve = curvesByLevelType[entry.Key];
            }
        }
        foreach (var entry in spawnRateByCustomLevelType)
        {
            curvesByCustomLevelType[entry.Key] = CreateCurveFromString(entry.Value, prefab.name, entry.Key);
        }

        // Register the map object with a single lambda function
        RegisteredCRMapObject registeredMapObject = new RegisteredCRMapObject
        {
            alignWithTerrain = _alignWithTerrain,
            hasNetworkObject = mapObjDef.spawnableMapObject.spawnableObject.prefabToSpawn.GetComponent<NetworkObject>() != null,
            outsideObject = mapObjDef.spawnableMapObject,
            levels = Levels.LevelTypes.All,
            spawnLevelOverrides = curvesByCustomLevelType.Keys.ToArray().Select(s => s.ToLowerInvariant()).ToArray(),
            spawnRateFunction =
            level =>
            {
                return CurveFunction(level, prefab, curvesByLevelType, curvesByCustomLevelType, vanillaCurveExists, vanillaAnimationCurve, moddedCurveExists, moddedAnimationCurve, allCurveExists, allAnimationCurve);
            }
        };
        RoundManagerPatch.registeredMapObjects.Add(registeredMapObject);
    }

    protected void RegisterEnemyWeatherMultipliers(CREnemyDefinition enemyDefinition, string weatherMultipliers)
    {
        List<string> weatherMultipliersList = weatherMultipliers.Split(',').Select(s => s.Trim()).ToList();
        List<(string weatherName, float Multiplier)> weatherNameWithMultplierList = new();
        foreach (var weatherMultiplierInList in weatherMultipliersList.Select(s => s.Split(':')))
        {
            string weatherName = weatherMultiplierInList[0];
            if (float.TryParse(weatherMultiplierInList[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float multiplier))
            {
                Plugin.ExtendedLogging($"Added Weather: {weatherName} to enemy: {enemyDefinition.enemyType.enemyName} with multiplier: {multiplier}");
                enemyDefinition.WeatherMultipliers.Add(weatherName, multiplier);
            }
            else
            {
                Plugin.Logger.LogError($"Weather: {weatherName} given invalid multiplier: {weatherMultiplierInList[1]}");
            }
        }
    }

    protected bool TryGetCurveDictAndLevelTag(Dictionary<string, AnimationCurve> curvesByCustomLevel, SelectableLevel level, out string tagName)
    {
        tagName = string.Empty;
        ExtendedLevel? extendedLevel = LethalLevelLoader.PatchedContent.CustomExtendedLevels.Where(x => x.SelectableLevel == level).FirstOrDefault() ?? LethalLevelLoader.PatchedContent.VanillaExtendedLevels.Where(x => x.SelectableLevel == level).FirstOrDefault();
        if (extendedLevel == null) return false;
        foreach (var curve in curvesByCustomLevel)
        {
            foreach (var tag in extendedLevel.ContentTags)
            {
                if (tag.contentTagName.ToLowerInvariant() == curve.Key)
                {
                    tagName = curve.Key;
                    return true;
                }
            }
        }
        return false;
    }

    protected void RegisterInsideMapObjectWithConfig(GameObject prefab, string configString)
    {
        // Create the map object definition
        SpawnableMapObjectDef mapObjDef = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
        mapObjDef.spawnableMapObject = new SpawnableMapObject
        {
            prefabToSpawn = prefab
        };

        // Parse the configuration string
        (Dictionary<Levels.LevelTypes, string> spawnRateByLevelType, Dictionary<string, string> spawnRateByCustomLevelType) = ConfigParsingWithCurve(configString);

        // Create dictionaries to hold animation curves for each level type
        Dictionary<Levels.LevelTypes, AnimationCurve> curvesByLevelType = new();
        Dictionary<string, AnimationCurve> curvesByCustomLevelType = new();

        bool allCurveExists = false;
        AnimationCurve allAnimationCurve = AnimationCurve.Linear(0, 0, 1, 0);

        bool vanillaCurveExists = false;
        AnimationCurve vanillaAnimationCurve = AnimationCurve.Linear(0, 0, 1, 0);

        bool moddedCurveExists = false;
        AnimationCurve moddedAnimationCurve = AnimationCurve.Linear(0, 0, 1, 0);

        // Populate the animation curves
        foreach (var entry in spawnRateByLevelType)
        {
            Plugin.ExtendedLogging($"Registering map object {prefab.name} for level {entry.Key} with curve {entry.Value}");
            curvesByLevelType[entry.Key] = CreateCurveFromString(entry.Value, prefab.name, entry.Key.ToString());
            if (entry.Key == Levels.LevelTypes.Vanilla)
            {
                vanillaCurveExists = true;
                vanillaAnimationCurve = curvesByLevelType[entry.Key];
            }
            else if (entry.Key == Levels.LevelTypes.Modded)
            {
                moddedCurveExists = true;
                moddedAnimationCurve = curvesByLevelType[entry.Key];
            }
            else if (entry.Key == Levels.LevelTypes.All)
            {
                allCurveExists = true;
                allAnimationCurve = curvesByLevelType[entry.Key];
            }
        }
        foreach (var entry in spawnRateByCustomLevelType)
        {
            curvesByCustomLevelType[entry.Key] = CreateCurveFromString(entry.Value, prefab.name, entry.Key);
        }

        // Register the map object with a single lambda function
        MapObjects.RegisterMapObject(
            mapObjDef,
            Levels.LevelTypes.All,
            curvesByCustomLevelType.Keys.ToArray().Select(s => s.ToLowerInvariant()).ToArray(),
            level =>
            {
                return CurveFunction(level, prefab, curvesByLevelType, curvesByCustomLevelType, vanillaCurveExists, vanillaAnimationCurve, moddedCurveExists, moddedAnimationCurve, allCurveExists, allAnimationCurve);
            }
        );
    }

    public AnimationCurve CurveFunction(SelectableLevel level, GameObject prefab, Dictionary<Levels.LevelTypes, AnimationCurve> curvesByLevelType, Dictionary<string, AnimationCurve> curvesByCustomLevelType, bool vanillaCurveExists, AnimationCurve vanillaAnimationCurve, bool moddedCurveExists, AnimationCurve moddedAnimationCurve, bool allCurveExists, AnimationCurve allAnimationCurve)
    {
        if (level == null)
            return AnimationCurve.Linear(0, 0, 1, 0);

        Plugin.ExtendedLogging($"Registering prefab {prefab.name} for level {level}, vanillaCurveExists: {vanillaCurveExists}, moddedCurveExists: {moddedCurveExists}, allCurveExists: {allCurveExists}");
        string actualLevelName = Levels.Compatibility.GetLLLNameOfLevel(level.name);
        Plugin.ExtendedLogging($"actual level name: {actualLevelName}");

        bool isValidLevelType = Enum.TryParse(actualLevelName, true, out Levels.LevelTypes levelType);
        bool isVanilla = false;
        if (isValidLevelType && levelType != Levels.LevelTypes.Modded)
        {
            isVanilla = true;
        }

        if (curvesByLevelType.TryGetValue(levelType, out AnimationCurve curve))
        {
            return curve;
        }
        else if (curvesByCustomLevelType.TryGetValue(actualLevelName, out curve))
        {
            return curve;
        }
        else if (TryGetCurveDictAndLevelTag(curvesByCustomLevelType, level, out string tagName) && curvesByCustomLevelType.TryGetValue(tagName, out curve))
        {
            Plugin.ExtendedLogging($"registering a mapobject through a tag, nice.");
            return curve;
        }
        else if (isVanilla && vanillaCurveExists)
        {
            return vanillaAnimationCurve;
        }
        else if (moddedCurveExists)
        {
            return moddedAnimationCurve;
        }
        else if (allCurveExists)
        {
            return allAnimationCurve;
        }
        Plugin.ExtendedLogging($"Failed to find curve for level: {level}");
        return AnimationCurve.Linear(0, 0, 1, 0); // Default case if no curve matches
    }

    protected (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) ConfigParsing(string? configMoonRarity)
    {
        Dictionary<Levels.LevelTypes, int> spawnRateByLevelType = new();
        Dictionary<string, int> spawnRateByCustomLevelType = new();
        if (configMoonRarity == null)
        {
            return (spawnRateByLevelType, spawnRateByCustomLevelType);
        }
        foreach (string entry in configMoonRarity.Split(',').Select(s => s.Trim()))
        {
            string[] entryParts = entry.Split(':').Select(s => s.Trim()).ToArray();

            if (entryParts.Length != 2) continue;

            string name = entryParts[0].ToLowerInvariant();

            if (!int.TryParse(entryParts[1], out int spawnrate)) continue;

            if (name == "custom")
            {
                name = "modded";
            }

            if (Enum.TryParse(name, true, out Levels.LevelTypes levelType))
            {
                spawnRateByLevelType[levelType] = spawnrate;
            }
            else
            {
                // Try appending "Level" to the name and re-attempt parsing
                string modifiedName = name + "Level";
                if (System.Enum.TryParse(modifiedName, true, out levelType))
                {
                    spawnRateByLevelType[levelType] = spawnrate;
                }
                else
                {
                    Plugin.ExtendedLogging($"Failed to parse level type: {name}");
                    spawnRateByCustomLevelType[name] = spawnrate;
                }
            }
        }
        return (spawnRateByLevelType, spawnRateByCustomLevelType);
    }

    protected (Dictionary<Levels.LevelTypes, string> spawnRateByLevelType, Dictionary<string, string> spawnRateByCustomLevelType) ConfigParsingWithCurve(string configMoonRarity)
    {
        Dictionary<Levels.LevelTypes, string> spawnRateByLevelType = new();
        Dictionary<string, string> spawnRateByCustomLevelType = new();
        foreach (string entry in configMoonRarity.Split('|').Select(s => s.Trim()))
        {
            string[] entryParts = entry.Split('-').Select(s => s.Trim()).ToArray();

            if (entryParts.Length != 2) continue;

            string name = entryParts[0].ToLowerInvariant();

            if (name == "custom")
            {
                name = "modded";
            }

            if (System.Enum.TryParse(name, true, out Levels.LevelTypes levelType))
            {
                spawnRateByLevelType[levelType] = entryParts[1];
            }
            else
            {
                // Try appending "Level" to the name and re-attempt parsing
                string modifiedName = name + "level";
                if (System.Enum.TryParse(modifiedName, true, out levelType))
                {
                    spawnRateByLevelType[levelType] = entryParts[1];
                }
                else
                {
                    spawnRateByCustomLevelType[name] = entryParts[1];
                }
            }
        }
        return (spawnRateByLevelType, spawnRateByCustomLevelType);
    }

    protected int[] ChangeItemValues(string config)
    {
        string[] configParts = config.Split(',').Select(s => s.Trim()).ToArray();
        foreach (string configPart in configParts)
        {
            configPart.Trim();
        }
        int minWorthInt = -1;
        int maxWorthInt = -1;
        if (configParts.Length == 2)
        {
            Plugin.ExtendedLogging("[Scrap Worth] Changing item worth between " + configParts[0] + " and " + configParts[1]);
            minWorthInt = int.Parse(configParts[0]);
            maxWorthInt = int.Parse(configParts[1]);
        }
        return [minWorthInt, maxWorthInt];
    }

    public AnimationCurve CreateCurveFromString(string keyValuePairs, string nameOfThing, string MoonName)
    {
        // Split the input string into individual key-value pairs
        string[] pairs = keyValuePairs.Split(';').Select(s => s.Trim()).ToArray();
        if (pairs.Length == 0)
        {
            if (int.TryParse(keyValuePairs, out int result))
            {
                return new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, result));
            }
            else
            {
                Plugin.Logger.LogError($"Invalid key-value pairs format: {keyValuePairs}");
                return AnimationCurve.Linear(0, 0, 1, 0);
            }
        }
        List<Keyframe> keyframes = new();

        // Iterate over each pair and parse the key and value to create keyframes
        foreach (string pair in pairs)
        {
            string[] splitPair = pair.Split(',').Select(s => s.Trim()).ToArray();
            if (splitPair.Length == 2 &&
                float.TryParse(splitPair[0], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float time) &&
                float.TryParse(splitPair[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                keyframes.Add(new Keyframe(time, value));
            }
            else
            {
                Plugin.Logger.LogError($"Failed config for hazard: {nameOfThing}");
                Plugin.Logger.LogError($"Split pair length: {splitPair.Length}");
                if (splitPair.Length != 2)
                {
                    Plugin.Logger.LogError($"Invalid key,value pair format: {pair}");
                    continue;
                }
                Plugin.Logger.LogError($"Could parse first value: {float.TryParse(splitPair[0], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float key1)}, instead got: {key1}, with splitPair0 being: {splitPair[0]}");
                Plugin.Logger.LogError($"Could parse second value: {float.TryParse(splitPair[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float value2)}, instead got: {value2}, with splitPair1 being: {splitPair[1]}");
                Plugin.Logger.LogError($"Invalid key,value pair format: {pair}");
            }
        }

        // Create the animation curve with the generated keyframes and apply smoothing
        var curve = new AnimationCurve(keyframes.ToArray());
        /*for (int i = 0; i < keyframes.Count; i++)
        {
            curve.SmoothTangents(i, 0.5f); // Adjust the smoothing as necessary
        }*/

        return curve;
    }
}