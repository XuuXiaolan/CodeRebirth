using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CodeRebirth.src.Content.Maps;
using LethalLib.Extras;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.src.Util;

public class ContentHandler<T> where T: ContentHandler<T>
{
	internal static T Instance { get; private set; } = null!;

	internal ContentHandler()
    {
		Instance = (T)this;
	}
	
    protected void RegisterEnemyWithConfig(string configMoonRarity, EnemyType enemy, TerminalNode? terminalNode, TerminalKeyword? terminalKeyword, float powerLevel, int spawnCount)
    {
        enemy.MaxCount = spawnCount;
        enemy.PowerLevel = powerLevel;
        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
        Enemies.RegisterEnemy(enemy, spawnRateByLevelType, spawnRateByCustomLevelType, terminalNode, terminalKeyword);
    }

    protected void RegisterScrapWithConfig(string configMoonRarity, Item scrap, int itemWorthMin, int itemWorthMax)
    {
        if (itemWorthMax != -1 && itemWorthMin != -1)
        {
            if (itemWorthMax < itemWorthMin)
            {
                itemWorthMax = itemWorthMin;
            }
            scrap.minValue = (int)(itemWorthMin/0.4f);
            scrap.maxValue = (int)(itemWorthMax/0.4f);
        }

        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
        Items.RegisterScrap(scrap, spawnRateByLevelType, spawnRateByCustomLevelType);
    }

    protected void RegisterShopItemWithConfig(bool enabledScrap, Item item, TerminalNode terminalNode, int itemCost, string configMoonRarity, int minWorth, int maxWorth)
    {
        Items.RegisterShopItem(item, null!, null!, terminalNode, itemCost);
        if (enabledScrap)
        {
            RegisterScrapWithConfig(configMoonRarity, item, minWorth, maxWorth);
        }
    }

