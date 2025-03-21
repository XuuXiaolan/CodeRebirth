using BepInEx.Configuration;
using CodeRebirth.src.Util.Extensions;

namespace CodeRebirth.src.MiscScripts.ConfigManager;
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
    public static ItemConfig CreateItemConfig(
        ConfigFile configFile,
        string itemName,
        string keyName,
        string defaultSpawnWeights,
        bool createSpawnWeightsConfig,
        bool isScrapItem,
        bool createIsScrapItemConfig,
        bool isShopItem,
        bool createIsShopItemConfig,
        int cost)
    {

        Plugin.ExtendedLogging($"Creating config for {itemName} | {keyName} with SpawnWeights: {createSpawnWeightsConfig}, IsScrapItem: {createIsScrapItemConfig}, IsShopItem: {createIsShopItemConfig}");
        return new ItemConfig
        {
            Enabled = CRConfigManager.CRConfigs[keyName].Enabled,
            SpawnWeights = createSpawnWeightsConfig ? CRConfigManager.CreateEntry(configFile, keyName, $"{itemName}", "Spawn Weights", defaultSpawnWeights, $"MoonName:Rarity Spawn weights for {itemName}.") : null,
            IsScrapItem = createIsScrapItemConfig ? CRConfigManager.CreateEntry(configFile, keyName, $"{itemName}", "Is Scrap Item", isScrapItem, $"Whether {itemName} is a scrap item.") : null,
            Value = isScrapItem ? CRConfigManager.CreateEntry(configFile, keyName, $"{itemName}", "Value", "-1,-1", $"how much {itemName} is worth when spawning, formatted as min,max where -1,-1 is the default.") : null,
            IsShopItem = createIsShopItemConfig ? CRConfigManager.CreateEntry(configFile, keyName, $"{itemName}", "Is Shop Item", isShopItem, $"Whether {itemName} is a shop item.") : null,
            Cost = isShopItem ? CRConfigManager.CreateEntry(configFile, keyName, $"{itemName}", "Cost", cost, $"Cost for {itemName} in the shop.") : null
        };
    }

    public static void LoadConfigForItem(
        ConfigFile configFile,
        string itemName,
        string keyName,
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
            keyName,
            defaultSpawnWeight,
            createSpawnWeightsConfig,
            isScrapItem,
            createIsScrapItemConfig,
            isShopItem,
            createIsShopItemConfig,
            cost
        );
        CRConfigManager.CRConfigs[$"{keyName} | {itemName}".CleanStringForConfig()] = config; // i.e. `DuckSong | Grape`
    }

    public static ItemConfig GetItemConfig(string keyName, string itemName)
    {
        return (ItemConfig)CRConfigManager.CRConfigs[$"{keyName} | {itemName}".CleanStringForConfig()];
    }
}
