namespace CodeRebirth.src.MiscScripts;

using System.Collections.Generic;
using BepInEx.Configuration;

public static class ConfigCreator
{
    public static ConfigEntry<T> CreateEntry<T>(
        ConfigFile configFile,
        string enemyName,
        string settingName,
        T defaultValue,
        string description)
    {
        string section = $"{enemyName} Options";
        string key = $"{enemyName} | {settingName}";
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
            Enabled = CreateEntry(configFile, enemyName, "Enabled", defaultEnabled, $"Whether {enemyName} is enabled."),
            SpawnWeights = CreateEntry(configFile, enemyName, "Spawn Weights", defaultSpawnWeights, $"Spawn weights for {enemyName}."),
            PowerLevel = CreateEntry(configFile, enemyName, "Power Level", defaultPowerLevel, $"Power level for {enemyName}."),
            MaxSpawnCount = CreateEntry(configFile, enemyName, "Max Spawn Count", defaultMaxSpawnCount, $"Max spawn count for {enemyName}.")
        };
    }

    public static ConfigEntry<T> CreateGeneralConfig<T>(
        ConfigFile configFile,
        string EnemyName,
        string Settings,
        T DynamicConfigType,
        string Description)
    {
        return CreateEntry(configFile, EnemyName, Settings, DynamicConfigType, Description);
    }
}

public class EnemyConfig
{
    public ConfigEntry<bool> Enabled;
    public ConfigEntry<string> SpawnWeights;
    public ConfigEntry<float> PowerLevel;
    public ConfigEntry<int> MaxSpawnCount;
}

public static class EnemyConfigManager
{
    private static readonly Dictionary<string, EnemyConfig> enemyConfigs = new();

    // Call this during your initialization phase for each enemy
    public static void LoadConfigForEnemy(
        ConfigFile configFile, 
        string enemyName, 
        string defaultSpawnWeight, 
        float defaultPowerLevel, 
        int defaultSpawnCount)
    {
        var config = ConfigCreator.CreateEnemyConfig(
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


public class ItemConfig
{
    public ConfigEntry<bool> Enabled;
    public ConfigEntry<string> SpawnWeights;
    public ConfigEntry<bool> IsShopItem;
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