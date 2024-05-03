using System;
using System.Linq;
using CodeRebirth.src;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.WeatherStuff;
public class Meteors : NetworkBehaviour {
    #pragma warning disable CS0649
    [SerializeField] private float speed;
    private Vector3 spawnLocation;
    private Vector3 landLocation;
    private float timeToLand;
    private ParticleSystem fireParticles;
    [SerializeField] public GameObject craterPrefab;
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>();
    public void SetParams(Vector3 spawnLocation, Vector3 landLocation) {        
        this.speed += speed;
        this.spawnLocation = spawnLocation;
        this.landLocation = landLocation;
        timeToLand = Vector3.Distance(spawnLocation, landLocation) / speed;
        timeRemaining.Value = 1; // Initialize remaining time to 1 for Lerp calculation
        fireParticles = this.GetComponentInChildren<ParticleSystem>();
        if (IsServer) {
            Plugin.Logger.LogInfo($"Actual Time to Land: {timeToLand}");
        }
    }

    private void Update() {
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
        if (IsOwner && Physics.OverlapSphere(transform.position, 5).Any(x => x.gameObject.layer == LayerMask.NameToLayer("Terrain") || x.gameObject.layer == LayerMask.NameToLayer("Room"))) { 
            HandleLandingServerRpc();
        }
    }
    [ServerRpc]
    private void HandleLandingServerRpc() {
        GameObject craterInstance = Instantiate(craterPrefab, landLocation, Quaternion.identity);
        CraterController craterController = craterInstance.GetComponent<CraterController>();

        if (craterController != null) {
            Plugin.Logger.LogInfo("CraterController instantiated successfully.");
            craterController.ShowCrater(landLocation);
        } else {
            Plugin.Logger.LogError("Failed to get CraterController from instantiated prefab.");
        }
        Explode();
        TrySpawnScrap();
        DestroyNetworkObjectServerRpc();
    }

    private void Explode() {
        fireParticles.Stop();
        Landmine.SpawnExplosion(landLocation, true, 0f, 1f, 10, 25, Plugin.BigExplosion);
    }

    private void TrySpawnScrap() {
        if (IsHost) {
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("Meteorite", landLocation);
        }
    }

    [ServerRpc]
    private void DestroyNetworkObjectServerRpc() {
        if (IsServer) {

            Destroy(gameObject);
        }
    }
}

public class CraterController : MonoBehaviour
{
    // Assign the crater mesh in the Inspector to hide or show it!
    public GameObject craterMesh;
    private bool craterVisible = false;

    public void Awake()
    {
        craterMesh.SetActive(false); // Initially hide the crater
    }

    // Method to show the crater at the specified impact location
    public void ShowCrater(Vector3 impactLocation)
    {
        transform.position = impactLocation; // Position the crater at the impact location
        craterMesh.SetActive(true);
        craterVisible = true;
    }

    // Optionally, a method to hide the crater if needed for game logic.
    public void HideCrater()
    {
        craterVisible = false; 
        craterMesh.SetActive(false); // Hide the crater
    }
}