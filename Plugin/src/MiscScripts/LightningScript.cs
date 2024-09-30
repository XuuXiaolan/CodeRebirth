using CodeRebirth.src.Util.Extensions;
using DigitalRuby.ThunderAndLightning;
using UnityEngine;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace CodeRebirth.src.MiscScripts;
public class LightningStrikeScript
{

    public static void SpawnLightningBolt(Vector3 strikePosition)
    {
        System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
        Vector3 offset = new Vector3(random.NextFloat(-32, 32), 0f, random.NextFloat(-32, 32));
        Vector3 vector = strikePosition + Vector3.up * 160f + offset;

        StormyWeather stormy = UnityEngine.Object.FindObjectOfType<StormyWeather>(true);
        if (stormy == null)
        {
            Plugin.Logger.LogWarning("StormyWeather not found");
            return;
        }

        // Plugin.ExtendedLogging($"{vector} -> {strikePosition}");

        LightningBoltPrefabScript localLightningBoltPrefabScript = Object.Instantiate(stormy.targetedThunder);
        localLightningBoltPrefabScript.enabled = true;

        if (localLightningBoltPrefabScript == null)
        {
            Plugin.Logger.LogWarning("localLightningBoltPrefabScript not found");
            return;
        }

        localLightningBoltPrefabScript.GlowWidthMultiplier = 2.5f;
        localLightningBoltPrefabScript.DurationRange = new RangeOfFloats { Minimum = 0.6f, Maximum = 1.2f };
        localLightningBoltPrefabScript.TrunkWidthRange = new RangeOfFloats { Minimum = 0.6f, Maximum = 1.2f };
        localLightningBoltPrefabScript.Camera = GameNetworkManager.Instance.localPlayerController.gameplayCamera;
        localLightningBoltPrefabScript.Source.transform.position = vector;
        localLightningBoltPrefabScript.Destination.transform.position = strikePosition;
        localLightningBoltPrefabScript.AutomaticModeSeconds = 0.2f;
        localLightningBoltPrefabScript.Generations = 8;
        localLightningBoltPrefabScript.CreateLightningBoltsNow();

        AudioSource audioSource = Object.Instantiate(stormy.targetedStrikeAudio);
        audioSource.transform.position = strikePosition + Vector3.up * 0.5f;
        audioSource.enabled = true;
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
        {
            audioSource.volume = 0f;
        }
        else
        {
            audioSource.volume = 0.20f;
        }
        stormy.PlayThunderEffects(strikePosition, audioSource);
    }
}