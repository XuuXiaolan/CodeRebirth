using System;
using System.Collections.Generic;
using CodeRebirth.src.Util.AssetLoading;
using UnityEngine;
using WeatherRegistry;

namespace CodeRebirth.src.Content.Weathers;

[CreateAssetMenu(fileName = "CRWeatherDefinition", menuName = "CodeRebirth/CRWeatherDefinition", order = 1)]
public class CRWeatherDefinition : CRContentDefinition
{
    public Weather Weather;

    public Weather? GetWeatherByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (Weather.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            return Weather;

        return null;
    }
}

public static class CRWeatherDefinitionExtensions
{
    public static CRWeatherDefinition? GetCRWeatherDefinitionWithWeatherName(this List<CRWeatherDefinition> WeatherDefinitions, string weatherName)
    {
        foreach (var entry in WeatherDefinitions)
        {
            if (entry.GetWeatherByName(weatherName) != null)
                return entry;
        }
        return null;
    }
}