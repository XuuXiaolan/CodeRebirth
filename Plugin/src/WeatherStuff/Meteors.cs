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
    private System.Random random;
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>();

    public void SetParams(Vector3 spawnLocation, Vector3 landLocation) {
        this.random = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
        this.speed += random.Next(100, 400);
        this.spawnLocation = spawnLocation;
        this.landLocation = landLocation;
        timeToLand = Vector3.Distance(spawnLocation, landLocation) / speed;
        timeRemaining.Value = 1; // Initialize remaining time to 1 for Lerp calculation

        if (IsServer) {
            Plugin.Logger.LogInfo($"Actual Time to Land: {timeToLand}");
        }
    }

    private void Update() {
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
        Explode();
        TrySpawnScrap();
        DestroyNetworkObjectServerRpc();
    }

    private void Explode() {
        Landmine.SpawnExplosion(landLocation, true, 0f, 1f, 10, 25);
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