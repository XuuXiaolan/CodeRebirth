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
using CodeRebirth.Misc.LightningScript;
using UnityEngine.Serialization;
using Random = System.Random;
using UnityEngine.AI;
using System.Collections.Generic;
using CodeRebirth.Util.PlayerManager;
using CodeRebirth.Util.Extensions;

namespace CodeRebirth.WeatherStuff;
public class Tornados : NetworkBehaviour
{
    private NavMeshAgent agent;
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
        Fire,
        Blood,
        Windy,
        Smoke,
        Water,
        Electric,
    }
    private TornadoType tornadoType = TornadoType.Fire;
    private bool damageTimer = true;
    private float originalPlayerSpeed = 0;
    public Transform eye;
    private bool lightningBoltTimer = true;
    private Random random = new Random();

    [ClientRpc]
    public void SetupTornadoClientRpc(Vector3 origin, int typeIndex) {
        this.origin = origin;
        this.origin = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: origin, radius: 10f, randomSeed: new Random(30));
        this.transform.position = this.origin;
        this.tornadoType = (TornadoType)typeIndex;
        Plugin.Logger.LogInfo($"Setting up tornado of type: {tornadoType} at {origin}");
        random = new Random(StartOfRound.Instance.randomMapSeed + 325);
        SetupTornadoType();
        UpdateAudio(); // Make sure audio works correctly on the first frame.
    }

    private void Awake() {
        TornadoWeather.Instance.AddTornado(this);
    }

    private void SetupTornadoType() {
        switch (tornadoType) {
            case TornadoType.Fire:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Fire")) {
                        particleSystem.gameObject.SetActive(true);   
                    }
                }
                break;
            case TornadoType.Blood:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Blood")) {
                        particleSystem.gameObject.SetActive(true);   
                    }
                }
                break;
            case TornadoType.Windy:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Wind")) {
                        particleSystem.gameObject.SetActive(true);   
                    }
                }
                break;
            case TornadoType.Smoke:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Smoke")) {
                        particleSystem.gameObject.SetActive(true);   
                    }
                }
                break;
            case TornadoType.Water:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Water")) {
                        particleSystem.gameObject.SetActive(true);   
                    }
                }
                initialSpeed /= 2;
                break;
            case TornadoType.Electric:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Electric")) {
                        particleSystem.gameObject.SetActive(true);   
                    }
                }
                initialSpeed *= 2;
                break;
        }
        Init();
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
        int i = 0;
        WaitForSeconds wait = new WaitForSeconds(0.05f); // Execute every 0.05 seconds
        var localPlayerController = GameNetworkManager.Instance.localPlayerController;
        CodeRebirthPlayerManager localPlayerManager = localPlayerController.gameObject.GetComponent<CodeRebirthPlayerManager>();
        while (true) {
            yield return wait; // Reduced frequency of execution
            HandleTornadoActions(localPlayerManager, localPlayerController);
            i++;
            if (i > 14400) {
                Plugin.Logger.LogFatal("This tornado loop ran too many iterations.");
                yield break;
            }
        }
    }
    private void HandleTornadoActions(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController) {
        bool doesTornadoAffectPlayer = !localPlayerManager.ridingHoverboard && !StartOfRound.Instance.shipBounds.bounds.Contains(localPlayerController.transform.position) && !localPlayerController.isInsideFactory && localPlayerController != null && localPlayerController.isPlayerControlled && !localPlayerController.isPlayerDead && !localPlayerController.isInHangarShipRoom && !localPlayerController.inAnimationWithEnemy && !localPlayerController.enteringSpecialAnimation && !localPlayerController.isClimbingLadder;
        if (doesTornadoAffectPlayer) {
            float distanceToTornado = Vector3.Distance(transform.position, localPlayerController.transform.position);
            bool hasLineOfSight = TornadoHasLineOfSightToPosition(localPlayerController.transform.position);
            Vector3 directionToCenter = (transform.position - localPlayerController.transform.position).normalized;
            float forceStrength = CalculatePullStrength(distanceToTornado, hasLineOfSight);
            localPlayerController.externalForces += directionToCenter * forceStrength;
        }
        // All of these should activate some sort of particle effect on the player.
        switch (tornadoType) {
            case TornadoType.Fire:
                break;
            case TornadoType.Blood:
                break;
            case TornadoType.Windy:
                break;
            case TornadoType.Smoke:
                break;
            case TornadoType.Water:
                if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) < 10 && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water] = true;
                    localPlayerController.isMovementHindered++;
			        localPlayerController.hinderedMultiplier *= 3f;  
                } else if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) >= 10 && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water] = false;
                    localPlayerController.isMovementHindered = Mathf.Clamp(localPlayerController.isMovementHindered - 1, 0, 1000);
			        localPlayerController.hinderedMultiplier /= 3f;
                }
                break;
            case TornadoType.Electric:
                if (lightningBoltTimer) {
                    lightningBoltTimer = false;
                    StartCoroutine(LightningBoltTimer());
                    Vector3 strikePosition = GetRandomTargetPosition(random, outsideNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25);
                    LightningStrikeScript.SpawnLightningBolt(strikePosition);
                }
                if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) < 10 && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric] = true;
                    originalPlayerSpeed = localPlayerController.movementSpeed;
                    localPlayerController.movementSpeed *= 1.3f;
                } else if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) >= 10 && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric] = false;
                    localPlayerController.movementSpeed = originalPlayerSpeed;
                }
                break;
        }
    }
    private IEnumerator LightningBoltTimer() {
        yield return new WaitForSeconds(random.NextFloat(0f, 1f) * 8);
        lightningBoltTimer = true;
    }
    private float CalculatePullStrength(float distance, bool hasLineOfSight) {
        float maxDistance = 150f + (tornadoType == TornadoType.Windy ? 50f : 0f);
        float minStrength = 0.1f;
        float maxStrength = (hasLineOfSight ? 30f : 6f) * (tornadoType == TornadoType.Windy ? 1.5f : 1f);

        // Calculate exponential strength based on distance
        float normalizedDistance = (maxDistance - distance) / maxDistance;
        if (distance <= 2.5f && damageTimer) {
            damageTimer = false;
            StartCoroutine(DamageTimer());
            HandleTornadoTypeDamage();
        }
        return Mathf.Clamp(Mathf.Lerp(minStrength, maxStrength, normalizedDistance * normalizedDistance), minStrength, maxStrength);
    }

    private void HandleTornadoTypeDamage() {
        var localPlayer = GameNetworkManager.Instance.localPlayerController;
        switch (tornadoType) {
            case TornadoType.Fire:
                // make the screen a firey mess
                if (localPlayer.health > 20) {
                    localPlayer.DamagePlayer(3);
                }
                break;
            case TornadoType.Blood:
                if (localPlayer.health < 60 && localPlayer.health > 30) {
                    localPlayer.DamagePlayer(-1);
                } else if (localPlayer.health < 30) {
                    localPlayer.DamagePlayer(-2);
                } else if (localPlayer.health > 60 && localPlayer.health < 101) {
                    localPlayer.DamagePlayer(4);
                }
                break;
            case TornadoType.Windy:
                if (localPlayer.health > 50) {
                    localPlayer.DamagePlayer(1);
                } 
                break;
            case TornadoType.Smoke:
                // make the player screen a smokey mess.
                break;
            case TornadoType.Water:
                break;
            case TornadoType.Electric:
                // spawn lightning around
                if (localPlayer.health > 20) {
                    localPlayer.DamagePlayer(1);
                }
                break;
        }
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
            yield return new WaitForSeconds(0.5f);
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

    public bool TornadoHasLineOfSightToPosition(Vector3 pos, float width = 360, int range = 150, float proximityAwareness = 5f) {
        if (Vector3.Distance(eye.position, pos) < range) {
            if (!Physics.Raycast(eye.position, (pos - eye.position).normalized, Vector3.Distance(eye.position, pos), StartOfRound.Instance.collidersAndRoomMaskAndPlayers)) {
                Vector3 to = pos - eye.position;
                if (Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness) {
                    return true;
                }
            }
        }
        return false;
    }
    public Vector3 GetRandomTargetPosition(Random random, List<GameObject> nodes, float minX, float maxX, float minY, float maxY, float minZ, float maxZ, float radius) {
		try {
			var nextNode = random.NextItem(nodes);
			Vector3 position = nextNode.transform.position;

			position += new Vector3(random.NextFloat(minX, maxX), random.NextFloat(minY, maxY), random.NextFloat(minZ, maxZ));
			position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: position, radius: radius, randomSeed: random);
		    return position;
		} catch {
			Plugin.Logger.LogFatal("Selecting random position failed.");
			return new Vector3(0,0,0);
		}
	}
}