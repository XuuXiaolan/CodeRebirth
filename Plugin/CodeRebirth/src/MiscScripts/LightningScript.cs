using CodeRebirthLib.Utils;
using DigitalRuby.ThunderAndLightning;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class LightningStrikeScript
{
    public static StormyWeather? stormyWeather = null;
    public static void SpawnLightningBolt(Vector3 strikePosition, System.Random lightningRandom, AudioSource lightningSource)
    {
        Vector3 offset = new(lightningRandom.NextFloat(-32, 32), 0f, lightningRandom.NextFloat(-32, 32));
        Vector3 vector = strikePosition + Vector3.up * 160f + offset;

        if (stormyWeather == null)
        {
            stormyWeather = UnityEngine.Object.FindObjectOfType<StormyWeather>(true);
            Plugin.ExtendedLogging("Getting stormy weather for the first time");
        }

        // Plugin.ExtendedLogging($"{vector} -> {strikePosition}");

        LightningBoltPrefabScript localLightningBoltPrefabScript = UnityEngine.Object.Instantiate(stormyWeather.targetedThunder);
        localLightningBoltPrefabScript.enabled = true;

        localLightningBoltPrefabScript.GlowWidthMultiplier = 2.5f;
        localLightningBoltPrefabScript.DurationRange = new RangeOfFloats { Minimum = 0.6f, Maximum = 1.2f };
        localLightningBoltPrefabScript.TrunkWidthRange = new RangeOfFloats { Minimum = 0.6f, Maximum = 1.2f };
        localLightningBoltPrefabScript.Camera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
        localLightningBoltPrefabScript.Source.transform.position = vector;
        localLightningBoltPrefabScript.Destination.transform.position = strikePosition;
        localLightningBoltPrefabScript.AutomaticModeSeconds = 0.2f;
        localLightningBoltPrefabScript.Generations = 8;
        localLightningBoltPrefabScript.CreateLightningBoltsNow();

        lightningSource.transform.position = strikePosition + Vector3.up * 0.5f;
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
        {
            lightningSource.volume = 0f;
        }
        else
        {
            lightningSource.volume = 0.20f;
        }
        stormyWeather.PlayThunderEffects(strikePosition, lightningSource);
    }
}