using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace CodeRebirth.src.MiscScripts;
public class WeatherController : MonoBehaviour
{
    public Renderer[] renderersToDisableEmissiveness = null!;
    public Cubemap cubemapReplacement = null!;
    public VolumeProfile volumeProfile = null!;
    public Light[] lightsToDeactivate = null!;
    public LocalVolumetricFog localVolumetricFog = null!;
    public Vector3 fogPlacement = Vector3.zero;
    public GameObject[] gameObjectsToActivate = null!;

    public void Start()
    {
        string weatherName = WeatherRegistry.WeatherManager.GetCurrentLevelWeather().name.ToLowerInvariant();
        Plugin.ExtendedLogging($"Weather: {weatherName}");
        if (weatherName == "blackout" || weatherName == "night shift")
        {
            HandleDarkness();
        }
    }

    private void HandleDarkness()
    {
        foreach (GameObject gameObject in gameObjectsToActivate)
        {
            gameObject.SetActive(true);
        }

        foreach (Light light in lightsToDeactivate)
        {
            light.intensity = 0;
        }
        localVolumetricFog.transform.localPosition = fogPlacement;

        Material material = renderersToDisableEmissiveness[0].GetSharedMaterial();
        material.SetColor("_EmissiveColor", Color.white);

        if (volumeProfile.TryGet(out HDRISky hdriSky))
        {
            hdriSky.hdriSky.overrideState = true;
            hdriSky.hdriSky.value = cubemapReplacement;
            hdriSky.exposure.overrideState = true;
            hdriSky.exposure.value = 2f;
        }
    }
}