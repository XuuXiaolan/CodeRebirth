using BepInEx.Configuration;
using CodeRebirth.src.Util.Extensions;

namespace CodeRebirth.src.MiscScripts.ConfigManager;
public class EnemyConfig : CRConfig
{
    public ConfigEntry<string> SpawnWeights;
    public ConfigEntry<string> WeatherMultipliers;
    public ConfigEntry<float> PowerLevel;
    public ConfigEntry<int> MaxSpawnCount;
}

public static class EnemyConfigManager
{
    public static EnemyConfig CreateEnemyConfig(
        ConfigFile configFile,
        string enemyName,
        string keyName,
        string defaultSpawnWeights,
        string defaultWeatherMultipliers,
        float defaultPowerLevel,
        int defaultMaxSpawnCount)
    {
        return new EnemyConfig
        {
            Enabled = CRConfigManager.CRConfigs[keyName].Enabled,
            SpawnWeights = CRConfigManager.CreateEntry(configFile, keyName, $"{enemyName}", "Spawn Weights", defaultSpawnWeights, $"Spawn weights for {enemyName}."),
            WeatherMultipliers = CRConfigManager.CreateEntry(configFile, keyName, $"{enemyName}", "Weather Multpliers", defaultWeatherMultipliers, $"Weather x Spawnweight smultpliers for {enemyName}."),
            PowerLevel = CRConfigManager.CreateEntry(configFile, keyName, $"{enemyName}", "Power Level", defaultPowerLevel, $"Power level for {enemyName}."),
            MaxSpawnCount = CRConfigManager.CreateEntry(configFile, keyName, $"{enemyName}", "Max Spawn Count", defaultMaxSpawnCount, $"Max spawn count for {enemyName}.")
        };
    }

    public static void LoadConfigForEnemy(
        ConfigFile configFile,
        string enemyName,
        string keyName,
        string defaultSpawnWeight,
        string defaultWeatherMultipliers,
        float defaultPowerLevel,
        int defaultSpawnCount)
    {
        var config = CreateEnemyConfig(
            configFile,
            enemyName,
            keyName,
            defaultSpawnWeight,
            defaultWeatherMultipliers,
            defaultPowerLevel,
            defaultSpawnCount
        );
        CRConfigManager.CRConfigs[$"{keyName} | {enemyName}".CleanStringForConfig()] = config;
    }

    public static EnemyConfig GetEnemyConfig(string keyName, string enemyName)
    {
        return (EnemyConfig)CRConfigManager.CRConfigs[$"{keyName} | {enemyName}".CleanStringForConfig()]; // i.e. `DuckSong | Duck`
    }
}