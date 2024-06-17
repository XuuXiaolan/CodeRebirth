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
using UnityEngine.AI;
using System.Collections.Generic;

namespace CodeRebirth.WeatherStuff;
public class Tornados : NetworkBehaviour
{
    #pragma warning disable 0414
    #pragma warning disable 0169
    #pragma warning disable 0649
    private float walkingSpeed = 4f;
    private float runningSpeed = 8f;
    private float timeBeforeNextMove = 1f;
    private AudioClip[] noiseSFX;
    private NavMeshAgent agent;
    private Vector3 destination;
    private Vector3 previousPosition;
    private Vector3 agentLocalVelocity;

    [Header("Properties")]
    [SerializeField]
    private float initialSpeed = 5f;
    [Space(5f)]
    [Header("Audio")]
    [SerializeField]
    AudioSource normalTravelAudio, closeTravelAudio;
    [Space(5f)]
    [Header("Graphics")]
    [SerializeField]
    private ParticleSystem[] tornadoParticles;
    #pragma warning restore 0649
    #pragma warning restore 0414
    #pragma warning restore 0169

    private List<GameObject> outsideNodes = new List<GameObject>();
    private Vector3 origin;

    public enum TornadoType
    {
        Random,
        Fire,
        Electric,
        Windy,
        Blood
    }
    public TornadoType tornadoType = TornadoType.Random;
    private bool damageTimer = true;

    [ClientRpc]
    public void SetupTornadoClientRpc(Vector3 origin, int typeIndex) {
        this.origin = origin;
        this.origin = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: origin, radius: 10f, randomSeed: new Random(30));
        this.transform.position = this.origin;
        // this.tornadoType = (TornadoType)typeIndex;
        Init();
        UpdateAudio(); // Make sure audio works correctly on the first frame.
    }

    private void Awake() {
        TornadoWeather.Instance.AddTornado(this);
    }

    private void Init() {
        this.outsideNodes = RoundManager.Instance.outsideAINodes.ToList(); // would travel between these nodes using a search routine.
        agent = GetComponent<NavMeshAgent>();
        agent.speed = initialSpeed;
        StartCoroutine(TornadoUpdate());
        if (IsHost) {
            StartAISearchRoutine(outsideNodes);
        }
    }
    private void FixedUpdate() {
        UpdateAudio();
    }
    private IEnumerator TornadoUpdate() {
        WaitForSeconds wait = new WaitForSeconds(0.05f); // Execute every 0.05 seconds
        while (true) {
            yield return wait; // Reduced frequency of execution

            var localPlayerController = GameNetworkManager.Instance.localPlayerController;
            if (localPlayerController == null) {
                continue; // Skip if localPlayerController is not set
            }

            // Check if player is inside the ship bounds or factory
            if (!(StartOfRound.Instance.shipBounds.bounds.Contains(localPlayerController.transform.position) || localPlayerController.isInsideFactory)) {
                float distanceToTornado = Vector3.Distance(transform.position, localPlayerController.transform.position);

                // Check if player is within 100 units of the tornado
                if (distanceToTornado < 100f) {
                    Vector3 directionToCenter = (transform.position - localPlayerController.transform.position).normalized;
                    float forceStrength = CalculatePullStrength(distanceToTornado);
                    localPlayerController.externalForces += directionToCenter * forceStrength;
                }
            }
        }
    }
    private float CalculatePullStrength(float distance) {
        float maxDistance = 100f;
        float minStrength = 0.25f;
        float maxStrength = 50f;

        // Calculate exponential strength based on distance
        float normalizedDistance = (maxDistance - distance) / maxDistance;
        if (distance <= 2.5f && damageTimer) {
            damageTimer = false;
            StartCoroutine(DamageTimer());
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(5);
        }
        return Mathf.Clamp(Mathf.Lerp(minStrength, maxStrength, normalizedDistance * normalizedDistance), minStrength, maxStrength);
    }

    private IEnumerator DamageTimer() {
        yield return new WaitForSeconds(0.5f);
        damageTimer = true;
    }
    private void UpdateAudio() {
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
        {
            normalTravelAudio.volume = 0;
            closeTravelAudio.volume = 0;
        }
    }

    public void StartAISearchRoutine(List<GameObject> nodes) {
        StartCoroutine(AISearchRoutine(nodes));
    }

    private IEnumerator AISearchRoutine(List<GameObject> nodes) {
        while (true) {
            if (nodes.Count == 0) yield break;

            List<GameObject> nearbyNodes = nodes.Where(node => Vector3.Distance(node.transform.position, transform.position) < 20f).ToList();

            if (nearbyNodes.Count == 0) yield break;

            GameObject targetNode = nearbyNodes[UnityEngine.Random.Range(0, nearbyNodes.Count)];
            agent.SetDestination(targetNode.transform.position);

            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance) {
                yield return null;
            }
        }
    }
}