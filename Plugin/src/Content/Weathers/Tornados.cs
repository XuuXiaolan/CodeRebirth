using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.MiscScripts;
using Random = System.Random;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using System;
using WeatherRegistry;
using static CodeRebirth.src.Content.Weathers.Tornados;

namespace CodeRebirth.src.Content.Weathers;
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
    [SerializeField]
    private AudioSource yeetAudio = null!;
    
    [Space(5f)]
    [Header("Graphics")]
    [SerializeField]
    private ParticleSystem[] tornadoParticles = null!;

    private List<GameObject> outsideNodes = new List<GameObject>();
    private Vector3 origin;
    public Transform[] eyes = null!;
    public Transform throwingPoint = null!;
    private List<string> WhitelistedTornados = new List<string>();

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
    private Random tornadoRandom = new Random();
    private bool isDebugging = false;
    private float timeSinceBeingInsideTornado = 0;
    public static Tornados? Instance { get; private set; }

    private TornadoSelector tornadoSelector;

    public void OnEnable() {
        Instance = this;
        tornadoSelector = new TornadoSelector();
    }

    public void OnDisable() {
        Instance = null!;
    }

    [ClientRpc]
    public void SetupTornadoClientRpc(Vector3 origin) {
        outsideNodes = RoundManager.Instance.outsideAINodes.ToList();
        this.origin = origin;
        tornadoRandom = new Random(StartOfRound.Instance.randomMapSeed + 325);
        this.origin = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: origin, radius: 10f, randomSeed: tornadoRandom);
        this.transform.position = this.origin;

        // Use the TornadoSelector class to select the tornado type based on config
        int typeIndex = tornadoSelector.SelectTornadoIndex(Plugin.ModConfig.ConfigTornadoMoonWeatherTypes.Value);
        if (typeIndex < 0)
        {
            typeIndex = (int)TornadoType.Smoke;
            Plugin.Logger.LogWarning("Your config is poorly done. Tornado type index is out of bounds. Defaulting to Smoke tornado.");
        }
        this.tornadoType = (TornadoType)typeIndex;

        WhitelistedTornados = Plugin.ModConfig.ConfigTornadoCanFlyYouAwayWeatherTypes.Value.ToLower().Split(',').Select(s => s.Trim()).ToList();
        Plugin.ExtendedLogging($"Setting up tornado of type: {tornadoType} at {origin}");
        SetupTornadoType();
        UpdateAudio(); // Ensure audio works correctly on the first frame.
    }

    public override void Start() {
        base.Start();
#if DEBUG
        isDebugging = true;
#endif
        initialSpeed = Plugin.ModConfig.ConfigTornadoSpeed.Value;
        if (TornadoWeather.Instance != null) TornadoWeather.Instance.AddTornado(this);
        timeSinceBeingInsideTornado = 0;

        if (Vector3.Distance(this.transform.position, StartOfRound.Instance.shipBounds.transform.position) <= 20) {
            SetDestinationToPosition(ChooseFarthestNodeFromPosition(this.transform.position, avoidLineOfSight: false).position, true);
        }

        if (WeatherManager.GetCurrentWeather(StartOfRound.Instance.currentLevel) != WeatherHandler.Instance.TornadoesWeather) {
            Plugin.ExtendedLogging("Tornado spawned as an enemy?");
            outsideNodes = RoundManager.Instance.outsideAINodes.ToList();
            tornadoRandom = new Random(StartOfRound.Instance.randomMapSeed + 325);
            this.origin = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: Vector3.zero, radius: 100f, randomSeed: tornadoRandom);
            this.transform.position = this.origin;

            // Use the TornadoSelector class to select the tornado type based on config
            int typeIndex = tornadoSelector.SelectTornadoIndex(Plugin.ModConfig.ConfigTornadoMoonWeatherTypes.Value);
            if (typeIndex < 0)
            {
                typeIndex = (int)TornadoType.Smoke;
                Plugin.Logger.LogWarning("Your config is poorly done. Tornado type index is out of bounds. Defaulting to Smoke tornado.");
            }
            this.tornadoType = (TornadoType)typeIndex;

            WhitelistedTornados = Plugin.ModConfig.ConfigTornadoCanFlyYouAwayWeatherTypes.Value.ToLower().Split(',').Select(s => s.Trim()).ToList();
            Plugin.ExtendedLogging($"Setting up tornado of type: {tornadoType} at {origin}");
            SetupTornadoType();
            UpdateAudio(); // Ensure audio works correctly on the first frame.
        }
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

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerFlingingServerRpc(int PlayerID) {
        SetPlayerFlingingClientRpc(PlayerID);
    }

    [ClientRpc]
    public void SetPlayerFlingingClientRpc(int PlayerID) {
        if (PlayerID == -1) {
            Plugin.Logger.LogError("PlayerID is -1, this should never happen!");
            return;
        }
        StartOfRound.Instance.allPlayerScripts[PlayerID].GetCRPlayerData().flingingAway = true;
        StartCoroutine(AutoReEnableKinematicsAfter10Seconds(StartOfRound.Instance.allPlayerScripts[PlayerID]));
    }

    public IEnumerator AutoReEnableKinematicsAfter10Seconds(PlayerControllerB player) {
        yield return new WaitForSeconds(10f);
        player.playerRigidbody.isKinematic = true;
        player.GetCRPlayerData().flingingAway = false;
        player.GetCRPlayerData().flung = false;
    }
    public override void Update() {
        base.Update();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        
        var localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (TornadoConditionsAreMet(localPlayer) && Vector3.Distance(localPlayer.transform.position, this.transform.position) <= 10f && !localPlayer.GetCRPlayerData().flingingAway && (WhitelistedTornados.Contains(tornadoType.ToString().ToLower()) || WhitelistedTornados.Contains("all")) && tornadoType != TornadoType.Water) {
            timeSinceBeingInsideTornado = Mathf.Clamp(timeSinceBeingInsideTornado + Time.deltaTime, 0, 49f);
        } else if (Vector3.Distance(localPlayer.transform.position, this.transform.position) > 10f && !localPlayer.GetCRPlayerData().flingingAway) {
            timeSinceBeingInsideTornado = Mathf.Clamp(timeSinceBeingInsideTornado - Time.deltaTime, 0, 49f);
        }

        if (timeSinceBeingInsideTornado >= Plugin.ModConfig.ConfigTornadoInsideBeforeThrow.Value) {
            SetPlayerFlingingServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, localPlayer));
        }

        if (lightningBoltTimer && tornadoType == TornadoType.Electric) {
            lightningBoltTimer = false;
            StartCoroutine(LightningBoltTimer());
            Vector3 strikePosition = GetRandomTargetPosition(tornadoRandom, outsideNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25);
            CRUtilities.CreateExplosion(strikePosition, true, 20, 0, 4, 1, CauseOfDeath.Burning, null, null);
            LightningStrikeScript.SpawnLightningBolt(strikePosition);
        }
    }

    private void Init() {
        StartSearch(this.transform.position);
        agent.speed = initialSpeed;
    }

    private void FixedUpdate() {
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        UpdateAudio();
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
            HandleTornadoActions(player);   
            if (player.GetCRPlayerData().flingingAway && !player.GetCRPlayerData().flung) {
                //Plugin.Logger.LogDebug("Tornado is flinging away");
                Vector3 directionToCenter = (throwingPoint.position - player.transform.position).normalized;
                Rigidbody playerRigidbody = player.playerRigidbody;

                Vector3 spiralForce = CalculateSpiralForce(directionToCenter, 3f); // Adjust the second parameter to control spiral intensity

                playerRigidbody.AddForce(spiralForce, ForceMode.Impulse);

                if (player.transform.position.y >= throwingPoint.position.y) {
                    Vector3 verticalForce = CalculateVerticalForce(player.transform.position.y, throwingPoint.position.y, (float)tornadoRandom.NextDouble(10f, 50f), (float)tornadoRandom.NextDouble(1f, 20f)); // Adjust the last two parameters to control vertical and forward force
                    playerRigidbody.AddForce(verticalForce, ForceMode.VelocityChange);
                    if (player == GameNetworkManager.Instance.localPlayerController) timeSinceBeingInsideTornado = 0f;
                    if (Plugin.ModConfig.ConfigTornadoYeetSFX.Value) yeetAudio.Play();
                    player.GetCRPlayerData().flung = true;
                    player.GetCRPlayerData().flingingAway = false;
                }
            }
        }
    }

    private Vector3 CalculateSpiralForce(Vector3 directionToCenter, float spiralIntensity) {
        Vector3 spiralDirection = Vector3.Cross(directionToCenter, Vector3.up).normalized;
        return spiralDirection * spiralIntensity + directionToCenter * spiralIntensity;
    }

    private Vector3 CalculateVerticalForce(float playerY, float throwingPointY, float upwardForce, float forwardForce) {
        if (playerY >= throwingPointY) {
            return (Vector3.up * upwardForce + Vector3.forward * forwardForce).normalized;
        }
        return Vector3.zero;
    }

    private void HandleTornadoActions(PlayerControllerB player) {
        bool doesTornadoAffectPlayer = TornadoConditionsAreMet(player);
        if (isDebugging && player.currentlyHeldObjectServer != null && player.currentlyHeldObjectServer.itemProperties.itemName == "Key") {
            doesTornadoAffectPlayer = false;
        }
        if (doesTornadoAffectPlayer) {
            float distanceToTornado = Vector3.Distance(transform.position, player.transform.position);
            bool hasLineOfSight = TornadoHasLineOfSightToPosition();
            Vector3 directionToCenter = (transform.position - player.transform.position).normalized;
            if (player.GetCRPlayerData().flingingAway && player.playerRigidbody.isKinematic) {
                player.playerRigidbody.isKinematic = false;
                player.playerRigidbody.AddForce(Vector3.up * 10f, ForceMode.Impulse);
            } else if (distanceToTornado <= 75) {
                float forceStrength = CalculatePullStrength(distanceToTornado, hasLineOfSight, player);
                player.externalForces += directionToCenter * forceStrength * Time.fixedDeltaTime * 30f;
            }
        }
        HandleStatusEffects(player);
    }

    public bool TornadoConditionsAreMet(PlayerControllerB player) {
        return  !player.inVehicleAnimation && 
                !player.GetCRPlayerData().ridingHoverboard && 
                !StartOfRound.Instance.shipBounds.bounds.Contains(player.transform.position) && 
                !player.isInsideFactory && 
                player.isPlayerControlled && 
                !player.isPlayerDead && 
                !player.isInHangarShipRoom && 
                !player.inAnimationWithEnemy && 
                !player.enteringSpecialAnimation && 
                !player.isClimbingLadder;
    }
    private void HandleStatusEffects(PlayerControllerB player) {
        float closeRange = 10f;
        float midRange = 20f;
        float longRange = 30f;

        switch (tornadoType) {
            case TornadoType.Fire:
                ApplyFireStatusEffect(player, midRange);
                break;
            case TornadoType.Blood:
                ApplyBloodStatusEffect(player, closeRange);
                break;
            case TornadoType.Windy:
                ApplyWindyStatusEffect(player, 110f);
                break;
            case TornadoType.Smoke:
                ApplySmokeStatusEffect(player, longRange);
                break;
            case TornadoType.Water:
                ApplyWaterStatusEffect(player, closeRange);
                break;
            case TornadoType.Electric:
                ApplyElectricStatusEffect(player, midRange);
                break;
        }
    }

    private void ApplyFireStatusEffect(PlayerControllerB player, float range) {
        float distance = Vector3.Distance(player.transform.position, this.transform.position);
        if (distance < range && !player.GetCRPlayerData().Fire) {
            player.GetCRPlayerData().Fire = true;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Fire, true);
        } else if (distance >= range && player.GetCRPlayerData().Fire) {
            player.GetCRPlayerData().Fire = false;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Fire, false);
        }
    }

    private void ApplyBloodStatusEffect(PlayerControllerB player, float range) {
        float distance = Vector3.Distance(player.transform.position, this.transform.position);
        if (distance < range && !player.GetCRPlayerData().Blood) {
            player.GetCRPlayerData().Blood = true;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Blood, true);
        } else if (distance >= range && player.GetCRPlayerData().Blood) {
            player.GetCRPlayerData().Blood = false;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Blood, false);
        }
    }

    private void ApplyWindyStatusEffect(PlayerControllerB player, float range) {
        float distance = Vector3.Distance(player.transform.position, this.transform.position);
        if (distance < range && !player.GetCRPlayerData().Windy) {
            player.GetCRPlayerData().Windy = true;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Windy, true);
        } else if (distance >= range && player.GetCRPlayerData().Windy) {
            player.GetCRPlayerData().Windy = false;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Windy, false);
        }
    }

    private void ApplySmokeStatusEffect(PlayerControllerB player, float range) {
        float distance = Vector3.Distance(player.transform.position, this.transform.position);
        if (distance < range && !player.GetCRPlayerData().Smoke) {
            player.GetCRPlayerData().Smoke = true;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Smoke, true);
        } else if (distance >= range && player.GetCRPlayerData().Smoke) {
            player.GetCRPlayerData().Smoke = false;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Smoke, false);
        }
    }

    private void ApplyWaterStatusEffect(PlayerControllerB player, float range) {
        float distance = Vector3.Distance(player.transform.position, this.transform.position);
        if (distance < range && !player.GetCRPlayerData().Water && player.isUnderwater) {
            player.GetCRPlayerData().Water = true;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Water, true);
        } else if (distance >= range && player.GetCRPlayerData().Water) {
            player.GetCRPlayerData().Water = false;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Water, false);
        }
    }

    private void ApplyElectricStatusEffect(PlayerControllerB player, float range) {
        float distance = Vector3.Distance(player.transform.position, this.transform.position);
        if (distance < range && !player.GetCRPlayerData().Electric) {
            player.GetCRPlayerData().Electric = true;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Electric, true);
            originalPlayerSpeed = player.movementSpeed;
            player.movementSpeed *= 1.5f;
        } else if (distance >= range && player.GetCRPlayerData().Electric) {
            player.GetCRPlayerData().Electric = false;
            CodeRebirthPlayerManager.ChangeActiveStatus(player, CodeRebirthStatusEffects.Electric, false);
            player.movementSpeed = originalPlayerSpeed;
        }
    }

    private IEnumerator LightningBoltTimer() {
        yield return new WaitForSeconds(tornadoRandom.NextFloat(0f, 1f) * 16f);
        lightningBoltTimer = true;
    }

    private float CalculatePullStrength(float distance, bool hasLineOfSight, PlayerControllerB localPlayerController) {
        float maxDistance = 150f;
        float minStrength = 0;
        float maxStrength =
            (hasLineOfSight ? Plugin.ModConfig.ConfigTornadoPullStrength.Value : 0.125f * Plugin.ModConfig.ConfigTornadoPullStrength.Value)
            * (tornadoType == TornadoType.Smoke ? 1.25f : 1f)
            * (localPlayerController.GetCRPlayerData().Water ? 0.25f : 1f)
            * (tornadoType == TornadoType.Fire && localPlayerController.health <= 20 ? 0.25f : 1f);

        // Calculate the normalized distance and apply an exponential falloff
        float normalizedDistance = distance / maxDistance;
        float strengthFalloff = (1-normalizedDistance)*(1-normalizedDistance); // Use an exponential falloff for smoother results

        // Calculate the pull strength based on the falloff
        float pullStrength = Mathf.Lerp(minStrength, maxStrength, strengthFalloff);

        if (distance <= 2.5f && damageTimer && GameNetworkManager.Instance.localPlayerController == localPlayerController) {
            damageTimer = false;
            StartCoroutine(DamageTimer());
            HandleTornadoTypeDamage(localPlayerController);
        }
        
        return Mathf.Clamp(pullStrength, 0, maxStrength);
    }

    private void HandleTornadoTypeDamage(PlayerControllerB localPlayerController) {
        if (localPlayerController == null) return;
        switch (tornadoType) {
            case TornadoType.Fire:
                // make the screen a firey mess
                if (localPlayerController.health > 20) {
                    localPlayerController.DamagePlayer(3, causeOfDeath: CauseOfDeath.Burning);
                }
                break;
            case TornadoType.Blood:
                localPlayerController.DamagePlayer(Math.Clamp(tornadoRandom.NextInt(-5, 5), 0, 100));
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
        if (player.GetCRPlayerData().flingingAway) return false;
        foreach (Transform eye in eyes)
        {
            if (Physics.Linecast(eye.position, player.gameplayCamera.transform.position, StartOfRound.Instance.playersMask, queryTriggerInteraction: QueryTriggerInteraction.Ignore))
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

public class TornadoSelector
{
    private string currentMoonName = LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName; // Dummy current moon, replace with actual value

    private Dictionary<string, TornadoType> tornadoTypeMapping = new Dictionary<string, TornadoType>
    {
        { "Fire", TornadoType.Fire },
        { "Blood", TornadoType.Blood },
        { "Windy", TornadoType.Windy },
        { "Smoky", TornadoType.Smoke },
        { "Water", TornadoType.Water },
        { "Electric", TornadoType.Electric },
    };

    public int SelectTornadoIndex(string tornadoConfigString)
    {
        string tornadoTypeString = tornadoConfigString;
        string[] tornadoEntries = tornadoTypeString.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

        List<TornadoType> validTornadoTypes = new List<TornadoType>();

        foreach (var entry in tornadoEntries)
        {
            string[] parts = entry.Split(':');
            string tornadoName = parts[0].Trim();
            string moonConditions = parts[1].Trim();

            if (tornadoTypeMapping.ContainsKey(tornadoName))
            {
                TornadoType tornadoType = tornadoTypeMapping[tornadoName];

                // Split moonConditions by comma and loop through them
                string[] moonConditionsArray = moonConditions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(m => m.Trim())
                                                            .ToArray();

                foreach (string moonCondition in moonConditionsArray)
                {
                    if (IsValidForMoon(moonCondition))
                    {
                        validTornadoTypes.Add(tornadoType);
                        break; // No need to continue checking if one condition matches
                    }
                }
            }
        }

        if (validTornadoTypes.Count > 0)
        {
            Random rand = new Random();
            int randomIndex = rand.Next(validTornadoTypes.Count);
            return (int)validTornadoTypes[randomIndex];
        }

        return -1; // Or another default value indicating no valid tornado types found
    }

    private bool IsValidForMoon(string moonCondition)
    {
        if (moonCondition == "All")
        {
            return true;
        }
        else if ((moonCondition == "Vanilla" && LethalLevelLoader.PatchedContent.VanillaExtendedLevels.Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel))) || (moonCondition == "Custom" && LethalLevelLoader.PatchedContent.CustomExtendedLevels.Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel))) || (moonCondition == "Custom"))
        {
            // Dummy logic for Vanilla or Custom
            return true;
        }
        else
        {
            // MoonName logic, for now just checks if the current moon matches the condition
            return currentMoonName.Equals(moonCondition, StringComparison.OrdinalIgnoreCase);
        }
    }
}