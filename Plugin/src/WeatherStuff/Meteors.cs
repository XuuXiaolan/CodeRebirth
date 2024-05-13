using System;
using System.Collections;
using System.Linq;
using CodeRebirth.src;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using CodeRebirth.Collisions;

namespace CodeRebirth.WeatherStuff;
public class Meteors : NetworkBehaviour {
    #pragma warning disable CS0649
    [SerializeField] private float speed;
    [SerializeField] private int chanceToSpawnScrap;
    private Vector3 spawnLocation;
    private Vector3 landLocation;
    private float timeToLand;
    private ParticleSystem fireParticles;
    private int randomInt;
    [SerializeField] public GameObject craterPrefab;
    public AudioSource meteorImpact;
    public AudioSource meteorTravel;
    public AudioSource meteorCloseTravel;
    private bool landed = false;
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>();
    public void SetParams(Vector3 spawnLocation, Vector3 landLocation, int randomInt) {
        SetParamsStuffClientRpc(spawnLocation, landLocation, randomInt);
        if (IsServer) {
            timeRemaining.Value = 1; // Initialize remaining time to 1 for Lerp calculation
        }      
        if (IsServer) {
            Plugin.Logger.LogInfo($"Actual Time to Land: {timeToLand}");
        }
    }
    [ClientRpc]
    public void SetParamsStuffClientRpc(Vector3 spawnLocation, Vector3 landLocation, int randomInt) {
        fireParticles = this.GetComponentInChildren<ParticleSystem>();
        this.randomInt = randomInt;
        this.speed += speed;
        this.spawnLocation = spawnLocation;
        this.landLocation = landLocation;
        timeToLand = Vector3.Distance(spawnLocation, landLocation) / speed;
    }

    private void Update() {
        if (NetworkObject.IsSpawned == false) return;
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) {
            meteorImpact.volume = 0f;
            meteorTravel.volume = 0f;
            meteorCloseTravel.volume = 0f;
        } else {
            meteorImpact.volume = 1f;
            meteorTravel.volume = 1f;
            meteorCloseTravel.volume = 1f;
        }
        if (timeToLand*timeRemaining.Value <= 4.106f && !meteorCloseTravel.enabled && meteorTravel.enabled) {
            meteorTravel.enabled = false;
            meteorCloseTravel.enabled = true;
            Plugin.Logger.LogInfo("enabled close sounds");
        }
        this.transform.LookAt(landLocation);
        if (timeRemaining.Value > 0) {
            UpdatePosition();
        } else {
            CheckLanding();
        }
    }

    private void UpdatePosition() {
        if (!IsOwner) return;
        transform.position = Vector3.Lerp(landLocation, spawnLocation, timeRemaining.Value);
        timeRemaining.Value -= Time.deltaTime / timeToLand;
    }

    private void CheckLanding() {
        if (!landed && Physics.OverlapSphere(transform.position, 5).Any(x => x.gameObject.layer == LayerMask.NameToLayer("Terrain") || x.gameObject.layer == LayerMask.NameToLayer("Room"))) { 
            HandleLanding();
            meteorTravel.enabled = false;
            meteorImpact.enabled = true;
            // play impact audio?
        }
    }

    private void HandleLanding() {
        if (IsServer) {
            CreateCraterClientRpc(landLocation);
            TrySpawnScrap();
            DestroyNetworkObjectServerRpc();
        }
    }
    [ClientRpc]
    private void CreateCraterClientRpc(Vector3 rockLandedPosition) {
        Explode();
        GameObject craterInstance = Instantiate(Plugin.BetterCrater, rockLandedPosition, Quaternion.identity, Plugin.meteorShower.effectPermanentObject.transform);
        CraterController craterController = craterInstance.GetComponent<CraterController>();
        if (craterController != null) {
            landed = true;
            Plugin.Logger.LogInfo("CraterController instantiated successfully.");
            craterController.ShowCrater(landLocation);
        } else {
            Plugin.Logger.LogError("Failed to get CraterController from instantiated prefab.");
        }
        GetComponentInChildren<ParticleSystem>().Stop();
        this.transform.Find("FlameStream").GetComponentInChildren<ParticleSystem>().Stop();
    }
    private void Explode() {
        fireParticles.Stop();
        Landmine.SpawnExplosion(landLocation, true, 0f, 10f, 25, 75, Plugin.BigExplosion);
    }

    private void TrySpawnScrap() {
        if ((IsHost || IsServer) && randomInt >= (100-chanceToSpawnScrap)) {
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("Meteorite", this.transform.position + new Vector3(0, -0.6f, 0));
        }
    }

    [ServerRpc]
    private void DestroyNetworkObjectServerRpc() {
        if (IsServer) {
            StartCoroutine(DestroyDelay());
        }
    }

    private IEnumerator DestroyDelay() {
        yield return new WaitForSeconds(10f);
        if (IsServer) {
            this.GetComponent<NetworkObject>().Despawn();
        }
    }
}
public class CraterController : MonoBehaviour
{
    public GameObject craterMesh;
    private bool craterVisible = false;
    private ColliderIdentifier fireCollider;

    private void Awake()
    {
        craterMesh.SetActive(false); // Initially hide the crater
        fireCollider = this.transform.Find("WildFire").GetComponent<ColliderIdentifier>();
        fireCollider.enabled = false; // Make sure it's disabled on start
    }

    public void ShowCrater(Vector3 impactLocation)
    {
        transform.position = impactLocation; // Position the crater at the impact location
        
        // Perform a raycast downward from a position slightly above the impact location
        RaycastHit hit;
        float raycastDistance = 50f; // Max distance the raycast will check for terrain
        Vector3 raycastOrigin = impactLocation; // Start the raycast 5 units above the impact location

        // Ensure the crater has a unique material instance to modify
        Renderer craterRenderer = craterMesh.GetComponent<Renderer>();
        if (craterRenderer != null) {
            craterRenderer.material = new Material(craterRenderer.material); // Create a new instance of the material
            craterRenderer.material.color = Color.blue;
            // Cast the ray to detect terrain
            if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, raycastDistance, LayerMask.GetMask("Room"))) {
                // Check if the object hit is tagged as "Terrain"
                if (hit.collider.gameObject.tag == "Grass") {
                    // Additional logic for when the terrain is correctly tagged
                    craterRenderer.material.color = new Color(0.043f, 0.141f, 0.043f);
                    Plugin.Logger.LogInfo("Found Grass!");
                } else if (hit.collider.gameObject.tag == "Snow"){
                    craterRenderer.material.color = new Color(0.925f, 0.929f, 1f);
                    Plugin.Logger.LogInfo("Found Snow!");
                } else if (hit.collider.gameObject.tag == "Gravel"){
                    craterRenderer.material.color = new Color(0.851f, 0.851f, 0.851f);
                    Plugin.Logger.LogInfo("Found Gravel!");
                } else {
                    Debug.LogWarning("The hit object is not tagged as 'Terrain'.");
                }
            } else {
                Debug.LogWarning("Terrain not found below the impact point.");
            }
        } else {
            Debug.LogWarning("Renderer component not found on the crater object.");
        }
        craterMesh.SetActive(true);
        craterVisible = true;
        fireCollider.enabled = true; // Enable the ColliderIdentifier
    }

    public void HideCrater()
    {
        craterVisible = false;
        craterMesh.SetActive(false);
        fireCollider.enabled = false; // Ensure the ColliderIdentifier is disabled when the crater is hidden
    }
}