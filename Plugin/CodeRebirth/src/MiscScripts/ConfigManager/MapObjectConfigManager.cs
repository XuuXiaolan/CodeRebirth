using BepInEx.Configuration;
using CodeRebirth.src.Util.Extensions;

namespace CodeRebirth.src.MiscScripts.ConfigManager;
public class MapObjectConfig : CRConfig
{
    public ConfigEntry<string>? InsideCurveSpawnWeights = null;
    public ConfigEntry<bool>? InsideHazard = null;
    public ConfigEntry<string>? OutsideCurveSpawnWeights = null;
    public ConfigEntry<bool>? OutsideHazard = null;
}

public static class MapObjectConfigManager
{
    public static MapObjectConfig CreateMapObjectConfig(
        ConfigFile configFile,
        string mapObjectName,
        string keyName,
        bool isInsideHazard,
        bool createInsideHazardConfig,
        string defaultInsideCurveSpawnWeights,
        bool createInsideCurveSpawnWeightsConfig,
        bool isOutsideHazard,
        bool createOutsideHazardConfig,
        string defaultOutsideCurveSpawnWeights,
        bool createOutsideCurveSpawnWeightsConfig)
    {

        Plugin.ExtendedLogging($"Creating config for {mapObjectName} | {keyName}");
        return new MapObjectConfig
        {
            Enabled = CRConfigManager.CRConfigs[keyName].Enabled,
            InsideHazard = createInsideHazardConfig ? CRConfigManager.CreateEntry(configFile, keyName, $"{mapObjectName}", "Is Inside Hazard", isInsideHazard, $"Whether {mapObjectName} is able to spawn as an Inside Hazard.") : null,
            InsideCurveSpawnWeights = createInsideCurveSpawnWeightsConfig ? CRConfigManager.CreateEntry(configFile, keyName, $"{mapObjectName}", "Inside Curve Spawn Weights", defaultInsideCurveSpawnWeights, $"The Inside MoonName:CurveSpawnWeight for the {mapObjectName}.") : null,
            OutsideHazard = createOutsideHazardConfig ? CRConfigManager.CreateEntry(configFile, keyName, $"{mapObjectName}", "Is Outside Hazard", isOutsideHazard, $"Whether {mapObjectName} is able to spawn as an Outside Hazard.") : null,
            OutsideCurveSpawnWeights = createOutsideCurveSpawnWeightsConfig ? CRConfigManager.CreateEntry(configFile, keyName, $"{mapObjectName}", "Outside Curve Spawn Weights", defaultOutsideCurveSpawnWeights, $"The Outside MoonName:CurveSpawnWeight for the {mapObjectName}.") : null,
        };
    }

    public static void LoadConfigForMapObject(
        ConfigFile configFile,
        string mapObjectName,
        string keyName,
        bool isInsideHazard,
        bool createInsideHazardConfig,
        string defaultInsideCurveSpawnWeights,
        bool createInsideCurveSpawnWeightsConfig,
        bool isOutsideHazard,
        bool createOutsideHazardConfig,
        string defaultOutsideCurveSpawnWeights,
        bool createOutsideCurveSpawnWeightsConfig)
    {
        var config = CreateMapObjectConfig(
            configFile,
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
        CRConfigManager.CRConfigs[$"{keyName} | {mapObjectName}".CleanStringForConfig()] = config; // i.e. `DuckSong | Grape`
    }

    public static MapObjectConfig GetMapObjectConfig(string keyName, string itemName)
    {
        return (MapObjectConfig)CRConfigManager.CRConfigs[$"{keyName} | {itemName}".CleanStringForConfig()];
    }
}
