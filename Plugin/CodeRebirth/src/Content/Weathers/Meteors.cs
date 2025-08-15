using System.Collections;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Util;
using CodeRebirth.src.MiscScripts;
using UnityEngine.Events;
using CodeRebirthLib;


namespace CodeRebirth.src.Content.Weathers;
public class Meteors : FallingObjectBehaviour
{
    private float chanceToSpawnScrap;

    [Header("Audio")]
    public AudioSource ImpactAudio = null!;
    public AudioSource NormalTravelAudio = null!;
    public AudioSource CloseTravelAudio = null!;

    [Header("Graphics")]
    public GameObject? FireTrail = null;

    [Header("Events")]
    public UnityEvent _onMeteorLand;

    protected override void OnImpact()
    {
        base.OnImpact();
        StartCoroutine(Impact()); // Start the impact effects
    }

    protected override void OnSetup()
    {
        base.OnSetup();
        StartCoroutine(UpdateAudio()); // Make sure audio works correctly on the first frame.
        FireTrail?.SetActive(true);
    }

    public void SetupAsLooping(bool isBig)
    {
        StopMoving();
        if (!isBig)
        {
            NormalTravelAudio.enabled = false;
            CloseTravelAudio.enabled = false;
            return;
        }
        StartCoroutine(UpdateAudio()); // Make sure audio works correctly on the first frame.
    }

    public void Start()
    {
        MeteorShower.meteors.Add(this);
        NormalTravelAudio.Play();
        FireTrail?.SetActive(false);

        chanceToSpawnScrap = Plugin.ModConfig.ConfigMeteorShowerMeteoriteSpawnChance.Value;
    }
    private IEnumerator UpdateAudio()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f);
            if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
            {
                NormalTravelAudio.volume = 0;
                CloseTravelAudio.volume = 0;
                ImpactAudio.volume = 0.05f;
            }
            else
            {
                NormalTravelAudio.volume = Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                CloseTravelAudio.volume = Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                ImpactAudio.volume = Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
            }
            if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed)
            {
                NormalTravelAudio.volume = Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                CloseTravelAudio.volume = Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                ImpactAudio.volume = Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
            }
            if (((1 - Progress) * _travelTime) <= CloseTravelAudio.clip.length && !CloseTravelAudio.isPlaying)
            {
                NormalTravelAudio.volume = 0.5f * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value;
                CloseTravelAudio.Play();
            }
        }
    }

    private IEnumerator Impact()
    {
        ImpactAudio.Play();

        if (IsServer && UnityEngine.Random.Range(0, 100) < chanceToSpawnScrap)
        {
            Item[] meteorites =
            [
                LethalContent.Items[NamespacedKey<CRItemInfo>.From("code_rebirth", "amethyst_crystal")].Item,
                LethalContent.Items[NamespacedKey<CRItemInfo>.From("code_rebirth", "sapphire_crystal")].Item,
                LethalContent.Items[NamespacedKey<CRItemInfo>.From("code_rebirth", "emerald_crystal")].Item,
                LethalContent.Items[NamespacedKey<CRItemInfo>.From("code_rebirth", "quartz_crystal")].Item,
                LethalContent.Items[NamespacedKey<CRItemInfo>.From("code_rebirth", "ruby_crystal")].Item,
                LethalContent.Items[NamespacedKey<CRItemInfo>.From("code_rebirth", "topaz_crystal")].Item
            ];
            int randomNumber = UnityEngine.Random.Range(0, meteorites.Length);
            CodeRebirthUtils.Instance.SpawnScrap(meteorites[randomNumber], _target, false, true, 0);
        }

        GameObject craterInstance = Instantiate(WeatherHandler.Instance.Meteorite!.CraterPrefab, _target, Quaternion.identity);
        craterInstance.transform.up = _normal;
        CraterController craterController = craterInstance.GetComponent<CraterController>();
        craterController.ShowCrater(_target, _normal);

        FireTrail?.SetActive(false);

        CRUtilities.CreateExplosion(transform.position, true, 100, 0, 15, 4, null, WeatherHandler.Instance.Meteorite.ExplosionPrefab, 25f);
        _onMeteorLand.Invoke();

        if (!IsServer) yield break;
        yield return new WaitForSeconds(10f);
        if (!NetworkObject.IsSpawned) yield break;
        MeteorShower.meteors.Remove(this);
        NetworkObject.Despawn();
    }
}

public class CraterController : MonoBehaviour
{
    public void Start()
    {
        MeteorShower.craters.Add(this);
        StartCoroutine(DespawnAfterDelay(60f));
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        MeteorShower.craters.Remove(this);
        Destroy(this.gameObject);
    }

    public void ShowCrater(Vector3 impactLocation, Vector3 normal)
    {
        transform.position = impactLocation + new Vector3(0, 3f, 0); // Position the crater at the impact location
        transform.up = normal;
        Plugin.ExtendedLogging($"Crater position: {transform.position} with normal: {normal}");
        this.gameObject.SetActive(true);
    }
}