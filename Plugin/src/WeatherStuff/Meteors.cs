using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using CodeRebirth.src;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using CodeRebirth.Collisions;
using CodeRebirth.Misc;
using UnityEngine.Serialization;
using Random = System.Random;
using CodeRebirth.Util.Spawning;

namespace CodeRebirth.WeatherStuff;
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
    private ParticleSystem FireTrail = null!;
    [SerializeField]
    AnimationCurve animationCurve = AnimationCurve.Linear(0,0,1,1);
    
    Vector3 origin, target;

    float timeInAir, travelTime;
    bool isMoving;
    
    public float Progress => timeInAir / travelTime;

    [ClientRpc]
    public void SetupMeteorClientRpc(Vector3 origin, Vector3 target) {
        this.origin = origin;
        this.target = target;
        float distance = Vector3.Distance(origin, target);
        initialSpeed = Plugin.ModConfig.ConfigMeteorSpeed.Value;
        travelTime = Mathf.Sqrt(2 * distance / initialSpeed);  // Time to reach the target, adjusted for acceleration
        isMoving = true;
        transform.LookAt(target);
        UpdateAudio(); // Make sure audio works correctly on the first frame.
        FireTrail.Play();
    }

    public void SetupAsLooping() {
        isMoving = false;
    }
    
    private void Awake() {
        if (MeteorShower.Instance != null) MeteorShower.Instance.AddMeteor(this);
        NormalTravelAudio.Play();
        FireTrail.Stop();
        chanceToSpawnScrap = Plugin.ModConfig.ConfigMeteorShowerMeteoriteSpawnChance.Value;
    }

    private void Update() {
        UpdateAudio();
        if (!isMoving) return;

        timeInAir += Time.deltaTime;
        MoveMeteor();
    }

    void MoveMeteor()
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

    private void UpdateAudio() {
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) {
            NormalTravelAudio.volume = 0;
            CloseTravelAudio.volume = 0;
            ImpactAudio.volume = 0.05f;
        } else {
            NormalTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
            CloseTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
            ImpactAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
        }
        if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed) {
            NormalTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value) * Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
            CloseTravelAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value) * Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
            ImpactAudio.volume = Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorShowerInShipVolume.Value) * Mathf.Clamp01(Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
        }
        if (((1-Progress)*travelTime) <= 4.106f && !CloseTravelAudio.isPlaying) {
            NormalTravelAudio.volume = Mathf.Clamp01(0.5f * Plugin.ModConfig.ConfigMeteorsDefaultVolume.Value);
            CloseTravelAudio.Play();
        }
    }

    private IEnumerator Impact() {
        isMoving = false;

        ImpactAudio.Play();
            
        if (IsHost && UnityEngine.Random.Range(0, 10000) < (int)chanceToSpawnScrap*100) {
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("Meteorite", target);
        }
            
        GameObject craterInstance = Instantiate(WeatherHandler.Instance.Meteorite.CraterPrefab, target, Quaternion.identity);
        CraterController craterController = craterInstance.GetComponent<CraterController>();
        if (craterController != null) {
            craterController.ShowCrater(target);
        }
        
        FireTrail.Stop();
        
        Utilities.CreateExplosion(transform.position, true, 100, 0, 10, 4, CauseOfDeath.Blast, null, WeatherHandler.Instance.Meteorite.ExplosionPrefab);

        yield return new WaitForSeconds(10f); // allow the last particles from the fire trail to still emit. <-- Actually i think the meteor just looks cool staying on the ground for an extra 10 seconds.
        if(IsHost)
            Destroy(gameObject);
        if (MeteorShower.Instance != null) MeteorShower.Instance.RemoveMeteor(this);
    }
}
public class CraterController : MonoBehaviour // Change this to use decals!!
{
    [SerializeField]
    [Tooltip("The GameObject that will be spawned when the meteor hits the ground.")]
    private GameObject craterMesh = null!;
    private ColliderIdentifier fireCollider = null!;

    private void Awake()
    {
        fireCollider = this.transform.Find("WildFire").GetComponent<ColliderIdentifier>();
        ToggleCrater(false);
        if (MeteorShower.Instance != null) MeteorShower.Instance.AddCrater(this);
    }
    public void ShowCrater(Vector3 impactLocation)
    {
        transform.position = impactLocation + new Vector3(0, 3f, 0); // Position the crater at the impact location
    
        craterMesh.SetActive(true);
        fireCollider.enabled = true; // Enable the ColliderIdentifier
    }
    void ToggleCrater(bool enable)
    {
        craterMesh.SetActive(enable);
        fireCollider.enabled = enable;
    }
    public void HideCrater()
    {
        ToggleCrater(false);
    }
}