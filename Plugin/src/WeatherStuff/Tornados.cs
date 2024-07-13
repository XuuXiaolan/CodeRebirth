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
using AmazingAssets.TerrainToMesh;
using CodeRebirth.Misc;
using CodeRebirth.Util.Spawning;
using Mono.Cecil.Cil;
using UnityEngine.Rendering.VirtualTexturing;
using CodeRebirth.src.EnemyStuff;
using Imperium;

namespace CodeRebirth.WeatherStuff;
public class Tornados : EnemyAI
{
    private SimpleWanderRoutine currentWander = null!;
    private Coroutine wanderCoroutine = null!;

    [Header("Properties")]
    [SerializeField]
    private float initialSpeed = 5f;
    [SerializeField]
    private BoxCollider waterDrownCollider = null!;

    [Space(5f)]
    [Header("Audio")]
    [SerializeField]
    private AudioSource normalTravelAudio = null!;
    [SerializeField]
    private AudioSource closeTravelAudio = null!;
    
    [Space(5f)]
    [Header("Graphics")]
    [SerializeField]
    private ParticleSystem[] tornadoParticles = null!;


    private List<GameObject> outsideNodes = new List<GameObject>();
    private Vector3 origin;
    private PlayerControllerB? localPlayerController;
    private CodeRebirthPlayerManager? localPlayerManager;

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
    [SerializeField]
    private Material sphereMaterial = null!; // Reference to the material for the sphere (make sure to assign this in the inspector)

    private float debugSphereRadius = 60f; // Radius for the debug sphere
    private Random random = new Random();

