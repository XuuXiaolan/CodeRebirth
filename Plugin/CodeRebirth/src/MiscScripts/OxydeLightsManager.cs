using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class OxydeLightsManager : MonoBehaviour
{
    [SerializeField]
    private Color[] _colors = [];
    [SerializeField]
    private Renderer[] _renderers = [];

    private Light[] _lights = [];
    private int _currentLightIndex = 0;

    internal static OxydeLightsManager oxydeLightsManager = null!;
    private void Start()
    {
        oxydeLightsManager = this;
        IncrementLights();
    }

    public void IncrementLights()
    {
        if (_currentLightIndex >= _colors.Length - 1 || _colors.Length == 0)
            return;

        foreach (var light in _lights)
            light.color = _colors[_currentLightIndex];

        foreach (var renderer in _renderers)
        {
            foreach (var sharedMaterial in renderer.sharedMaterials)
            {
                sharedMaterial.color = _colors[_currentLightIndex];
            }
        }
        _currentLightIndex++;
    }
}