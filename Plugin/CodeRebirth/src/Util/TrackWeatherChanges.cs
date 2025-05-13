using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

namespace CodeRebirth.src.Util;
public class TrackWeatherChanges : MonoBehaviour
{
    private readonly Dictionary<SelectableLevel, Dictionary<EnemyType, int>> _enemyTypesEdited = new Dictionary<SelectableLevel, Dictionary<EnemyType, int>>();

    private readonly List<SelectableLevel> _levels = new();
    private readonly List<string> _weatherNames = new();
    private int _numberOfLevels = 0;

    public void Start()
    {
        foreach (var level in StartOfRound.Instance.levels)
        {
            _levels.Add(level);
            string weatherName = level.currentWeather.ToString();
            _weatherNames.Add(weatherName);
            // Plugin.ExtendedLogging($"Added level {level.name} with weather {weatherName}");

            // Ensure we have a per-level dictionary
            _enemyTypesEdited[level] = new Dictionary<EnemyType, int>();

            // Process all enemy lists in one go
            ProcessEnemyList(level, level.Enemies, weatherName);
            ProcessEnemyList(level, level.OutsideEnemies, weatherName);
            ProcessEnemyList(level, level.DaytimeEnemies, weatherName);

            _numberOfLevels++;
        }
    }

    public void Update()
    {
        for (int i = 0; i < _numberOfLevels; i++)
        {
            var level = _levels[i];
            var currentWeatherName = level.currentWeather.ToString();
            if (currentWeatherName == _weatherNames[i])
                continue;

            var previousWeatherName = _weatherNames[i];
            _weatherNames[i] = currentWeatherName;
            // Plugin.ExtendedLogging($"Updated level {level.name} weather from {previousWeatherName} to {currentWeatherName}");

            ProcessEnemyList(level, level.Enemies, currentWeatherName);
            ProcessEnemyList(level, level.OutsideEnemies, currentWeatherName);
            ProcessEnemyList(level, level.DaytimeEnemies, currentWeatherName);
        }
    }

    private void ProcessEnemyList(SelectableLevel level, IEnumerable<SpawnableEnemyWithRarity> spawnableEnemyWithRarities, string weatherName)
    {
        foreach (SpawnableEnemyWithRarity spawnable in spawnableEnemyWithRarities)
        {
            // Plugin.ExtendedLogging($"({level.name}) default {spawnable.enemyType.enemyName} weight = {spawnable.rarity}");
            bool success = EditEnemyRarityValues(level, spawnable, weatherName, out int oldR, out int newR);
            // Plugin.ExtendedLogging($"EditEnemyRarityValues returned {success} for {spawnable.enemyType.enemyName} on {level.name}: old={oldR}, new={newR}");
        }
    }

    private bool EditEnemyRarityValues(SelectableLevel level, SpawnableEnemyWithRarity spawnable, string weatherName, out int oldRarity, out int newRarity)
    {
        oldRarity = spawnable.rarity;
        newRarity = oldRarity;

        var enemyDef = CodeRebirthRegistry.RegisteredCREnemies.GetCREnemyDefinitionWithEnemyName(spawnable.enemyType.enemyName);
        if (enemyDef == null)
            return false;

        Dictionary<EnemyType, int> levelDict = _enemyTypesEdited[level];
        if (!levelDict.TryGetValue(spawnable.enemyType, out var baseRarity))
        {
            baseRarity = oldRarity;
            levelDict[spawnable.enemyType] = baseRarity;
            Plugin.ExtendedLogging($"Recorded base rarity {baseRarity} for {spawnable.enemyType.enemyName} in level {level.name}");
        }

        enemyDef.WeatherMultipliers.TryGetValue(weatherName, out float multiplier);
        oldRarity = baseRarity;
        Plugin.ExtendedLogging($"Multiplier for enemy {spawnable.enemyType.enemyName} in level {level.name} with weather {weatherName} is {multiplier}");
        newRarity = Mathf.FloorToInt(oldRarity * multiplier + 0.5f);

        spawnable.rarity = newRarity;
        return true;
    }
}
