using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Content.Items;
using CodeRebirth.src.Content.Maps;
using CodeRebirth.src.Content.Unlockables;
using CodeRebirth.src.Content.Weathers;

namespace CodeRebirth.src.Util;
public class CodeRebirthRegistry
{
    public static List<CRWeatherDefinition> RegisteredCRWeathers = [];
    public static List<CRMapObjectDefinition> RegisteredCRMapObjects = [];
    public static List<CREnemyDefinition> RegisteredCREnemies = [];
    public static List<CRItemDefinition> RegisteredCRItems = [];
    public static List<CRUnlockableDefinition> RegisteredCRUnlockables = [];
}