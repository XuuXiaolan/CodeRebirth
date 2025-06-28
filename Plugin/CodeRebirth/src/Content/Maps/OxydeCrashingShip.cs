using System;
using System.Collections;
using System.Linq;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util.Extensions;
using LethalLevelLoader;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class OxydeCrashingShip : FallingObjectBehaviour
{
    [Header("Audio")]
    [SerializeField]
    private AudioSource _ImpactAudio = null!;
    [SerializeField]
    private AudioSource _NormalTravelAudio = null!;
    [SerializeField]
    private AudioSource _CloseTravelAudio = null!;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        LevelManager.TryGetExtendedLevel(StartOfRound.Instance.levels.Where(x => x.sceneName == "Oxyde").FirstOrDefault(), out ExtendedLevel? extendedLevel);
        if (extendedLevel == null || (!extendedLevel.IsRouteHidden && !extendedLevel.IsRouteLocked))
        {
            if (IsServer)
            {
                this.NetworkObject.Despawn(true);
            }
            return;
        }
        Vector3 spawnPosition = this.transform.position;
        this.transform.position = new Vector3(9999, 9999, 9999);
        if (!IsServer)
            return;

        StartCoroutine(WaitToDoYourThing(spawnPosition));
    }

    public IEnumerator WaitToDoYourThing(Vector3 spawnPosition)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(180f, 360f));
        Vector3 origin = CalculateRandomSkyOrigin((Direction)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Direction)).Length), spawnPosition, new System.Random(UnityEngine.Random.Range(0, 10000)));
        SetupFallingObjectServerRpc(origin, spawnPosition, 25);
    }

    protected override void OnImpact()
    {
        base.OnImpact();
        Impact(); // Start the impact effects
    }

    protected override void OnSetup()
    {
        base.OnSetup();
        StartCoroutine(UpdateAudio()); // Make sure audio works correctly on the first frame.
    }

    public void Start()
    {
        _NormalTravelAudio.Play();
    }

    private IEnumerator UpdateAudio()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f);
            if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
            {
                _NormalTravelAudio.volume = 0;
                _CloseTravelAudio.volume = 0;
                _ImpactAudio.volume = 0.05f;
            }
            else
            {
                _NormalTravelAudio.volume = Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _CloseTravelAudio.volume = Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _ImpactAudio.volume = Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
            }
            if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed)
            {
                _NormalTravelAudio.volume = Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _CloseTravelAudio.volume = Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _ImpactAudio.volume = Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
            }
            if (((1 - Progress) * _travelTime) <= _CloseTravelAudio.clip.length && !_CloseTravelAudio.isPlaying)
            {
                _NormalTravelAudio.volume = 0.5f * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                _CloseTravelAudio.Play();
            }
        }
    }

    private void Impact()
    {
        _ImpactAudio.Play();
        HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        CRUtilities.CreateExplosion(transform.position, true, 50, 0, 25, 4, null, null, 100f);
    }
}