using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class OxydeLightsManager : MonoBehaviour
{
    [SerializeField]
    private Color[] _colors = [];

    private Light[] _lights = [];
    private int _currentLightIndex = 0;

    internal static OxydeLightsManager oxydeLightsManager = null!;
    private void Start()
    {
        oxydeLightsManager = this;
    }

    public void IncrementLights()
    {
        if (_currentLightIndex >= _colors.Length - 1)
            return;

        _currentLightIndex++;
        foreach (var light in _lights)
            light.color = _colors[_currentLightIndex];
    }
}