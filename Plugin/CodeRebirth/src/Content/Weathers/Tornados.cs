using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using CodeRebirth.src.Util;
using Dawn.Utils;
using System;
using static CodeRebirth.src.Util.PlayerControllerBExtensions;
using CodeRebirth.src.Content.Enemies;


namespace CodeRebirth.src.Content.Weathers;
public class Tornados : CodeRebirthEnemyAI
{
    [Header("Properties")]
    public float initialSpeed = 5f;

    [Space(5f)]
    [Header("Audio")]
    public AudioSource normalTravelAudio = null!;
    public AudioSource closeTravelAudio = null!;

    public Transform[] eyes = null!;
    public Transform throwingPoint = null!;
    public TornadoType tornadoType = TornadoType.Smoke;

    public enum TornadoType
    {
        Fire = 1,
        Smoke = 2,
        Water = 3,
    }

    private bool damageTimer = true;
    private float timeSinceBeingInsideTornado = 0;

    public override void Start()
    {
        base.Start();
        initialSpeed = Plugin.ModConfig.ConfigTornadoSpeed.Value;

        Plugin.ExtendedLogging($"Setting up tornado of type: {tornadoType} at {transform.position}");
        SetupTornadoType();
    }

    private void SetupTornadoType()
    {
        switch (tornadoType)
        {
            case TornadoType.Fire:
                // spawn like 2 more firey things
                break;
            case TornadoType.Smoke:
                break;
            case TornadoType.Water:
                // activate flooded + rainy
                initialSpeed /= 2;
                break;
        }
        Init();
    }

    private void Init()
    {
        smartAgentNavigator.StartSearchRoutine(40f);
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
        StartCoroutine(StopFlingingPlayer(StartOfRound.Instance.allPlayerScripts[PlayerID]));
    }

    public IEnumerator StopFlingingPlayer(PlayerControllerB player)
    {
        yield return new WaitForSeconds(10f);
        player.SetFlingingAway(false);
    }

    private void HandleLocalPlayerInsideTornado(PlayerControllerB localPlayer)
    {
        if (localPlayer == null || !localPlayer.isPlayerControlled || localPlayer.isPlayerDead || localPlayer.IsFlingingAway()) return;
        float distanceOfLocalPlayerToTornado = Vector3.Distance(localPlayer.transform.position, eye.transform.position);
        if (TornadoConditionsAreMet(localPlayer) && distanceOfLocalPlayerToTornado <= 10f)
        {
            timeSinceBeingInsideTornado = Mathf.Clamp(timeSinceBeingInsideTornado + Time.fixedDeltaTime, 0, Plugin.ModConfig.ConfigTornadoInsideBeforeThrow.Value + 20f);
        }
        else
        {
            timeSinceBeingInsideTornado = Mathf.Clamp(timeSinceBeingInsideTornado - Time.fixedDeltaTime, 0, Plugin.ModConfig.ConfigTornadoInsideBeforeThrow.Value + 20f);
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

        if (tornadoType == TornadoType.Smoke) HandleLocalPlayerInsideTornado(localPlayer);
        if (tornadoType != TornadoType.Water) HandleTornadoDamageAndPulling(localPlayer);
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
        return !player.inVehicleAnimation &&
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
            case TornadoType.Smoke:
                player.ApplyStatusEffect(CodeRebirthStatusEffects.Smoke, longRange, distance);
                break;
            case TornadoType.Water:
                player.ApplyStatusEffect(CodeRebirthStatusEffects.Water, closeRange, distance);
                break;
        }
    }

    private float CalculatePullStrength(float distance, bool hasLineOfSight, PlayerControllerB localPlayerController)
    {
        float maxDistance = 100f;
        float minStrength = 0;
        float maxStrength = DetermineTornadoMaxStrength(hasLineOfSight, localPlayerController);

        // Calculate the normalized distance and apply an exponential falloff
        float normalizedDistance = Mathf.Clamp01(1 - distance / maxDistance);
        float strengthFalloff = normalizedDistance * normalizedDistance; // Use an exponential falloff for smoother results

        // Calculate the pull strength based on the falloff
        // Plugin.ExtendedLogging($"Pull strength falloff: {strengthFalloff}");
        float pullStrength = Mathf.Lerp(minStrength, maxStrength, strengthFalloff);
        return pullStrength;
    }

    private float DetermineTornadoMaxStrength(bool hasLineOfSight, PlayerControllerB localPlayerController)
    {
        return (hasLineOfSight ? Plugin.ModConfig.ConfigTornadoPullStrength.Value : 0.125f * Plugin.ModConfig.ConfigTornadoPullStrength.Value)
            * (tornadoType != TornadoType.Smoke ? 0.2f : 0.75f);
    }

    private void HandleTornadoTypeDamage(PlayerControllerB localPlayerController) // todo: redo this mess.
    {
        if (localPlayerController == null) return;
        switch (tornadoType)
        {
            case TornadoType.Fire:
                // make the screen a firey mess
                localPlayerController.DamagePlayer(3, causeOfDeath: CauseOfDeath.Burning);
                break;
            case TornadoType.Smoke:
                localPlayerController.DamagePlayer(2);
                // make the player screen a smokey mess.
                break;
            case TornadoType.Water:
                // Drown the player.
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
            if (Physics.Linecast(eye.transform.position, eye.position, MoreLayerMasks.CollidersAndRoomAndInteractableAndRailingAndEnemiesAndTerrainAndHazardAndVehicleMask, QueryTriggerInteraction.Ignore)) continue;
            bestDistanceLOS = Mathf.Min(bestDistanceLOS, distance);
        }
        // Plugin.ExtendedLogging($"Best distance with LOS: {bestDistanceLOS} and overall: {bestDistanceOverall}");
    }

    public Vector3 GetRandomTargetPosition(System.Random random, IEnumerable<GameObject> nodes, float minX, float maxX, float minY, float maxY, float minZ, float maxZ, float radius)
    {
        int randomNodeIndex = random.Next(nodes.Count());
        GameObject nextNode = nodes.ElementAt(randomNodeIndex);
        Vector3 position = nextNode.transform.position;

        position += new Vector3(random.NextFloat(minX, maxX), random.NextFloat(minY, maxY), random.NextFloat(minZ, maxZ));
        position = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(pos: position, radius: radius, randomSeed: random);
        return position;
    }
}