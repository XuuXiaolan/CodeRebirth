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
        if (string.IsNullOrEmpty(name)) return null;
        if (Weather.Name.ToLowerInvariant().Contains(name.ToLowerInvariant())) return Weather;
        return null;
    }
}