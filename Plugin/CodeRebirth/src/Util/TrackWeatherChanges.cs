using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Util;
public class TrackWeatherChanges : MonoBehaviour
{
    private List<SelectableLevel> _levels = new();
    private List<string> _weatherNames = new();
    private int _numberOfLevels = 0;

    public void Start()
    {
        foreach (var level in StartOfRound.Instance.levels)
        {
            _numberOfLevels++;
            _levels.Add(level);
            _weatherNames.Add(WeatherRegistry.WeatherManager.GetCurrentWeatherName(level));
            Plugin.ExtendedLogging($"Added level {level.name} with weather {_weatherNames[_levels.Count - 1]}");
            foreach (var spawnableEnemyWithRarity in level.Enemies)
            {
                Plugin.ExtendedLogging($"By default, {spawnableEnemyWithRarity.enemyType.enemyName} has a weight of {spawnableEnemyWithRarity.rarity} on {level.name}");
            }
            // invoke an event detailing what previous weather was, what the current weather is, and what level it's on so that all enemies can take that info and change accordingly
        }
    }

    public void Update()
    {
        for (int i = 0; i < _numberOfLevels - 1; i++)
        {
            SelectableLevel level = _levels[i];
            if (WeatherRegistry.WeatherManager.GetCurrentWeatherName(level) == _weatherNames[i])
                continue;

            // invoke an event detailing what previous weather was, what the current weather is, and what level it's on so that all enemies can take that info and change accordingly
            string previousWeatherName = _weatherNames[i];
            _weatherNames[i] = WeatherRegistry.WeatherManager.GetCurrentWeatherName(level);
            Plugin.ExtendedLogging($"Updated level {level.name} with weather {_weatherNames[i]} from previous weather {previousWeatherName}");
        }
    }
}