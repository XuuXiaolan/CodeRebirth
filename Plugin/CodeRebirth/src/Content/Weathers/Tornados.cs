using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using CodeRebirth.src.MiscScripts;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using System;
using static CodeRebirth.src.Content.Weathers.Tornados;
using static CodeRebirth.src.Util.PlayerControllerBExtensions;
using CodeRebirth.src.Content.Enemies;
using static UnityEngine.ParticleSystem;

namespace CodeRebirth.src.Content.Weathers;
public class Tornados : CodeRebirthEnemyAI
{
    [Header("Properties")]
    public float initialSpeed = 5f;
    public BoxCollider waterDrownCollider = null!;

    [Space(5f)]
    [Header("Audio")]
    public AudioSource normalTravelAudio = null!;
    public AudioSource closeTravelAudio = null!;
    public AudioSource lightningSource = null!;
    
    [Space(5f)]
    [Header("Graphics")]
    public ParticleSystem[] tornadoParticles = null!;
    public Material mainMaterial = null!;

    private GameObject[] outsideNodes = [];
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
    private System.Random tornadoRandom = new System.Random();
    private float timeSinceBeingInsideTornado = 0;
    private Dictionary<TornadoType, Color> tornadoTypeColourMapping = new();

    public override void Start()
    {
        base.Start();
        tornadoTypeColourMapping = new()
        {
            { TornadoType.Fire, new Color(1, 54/255, 0, 1)},
            { TornadoType.Blood, new Color(238/255, 0, 0, 1)},
            { TornadoType.Windy, new Color(42/255, 1, 0, 1)},
            { TornadoType.Smoke, new Color(1, 1, 1, 1)},
            { TornadoType.Water, new Color(0, 169/255, 1, 1)},
            { TornadoType.Electric, new Color(1, 1, 118/255, 1)},
        };
        tornadoRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 325);
        initialSpeed = Plugin.ModConfig.ConfigTornadoSpeed.Value;
        outsideNodes = RoundManager.Instance.outsideAINodes;
        int typeIndex = new TornadoSelector().SelectTornadoIndex(Plugin.ModConfig.ConfigTornadoMoonWeatherTypes.Value);
        tornadoType = (TornadoType)typeIndex;

