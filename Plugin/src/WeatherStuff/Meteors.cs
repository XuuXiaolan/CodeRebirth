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
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>();
    public void SetParams(Vector3 spawnLocation, Vector3 landLocation, int randomInt) {        
        this.speed += speed;
        this.spawnLocation = spawnLocation;
        this.landLocation = landLocation;
        timeToLand = Vector3.Distance(spawnLocation, landLocation) / speed;
        timeRemaining.Value = 1; // Initialize remaining time to 1 for Lerp calculation
        fireParticles = this.GetComponentInChildren<ParticleSystem>();
        this.randomInt = randomInt;
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
        Landmine.SpawnExplosion(landLocation, true, 0f, 10f, 25, 75, Plugin.BigExplosion);
    }

    private void TrySpawnScrap() {
        if ((IsHost || IsServer) && randomInt >= (100-chanceToSpawnScrap)) {
            Plugin.CRUtils.GetComponent<CodeRebirthUtils>().SpawnScrapServerRpc("Meteorite", landLocation + new Vector3(0, -0.35f, 0));
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