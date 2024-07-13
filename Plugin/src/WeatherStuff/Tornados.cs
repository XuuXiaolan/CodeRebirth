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
public class Tornados : EnemyAI
{
    [Header("Properties")]
    [SerializeField]
    private float initialSpeed = 5f;
    [Space(5f)]
    [Header("Audio")]
    [SerializeField]
    private AudioSource normalTravelAudio = null!, closeTravelAudio = null!;
    [Space(5f)]
    [Header("Graphics")]
    [SerializeField]
    private ParticleSystem[] tornadoParticles = null!;


    private List<GameObject> outsideNodes = new List<GameObject>();
    private Vector3 origin;
    private PlayerControllerB? localPlayerController;
    private CodeRebirthPlayerManager? localPlayerManager;
    private int iterations = 0;

    public enum TornadoType
    {
        Fire = 1,
        Blood = 2,
        Windy = 3,
        Smoke = 4,
        Water = 5,
        Electric = 6,
    }
    private TornadoType tornadoType = TornadoType.Fire;
    private bool damageTimer = true;
    private float originalPlayerSpeed = 0;
    private bool lightningBoltTimer = true;
    private Random random = new Random();

    [ClientRpc]
    public void SetupTornadoClientRpc(Vector3 origin, int typeIndex) {
        this.origin = origin;
        random = new Random(StartOfRound.Instance.randomMapSeed + 325);
        this.origin = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: origin, radius: 10f, randomSeed: random);
        this.transform.position = this.origin;
        this.tornadoType = (TornadoType)typeIndex;
        Plugin.Logger.LogInfo($"Setting up tornado of type: {tornadoType} at {origin}");
        SetupTornadoType();
        UpdateAudio(); // Make sure audio works correctly on the first frame.
    }

    private void Awake() {
        if (TornadoWeather.Instance != null) TornadoWeather.Instance.AddTornado(this);
        localPlayerController = GameNetworkManager.Instance.localPlayerController;
        localPlayerManager = localPlayerController.gameObject.GetComponent<CodeRebirthPlayerManager>();
    }

    private void SetupTornadoType() {
        int i = 0;
        switch (tornadoType) {
            case TornadoType.Fire:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Fire")) {
                        particleSystem.gameObject.SetActive(true);
                        i++;
                    }
                    if (i == 7) break;
                }
                break;
            case TornadoType.Blood:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Blood")) {
                        particleSystem.gameObject.SetActive(true);   
                        i++;
                    }
                    if (i == 7) break;
                }
                break;
            case TornadoType.Windy:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Wind")) {
                        particleSystem.gameObject.SetActive(true);   
                        i++;
                    }
                    if (i == 7) break;
                }
                break;
            case TornadoType.Smoke:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Smoke")) {
                        particleSystem.gameObject.SetActive(true);   
                        i++;
                    }
                    if (i == 7) break;
                }
                break;
            case TornadoType.Water:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Water")) {
                        particleSystem.gameObject.SetActive(true);   
                        i++;
                    }
                    if (i == 7) break;
                }
                initialSpeed /= 2;
                break;
            case TornadoType.Electric:
                foreach (ParticleSystem particleSystem in tornadoParticles) {
                    if (particleSystem.gameObject.name.Contains("Electric")) {
                        particleSystem.gameObject.SetActive(true);   
                        i++;
                    }
                    if (i == 7) {
                        initialSpeed *= 2;
                        break;
                    }
                }
                break;
        }
        Init();
    }
    private void Init() {
        StartSearch(this.transform.position);
        agent.speed = initialSpeed;
    }
    private void FixedUpdate() {
        UpdateAudio();
    }

    public override void DoAIInterval() {
        base.DoAIInterval();
        if (localPlayerController == null || localPlayerManager == null || isEnemyDead || StartOfRound.Instance.allPlayersDead || iterations > 14400) return;
        HandleTornadoActions(localPlayerManager, localPlayerController);
        iterations++;
    }
    private void HandleTornadoActions(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController) {
        bool doesTornadoAffectPlayer = !localPlayerController.inVehicleAnimation && !localPlayerManager.ridingHoverboard && !StartOfRound.Instance.shipBounds.bounds.Contains(localPlayerController.transform.position) && !localPlayerController.isInsideFactory && localPlayerController.isPlayerControlled && !localPlayerController.isPlayerDead && !localPlayerController.isInHangarShipRoom && !localPlayerController.inAnimationWithEnemy && !localPlayerController.enteringSpecialAnimation && !localPlayerController.isClimbingLadder;
        if (doesTornadoAffectPlayer) {
            float distanceToTornado = Vector3.Distance(transform.position, localPlayerController.transform.position);
            bool hasLineOfSight = TornadoHasLineOfSightToPosition();
            Vector3 directionToCenter = (transform.position - localPlayerController.transform.position).normalized;
            float forceStrength = CalculatePullStrength(distanceToTornado, hasLineOfSight);
            localPlayerController.externalForces += directionToCenter * forceStrength;
        }
        // All of these should activate some sort of particle effect on the player.
        switch (tornadoType) {
            case TornadoType.Fire:
                if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) < 30 && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Fire]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Fire] = true;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Fire, true);
                } else if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) >= 30 && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Fire]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Fire] = false;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Fire, false);
                }
                break;
            case TornadoType.Blood:
                if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) < 20 && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Blood]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Blood] = true;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Blood, true);
                } else if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) >= 20 && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Blood]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Blood] = false;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Blood, false);
                }
                break;
            case TornadoType.Windy:
                if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) < 20 && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Windy]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Windy] = true;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Windy, true);
                } else if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) >= 20 && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Windy]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Windy] = false;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Windy, false);
                }
                break;
            case TornadoType.Smoke:
                if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) < 20 && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Smoke]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Smoke] = true;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Smoke, true);
                } else if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) >= 20 && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Smoke]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Smoke] = false;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Smoke, false);
                }
                break;
            case TornadoType.Water:
                if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) < 10 && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water] = true;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Water, true);
                    localPlayerController.isMovementHindered++;
			        localPlayerController.hinderedMultiplier *= 3f; 
                } else if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) >= 10 && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water] = false;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Water, false);
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
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Electric, true);
                    originalPlayerSpeed = localPlayerController.movementSpeed;
                    localPlayerController.movementSpeed *= 1.3f;
                } else if (Vector3.Distance(localPlayerController.transform.position, this.transform.position) >= 10 && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric]) {
                    localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric] = false;
                    localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Electric, false);
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
        float maxDistance = 75f + (tornadoType == TornadoType.Windy ? 25f : 0f);
        float minStrength = 0;
        float maxStrength = (hasLineOfSight ? 30f : 2f) * (tornadoType == TornadoType.Windy ? 1.5f : 1f);

        // Calculate exponential strength based on distance
        float normalizedDistance = (maxDistance - distance) / maxDistance;
        if (distance <= 2.5f && damageTimer) {
            damageTimer = false;
            StartCoroutine(DamageTimer());
            HandleTornadoTypeDamage();
        }
        return Mathf.Clamp(Mathf.Lerp(minStrength, maxStrength, normalizedDistance * normalizedDistance), 0, maxStrength);
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
        } else if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed) {
            normalTravelAudio.volume = Plugin.ModConfig.ConfigTornadoDefaultVolume.Value * Plugin.ModConfig.ConfigTornadoInShipVolume.Value;
            closeTravelAudio.volume = Plugin.ModConfig.ConfigTornadoDefaultVolume.Value * Plugin.ModConfig.ConfigTornadoInShipVolume.Value;
        } else {
            normalTravelAudio.volume = Plugin.ModConfig.ConfigTornadoDefaultVolume.Value;
            closeTravelAudio.volume = Plugin.ModConfig.ConfigTornadoDefaultVolume.Value;
        }
    }

    public bool TornadoHasLineOfSightToPosition(int range = 100) {
        return CheckLineOfSightForPlayer(360, range, 5);
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