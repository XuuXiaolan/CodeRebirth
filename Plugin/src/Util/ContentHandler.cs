using System.Collections.Generic;
using System.Linq;
using LethalLib.Modules;

namespace CodeRebirth.Util;

public class ContentHandler<T> where T: ContentHandler<T> {
	internal static T Instance { get; private set; }

	internal ContentHandler() {
		Instance = (T)this;
	}
	
    protected void RegisterEnemyWithConfig(bool enabled, string configMoonRarity, EnemyType enemy, TerminalNode terminalNode, TerminalKeyword terminalKeyword, float powerLevel, int spawnCount) {
        enemy.MaxCount = spawnCount;
        enemy.PowerLevel = powerLevel;
        if (enabled) { 
            (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
            Enemies.RegisterEnemy(enemy, spawnRateByLevelType, spawnRateByCustomLevelType, terminalNode, terminalKeyword);
        } else {
            Enemies.RegisterEnemy(enemy, 0, Levels.LevelTypes.All, terminalNode, terminalKeyword);
        }
    }
    protected void RegisterScrapWithConfig(bool enabled, string configMoonRarity, Item scrap) {
        if (enabled) { 
            (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigParsing(configMoonRarity);
            Items.RegisterScrap(scrap, spawnRateByLevelType, spawnRateByCustomLevelType);
        } else {
            Items.RegisterScrap(scrap, 0, Levels.LevelTypes.All);
        }
    }
    protected void RegisterShopItemWithConfig(bool enabledShopItem, bool enabledScrap, Item item, TerminalNode terminalNode, int itemCost, string configMoonRarity) {
        if (enabledShopItem) { 
            Items.RegisterShopItem(item, null, null, terminalNode, itemCost);
        }
        if (enabledScrap) {
            RegisterScrapWithConfig(true, configMoonRarity, item);
        }
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
                spawnRateByCustomLevelType[name] = spawnrate;
            }
        }
        return (spawnRateByLevelType, spawnRateByCustomLevelType);
    }
}