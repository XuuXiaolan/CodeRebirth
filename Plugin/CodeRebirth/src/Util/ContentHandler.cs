using System.Collections.Generic;
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
	
    protected void RegisterEnemyWithConfig(string configMoonRarity, EnemyType enemy, TerminalNode terminalNode, TerminalKeyword terminalKeyword, float powerLevel, int spawnCount)
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
        SpawnableMapObjectDef mapObjDef = ScriptableObject.CreateInstance<SpawnableMapObjectDef>();
        mapObjDef.spawnableMapObject = new SpawnableMapObject
        {
            prefabToSpawn = prefab
        };
        MapObjectHandler.hazardPrefabs.Add(prefab);

        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configString);
        foreach (var entry in spawnRateByLevelType)
        {
            MapObjects.RegisterMapObject(mapObjDef, entry.Key, (level) => 
			new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(entry.Value, 0, 1000))));
        }
        foreach (var entry in spawnRateByCustomLevelType)
        {
            MapObjects.RegisterMapObject(mapObjDef, Levels.LevelTypes.None, [entry.Key], (level) => 
			new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, Mathf.Clamp(entry.Value, 0, 1000))));
        }
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
            string[] entryParts = entry.Split(':');

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

    protected int[] ChangeItemValues(string config)
    {
        string[] configParts = config.Split(',');
        foreach (string configPart in configParts)
        {
            configPart.Trim();
        }
        int minWorthInt = -1;
        int maxWorthInt = -1;
        if (configParts.Length == 2)
        {
            Plugin.ExtendedLogging("Changing item worth between " + configParts[0] + " and " + configParts[1]);
            minWorthInt = int.Parse(configParts[0]);
            maxWorthInt = int.Parse(configParts[1]);
        }
        return [minWorthInt, maxWorthInt];
    }
}