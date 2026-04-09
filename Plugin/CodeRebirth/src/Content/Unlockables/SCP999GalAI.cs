using System;
using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.MiscScripts;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;

public class SCP999GalAI : NetworkBehaviour
{
    public CRNoiseListener _SCP999GalNoiseListener = null!; // todo implement this 
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;
    public InteractTrigger HealTrigger = null!;
    public GameObject particleSystemGameObject = null!;
    public List<Transform> revivePositions = new();

    private float boomboxTimer = 0f;
    private bool boomboxPlaying = false;
    private bool currentlyHealing = false;
    private float nearbyAnimationTimer = 1f;
    private float cooldownTimer = 5f;
    private static readonly int playerIsNearby = Animator.StringToHash("playerIsNearby"); // bool
    private static readonly int isDancing = Animator.StringToHash("isDancing"); // bool
    private static readonly int doSquishAnimation = Animator.StringToHash("doSquish"); // trigger
    private static readonly int doSucceedAnimation = Animator.StringToHash("doSucceed"); // trigger
    private static readonly int doFailAnimation = Animator.StringToHash("doFail"); // trigger
    private NetworkVariable<int> healChargeCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> reviveChargeCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Dictionary<PlayerControllerB, float> playerMaxHealthDict = new();

    public static List<SCP999GalAI> Instances = new();

