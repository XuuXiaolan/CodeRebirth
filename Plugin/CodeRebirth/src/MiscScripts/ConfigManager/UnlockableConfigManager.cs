using BepInEx.Configuration;
using CodeRebirth.src.Util.Extensions;

namespace CodeRebirth.src.MiscScripts.ConfigManager;
public class UnlockableConfig : CRConfig
{
    public ConfigEntry<int> Cost;
    public ConfigEntry<bool> IsShipUpgrade;
    public ConfigEntry<bool> IsDecor;
    public ConfigEntry<bool>? IsProgressive = null;
}

public static class UnlockableConfigManager
{
    public static UnlockableConfig CreateUnlockableConfig(
        ConfigFile configFile,
        string unlockableName,
        string keyName,
        int cost,
        bool isShipUpgrade,
        bool isDecor,
        bool isProgressive,
        bool createIsProgressiveConfig)
    {
        return new UnlockableConfig
        {
            Enabled = CRConfigManager.CRConfigs[keyName].Enabled,
            Cost = CRConfigManager.CreateEntry(configFile, keyName, $"{unlockableName}", "Cost", cost, $"Cost for {unlockableName} in the shop."),
            IsShipUpgrade = CRConfigManager.CreateEntry(configFile, keyName, $"{unlockableName}", "Is Ship Upgrade", isShipUpgrade, $"Whether {unlockableName} is considered a ship upgrade."),
            IsDecor = CRConfigManager.CreateEntry(configFile, keyName, $"{unlockableName}", "Is Decor", isDecor, $"Whether {unlockableName} is considered a decor."),
            IsProgressive = createIsProgressiveConfig ? CRConfigManager.CreateEntry(configFile, keyName, $"{unlockableName}", $"Is Progressive", isProgressive, $"Whether {unlockableName} is considered a progressive purchase.") : null
        };
    }

    public static void LoadConfigForUnlockable(
        ConfigFile configFile,
        string unlockableName,
        string keyName,
        int cost,
        bool isShipUpgrade,
        bool isDecor,
        bool isProgressive,
        bool createIsProgressiveConfig)
    {
        var config = CreateUnlockableConfig(
            configFile,
            unlockableName,
            keyName,
            cost,
            isShipUpgrade,
            isDecor,
            isProgressive,
            createIsProgressiveConfig
        );
        CRConfigManager.CRConfigs[$"{keyName} | {unlockableName}".CleanStringForConfig()] = config;
    }

    public static UnlockableConfig GetUnlockableConfig(string keyName, string unlockableName)
    {
        return (UnlockableConfig)CRConfigManager.CRConfigs[$"{keyName} | {unlockableName}".CleanStringForConfig()]; // i.e. `DuckSong | Duck`
    }
}