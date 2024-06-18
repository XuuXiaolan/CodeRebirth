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
    public Transform eye;

    float updateTimer = 0;
    
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
        if (IsHost) {
            StartAISearchRoutine(outsideNodes);
        }
    }

    void Update() {
        updateTimer += Time.deltaTime;
    }

    private void FixedUpdate() {
        UpdateAudio();

        if (updateTimer > 0.05f) {
            Step();
            updateTimer = 0;
        }
    }
    private void Step() {
        var localPlayerController = GameNetworkManager.Instance.localPlayerController;
        if (!StartOfRound.Instance.shipBounds.bounds.Contains(localPlayerController.transform.position) && !localPlayerController.isInsideFactory && TornadoHasLineOfSightToPosition(this.transform.position)) {
            float distanceToTornado = Vector3.Distance(transform.position, localPlayerController.transform.position);
            // Check if player is within 75 units of the tornado
            Vector3 directionToCenter = (transform.position - localPlayerController.transform.position).normalized;
            float forceStrength = CalculatePullStrength(distanceToTornado);
            localPlayerController.externalForces += directionToCenter * forceStrength;
        }
    }
    private float CalculatePullStrength(float distance) {
        float maxDistance = 75f;
        float minStrength = 0.15f;
        float maxStrength = 35f;

        // Calculate exponential strength based on distance
        float normalizedDistance = (maxDistance - distance) / maxDistance;
        if (distance <= 2.5f && damageTimer) {
            damageTimer = false;
            StartCoroutine(DamageTimer());
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(2);
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
            yield return new WaitForSeconds(2f);
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

    public bool TornadoHasLineOfSightToPosition(Vector3 pos, float range = 75) {
        return Vector3.Distance(eye.position, pos) < range && !Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault);
    }
}