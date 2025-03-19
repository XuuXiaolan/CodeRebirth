namespace CodeRebirth.src.MiscScripts;

using System.Collections.Generic;
using BepInEx.Configuration;

public static class CRConfigManager
{
    public static readonly Dictionary<string, CRConfig> CRConfigs = new();

    public static CRConfig CreateEnabledEntry(
        ConfigFile configFile,
        string name,
        string settingName,
        bool defaultValue,
        string description)
    {
        return new CRConfig
        {
            Enabled = CreateEntry(configFile, name, settingName, defaultValue, description)
        };
    }

    public static ConfigEntry<T> CreateEntry<T>(
        ConfigFile configFile,
        string name,
        string settingName,
        T defaultValue,
        string description)
    {
        string section = $"{name} Options";
        string key = $"{name} | {settingName}";
        var definition = new ConfigDefinition(section, key);
        
        // Check if the key already exists
        if (configFile.TryGetEntry<T>(definition, out var existingEntry))
        {
            Plugin.ExtendedLogging($"Config entry already exists: {key}");
            return existingEntry;
        }
        // Otherwise, create and bind a new entry.
        return configFile.Bind(section, key, defaultValue, description);
    }

    public static ConfigEntry<T> CreateGeneralConfig<T>(
        ConfigFile configFile,
        string keyName,
        string Settings,
        T DynamicConfigType,
        string Description)
    {
        return CreateEntry(configFile, keyName, Settings, DynamicConfigType, Description);
    }

    public static bool GetEnabledConfigResult(string keyName)
    {
        return CRConfigs[keyName].Enabled.Value;
    }
}

public class CRConfig 
{
    public ConfigEntry<bool> Enabled;
}

public class EnemyConfig : CRConfig
{
    public ConfigEntry<string> SpawnWeights;
    public ConfigEntry<float> PowerLevel;
    public ConfigEntry<int> MaxSpawnCount;
}

public static class EnemyConfigManager
{
    private static readonly Dictionary<string, EnemyConfig> enemyConfigs = new();

    public static EnemyConfig CreateEnemyConfig(
        ConfigFile configFile,
        string enemyName,
        bool defaultEnabled,
        string defaultSpawnWeights,
        float defaultPowerLevel,
        int defaultMaxSpawnCount)
    {
        return new EnemyConfig
        {
            Enabled = CRConfigManager.CreateEntry(configFile, enemyName, "Enabled", defaultEnabled, $"Whether {enemyName} is enabled."),
            SpawnWeights = CRConfigManager.CreateEntry(configFile, enemyName, "Spawn Weights", defaultSpawnWeights, $"Spawn weights for {enemyName}."),
            PowerLevel = CRConfigManager.CreateEntry(configFile, enemyName, "Power Level", defaultPowerLevel, $"Power level for {enemyName}."),
            MaxSpawnCount = CRConfigManager.CreateEntry(configFile, enemyName, "Max Spawn Count", defaultMaxSpawnCount, $"Max spawn count for {enemyName}.")
        };
    }

    public static void LoadConfigForEnemy(
        ConfigFile configFile, 
        string enemyName, 
        string defaultSpawnWeight, 
        float defaultPowerLevel, 
        int defaultSpawnCount)
    {
        var config = CreateEnemyConfig(
            configFile,
            enemyName,
            true,
            defaultSpawnWeight,
            defaultPowerLevel,
            defaultSpawnCount
        );
        enemyConfigs[enemyName] = config;
    }

    public static EnemyConfig GetEnemyConfig(string enemyName)
    {
        return enemyConfigs[enemyName];
    }
}

public class ItemConfig : CRConfig
{
    public ConfigEntry<string>? SpawnWeights = null;
    public ConfigEntry<bool>? IsScrapItem = null;
    public ConfigEntry<string>? Value = null;
    public ConfigEntry<bool>? IsShopItem = null;
    public ConfigEntry<int>? Cost = null;
}

public static class ItemConfigManager
{

    private static readonly Dictionary<string, ItemConfig> itemConfigs = new();

    public static ItemConfig CreateItemConfig(
        ConfigFile configFile,
        string itemName,
        bool defaultEnabled,
        string defaultSpawnWeights,
        bool createSpawnWeightsConfig,
        bool isScrapItem,
        bool createIsScrapItemConfig,
        bool isShopItem,
        bool createIsShopItemConfig,
        int cost)
    {
        return new ItemConfig
        {
            Enabled = CRConfigManager.CreateEntry(configFile, itemName, "Enabled", defaultEnabled, $"Whether {itemName} is enabled."),
            SpawnWeights = createSpawnWeightsConfig ? CRConfigManager.CreateEntry(configFile, itemName, "Spawn Weights", defaultSpawnWeights, $"MoonName:Rarity Spawn weights for {itemName}.") : null,
            IsScrapItem = createIsScrapItemConfig ? CRConfigManager.CreateEntry(configFile, itemName, "Is Scrap Item", isScrapItem, $"Whether {itemName} is a scrap item.") : null,
            Value = CRConfigManager.CreateEntry(configFile, itemName, "Value", "-1,-1", $"how much {itemName} is worth when spawning, formatted as min,max where -1,-1 is the default."),
            IsShopItem = createIsShopItemConfig ? CRConfigManager.CreateEntry(configFile, itemName, "Is Shop Item", isShopItem, $"Whether {itemName} is a shop item.") : null,
            Cost = createIsScrapItemConfig ? CRConfigManager.CreateEntry(configFile, itemName, "Cost", cost, $"Cost for {itemName} in the shop.") : null
        };
    }

    public static void LoadConfigForItem(
        ConfigFile configFile,
        string itemName,
        string defaultSpawnWeight,
        bool createSpawnWeightsConfig,
        bool isScrapItem,
        bool createIsScrapItemConfig,
        bool isShopItem,
        bool createIsShopItemConfig,
        int cost)
    {
        var config = CreateItemConfig(
            configFile,
            itemName,
            true,
            defaultSpawnWeight,
            createSpawnWeightsConfig,
            isScrapItem,
            createIsScrapItemConfig,
            isShopItem,
            createIsShopItemConfig,
            cost
        );
        itemConfigs[itemName] = config;
        CRConfigManager.CRConfigs[itemName] = config;
    }

    public static ItemConfig GetItemConfig(string itemName)
    {
        return itemConfigs[itemName];
    }
}

public class InsideHazardConfig
{
    public ConfigEntry<bool> Enabled;
    public ConfigEntry<string> CurveSpawnWeights;
}

public class UnlockablesConfig
{
    public ConfigEntry<bool> Enabled;
    public ConfigEntry<string> SpawnWeights;
    public ConfigEntry<float> PowerLevel;
    public ConfigEntry<int> MaxSpawnCount;
}