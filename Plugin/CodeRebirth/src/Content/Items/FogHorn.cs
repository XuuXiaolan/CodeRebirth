using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class FogHorn : GrabbableObject
{
    public AudioSource audioSource = null!;
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        audioSource.Play();
        WeatherRegistry.WeatherController.ChangeCurrentWeather(LevelWeatherType.None);
    }
}