    [ClientRpc]
    public void SetupTornadoClientRpc(Vector3 origin, int typeIndex) {
        outsideNodes = RoundManager.Instance.outsideAINodes.ToList();
        this.origin = origin;
        random = new Random(StartOfRound.Instance.randomMapSeed + 325);
        this.origin = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: origin, radius: 10f, randomSeed: random);
        this.transform.position = this.origin;
        this.tornadoType = (TornadoType)typeIndex;
        Plugin.Logger.LogInfo($"Setting up tornado of type: {tornadoType} at {origin}");
        SetupTornadoType();
        UpdateAudio(); // Make sure audio works correctly on the first frame.
    }

    public override void Start() {
        base.Start();
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
        if (localPlayerController == null || localPlayerManager == null || isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        HandleTornadoActions(localPlayerManager, localPlayerController);
    }

    private void HandleTornadoActions(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController) {
        bool doesTornadoAffectPlayer = !localPlayerController.inVehicleAnimation && 
                                        !localPlayerManager.ridingHoverboard && 
                                        !StartOfRound.Instance.shipBounds.bounds.Contains(localPlayerController.transform.position) && 
                                        !localPlayerController.isInsideFactory && 
                                        localPlayerController.isPlayerControlled && 
                                        !localPlayerController.isPlayerDead && 
                                        !localPlayerController.isInHangarShipRoom && 
                                        !localPlayerController.inAnimationWithEnemy && 
                                        !localPlayerController.enteringSpecialAnimation && 
                                        !localPlayerController.isClimbingLadder;
        if (localPlayerController.currentlyHeldObjectServer != null && localPlayerController.currentlyHeldObjectServer.itemProperties.itemName == "Key") {
            doesTornadoAffectPlayer = false;
        }
        if (doesTornadoAffectPlayer) {
            float distanceToTornado = Vector3.Distance(transform.position, localPlayerController.transform.position);
            bool hasLineOfSight = TornadoHasLineOfSightToPosition();
            Vector3 directionToCenter = (transform.position - localPlayerController.transform.position).normalized;
            float forceStrength = CalculatePullStrength(distanceToTornado, hasLineOfSight);
            localPlayerController.externalForces += directionToCenter * forceStrength;
        }

        HandleStatusEffects(localPlayerManager, localPlayerController);
    }

    private void HandleStatusEffects(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController) {
        float closeRange = 10f;
        float midRange = 20f;
        float longRange = 30f;

        switch (tornadoType) {
            case TornadoType.Fire:
                ApplyFireStatusEffect(localPlayerManager, localPlayerController, midRange);
                break;
            case TornadoType.Blood:
                ApplyBloodStatusEffect(localPlayerManager, localPlayerController, closeRange);
                break;
            case TornadoType.Windy:
                ApplyWindyStatusEffect(localPlayerManager, localPlayerController, longRange);
                break;
            case TornadoType.Smoke:
                ApplySmokeStatusEffect(localPlayerManager, localPlayerController, longRange);
                break;
            case TornadoType.Water:
                ApplyWaterStatusEffect(localPlayerManager, localPlayerController, closeRange);
                break;
            case TornadoType.Electric:
                ApplyElectricStatusEffect(localPlayerManager, localPlayerController, midRange);
                break;
        }
    }

    private void ApplyFireStatusEffect(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController, float range) {
        float distance = Vector3.Distance(localPlayerController.transform.position, this.transform.position);
        if (distance < range && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Fire]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Fire] = true;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Fire, true);
        } else if (distance >= range && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Fire]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Fire] = false;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Fire, false);
        }
    }

    private void ApplyBloodStatusEffect(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController, float range) {
        float distance = Vector3.Distance(localPlayerController.transform.position, this.transform.position);
        if (distance < range && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Blood]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Blood] = true;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Blood, true);
        } else if (distance >= range && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Blood]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Blood] = false;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Blood, false);
        }
    }

    private void ApplyWindyStatusEffect(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController, float range) {
        float distance = Vector3.Distance(localPlayerController.transform.position, this.transform.position);
        if (distance < range && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Windy]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Windy] = true;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Windy, true);
        } else if (distance >= range && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Windy]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Windy] = false;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Windy, false);
        }
    }

    private void ApplySmokeStatusEffect(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController, float range) {
        float distance = Vector3.Distance(localPlayerController.transform.position, this.transform.position);
        if (distance < range && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Smoke]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Smoke] = true;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Smoke, true);
        } else if (distance >= range && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Smoke]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Smoke] = false;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Smoke, false);
        }
    }

    private void ApplyWaterStatusEffect(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController, float range) {
        float distance = Vector3.Distance(localPlayerController.transform.position, this.transform.position);
        if (distance < range && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water] = true;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Water, true);
            localPlayerController.isMovementHindered++;
            localPlayerController.hinderedMultiplier *= 3f;
        } else if (distance >= range && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water] = false;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Water, false);
            localPlayerController.isMovementHindered = Mathf.Clamp(localPlayerController.isMovementHindered - 1, 0, 1000);
            localPlayerController.hinderedMultiplier /= 3f;
        }
    }

    private void ApplyElectricStatusEffect(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController, float range) {
        if (lightningBoltTimer) {
            lightningBoltTimer = false;
            StartCoroutine(LightningBoltTimer());
            Vector3 strikePosition = GetRandomTargetPosition(random, outsideNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25);
            Misc.Utilities.CreateExplosion(strikePosition, true, 20, 0, 4, 1, CauseOfDeath.Burning, null, null);
            LightningStrikeScript.SpawnLightningBolt(strikePosition);
        }

        float distance = Vector3.Distance(localPlayerController.transform.position, this.transform.position);
        if (distance < range && !localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric] = true;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Electric, true);
            originalPlayerSpeed = localPlayerController.movementSpeed;
            localPlayerController.movementSpeed *= 1.5f;
        } else if (distance >= range && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Electric] = false;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Electric, false);
            localPlayerController.movementSpeed = originalPlayerSpeed;
        }
    }

    private IEnumerator LightningBoltTimer() {
        yield return new WaitForSeconds(random.NextFloat(0f, 1f) * 8);
        lightningBoltTimer = true;
    }
    private float CalculatePullStrength(float distance, bool hasLineOfSight) {
        float maxDistance = 75f + (tornadoType == TornadoType.Smoke ? 25f : 0f);
        float minStrength = 0;
        float maxStrength = (hasLineOfSight ? 30f : 5f) * (tornadoType == TornadoType.Smoke ? 1.5f : 1f);

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

    public void LateUpdate()
    {
        if (!Plugin.ImperiumIsOn || !Plugin.ModConfig.ConfigEnableImperiumDebugs.Value || StartOfRound.Instance == null) return;
        Plugin.Logger.LogInfo("Drawing sphere OLD");
        if (eye != null && sphereMaterial != null)
        {
            Plugin.Logger.LogInfo("Drawing sphere");
            Imperium.API.Visualization.DrawSphere(this.gameObject, this.eye, 60, this.sphereMaterial, Imperium.API.Visualization.GizmoType.Custom, null, null);
        }
    }
    
    public bool TornadoHasLineOfSightToPosition(int range = 100)
    {
        // Define the layer mask to use for the sphere overlap and linecast checks
        LayerMask mask = StartOfRound.Instance.collidersAndRoomMaskAndDefault;

        // Get all colliders within the specified range using a sphere overlap check
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range, mask);

        // Iterate through all hit colliders to check for players
        foreach (var hitCollider in hitColliders)
        {
            // Check if the hit collider is a player
            var player = hitCollider.GetComponent<PlayerControllerB>();
            if (player != null && player.isPlayerControlled)
            {
                // Perform a linecast check to see if there is a clear line of sight to the player
                if (!Physics.Linecast(transform.position, player.transform.position, mask))
                {
                    // If there is a clear line of sight to at least one player, return true
                    return true;
                }
            }
        }

        // If no players were found or there was no clear line of sight to any player, return false
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