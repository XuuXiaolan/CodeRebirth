using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using CodeRebirth.src.Util.Extensions;

namespace CodeRebirth.src.MiscScripts.ConfigManager;
public class CRConfig 
{
    public ConfigEntry<bool> Enabled;
}

public static class CRConfigManager
{
    public static readonly Dictionary<string, CRConfig> CRConfigs = new();
    public static readonly Dictionary<string, ConfigEntryBase> CRGeneralConfigs = new();

    public static CRConfig CreateEnabledEntry(
        ConfigFile configFile,
        string keyName,
        string settingName,
        string settingDesc,
        bool defaultValue,
        string description)
    {
        return new CRConfig
        {
            Enabled = CreateEntry(configFile, keyName, settingName, settingDesc, defaultValue, description)
        };
    }

    public static ConfigEntry<T> CreateEntry<T>(
        ConfigFile configFile,
        string keyName,
        string settingName,
        string settingDesc,
        T defaultValue,
        string description)
    {
        if (string.IsNullOrEmpty(keyName) || string.IsNullOrEmpty(settingName) || string.IsNullOrEmpty(settingDesc))
        {
            throw new ArgumentException("Key name and/or setting name and/or Setting desc cannot be empty");
        }

        string section = $"{keyName} Options".CleanStringForConfig();
        string key = $"{settingName} | {settingDesc}".CleanStringForConfig();
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
        string settingName,
        string settingDesc,
        T DynamicConfigType,
        string Description)
    {
        string key = $"{settingName} | {settingDesc}".CleanStringForConfig();
        var entry = CreateEntry(configFile, keyName, settingName, settingDesc, DynamicConfigType, Description);
        CRGeneralConfigs[key] = entry;
        return entry;
    }

    public static bool GetEnabledConfigResult(string keyName)
    {
        return CRConfigs[keyName].Enabled.Value;
    }

    public static ConfigEntry<T> GetGeneralConfigEntry<T>(string settingName, string settingDesc)
    {
        string key = $"{settingName} | {settingDesc}".CleanStringForConfig();
        return (ConfigEntry<T>)CRGeneralConfigs[key];
    }
}

/*public class InsideHazardConfig
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
}*/