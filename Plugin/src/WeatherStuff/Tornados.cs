using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.Misc.LightningScript;
using Random = System.Random;
using System.Collections.Generic;
using CodeRebirth.Util.PlayerManager;
using CodeRebirth.Util.Extensions;
using System;
using CodeRebirth.Patches;

namespace CodeRebirth.WeatherStuff;
public class Tornados : EnemyAI
{

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
    public Transform[] eyes = null!;
    public Transform throwingPoint = null!;

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

    private Random tornadoRandom = new Random();
    private bool isDebugging = false;
    private float timeSinceBeingInsideTornado = 0;
    public static Tornados? Instance { get; private set; }

    public void OnEnable() {
        Instance = this;
    }

    public void OnDisable() {
        Instance = null!;
    }

    [ClientRpc]
    public void SetupTornadoClientRpc(Vector3 origin, int typeIndex) {
        outsideNodes = RoundManager.Instance.outsideAINodes.ToList();
        this.origin = origin;
        tornadoRandom = new Random(StartOfRound.Instance.randomMapSeed + 325);
        this.origin = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: origin, radius: 10f, randomSeed: tornadoRandom);
        this.transform.position = this.origin;
        this.tornadoType = (TornadoType)typeIndex;
        Plugin.Logger.LogInfo($"Setting up tornado of type: {tornadoType} at {origin}");
        SetupTornadoType();
        UpdateAudio(); // Make sure audio works correctly on the first frame.
    }

    public override void Start() {
        base.Start();
#if DEBUG
        isDebugging = true;
#endif
        if (TornadoWeather.Instance != null) TornadoWeather.Instance.AddTornado(this);
        timeSinceBeingInsideTornado = 0;
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
                waterDrownCollider.gameObject.SetActive(true);
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

    public override void Update() {
        base.Update();
        if (localPlayerController == null || localPlayerManager == null || isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        HandleTornadoActions(localPlayerManager, localPlayerController);
        if (PlayerControllerBPatch.TornadoKinematicPlayerManager != null) Plugin.Logger.LogInfo("Tornado is flinging away: " + PlayerControllerBPatch.TornadoKinematicPlayerManager.flingingAway.ToString());
        if (PlayerControllerBPatch.TornadoKinematicPlayer != null) Plugin.Logger.LogInfo("Tornado is flinging player: " + PlayerControllerBPatch.TornadoKinematicPlayer);
        if (PlayerControllerBPatch.TornadoKinematicPlayerManager != null) Plugin.Logger.LogInfo("Tornado is flinging player manager: " + PlayerControllerBPatch.TornadoKinematicPlayerManager);
        Plugin.Logger.LogInfo("Tornado patch: " + PlayerControllerBPatch.TornadoKinematicPatch);
        if (PlayerControllerBPatch.TornadoKinematicPatch && PlayerControllerBPatch.TornadoKinematicPlayer != null && PlayerControllerBPatch.TornadoKinematicPlayerManager != null && PlayerControllerBPatch.TornadoKinematicPlayerManager.flingingAway) {
            Plugin.Logger.LogInfo("Tornado is flinging away");
            Vector3 directionToCenter = (throwingPoint.position - PlayerControllerBPatch.TornadoKinematicPlayer.transform.position).normalized;
            float spiralForce = 125f; // Adjust this value to control the spiral intensity
            Vector3 spiralDirection = Vector3.Cross(directionToCenter, Vector3.up).normalized;
            Rigidbody playerRigidbody = PlayerControllerBPatch.TornadoKinematicPlayer.playerRigidbody;
            
            playerRigidbody.AddForce(spiralDirection * spiralForce + directionToCenter * spiralForce, ForceMode.Acceleration);

            if (PlayerControllerBPatch.TornadoKinematicPlayer.transform.position.y >= throwingPoint.position.y) {
                playerRigidbody.AddForce(Vector3.up * 150f + Vector3.forward * 150f, ForceMode.VelocityChange);
                timeSinceBeingInsideTornado = 0f;
                if (GameNetworkManager.Instance.localPlayerController == PlayerControllerBPatch.TornadoKinematicPlayer) SetPlayerFlingingAwayServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, localPlayerController), false);
                // restore kinematic etc after some point
            }
        }
        if (TornadoConditionsAreMet() && Vector3.Distance(localPlayerController.transform.position, this.transform.position) < 10f && !localPlayerManager.flingingAway) {
            timeSinceBeingInsideTornado = Mathf.Clamp(timeSinceBeingInsideTornado + Time.deltaTime, 0, 20f);
        } else if (!localPlayerManager.flingingAway) {
            timeSinceBeingInsideTornado = Mathf.Clamp(timeSinceBeingInsideTornado - Time.deltaTime, 0, 20f);
        }

        if (timeSinceBeingInsideTornado >= 5f) {
            SetPlayerFlingingAwayServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, localPlayerController), true);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerFlingingAwayServerRpc(int playerID, bool flingingAway) {
        SetPlayerFlingingAwayClientRpc(playerID, flingingAway);
    }

    [ClientRpc]
    private void SetPlayerFlingingAwayClientRpc(int playerID, bool flingingAway) {
        StartOfRound.Instance.allPlayerScripts[playerID].GetComponent<CodeRebirthPlayerManager>().flingingAway = flingingAway;
    }

    private void Init() {
        StartSearch(this.transform.position);
        agent.speed = initialSpeed;
    }

    private void FixedUpdate() {
        UpdateAudio();
    }

    private void HandleTornadoActions(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController) {
        bool doesTornadoAffectPlayer = TornadoConditionsAreMet();
        if (isDebugging && localPlayerController.currentlyHeldObjectServer != null && localPlayerController.currentlyHeldObjectServer.itemProperties.itemName == "Key") {
            doesTornadoAffectPlayer = false;
        }
        if (doesTornadoAffectPlayer) {
            float distanceToTornado = Vector3.Distance(transform.position, localPlayerController.transform.position);
            bool hasLineOfSight = TornadoHasLineOfSightToPosition();
            Vector3 directionToCenter = (transform.position - localPlayerController.transform.position).normalized;
            if (localPlayerManager.flingingAway && !PlayerControllerBPatch.TornadoKinematicPatch) {
                HandleFlingingPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, localPlayerController));
            } else if (distanceToTornado <= 75) {
                float forceStrength = CalculatePullStrength(distanceToTornado, hasLineOfSight);
                localPlayerController.externalForces += directionToCenter * forceStrength;
            }
        }

        HandleStatusEffects(localPlayerManager, localPlayerController);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleFlingingPlayerServerRpc(int playerID) {
        HandleFlingingPlayerClientRpc(playerID);
    }
    [ClientRpc]
    private void HandleFlingingPlayerClientRpc(int playerID) {
        if (StartOfRound.Instance.allPlayerScripts[playerID] == null) return;
        PlayerControllerB localPlayerControllerB = StartOfRound.Instance.allPlayerScripts[playerID];
        Rigidbody playerRigidbody = localPlayerControllerB.playerRigidbody;
        if (playerRigidbody == null) return;

        playerRigidbody.isKinematic = false;
        playerRigidbody.AddForce(Vector3.up * 10f, ForceMode.Acceleration);
        PlayerControllerBPatch.TornadoKinematicPlayer = localPlayerControllerB;
        PlayerControllerBPatch.TornadoKinematicPlayerManager = localPlayerControllerB.GetComponent<CodeRebirthPlayerManager>();
        PlayerControllerBPatch.TornadoKinematicPatch = true;
        // Disable Rigidbody and store mass
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetPlayerRigidBodyStuffServerRpc(int playerID) {
        ResetPlayerRigidBodyStuffClientRpc(playerID);
    }

    [ClientRpc]
    public void ResetPlayerRigidBodyStuffClientRpc(int playerID) {
        StartOfRound.Instance.allPlayerScripts[playerID].GetComponent<CodeRebirthPlayerManager>().flingingAway = false;
        PlayerControllerBPatch.TornadoKinematicPatch = false;
        StartOfRound.Instance.allPlayerScripts[playerID].playerRigidbody.isKinematic = true;
        PlayerControllerBPatch.TornadoKinematicPlayer = null;
    }

    public bool TornadoConditionsAreMet() {
        return  !localPlayerController!.inVehicleAnimation && 
                !localPlayerManager!.ridingHoverboard && 
                !StartOfRound.Instance.shipBounds.bounds.Contains(localPlayerController.transform.position) && 
                !localPlayerController.isInsideFactory && 
                localPlayerController.isPlayerControlled && 
                !localPlayerController.isPlayerDead && 
                !localPlayerController.isInHangarShipRoom && 
                !localPlayerController.inAnimationWithEnemy && 
                !localPlayerController.enteringSpecialAnimation && 
                !localPlayerController.isClimbingLadder;
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
                ApplyWindyStatusEffect(localPlayerManager, localPlayerController, 110f);
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
        } else if (distance >= range && localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water]) {
            localPlayerManager.statusEffects[CodeRebirthStatusEffects.Water] = false;
            localPlayerManager.ChangeActiveStatus(CodeRebirthStatusEffects.Water, false);
        }
    }

    private void ApplyElectricStatusEffect(CodeRebirthPlayerManager localPlayerManager, PlayerControllerB localPlayerController, float range) {
        if (lightningBoltTimer) {
            lightningBoltTimer = false;
            StartCoroutine(LightningBoltTimer());
            Vector3 strikePosition = GetRandomTargetPosition(tornadoRandom, outsideNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25);
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
        yield return new WaitForSeconds(tornadoRandom.NextFloat(0f, 1f) * 8);
        lightningBoltTimer = true;
    }

    private float CalculatePullStrength(float distance, bool hasLineOfSight) {
        float maxDistance = 75f + (tornadoType == TornadoType.Smoke ? 25f : 0f);
        float minStrength = 0;
        float maxStrength = (hasLineOfSight ? 30f : 5f) * (tornadoType == TornadoType.Smoke ? 1.5f : 1f) * (localPlayerManager!.statusEffects[CodeRebirthStatusEffects.Water] ? 0.7f : 1f) * (localPlayerManager.flingingAway ? 5f : 1f);

        // Calculate the normalized distance and apply an exponential falloff
        float normalizedDistance = distance / maxDistance;
        float strengthFalloff = (1-normalizedDistance)*(1-normalizedDistance); // Use an exponential falloff for smoother results

        // Calculate the pull strength based on the falloff
        float pullStrength = Mathf.Lerp(minStrength, maxStrength, strengthFalloff);

        if (distance <= 2.5f && damageTimer) {
            damageTimer = false;
            StartCoroutine(DamageTimer());
            HandleTornadoTypeDamage();
        }

        return Mathf.Clamp(pullStrength, 0, maxStrength);
    }

    private void HandleTornadoTypeDamage() {
        if (localPlayerController == null || localPlayerManager == null) return;
        switch (tornadoType) {
            case TornadoType.Fire:
                // make the screen a firey mess
                if (localPlayerController.health > 20) {
                    localPlayerController.DamagePlayer(3);
                }
                break;
            case TornadoType.Blood:
                localPlayerController.DamagePlayer(Math.Clamp(tornadoRandom.Next(-5, 6), 0, 100));
                break;
            case TornadoType.Windy:
                break;
            case TornadoType.Smoke:
            localPlayerController.DamagePlayer(2);
                // make the player screen a smokey mess.
                break;
            case TornadoType.Water:
                break;
            case TornadoType.Electric:
                // spawn lightning around
                if (localPlayerController.health > 20) {
                    localPlayerController.DamagePlayer(1);
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
        if (!Plugin.ImperiumIsOn || !Plugin.ModConfig.ConfigEnableImperiumDebugs.Value) return;
        if (eye != null && sphereMaterial != null)
        {
            //Imperium.API.Visualization.DrawSphere(this.gameObject, this.eye, 60, this.sphereMaterial, Imperium.API.Visualization.GizmoType.Custom, null, null);
        }
    }
    
    public bool TornadoHasLineOfSightToPosition(int range = 100)
    {
        // Get all colliders within the specified range using a sphere overlap check
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range, StartOfRound.Instance.playersMask);

        // Iterate through all hit colliders to check for players
        foreach (var hitCollider in hitColliders)
        {
            return CheckIfAPlayerIsAGivenCollider(hitCollider);
        }
        // If no players were found or there was no clear line of sight to any player, return false
        return false;
    }

    private bool CheckIfAPlayerIsAGivenCollider(Collider hitCollider) {
        // Check if the hit collider is a player by iterating through the list of all player scripts
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (hitCollider.gameObject == player.gameObject && player.isPlayerControlled)
            {
                // Perform a linecast check to see if there is a clear line of sight to the player
                if (CheckIfPlayerSeenByTornado(player)) {
                    return true;
                }
            }
        }
        return false;
    }
    public bool CheckIfPlayerSeenByTornado(PlayerControllerB player) {
        if (localPlayerManager != null && localPlayerManager.flingingAway) return false;
        foreach (Transform eye in eyes)
        {
            if (Physics.Linecast(eye.position, player.gameplayCamera.transform.position, StartOfRound.Instance.playersMask))
            {
                // If there is a clear line of sight to at least one player, return true
                return true;
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