        Plugin.ExtendedLogging($"Setting up tornado of type: {tornadoType} at {transform.position}");
        SetupTornadoType();
    }

    private void SetupTornadoType()
    {
        foreach (var particleSystem in tornadoParticles)
        {
            var main = particleSystem.main;
            main.startColor = tornadoTypeColourMapping[tornadoType];
        }
        Plugin.ExtendedLogging($"Setting up tornado of type: {tornadoType} with previous color {mainMaterial.GetColor("_BaseColor")} into new color {tornadoTypeColourMapping[tornadoType]}");
        mainMaterial.SetColor("_BaseColor", tornadoTypeColourMapping[tornadoType]);
        switch (tornadoType)
        {
            case TornadoType.Fire:
                break;
            case TornadoType.Blood:
                break;
            case TornadoType.Windy:
                break;
            case TornadoType.Smoke:
                break;
            case TornadoType.Water:
                waterDrownCollider.gameObject.SetActive(true);
                initialSpeed /= 2;
                break;
            case TornadoType.Electric:
                lightningSource.gameObject.SetActive(true);
                StartCoroutine(LightningBoltTimer());
                initialSpeed *= 2;

                break;
        }
        Init();
    }

    private void Init()
    {
        smartAgentNavigator.StartSearchRoutine(eye.transform.position, 40f);
        agent.speed = initialSpeed;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerFlingingServerRpc(int PlayerID)
    {
        SetPlayerFlingingClientRpc(PlayerID);
    }

    [ClientRpc]
    public void SetPlayerFlingingClientRpc(int PlayerID)
    {
        StartOfRound.Instance.allPlayerScripts[PlayerID].SetFlingingAway(true);
        StartOfRound.Instance.allPlayerScripts[PlayerID].SetFlung(true);
        StartCoroutine(AutoReEnableKinematicsAfter10Seconds(StartOfRound.Instance.allPlayerScripts[PlayerID]));
    }

    public IEnumerator AutoReEnableKinematicsAfter10Seconds(PlayerControllerB player)
    {
        yield return new WaitForSeconds(10f);
        player.SetFlingingAway(false);
    }

    public override void Update()
    {
        base.Update();
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;
        HandleLocalPlayerInsideTornado(GameNetworkManager.Instance.localPlayerController);
    }

    private void HandleLocalPlayerInsideTornado(PlayerControllerB localPlayer)
    {
        if (localPlayer == null || !localPlayer.isPlayerControlled || localPlayer.isPlayerDead || localPlayer.IsFlingingAway() || tornadoType == TornadoType.Water) return;
        float distanceOfLocalPlayerToTornado = Vector3.Distance(localPlayer.transform.position, eye.transform.position);
        if (TornadoConditionsAreMet(localPlayer) && distanceOfLocalPlayerToTornado <= 10f)
        {
            timeSinceBeingInsideTornado = Mathf.Clamp(timeSinceBeingInsideTornado + Time.deltaTime, 0, 49f);
        }
        else if (distanceOfLocalPlayerToTornado > 10f)
        {
            timeSinceBeingInsideTornado = Mathf.Clamp(timeSinceBeingInsideTornado - Time.deltaTime, 0, 49f);
        }

        if (timeSinceBeingInsideTornado >= Plugin.ModConfig.ConfigTornadoInsideBeforeThrow.Value)
        {
            SetPlayerFlingingServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, localPlayer));
        }
    }

    public void FixedUpdate()
    {
        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        UpdateAudio();
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer == null || localPlayer.isPlayerDead || !localPlayer.isPlayerControlled) return;

        HandleTornadoDamageAndPulling(localPlayer);
        HandleStatusEffects(localPlayer);
        if (!localPlayer.IsFlingingAway()) return;

        // Plugin.ExtendedLogging("Tornado is flinging away");
        Vector3 directionToCenter = (throwingPoint.position - localPlayer.transform.position).normalized;
        Vector3 spiralForce = CalculateSpiralForce(directionToCenter, 6); // Adjust the second parameter to control spiral intensity
        localPlayer.externalForceAutoFade += spiralForce; // maybe += ?
    }

    private Vector3 CalculateSpiralForce(Vector3 directionToCenter, float spiralIntensity)
    {
        Vector3 spiralDirection = Vector3.Cross(directionToCenter, Vector3.up).normalized;
        return spiralDirection * spiralIntensity + directionToCenter * spiralIntensity;
    }

    private void HandleTornadoDamageAndPulling(PlayerControllerB player)
    {
        bool doesTornadoAffectPlayer = TornadoConditionsAreMet(player);
        if (!doesTornadoAffectPlayer) return;

        CalculateTornadoLineOfSights(100, player, out float bestDistanceLOS, out float bestDistanceOverall);
        if (bestDistanceOverall == float.PositiveInfinity) return;

        Vector3 targetPosition = (eye.transform.position - player.transform.position).normalized;
        float forceStrengthWithLOS = CalculatePullStrength(bestDistanceLOS, true, player);
        float forceStrengthOverall = CalculatePullStrength(bestDistanceOverall, false, player);
        float forceStrength = Mathf.Max(forceStrengthWithLOS, forceStrengthOverall);
        // Plugin.ExtendedLogging($"Force strength: {forceStrength}");
        player.externalForceAutoFade += targetPosition * forceStrength * Time.fixedDeltaTime / 0.5f; // todo: test this with different fps

        if (bestDistanceLOS > 4f || !damageTimer) return;
        damageTimer = false;
        StartCoroutine(DamageTimer());
        HandleTornadoTypeDamage(player);
    }

    public bool TornadoConditionsAreMet(PlayerControllerB player)
    {
        return  !player.inVehicleAnimation &&
                !player.IsRidingHoverboard() &&
                !StartOfRound.Instance.shipBounds.bounds.Contains(player.transform.position) &&
                !player.isInsideFactory &&
                player.isPlayerControlled &&
                !player.isPlayerDead &&
                !player.isInHangarShipRoom &&
                !player.inAnimationWithEnemy &&
                !player.enteringSpecialAnimation &&
                !player.isClimbingLadder &&
                !player.IsFlingingAway();
    }

    private void HandleStatusEffects(PlayerControllerB player)
    {
        float closeRange = 10f;
        float midRange = 20f;
        float longRange = 30f;
        float distance = Vector3.Distance(player.transform.position, transform.position);

        switch (tornadoType)
        {
            case TornadoType.Fire:
                player.ApplyStatusEffect(CodeRebirthStatusEffects.Fire, midRange, distance);
                break;
            case TornadoType.Blood:
                player.ApplyStatusEffect(CodeRebirthStatusEffects.Blood, closeRange, distance);
                break;
            case TornadoType.Windy:
                player.ApplyStatusEffect(CodeRebirthStatusEffects.Windy, closeRange, distance);
                break;
            case TornadoType.Smoke:
                player.ApplyStatusEffect(CodeRebirthStatusEffects.Smoke, longRange, distance);
                break;
            case TornadoType.Water:
                player.ApplyStatusEffect(CodeRebirthStatusEffects.Water, closeRange, distance);
                break;
            case TornadoType.Electric:
                ApplyElectricStatusEffect(player, midRange, distance);
                break;
        }
    }

    private void ApplyElectricStatusEffect(PlayerControllerB player, float range, float distance)
    {
        switch (player.ApplyStatusEffect(CodeRebirthStatusEffects.Electric, range, distance))
        {
            case ApplyEffectResults.Applied:
                originalPlayerSpeed = player.movementSpeed;
                player.movementSpeed *= 1.5f;
                break;
            case ApplyEffectResults.Removed:
                player.movementSpeed = originalPlayerSpeed;
                break;
        }
    }

    private IEnumerator LightningBoltTimer()
    {
        while (true)
        {
            Vector3 strikePosition = GetRandomTargetPosition(tornadoRandom, outsideNodes, minX: -2, maxX: 2, minY: -5, maxY: 5, minZ: -2, maxZ: 2, radius: 25);
            CRUtilities.CreateExplosion(strikePosition, true, 20, 0, 4, 1, null, null, 1f);
            LightningStrikeScript.SpawnLightningBolt(strikePosition, tornadoRandom, lightningSource);
            yield return new WaitForSeconds(tornadoRandom.NextFloat(0, 17));
            yield return new WaitForEndOfFrame();
        }
    }

    private float CalculatePullStrength(float distance, bool hasLineOfSight, PlayerControllerB localPlayerController)
    {
        float maxDistance = 100f;
        float minStrength = 0;
        float maxStrength = DetermineTornadoMaxStrength(hasLineOfSight, localPlayerController);

        // Calculate the normalized distance and apply an exponential falloff
        float normalizedDistance = Mathf.Clamp01(1 - distance / maxDistance);
        float strengthFalloff = normalizedDistance*normalizedDistance; // Use an exponential falloff for smoother results

        // Calculate the pull strength based on the falloff
        // Plugin.ExtendedLogging($"Pull strength falloff: {strengthFalloff}");
        float pullStrength = Mathf.Lerp(minStrength, maxStrength, strengthFalloff);
        return pullStrength;
    }

    private float DetermineTornadoMaxStrength(bool hasLineOfSight, PlayerControllerB localPlayerController)
    {
        return (hasLineOfSight ? Plugin.ModConfig.ConfigTornadoPullStrength.Value : 0.125f * Plugin.ModConfig.ConfigTornadoPullStrength.Value)
            * (tornadoType == TornadoType.Smoke ? 1.25f : 1f)
            * (localPlayerController.HasEffectActive(CodeRebirthStatusEffects.Water) ? 0.5f : 1f)
            * (localPlayerController.HasEffectActive(CodeRebirthStatusEffects.Fire) ? 0.5f : 1f);
    }

    private void HandleTornadoTypeDamage(PlayerControllerB localPlayerController) // todo: redo this mess.
    {
        if (localPlayerController == null) return;
        switch (tornadoType)
        {
            case TornadoType.Fire:
                // make the screen a firey mess
                if (localPlayerController.health > 20)
                {
                    localPlayerController.DamagePlayer(3, causeOfDeath: CauseOfDeath.Burning);
                }
                break;
            case TornadoType.Blood:
                localPlayerController.DamagePlayer(tornadoRandom.Next(-5, 6));
                break;
            case TornadoType.Windy:
                break;
            case TornadoType.Smoke:
            localPlayerController.DamagePlayer(2);
                // make the player screen a smokey mess.
                break;
            case TornadoType.Water:
                // Drown the player.
                break;
            case TornadoType.Electric:
                // spawn lightning around
                if (localPlayerController.health > 20)
                {
                    localPlayerController.DamagePlayer(1);
                }
                break;
        }
    }

    private IEnumerator DamageTimer()
    {
        yield return new WaitForSeconds(0.5f);
        damageTimer = true;
    }

    private void UpdateAudio()
    {
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
        {
            normalTravelAudio.volume = 0;
            closeTravelAudio.volume = 0;
        }
        else if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed)
        {
            normalTravelAudio.volume = Plugin.ModConfig.ConfigTornadoDefaultVolume.Value * Plugin.ModConfig.ConfigTornadoInShipVolume.Value;
            closeTravelAudio.volume = Plugin.ModConfig.ConfigTornadoDefaultVolume.Value * Plugin.ModConfig.ConfigTornadoInShipVolume.Value;
        }
        else
        {
            normalTravelAudio.volume = Plugin.ModConfig.ConfigTornadoDefaultVolume.Value;
            closeTravelAudio.volume = Plugin.ModConfig.ConfigTornadoDefaultVolume.Value;
        }
    }
    
    public void CalculateTornadoLineOfSights(int range, PlayerControllerB player, out float bestDistanceLOS, out float bestDistanceOverall)
    {
        bestDistanceLOS = float.PositiveInfinity;
        bestDistanceOverall = float.PositiveInfinity;
        foreach (Transform eye in eyes)
        {
            float distance = Vector3.Distance(eye.transform.position, player.transform.position);
            if (distance > range) continue;
            bestDistanceOverall = Mathf.Min(bestDistanceOverall, distance);
            if (Physics.Raycast(eye.transform.position, (player.transform.position - eye.position).normalized, distance, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("Terrain", "InteractableObject", "MapHazards", "Vehicle", "Railing"), QueryTriggerInteraction.Ignore)) continue;
            bestDistanceLOS = Mathf.Min(bestDistanceLOS, distance);
        }
        // Plugin.ExtendedLogging($"Best distance with LOS: {bestDistanceLOS} and overall: {bestDistanceOverall}");
    }

    public Vector3 GetRandomTargetPosition(System.Random random, IEnumerable<GameObject> nodes, float minX, float maxX, float minY, float maxY, float minZ, float maxZ, float radius)
    {
		int randomNodeIndex = random.Next(0, nodes.Count());
        GameObject nextNode = nodes.ElementAt(randomNodeIndex);
        Vector3 position = nextNode.transform.position;

        position += new Vector3(random.NextFloat(minX, maxX), random.NextFloat(minY, maxY), random.NextFloat(minZ, maxZ));
        position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: position, radius: radius, randomSeed: random);
        return position;
	}
}

