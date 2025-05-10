using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

namespace CodeRebirth.src.Util;
public class TrackWeatherChanges : MonoBehaviour
{
    private List<SelectableLevel> _levels = new();
    private List<string> _weatherNames = new();
    private Dictionary<EnemyType, int> _enemyTypesEdited = new();
    private int _numberOfLevels = 0;

    public void Start()
    {
        foreach (var level in StartOfRound.Instance.levels)
        {
            _numberOfLevels++;
            string weatherName = level.currentWeather.ToString();

            _levels.Add(level);
            _weatherNames.Add(weatherName);
            Plugin.ExtendedLogging($"Added level {level.name} with weather {weatherName}");

            foreach (var spawnableEnemyWithRarity in level.Enemies)
            {
                Plugin.ExtendedLogging($"By default, {spawnableEnemyWithRarity.enemyType.enemyName} has a weight of {spawnableEnemyWithRarity.rarity} on {level.name}");
                bool success = EditEnemyRarityValues(spawnableEnemyWithRarity, weatherName, out int oldRarity, out int newRarity);
                Plugin.ExtendedLogging($"EditEnemyRarityValues returned {success} for {spawnableEnemyWithRarity.enemyType.enemyName} on {level.name} with old rarity {oldRarity} and new rarity {newRarity}");
            }

            foreach (var spawnableEnemyWithRarity in level.OutsideEnemies)
            {
                Plugin.ExtendedLogging($"By default, {spawnableEnemyWithRarity.enemyType.enemyName} has a weight of {spawnableEnemyWithRarity.rarity} on {level.name}");
                bool success = EditEnemyRarityValues(spawnableEnemyWithRarity, weatherName, out int oldRarity, out int newRarity);
                Plugin.ExtendedLogging($"EditEnemyRarityValues returned {success} for {spawnableEnemyWithRarity.enemyType.enemyName} on {level.name} with old rarity {oldRarity} and new rarity {newRarity}");
            }

            foreach (var spawnableEnemyWithRarity in level.DaytimeEnemies)
            {
                Plugin.ExtendedLogging($"By default, {spawnableEnemyWithRarity.enemyType.enemyName} has a weight of {spawnableEnemyWithRarity.rarity} on {level.name}");
                bool success = EditEnemyRarityValues(spawnableEnemyWithRarity, weatherName, out int oldRarity, out int newRarity);
                Plugin.ExtendedLogging($"EditEnemyRarityValues returned {success} for {spawnableEnemyWithRarity.enemyType.enemyName} on {level.name} with old rarity {oldRarity} and new rarity {newRarity}");
            }
            // invoke an event detailing what previous weather was, what the current weather is, and what level it's on so that all enemies can take that info and change accordingly
        }
    }

    public void Update()
    {
        for (int i = 0; i < _numberOfLevels - 1; i++)
        {
            SelectableLevel level = _levels[i];
            string currentWeatherName = level.currentWeather.ToString();
            if (currentWeatherName == _weatherNames[i])
                continue;

            // invoke an event detailing what previous weather was, what the current weather is, and what level it's on so that all enemies can take that info and change accordingly
            string previousWeatherName = _weatherNames[i];
            _weatherNames[i] = currentWeatherName;
            Plugin.ExtendedLogging($"Updated level {level.name} with weather {_weatherNames[i]} from previous weather {previousWeatherName}");
            foreach (var spawnableEnemyWithRarity in level.Enemies)
            {
                Plugin.ExtendedLogging($"By default, {spawnableEnemyWithRarity.enemyType.enemyName} has a weight of {spawnableEnemyWithRarity.rarity} on {level.name}");
                bool success = EditEnemyRarityValues(spawnableEnemyWithRarity, currentWeatherName, out int oldRarity, out int newRarity);
                Plugin.ExtendedLogging($"EditEnemyRarityValues returned {success} for {spawnableEnemyWithRarity.enemyType.enemyName} on {level.name} with old rarity {oldRarity} and new rarity {newRarity}");
            }

            foreach (var spawnableEnemyWithRarity in level.OutsideEnemies)
            {
                Plugin.ExtendedLogging($"By default, {spawnableEnemyWithRarity.enemyType.enemyName} has a weight of {spawnableEnemyWithRarity.rarity} on {level.name}");
                bool success = EditEnemyRarityValues(spawnableEnemyWithRarity, currentWeatherName, out int oldRarity, out int newRarity);
                Plugin.ExtendedLogging($"EditEnemyRarityValues returned {success} for {spawnableEnemyWithRarity.enemyType.enemyName} on {level.name} with old rarity {oldRarity} and new rarity {newRarity}");
            }

            foreach (var spawnableEnemyWithRarity in level.DaytimeEnemies)
            {
                Plugin.ExtendedLogging($"By default, {spawnableEnemyWithRarity.enemyType.enemyName} has a weight of {spawnableEnemyWithRarity.rarity} on {level.name}");
                bool success = EditEnemyRarityValues(spawnableEnemyWithRarity, currentWeatherName, out int oldRarity, out int newRarity);
                Plugin.ExtendedLogging($"EditEnemyRarityValues returned {success} for {spawnableEnemyWithRarity.enemyType.enemyName} on {level.name} with old rarity {oldRarity} and new rarity {newRarity}");
            }
        }
    }

    private bool EditEnemyRarityValues(SpawnableEnemyWithRarity spawnableEnemyWithRarity, string weatherName, out int oldRarity, out int newRarity)
    {
        oldRarity = spawnableEnemyWithRarity.rarity;
        newRarity = spawnableEnemyWithRarity.rarity;
        CREnemyDefinition? CREnemyDefinition = CodeRebirthRegistry.RegisteredCREnemies.GetCREnemyDefinitionWithEnemyName(spawnableEnemyWithRarity.enemyType.enemyName);
        if (CREnemyDefinition == null)
            return false;

        if (!_enemyTypesEdited.ContainsKey(spawnableEnemyWithRarity.enemyType))
        {
            _enemyTypesEdited.Add(spawnableEnemyWithRarity.enemyType, oldRarity);
            return true;
        }

        if (CREnemyDefinition.WeatherMultipliers.TryGetValue(weatherName, out float multiplier))
        {
            oldRarity = _enemyTypesEdited[spawnableEnemyWithRarity.enemyType];
            newRarity = Mathf.FloorToInt(oldRarity * multiplier + 0.5f);
            spawnableEnemyWithRarity.rarity = newRarity;
            return true;
        }
        else
        {
            multiplier = 1f;
            oldRarity = _enemyTypesEdited[spawnableEnemyWithRarity.enemyType];
            newRarity = Mathf.FloorToInt(oldRarity * multiplier + 0.5f);
            spawnableEnemyWithRarity.rarity = newRarity;
            return true;
        }
    }
}