    protected void RegisterInsideMapObjectWithConfig(GameObject prefab, string configString)
    {
        // Create the map object definition
        SpawnableMapObjectDef mapObjDef = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
        mapObjDef.spawnableMapObject = new SpawnableMapObject
        {
            prefabToSpawn = prefab
        };
        MapObjectHandler.hazardPrefabs.Add(prefab);

        // Parse the configuration string
        (Dictionary<Levels.LevelTypes, string> spawnRateByLevelType, Dictionary<string, string> spawnRateByCustomLevelType) = ConfigParsingWithCurve(configString);

        // Create dictionaries to hold animation curves for each level type
        Dictionary<Levels.LevelTypes, AnimationCurve> curvesByLevelType = new Dictionary<Levels.LevelTypes, AnimationCurve>();
        Dictionary<string, AnimationCurve> curvesByCustomLevelType = new Dictionary<string, AnimationCurve>();

        bool allCurveExists = false;
        AnimationCurve allAnimationCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));

        bool vanillaCurveExists = false;
        AnimationCurve vanillaAnimationCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));

        bool moddedCurveExists = false;
        AnimationCurve moddedAnimationCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));

        // Populate the animation curves
        foreach (var entry in spawnRateByLevelType)
        {
            Plugin.ExtendedLogging($"Registering map object {prefab.name} for level {entry.Key} with curve {entry.Value}");
            curvesByLevelType[entry.Key] = CreateCurveFromString(entry.Value, prefab.name, entry.Key.ToString());
            if (entry.Key.ToString().ToLowerInvariant() == "vanilla")
            {
                Plugin.ExtendedLogging($"Registering via Vanilla curve because this is vanilla");
                vanillaCurveExists = true;
                vanillaAnimationCurve = curvesByLevelType[entry.Key];
            }
            else if (entry.Key.ToString().ToLowerInvariant() == "modded" || entry.Key.ToString().ToLowerInvariant() == "custom")
            {
                Plugin.ExtendedLogging($"Registering via Modded curve because this is modded");
                moddedCurveExists = true;
                moddedAnimationCurve = curvesByLevelType[Levels.LevelTypes.Modded];
            }
        }
        foreach (var entry in spawnRateByCustomLevelType)
        {
            Plugin.ExtendedLogging($"Registering map object {prefab.name} for level {entry.Key} with curve {entry.Value}");
            curvesByCustomLevelType[entry.Key] = CreateCurveFromString(entry.Value, prefab.name, entry.Key);
        }

        // Register the map object with a single lambda function
        MapObjects.RegisterMapObject(
            mapObjDef,
            Levels.LevelTypes.All,
            curvesByCustomLevelType.Keys.ToArray().Select(s => s.ToLowerInvariant()).ToArray(),
            level =>
            {
                if (level == null) return new AnimationCurve([new Keyframe(0,0), new Keyframe(1,0)]);
                Plugin.ExtendedLogging($"Registering map object {prefab.name} for level {level}");
                string actualLevelName = level.ToString().Trim().Substring(0, Math.Max(0, level.ToString().Trim().Length - 23)).Trim().ToLowerInvariant();
                Levels.LevelTypes levelType = LevelToLevelType(actualLevelName);
                Plugin.ExtendedLogging($"Output Level type: {levelType}");
                Plugin.ExtendedLogging($"is Level Vanilla AND Contained in Curves: {curvesByLevelType.Keys.Contains(levelType)}");
                bool isVanilla = false;
                if (levelType != Levels.LevelTypes.None && levelType != Levels.LevelTypes.Modded)
                {
                    Plugin.ExtendedLogging($"is Level Vanilla: true");
                    isVanilla = true;
                }
                if (curvesByLevelType.TryGetValue(levelType, out AnimationCurve curve))
                {
                    Plugin.ExtendedLogging($"Managed to find curve for level: {actualLevelName} | with given curve: ");
                    /*foreach (Keyframe keyframe in curve.keys)
                    {
                        Plugin.ExtendedLogging($"({keyframe.time}, {keyframe.value})");
                    }*/
                    return curve;
                }
                else if (isVanilla && vanillaCurveExists)
                {
                    Plugin.ExtendedLogging("Managed to find curve for level: Vanilla | with given curve: ");
                    /*foreach (Keyframe keyframe in vanillaAnimationCurve.keys)
                    {
                        Plugin.ExtendedLogging($"({keyframe.time}, {keyframe.value})");
                    }*/
                    return vanillaAnimationCurve;
                }
                else if (curvesByCustomLevelType.TryGetValue(actualLevelName, out curve))
                {
                    Plugin.ExtendedLogging($"Managed to find curve for level: {actualLevelName} | with given curve: ");
                    /*foreach (Keyframe keyframe in curve.keys)
                    {
                        Plugin.ExtendedLogging($"({keyframe.time}, {keyframe.value})");
                    }*/
                    return curve;
                }
                else if (moddedCurveExists)
                {
                    Plugin.ExtendedLogging("Managed to find curve for level: Modded | with given curve: ");
                    /*foreach (Keyframe keyframe in moddedAnimationCurve.keys)
                    {
                        Plugin.ExtendedLogging($"({keyframe.time}, {keyframe.value})");
                    }*/
                    return moddedAnimationCurve;
                }
                else if (allCurveExists)
                {
                    Plugin.ExtendedLogging($"Managed to find curve for level: {actualLevelName} with type ALL | with given curve: ");
                    /*foreach (Keyframe keyframe in allAnimationCurve.keys)
                    {
                        Plugin.ExtendedLogging($"({keyframe.time}, {keyframe.value})");
                    }*/
                    return allAnimationCurve;
                }
                Plugin.ExtendedLogging($"Failed to find curve for level: {level}");
                return new AnimationCurve([new Keyframe(0,0), new Keyframe(1,0)]); // Default case if no curve matches
            });
    }

    protected Levels.LevelTypes LevelToLevelType(string levelName)
    {
        Plugin.ExtendedLogging($"Cutup Level type: {levelName}");
        return levelName switch
        {
            "experimentation" => Levels.LevelTypes.ExperimentationLevel,
            "assurance" => Levels.LevelTypes.AssuranceLevel,
            "offense" => Levels.LevelTypes.OffenseLevel,
            "march" => Levels.LevelTypes.MarchLevel,
            "vow" => Levels.LevelTypes.VowLevel,
            "dine" => Levels.LevelTypes.DineLevel,
            "rend" => Levels.LevelTypes.RendLevel,
            "titan" => Levels.LevelTypes.TitanLevel,
            "artifice" => Levels.LevelTypes.ArtificeLevel,
            "adamance" => Levels.LevelTypes.AdamanceLevel,
            "embrion" => Levels.LevelTypes.EmbrionLevel,
            "vanilla" => Levels.LevelTypes.Vanilla,
            "modded" => Levels.LevelTypes.Modded,
            _ => Levels.LevelTypes.None,
        };
    }

    protected string[] MapObjectConfigParsing(string configString)
    {
        var levelTypesList = new List<string>();

        foreach (string entry in configString.Split(',').Select(s => s.Trim()))
        {
            string name = entry;
            if (System.Enum.TryParse(name, true, out Levels.LevelTypes levelType))
            {
                levelTypesList.Add(name);
            }
            else
            {
                // Try appending "Level" to the name and re-attempt parsing
                string modifiedName = name + "Level";
                if (System.Enum.TryParse(modifiedName, true, out levelType))
                {
                    levelTypesList.Add(modifiedName);
                }
                else
                {
                    levelTypesList.Add(name);
                }
            }
        }

        return levelTypesList.ToArray();
    }

    protected (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) ConfigParsing(string configMoonRarity)
    {
        Dictionary<Levels.LevelTypes, int> spawnRateByLevelType = new();
        Dictionary<string, int> spawnRateByCustomLevelType = new();
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

            if (System.Enum.TryParse(name, true, out Levels.LevelTypes levelType))
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
        Plugin.ExtendedLogging($"Creating curve for {nameOfThing} on moon {MoonName} with key-value pairs: {keyValuePairs}");
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
                return new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
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
                Plugin.ExtendedLogging($"Adding keyframe for {nameOfThing} at time {time} with value {value}");
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