public class TornadoSelector
{
    private readonly Dictionary<string, TornadoType> tornadoTypeMapping = new()
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

        List<TornadoType> validTornadoTypes = new();

        foreach (var entry in tornadoEntries)
        {
            string[] parts = entry.Split(':');
            string tornadoName = parts[0].Trim();
            string moonConditions = parts[1].Trim();

            if (tornadoTypeMapping.ContainsKey(tornadoName))
            {
                TornadoType tornadoType = tornadoTypeMapping[tornadoName];

                // Split moonConditions by comma and loop through them
                IEnumerable<string> moonConditionsArray = moonConditions.Split([','], StringSplitOptions.RemoveEmptyEntries)
                                                                        .Select(m => m.Trim());

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
            System.Random rand = new(StartOfRound.Instance.randomMapSeed);
            int randomIndex = rand.Next(validTornadoTypes.Count);
            return (int)validTornadoTypes[randomIndex];
        }
        else
        {
            Plugin.Logger.LogWarning("Your config is poorly done. Tornado type index is out of bounds. Defaulting to Smoke tornado.");
            return (int)TornadoType.Smoke;
        }
    }

    private bool IsValidForMoon(string moonCondition)
    {
        if (moonCondition.ToLowerInvariant() == "all")
        {
            return true;
        }
        else if ((moonCondition.ToLowerInvariant() == "vanilla" && LethalLevelLoader.PatchedContent.VanillaExtendedLevels.Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel))) || (moonCondition.ToLowerInvariant() == "custom" && LethalLevelLoader.PatchedContent.CustomExtendedLevels.Any(level => level.Equals(LethalLevelLoader.LevelManager.CurrentExtendedLevel))))
        {
            // Dummy logic for Vanilla or Custom
            return true;
        }
        else
        {
            // MoonName logic, for now just checks if the current moon matches the condition
            return LethalLevelLoader.LevelManager.CurrentExtendedLevel.NumberlessPlanetName.Equals(moonCondition, StringComparison.OrdinalIgnoreCase);
        }
    }
}