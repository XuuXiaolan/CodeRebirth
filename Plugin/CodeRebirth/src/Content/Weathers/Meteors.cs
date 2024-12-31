using System.Collections;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.Util;
using CodeRebirth.src.MiscScripts;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Weathers;
public class Meteors : NetworkBehaviour {
    #pragma warning disable CS0649    
    [Header("Properties")]
    [SerializeField] private float initialSpeed = 50f;
    [SerializeField] private float chanceToSpawnScrap;

    [Header("Audio")]
    [SerializeField]
    private AudioSource ImpactAudio = null!;

    [SerializeField]
    private AudioSource NormalTravelAudio = null!, CloseTravelAudio = null!;

    [Header("Graphics")]
    [SerializeField]
    private GameObject? FireTrail = null;
    [SerializeField]
    AnimationCurve animationCurve = AnimationCurve.Linear(0,0,1,1);

    [Header("Events")]
    [SerializeField]
    UnityEvent _onMeteorLand;
    
    Vector3 origin, target;

    float timeInAir, travelTime;
    bool isMoving;
    bool isBig;

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
        this.isBig = isBig;
        StartCoroutine(UpdateAudio()); // Make sure audio works correctly on the first frame.
    }
    
    private void Awake()
    {
        MeteorShower.Instance?.AddMeteor(this);
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
        { // Checks if the progress is 100% or more
            transform.position = target; // Ensures the meteor position is set to the target at impact
            StartCoroutine(Impact()); // Start the impact effects
            return; // Exit to prevent further execution in this update
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
            if (!isMoving && !isBig)
            {
                NormalTravelAudio.volume = 0f;
                CloseTravelAudio.volume = 0f;
            }
        }
    }

    private IEnumerator Impact()
    {
        isMoving = false;

        ImpactAudio.Play();
            
        if (IsHost && UnityEngine.Random.Range(0, 100) < chanceToSpawnScrap)
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
        
        yield return new WaitForSeconds(10f); // allow the last particles from the fire trail to still emit. <-- Actually i think the meteor just looks cool staying on the ground for an extra 10 seconds.
        if (IsHost)
            Destroy(gameObject);
        MeteorShower.Instance?.RemoveMeteor(this);
    }
}

public class CraterController : MonoBehaviour
{

    private void Awake()
    {
        StartCoroutine(DespawnAfter20Seconds());
        MeteorShower.Instance?.AddCrater(this);
    }

    private IEnumerator DespawnAfter20Seconds()
    {
        yield return new WaitForSeconds(20);
        Destroy(this.gameObject);
        MeteorShower.Instance?.RemoveCrater(this);
    }

    public void ShowCrater(Vector3 impactLocation)
    {
        transform.position = impactLocation + new Vector3(0, 3f, 0); // Position the crater at the impact location

        this.gameObject.SetActive(true);
    }
}