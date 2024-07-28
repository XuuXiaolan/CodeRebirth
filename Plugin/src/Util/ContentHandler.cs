using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LethalLib.Extras;
using LethalLib.Modules;
using UnityEngine;

namespace CodeRebirth.Util;

public class ContentHandler<T> where T: ContentHandler<T> {
	internal static T Instance { get; private set; } = null!;

	internal ContentHandler() {
		Instance = (T)this;
	}
	
    protected void RegisterEnemyWithConfig(string configMoonRarity, EnemyType enemy, TerminalNode terminalNode, TerminalKeyword terminalKeyword, float powerLevel, int spawnCount) {
        enemy.MaxCount = spawnCount;
        enemy.PowerLevel = powerLevel;
        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
        Enemies.RegisterEnemy(enemy, spawnRateByLevelType, spawnRateByCustomLevelType, terminalNode, terminalKeyword);
    }
    protected void RegisterScrapWithConfig(string configMoonRarity, Item scrap) {
        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
        Items.RegisterScrap(scrap, spawnRateByLevelType, spawnRateByCustomLevelType);
    }
    protected void RegisterShopItemWithConfig(bool enabledScrap, Item item, TerminalNode terminalNode, int itemCost, string configMoonRarity) {
        Items.RegisterShopItem(item, null!, null!, terminalNode, itemCost);
        if (enabledScrap) {
            RegisterScrapWithConfig(configMoonRarity, item);
        }
    }

    
	protected void RegisterOutsideObjectWithConfig(string[] tags, int width, GameObject prefab, AnimationCurve curve, string configString) {
		SpawnableOutsideObjectDef outsideMapObjectDef = ScriptableObject.CreateInstance<SpawnableOutsideObjectDef>();
		outsideMapObjectDef.spawnableMapObject = new SpawnableOutsideObjectWithRarity
		{
			spawnableObject = ScriptableObject.CreateInstance<SpawnableOutsideObject>(),
		};
		outsideMapObjectDef.spawnableMapObject.spawnableObject.spawnableFloorTags = tags;
		outsideMapObjectDef.spawnableMapObject.spawnableObject.objectWidth = width;
		outsideMapObjectDef.spawnableMapObject.spawnableObject.prefabToSpawn = prefab;
		(Levels.LevelTypes[] vanillaLevelSpawnType, string[] CustomLevelType) = MapObjectConfigParsing(configString);

        Levels.LevelTypes levelTypes = 0;
        foreach (Levels.LevelTypes levelType in vanillaLevelSpawnType) {
            levelTypes |= levelType;
        }
        MapObjects.RegisterOutsideObject(outsideMapObjectDef, levelTypes, CustomLevelType, (level) => curve);
	}

    protected (Levels.LevelTypes[] vanillaLevelSpawnType, string[] customLevelType) MapObjectConfigParsing(string configString) {
        var spawnRateByLevelType = new List<Levels.LevelTypes>();
        var spawnRateByCustomLevelType = new List<string>();

        foreach (string entry in configString.Split(',').Select(s => s.Trim())) {
            string name = entry;
            if (System.Enum.TryParse(name, true, out Levels.LevelTypes levelType)) {
                spawnRateByLevelType.Add(levelType);
            } else {
                // Try appending "Level" to the name and re-attempt parsing
                string modifiedName = name + "Level";
                if (System.Enum.TryParse(modifiedName, true, out levelType)) {
                    spawnRateByLevelType.Add(levelType);
                } else {
                    spawnRateByCustomLevelType.Add(name);
                }
            }
        }
        return (spawnRateByLevelType.ToArray(), spawnRateByCustomLevelType.ToArray());
    }

    protected (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) ConfigParsing(string configMoonRarity) {
        Dictionary<Levels.LevelTypes, int> spawnRateByLevelType = new Dictionary<Levels.LevelTypes, int>();
        Dictionary<string, int> spawnRateByCustomLevelType = new Dictionary<string, int>();
        foreach (string entry in configMoonRarity.Split(',').Select(s => s.Trim())) {
            string[] entryParts = entry.Split(':');

            if (entryParts.Length != 2) continue;

            string name = entryParts[0];
            int spawnrate;

            if (!int.TryParse(entryParts[1], out spawnrate)) continue;

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
}