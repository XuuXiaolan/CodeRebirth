using System.Collections;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Util;
using CodeRebirth.src.MiscScripts;
using UnityEngine.Events;

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
    public AnimationCurve animationCurve = AnimationCurve.Linear(0,0,1,1);

    [Header("Events")]
    public UnityEvent _onMeteorLand;
    
    private Vector3 origin = Vector3.zero;
    private Vector3 target = Vector3.zero;

    private float timeInAir = 0;
    private float travelTime = 0;
    private bool isMoving = false;

    public float Progress => timeInAir / travelTime;

    [ClientRpc]
    public void SetupMeteorClientRpc(Vector3 origin, Vector3 target)
    {
        this.origin = origin;
        this.target = target;
        float distance = Vector3.Distance(origin, target);
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
        NormalTravelAudio.Play();
        FireTrail?.SetActive(false);

        chanceToSpawnScrap = Plugin.ModConfig.ConfigMeteorShowerMeteoriteSpawnChance.Value;
    }

    private void Update()
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
            if (((1-Progress)*travelTime) <= 4.106f && !CloseTravelAudio.isPlaying)
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
            if (UnityEngine.Random.Range(0, 100) <= 33.33f ) CodeRebirthUtils.Instance.SpawnScrapServerRpc("Sapphire Meteorite", target);
            else if (UnityEngine.Random.Range(0, 100) <= 33.33f ) CodeRebirthUtils.Instance.SpawnScrapServerRpc("Emerald Meteorite", target);
            else CodeRebirthUtils.Instance.SpawnScrapServerRpc("Ruby Meteorite", target);
        }

        GameObject craterInstance = Instantiate(WeatherHandler.Instance.Meteorite.CraterPrefab, target, Quaternion.identity);
        CraterController craterController = craterInstance.GetComponent<CraterController>();
        craterController.ShowCrater(target);

        FireTrail?.SetActive(false);
        
        CRUtilities.CreateExplosion(transform.position, true, 100, 0, 15, 4, null, WeatherHandler.Instance.Meteorite.ExplosionPrefab);
        _onMeteorLand.Invoke();
        
        if (!IsServer) yield break;
        yield return new WaitForSeconds(10f);
        NetworkObject.Despawn();
    }
}

public class CraterController : MonoBehaviour
{
    public void Start()
    {
        StartCoroutine(DespawnAfter20Seconds());
    }

    private IEnumerator DespawnAfter20Seconds()
    {
        float timeWhenSpawned = Time.time;
        yield return new WaitUntil(() => (StartOfRound.Instance.shipIsLeaving && Time.time >= timeWhenSpawned + 10f) || (Time.time >= timeWhenSpawned + 30f));
        Destroy(this.gameObject);
    }

    public void ShowCrater(Vector3 impactLocation)
    {
        transform.position = impactLocation + new Vector3(0, 3f, 0); // Position the crater at the impact location

        this.gameObject.SetActive(true);
    }
}