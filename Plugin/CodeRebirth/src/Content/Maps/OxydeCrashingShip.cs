using System;
using System.Collections;
using CodeRebirth.src.MiscScripts;
using Dawn;
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

    private Coroutine? _fallingRoutine = null!;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // have an LLL soft dependency
        if (!LethalContent.Moons.TryGetValue(NamespacedKey.From("code_rebirth", "oxyde"), out DawnMoonInfo moonInfo) || moonInfo.PurchasePredicate == ITerminalPurchasePredicate.AlwaysSuccess())
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

        _fallingRoutine = StartCoroutine(WaitToDoYourThing(spawnPosition, UnityEngine.Random.Range(180f, 360f)));
    }

    public IEnumerator WaitToDoYourThing(Vector3 spawnPosition, float delay)
    {
        yield return new WaitForSeconds(delay);
        Vector3 origin = CalculateRandomSkyOrigin((Direction)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Direction)).Length), spawnPosition, new System.Random(UnityEngine.Random.Range(0, 10000)));
        SetupFallingObjectServerRpc(origin, spawnPosition, 25);
    }

    public void Fall(Vector3 fallPosition, float speed)
    {
        if (_fallingRoutine != null)
            StopCoroutine(_fallingRoutine);

        Vector3 origin = CalculateRandomSkyOrigin((Direction)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Direction)).Length), fallPosition, new System.Random(UnityEngine.Random.Range(0, 10000)));

        SetupFallingObjectServerRpc(origin, fallPosition, speed);
    }

    protected override void OnImpact()
    {
        base.OnImpact();
        Impact(); // Start the impact effects
    }

    protected override void OnSetup()
    {
        base.OnSetup();
        _NormalTravelAudio.Play();
        StartCoroutine(UpdateAudio()); // Make sure audio works correctly on the first frame.
    }

    public void Start()
    {
    }

    private IEnumerator UpdateAudio()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
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
            if (!_CloseTravelAudio.isPlaying && ((1 - Progress) * _travelTime) <= _CloseTravelAudio.clip.length)
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