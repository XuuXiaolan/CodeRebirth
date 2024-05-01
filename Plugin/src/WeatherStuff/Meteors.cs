using System;
using CodeRebirth.src;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.WeatherStuff;
public class Meteors : NetworkBehaviour {
    #pragma warning disable CS0649
    [SerializeField] private float speed = 100f;

    private Vector3 spawnLocation;
    private Vector3 landLocation;
    private float timeToLand;
    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>();

    public void SetParams(Vector3 spawnLocation, Vector3 landLocation) {
        this.spawnLocation = spawnLocation;
        this.landLocation = landLocation;
        timeToLand = Vector3.Distance(spawnLocation, landLocation) / speed;
        timeRemaining.Value = 1; // Initialize remaining time to 1 for Lerp calculation

        if (IsServer) {
            Plugin.Logger.LogInfo($"Actual Time to Land: {timeToLand}");
        }
    }

    private void Update() {
        if (!IsOwner) return;

        if (timeRemaining.Value > 0) {
            UpdatePosition();
        } else {
            CheckLanding();
        }
    }

    private void UpdatePosition() {
        transform.position = Vector3.Lerp(landLocation, spawnLocation, timeRemaining.Value);
        timeRemaining.Value -= Time.deltaTime / timeToLand;
    }

    private void CheckLanding() {
        if (Vector3.Distance(transform.position, landLocation) < 5) {
            HandleLandingServerRpc();
        }
    }

    [ServerRpc]
    private void HandleLandingServerRpc() {
        Explode();
        DestroyNetworkObjectServerRpc();
    }

    private void Explode() {
        Landmine.SpawnExplosion(landLocation, true, 0f, 1f, 10, 25);
        TrySpawnScrap();
    }

    private void TrySpawnScrap() {
        if (IsHost) {
            CodeRebirthUtils.Instance.SpawnScrapServerRpc("MeteoriteContainer", landLocation);
        }
    }

    [ServerRpc]
    private void DestroyNetworkObjectServerRpc() {
        if (IsServer) {
            Destroy(gameObject);
        }
    }
}