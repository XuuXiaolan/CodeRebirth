using System.Collections;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Util;
using CodeRebirth.src.MiscScripts;
using UnityEngine.Events;
using CodeRebirth.src.Content.Items;

namespace CodeRebirth.src.Content.Weathers;
public class Meteors : NetworkBehaviour
{
    [Header("Properties")]
    public float initialSpeed = 50f;
    public float chanceToSpawnScrap;

    [Header("Audio")]
    public AudioSource ImpactAudio = null!;
    public AudioSource NormalTravelAudio = null!;
    public AudioSource CloseTravelAudio = null!;

    [Header("Graphics")]
    public GameObject? FireTrail = null;
    public AnimationCurve animationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Events")]
    public UnityEvent _onMeteorLand;

    private Vector3 origin = Vector3.zero;
    private Vector3 target = Vector3.zero;
    private Vector3 normal = Vector3.zero;

    private float timeInAir = 0;
    private float travelTime = 0;
    private bool isMoving = false;

    public float Progress => timeInAir / travelTime;

    [ClientRpc]
    public void SetupMeteorClientRpc(Vector3 _origin, Vector3 _target)
    {
        origin = _origin;
        target = _target;
        float distance = Vector3.Distance(origin, target);
        Ray ray = new Ray(origin, target - origin);
        Physics.Raycast(ray, out RaycastHit hit, distance + 5f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore);
        Plugin.ExtendedLogging($"Raycast hit: {hit.point} with normal: {hit.normal}");
        target = hit.point;
        normal = hit.normal;
        distance = Vector3.Distance(origin, target);
        initialSpeed = Plugin.ModConfig.ConfigMeteorSpeed.Value;
        travelTime = Mathf.Sqrt(2 * distance / initialSpeed);  // Time to reach the target, adjusted for acceleration
        isMoving = true;
        transform.LookAt(target);
        StartCoroutine(UpdateAudio()); // Make sure audio works correctly on the first frame.
        FireTrail?.SetActive(true);
    }

    public void SetupAsLooping(bool isBig)
    {
        isMoving = false;
        if (!isBig)
        {
            NormalTravelAudio.volume = 0f;
            CloseTravelAudio.volume = 0f;
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

    public void Update()
    {
        if (!isMoving) return;

        timeInAir += Time.deltaTime;
        MoveMeteor();
    }

    private void MoveMeteor()
    {
        float progress = Progress;
        if (progress >= 1.0f)
        {
            transform.position = target;
            StartCoroutine(Impact()); // Start the impact effects
            return;
        }

        Vector3 nextPosition = Vector3.Lerp(origin, target, animationCurve.Evaluate(progress));
        transform.position = nextPosition;
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
                NormalTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
                CloseTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
                ImpactAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
            }
            if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed)
            {
                NormalTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value) * Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
                CloseTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value) * Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
                ImpactAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value) * Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
            }
            if (((1 - Progress) * travelTime) <= 4.106f && !CloseTravelAudio.isPlaying)
            {
                NormalTravelAudio.volume = Mathf.Clamp01(0.5f * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
                CloseTravelAudio.Play();
            }
        }
    }

    private IEnumerator Impact()
    {
        isMoving = false;

        ImpactAudio.Play();

        if (IsServer && UnityEngine.Random.Range(0, 100) < chanceToSpawnScrap)
        {
            int randomNumber = UnityEngine.Random.Range(0, 3);
            if (randomNumber == 0) CodeRebirthUtils.Instance.SpawnScrapServerRpc(WeatherHandler.Instance.Meteorite!.ItemDefinitions.GetCRItemDefinitionWithItemName("Sapphire")?.item.itemName, target);
            else if (randomNumber == 1) CodeRebirthUtils.Instance.SpawnScrapServerRpc(WeatherHandler.Instance.Meteorite!.ItemDefinitions.GetCRItemDefinitionWithItemName("Emerald")?.item.itemName, target);
            else CodeRebirthUtils.Instance.SpawnScrapServerRpc(WeatherHandler.Instance.Meteorite!.ItemDefinitions.GetCRItemDefinitionWithItemName("Ruby")?.item.itemName, target);
        }

        GameObject craterInstance = Instantiate(WeatherHandler.Instance.Meteorite!.CraterPrefab, target, Quaternion.identity);
        craterInstance.transform.up = normal;
        CraterController craterController = craterInstance.GetComponent<CraterController>();
        craterController.ShowCrater(target, normal);

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