    private static readonly NamespacedKey NancyHealReviveCount = NamespacedKey.From("code_rebirth", "nancy_heal_revive_count");
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            if (!GameNetworkManager.Instance.gameHasStarted)
            {
                Vector2 reviveHealCount = DawnLib.GetCurrentContract()!.GetOrSetDefault(NancyHealReviveCount, new Vector2(0, 0));
                reviveChargeCount.Value = (int)reviveHealCount.x;
                healChargeCount.Value = (int)reviveHealCount.y;
            }
            else
            {
                RechargeGalHealsAndRevivesServerRpc(true, true);
            }
        }
        Instances.Add(this);
        StartCoroutine(UpdatePlayerHealthForSelf());
        QualitySettings.skinWeights = SkinWeights.FourBones;
        if (IsServer)
        {
            RechargeGalHealsAndRevivesServerRpc(true, true);
        }
        MakeTriggerInteractable(!StartOfRound.Instance.inShipPhase);
        _SCP999GalNoiseListener._onNoiseDetected.AddListener(DetectNoise);
        HealTrigger.onInteract.AddListener(HealPlayerInteraction);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
        {
            DawnLib.GetCurrentContract()!.Set(NancyHealReviveCount, new Vector2(reviveChargeCount.Value, healChargeCount.Value));
        }
        Instances.Remove(this);
    }

    public void Update()
    {
        if (nearbyAnimationTimer <= 0f)
        {
            nearbyAnimationTimer = 1f;
            bool foundPlayer = false;
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerDead || !player.isPlayerControlled || !player.isInHangarShipRoom)
                {
                    continue;
                }

                if (Vector3.Distance(transform.position, player.transform.position) <= 10f)
                {
                    foundPlayer = true;
                    break;
                }
            }
            animator.SetBool(playerIsNearby, foundPlayer);
        }

        if (cooldownTimer <= 0f)
        {
            bool moreThan0HealCapacity = healChargeCount.Value > 0;
            bool moreThan0ReviveCapacity = reviveChargeCount.Value > 0;
            if (moreThan0HealCapacity && moreThan0ReviveCapacity)
            {
                HealTrigger.hoverTip = "Heals Left: " + healChargeCount.Value + "\nRevives Left: " + reviveChargeCount.Value;
            }
            else if (moreThan0HealCapacity)
            {
                HealTrigger.hoverTip = "Heals Left: " + healChargeCount.Value;
            }
            else if (moreThan0ReviveCapacity)
            {
                HealTrigger.hoverTip = "Revives Left: " + reviveChargeCount.Value;
            }
            else
            {
                HealTrigger.hoverTip = "Not enough charges!";
            }
        }
        else
        {
            HealTrigger.hoverTip = "Healing Cooldown: " + Math.Round(cooldownTimer, 2);
        }

        if (!currentlyHealing)
        {
            // Plugin.ExtendedLogging($"Cooldown timer: {cooldownTimer}");
            cooldownTimer -= Time.deltaTime;
        }
        BoomboxUpdate();
    }

    private void BoomboxUpdate()
    {
        if (!boomboxPlaying)
        {
            return;
        }

        boomboxTimer += Time.deltaTime;
        if (boomboxTimer >= 2f)
        {
            boomboxTimer = 0f;
            boomboxPlaying = false;
            animator.SetBool(isDancing, false);
        }
    }

    public IEnumerator UpdatePlayerHealthForSelf()
    {
        yield return new WaitUntil(() => GameNetworkManager.Instance.localPlayerController != null);
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        ResetAndSetMaxHealthForPlayerRpc();
    }

    public int GrabMaxHealthForPlayer(PlayerControllerB player)
    {
        return Mathf.Max(player.health, 100);
    }

    [Rpc(SendTo.Everyone, DeferLocal = true)]
    private void ResetAndSetMaxHealthForPlayerRpc()
    {
        playerMaxHealthDict.Clear();
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        InformOtherClientsOfMaxHealthRpc(localPlayer, GrabMaxHealthForPlayer(localPlayer));
    }

    [Rpc(SendTo.Everyone, DeferLocal = true)]
    private void InformOtherClientsOfMaxHealthRpc(PlayerControllerReference player, int maxHealth)
    {
        PlayerControllerB playerControllerB = player;
        Plugin.ExtendedLogging($"Informing other clients that player {playerControllerB.playerUsername} has max health: {maxHealth}");
        if (playerMaxHealthDict.ContainsKey(playerControllerB))
        {
            playerMaxHealthDict[playerControllerB] = maxHealth;
            return;
        }
        playerMaxHealthDict.Add(playerControllerB, maxHealth);
    }

    private void HealPlayerInteraction(PlayerControllerB playerInteracting)
    {
        Plugin.ExtendedLogging($"Healing player: {playerInteracting} | Cooldown timer: {cooldownTimer} | Heal Charge count: {healChargeCount.Value} | Revive Charge count: {reviveChargeCount.Value}");
        if (boomboxPlaying)
        {
            return;
        }
        if (cooldownTimer > 0f || (healChargeCount.Value <= 0 && reviveChargeCount.Value <= 0))
        {
            Plugin.ExtendedLogging($"triggering squish animation.");
            TriggerAnimationServerRpc(doSquishAnimation);
            return;
        }

        // this being on the host's end sucks ass, see if i can make it local, and i dont even know if this works.
        DoHealingStuffServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
    }

    [ServerRpc(RequireOwnership = false)]
    private void TriggerAnimationServerRpc(int triggerHash)
    {
        networkAnimator.SetTrigger(triggerHash);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoHealingStuffServerRpc(int playerInteractingIndex)
    {
        Plugin.ExtendedLogging($"Player who poked index: {playerInteractingIndex}");
        bool galDidSomething = false;
        PlayerControllerB playerInteracting = StartOfRound.Instance.allPlayerScripts[playerInteractingIndex];
        bool onlyInteractedPlayerHealed = Plugin.ModConfig.Config999GalHealOnlyInteractedPlayer.Value;
        int healAmount = Plugin.ModConfig.Config999GalHealAmount.Value;
        float healingSpeed = Plugin.ModConfig.Config999GalHealSpeed.Value;
        bool reviveNearbyDeadPlayers = Plugin.ModConfig.Config999GalReviveNearbyDeadPlayers.Value;
        bool fail = false;
        if (UnityEngine.Random.Range(0f, 100f) <= Plugin.ModConfig.Config999GalFailureChance.Value)
        {
            fail = true;
        }
        if (reviveNearbyDeadPlayers && reviveChargeCount.Value > 0)
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                Plugin.ExtendedLogging($"Checking player {player.name} | dead: {player.isPlayerDead} | controlled: {player.isPlayerControlled}");
                if (player == null || !player.isPlayerDead || player.deadBody == null || !player.deadBody.gameObject.activeSelf || reviveChargeCount.Value <= 0) continue;
                float distanceFromGal = Vector3.Distance(transform.position, player.deadBody.transform.position);
                Plugin.ExtendedLogging($"Distance from gal: {distanceFromGal}");
                if (distanceFromGal > 5) continue;
                reviveChargeCount.Value--;
                galDidSomething = true;
                if (fail)
                {
                    Plugin.ExtendedLogging("Failed to revive player.");
                    if (NetworkManager.Singleton.IsServer) networkAnimator.SetTrigger(doFailAnimation);
                    return;
                }
                DoALotOfShitToRevivePlayerClientRpc(revivePositions[UnityEngine.Random.Range(0, revivePositions.Count)].position, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player));
            }
        }
        if (onlyInteractedPlayerHealed)
        {
            if (healChargeCount.Value <= 0 || playerInteracting == null || playerInteracting.isPlayerDead || !playerInteracting.isPlayerControlled) return;
            galDidSomething = true;
            HealPlayersClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting), healAmount, healingSpeed, fail);
        }
        else
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                Plugin.ExtendedLogging($"Checking player {player.name} | dead: {player.isPlayerDead} | controlled: {player.isPlayerControlled}");
                if (healChargeCount.Value <= 0 || player == null || player.isPlayerDead || !player.isPlayerControlled) continue;
                if (Vector3.Distance(transform.position, player.transform.position) > 5) continue;
                galDidSomething = true;
                HealPlayersClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player), healAmount, healingSpeed, fail);
            }
        }

        if (galDidSomething && !fail)
        {
            networkAnimator.SetTrigger(doSucceedAnimation);
        }

        cooldownTimer = Plugin.ModConfig.Config999GalHealCooldown.Value;
    }

    [ClientRpc]
    private void HealPlayersClientRpc(int playerIndex, int healAmount, float healingSpeed, bool fail)
    {
        StartCoroutine(HealPlayerOverTime(StartOfRound.Instance.allPlayerScripts[playerIndex], healAmount, healingSpeed, fail));
    }

    private IEnumerator HealPlayerOverTime(PlayerControllerB player, int healAmount, float healingSpeed, bool failed)
    {
        if (failed)
        {
            Plugin.ExtendedLogging($"Failed to heal player, taking up {50} health away.");
            if (IsServer)
            {
                networkAnimator.SetTrigger(doFailAnimation);
                healChargeCount.Value -= 50;
            }
            yield break;
        }
        currentlyHealing = true;

        var newParticles = GameObject.Instantiate(particleSystemGameObject, player.transform.position, Quaternion.identity);
        newParticles.SetActive(true);

        int totalHealthToHeal = healAmount;
        int healthHealed = 0;

        float timeElapsed = 0f;
        while (healthHealed < totalHealthToHeal && player.health < playerMaxHealthDict[player])
        {
            timeElapsed += Time.deltaTime;

            int healthThisFrame = Mathf.FloorToInt(healAmount * (timeElapsed / healingSpeed)) - healthHealed;

            if (healthThisFrame > 0)
            {
                healthHealed += healthThisFrame;
                if (player.IsLocalPlayer())
                {
                    TellHostAboutNewHealthServerRpc(healthThisFrame);
                }
                player.health += healthThisFrame;
                SetVisualChangesToPlayer(player);
            }
            yield return null;
        }

        if (healthHealed < totalHealthToHeal && player.health < playerMaxHealthDict[player])
        {
            player.health += (totalHealthToHeal - healthHealed);
            SetVisualChangesToPlayer(player);
        }

        currentlyHealing = false;
        Destroy(newParticles);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TellHostAboutNewHealthServerRpc(int healthToDecrease)
    {
        healChargeCount.Value -= healthToDecrease;
    }

    private void SetVisualChangesToPlayer(PlayerControllerB player)
    {
        if (player.health >= 20)
        {
            if (player.criticallyInjured || player.bleedingHeavily)
            {
                player.criticallyInjured = false;
                player.bleedingHeavily = false;
            }
            player.playerBodyAnimator.SetBool("Limp", false);
            if (GameNetworkManager.Instance.localPlayerController == player) HUDManager.Instance.UpdateHealthUI(player.health, false);
        }
    }

    [ClientRpc]
    private void DoALotOfShitToRevivePlayerClientRpc(Vector3 revivePosition, int PlayerScriptIndex)
    {
        DoStuffToRevivePlayer(revivePosition, PlayerScriptIndex);
    }

    public static void DoStuffToRevivePlayer(Vector3 revivePosition, int PlayerScriptIndex)
    {
        PlayerControllerB PlayerScript = StartOfRound.Instance.allPlayerScripts[PlayerScriptIndex];
        PlayerScript.isInsideFactory = false;
        PlayerScript.isInElevator = true;
        PlayerScript.isInHangarShipRoom = true;
        PlayerScript.ResetPlayerBloodObjects(PlayerScript.isPlayerDead);
        PlayerScript.health = 5;
        PlayerScript.isClimbingLadder = false;
        PlayerScript.clampLooking = false;
        PlayerScript.inVehicleAnimation = false;
        PlayerScript.disableMoveInput = false;
        PlayerScript.disableLookInput = false;
        PlayerScript.disableInteract = false;
        PlayerScript.ResetZAndXRotation();
        PlayerScript.thisController.enabled = true;
        if (PlayerScript.isPlayerDead)
        {
            PlayerScript.thisController.enabled = true;
            PlayerScript.isPlayerDead = false;
            PlayerScript.isPlayerControlled = true;
            PlayerScript.health = 5;
            PlayerScript.hasBeenCriticallyInjured = false;
            PlayerScript.criticallyInjured = false;
            PlayerScript.playerBodyAnimator.SetBool("Limp", value: false);
            PlayerScript.TeleportPlayer(revivePosition, false, 0f, false, true);
            PlayerScript.parentedToElevatorLastFrame = false;
            PlayerScript.overrideGameOverSpectatePivot = null;
            StartOfRound.Instance.SetPlayerObjectExtrapolate(enable: false);
            PlayerScript.setPositionOfDeadPlayer = false;
            PlayerScript.DisablePlayerModel(PlayerScript.gameObject, enable: true, disableLocalArms: true);
            PlayerScript.helmetLight.enabled = false;
            PlayerScript.Crouch(crouch: false);
            PlayerScript.playerBodyAnimator?.SetBool("Limp", false);
            PlayerScript.bleedingHeavily = false;
            if (PlayerScript.deadBody != null)
            {
                PlayerScript.deadBody.enabled = false;
                PlayerScript.deadBody.gameObject.SetActive(false);
            }
            PlayerScript.bleedingHeavily = true;
            PlayerScript.deadBody = null;
            PlayerScript.activatingItem = false;
            PlayerScript.twoHanded = false;
            PlayerScript.inShockingMinigame = false;
            PlayerScript.inSpecialInteractAnimation = false;
            PlayerScript.freeRotationInInteractAnimation = false;
            PlayerScript.disableSyncInAnimation = false;
            PlayerScript.inAnimationWithEnemy = null;
            PlayerScript.holdingWalkieTalkie = false;
            PlayerScript.speakingToWalkieTalkie = false;
            PlayerScript.isSinking = false;
            PlayerScript.isUnderwater = false;
            PlayerScript.sinkingValue = 0f;
            PlayerScript.statusEffectAudio.Stop();
            PlayerScript.DisableJetpackControlsLocally();
            PlayerScript.mapRadarDotAnimator.SetBool("dead", value: false);
            PlayerScript.hasBegunSpectating = false;
            PlayerScript.externalForceAutoFade = Vector3.zero;
            PlayerScript.hinderedMultiplier = 1f;
            PlayerScript.isMovementHindered = 0;
            PlayerScript.sourcesCausingSinking = 0;
            PlayerScript.reverbPreset = StartOfRound.Instance.shipReverb;
            SoundManager.Instance.earsRingingTimer = 0f;
            PlayerScript.voiceMuffledByEnemy = false;
            SoundManager.Instance.playerVoicePitchTargets[Array.IndexOf(StartOfRound.Instance.allPlayerScripts, PlayerScript)] = 1f;
            SoundManager.Instance.SetPlayerPitch(1f, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, PlayerScript));

            if (PlayerScript.currentVoiceChatIngameSettings == null)
            {
                StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            }
            if (PlayerScript.currentVoiceChatIngameSettings != null)
            {
                if (PlayerScript.currentVoiceChatIngameSettings.voiceAudio == null)
                {
                    PlayerScript.currentVoiceChatIngameSettings.InitializeComponents();
                }
                if (PlayerScript.currentVoiceChatIngameSettings.voiceAudio == null)
                {
                    return;
                }
                PlayerScript.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
            }

            HUDManager.Instance.UpdateBoxesSpectateUI();
            HUDManager.Instance.UpdateSpectateBoxSpeakerIcons();
        }
        if (GameNetworkManager.Instance.localPlayerController == PlayerScript)
        {
            PlayerScript.bleedingHeavily = false;
            PlayerScript.criticallyInjured = false;
            PlayerScript.health = 5;
            HUDManager.Instance.UpdateHealthUI(5, true);
            PlayerScript.playerBodyAnimator?.SetBool("Limp", false);
            PlayerScript.spectatedPlayerScript = null;
            StartOfRound.Instance.SetSpectateCameraToGameOverMode(false, PlayerScript);
            StartOfRound.Instance.SetPlayerObjectExtrapolate(false);
            HUDManager.Instance.audioListenerLowPass.enabled = false;
            HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
            HUDManager.Instance.RemoveSpectateUI();
            HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
        }
        StartOfRound.Instance.allPlayersDead = false;
        StartOfRound.Instance.livingPlayers++;
        StartOfRound.Instance.UpdatePlayerVoiceEffects();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RechargeGalHealsAndRevivesServerRpc(bool heal, bool revive)
    {
        int ActivePlayerAmount = 0;
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerControlled && !player.isPlayerDead)
            {
                ActivePlayerAmount++;
            }
        }
        Plugin.ExtendedLogging("Active Amount with dict: " + StartOfRound.Instance.ClientPlayerList.Count);
        Plugin.ExtendedLogging($"ActivePlayerAmount: {ActivePlayerAmount} | heal: {heal} | revive: {revive}");
        if (heal)
        {
            healChargeCount.Value = Plugin.ModConfig.Config999GalHealTotalAmount.Value * (Plugin.ModConfig.Config999GalScaleHealAndReviveWithPlayerCount.Value ? ActivePlayerAmount : 1);
        }
        if (revive)
        {
            reviveChargeCount.Value = Plugin.ModConfig.Config999GalReviveCharges.Value * (Plugin.ModConfig.Config999GalScaleHealAndReviveWithPlayerCount.Value ? ActivePlayerAmount : 1);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void MakeTriggerInteractableServerRpc(bool interactable)
    {
        MakeTriggerInteractableClientRpc(interactable);
    }

    [ClientRpc]
    public void MakeTriggerInteractableClientRpc(bool interactable)
    {
        MakeTriggerInteractable(interactable);
    }

    public void MakeTriggerInteractable(bool interactable)
    {
        HealTrigger.interactable = interactable;
    }

    public void DetectNoise(NoiseParams noiseParams)
    {
        if (noiseParams.noiseID != 5 || Physics.Linecast(transform.position, noiseParams.noisePosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            return;

        boomboxTimer = 0f;
        boomboxPlaying = true;
        animator.SetBool(isDancing, true);
    }
}