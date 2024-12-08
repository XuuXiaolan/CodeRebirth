using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class SCP999GalAI : NetworkBehaviour
{
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;
    public InteractTrigger HealTrigger = null!;
    public GameObject particleSystemGameObject = null!;
    public List<Transform> revivePositions = new();

    private System.Random random = new();
    private List<GameObject> particlesSpawned = new();
    private bool currentlyHealing = false;
    private float cooldownTimer = 0f;
    private bool nearbyPlayer = false;

    public void Start()
    {
        random = new System.Random(StartOfRound.Instance.randomMapSeed + 40);
        StartCoroutine(DetectingNearbyPlayer());
        HealTrigger.onInteract.AddListener(HealPlayerInteraction);
    }

    private void Update()
    {
        if (!currentlyHealing) cooldownTimer -= Time.deltaTime;
    }

    private IEnumerator DetectingNearbyPlayer()
    {
        while (true)
        {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerDead || !player.isPlayerControlled || !player.isInHangarShipRoom) continue;
                if (Vector3.Distance(transform.position, player.transform.position) <= 10f)
                {
                    nearbyPlayer = true;
                }
            }
            nearbyPlayer = false;
            yield return new WaitForSeconds(1f);
            yield return new WaitForEndOfFrame();
        }
    }

    private void HealPlayerInteraction(PlayerControllerB playerInteracting)
    {
        if (cooldownTimer > 0f) return;
        if (playerInteracting == null || playerInteracting != GameNetworkManager.Instance.localPlayerController) return;
        DoHealingStuffServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting));
    }

    [ServerRpc(RequireOwnership = false)]
    private void DoHealingStuffServerRpc(int playerInteractingIndex)
    {
        DoHealingStuffClientRpc(playerInteractingIndex);
    }

    [ClientRpc]
    private void DoHealingStuffClientRpc(int playerInteractingIndex)
    {
        cooldownTimer = Plugin.ModConfig.Config999GalHealCooldown.Value;
        PlayerControllerB playerInteracting = StartOfRound.Instance.allPlayerScripts[playerInteractingIndex];
        bool onlyInteractedPlayerHealed = Plugin.ModConfig.Config999GalHealOnlyInteractedPlayer.Value;
        int healAmount = Plugin.ModConfig.Config999GalHealAmount.Value;
        float healingSpeed = Plugin.ModConfig.Config999GalHealSpeed.Value;
        bool reviveNearbyDeadPlayers = Plugin.ModConfig.Config999GalReviveNearbyDeadPlayers.Value;
        if (reviveNearbyDeadPlayers)
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (!player.isPlayerDead) continue;
                float distanceFromGal = Vector3.Distance(transform.position, player.deadBody.transform.position);
                if (distanceFromGal <= 5)
                {
                    DoALotOfShitToRevivePlayer(player);
                }
            }
        }
        if (onlyInteractedPlayerHealed)
        {
            if (playerInteracting == null || playerInteracting.isPlayerDead || !playerInteracting.isPlayerControlled) return;
            StartCoroutine(HealPlayerOverTime(playerInteracting, healAmount, healingSpeed));
        }
        else
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerDead || !player.isPlayerControlled) continue;
                StartCoroutine(HealPlayerOverTime(player, healAmount, healingSpeed));
            }
        }
    }

    private IEnumerator HealPlayerOverTime(PlayerControllerB player, int healAmount, float healingSpeed)
    {
        currentlyHealing = true;
        
        // Instantiate the particle system at the player's position.
        var newParticles = GameObject.Instantiate(particleSystemGameObject, player.transform.position, Quaternion.identity);
        particlesSpawned.Add(newParticles);
        
        int totalHealthToHeal = healAmount;
        int healthHealed = 0;
        
        // This variable will track the time passed and calculate health to heal over time.
        float timeElapsed = 0f;

        // While we haven't healed the full amount yet.
        while (healthHealed < totalHealthToHeal)
        {
            // Accumulate time.
            timeElapsed += Time.deltaTime;

            // Calculate how much health to heal based on the elapsed time and healingSpeed.
            int healthThisFrame = Mathf.FloorToInt(healAmount * (timeElapsed / healingSpeed)) - healthHealed;

            // Heal the player (ensure we don't exceed the total amount).
            if (healthThisFrame > 0)
            {
                healthHealed += healthThisFrame;
                player.health += healthThisFrame;
                SetVisualChangesToPlayer(player);
            }

            // Wait for the next frame.
            yield return null;
        }
        
        // Ensure we do not heal more than the target amount.
        if (healthHealed < totalHealthToHeal)
        {
            player.health += (totalHealthToHeal - healthHealed);
            SetVisualChangesToPlayer(player);
        }

        // Done healing.
        currentlyHealing = false;
        
        // Clean up the particles.
        particlesSpawned.Remove(newParticles);
        Destroy(newParticles);
    }

    private void SetVisualChangesToPlayer(PlayerControllerB player)
    {
        if (player.health > 20)
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

    private void DoALotOfShitToRevivePlayer(PlayerControllerB PlayerScript)
    {
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
            print("playerInital is dead, reviving them.");
            PlayerScript.thisController.enabled = true;
            PlayerScript.isPlayerDead = false;
            PlayerScript.isPlayerControlled = true;
            PlayerScript.health = 5;
            PlayerScript.hasBeenCriticallyInjured = false;
            PlayerScript.criticallyInjured = false;
            PlayerScript.playerBodyAnimator.SetBool("Limp", value: false);
            PlayerScript.TeleportPlayer(revivePositions[random.Next(revivePositions.Count)].position, false, 0f, false, true);
            PlayerScript.parentedToElevatorLastFrame = false;
            PlayerScript.overrideGameOverSpectatePivot = null;
            StartOfRound.Instance.SetPlayerObjectExtrapolate(enable: false);
            PlayerScript.setPositionOfDeadPlayer = false;
            PlayerScript.DisablePlayerModel(PlayerScript.gameObject, enable: true, disableLocalArms: true);
            PlayerScript.helmetLight.enabled = false;
            PlayerScript.Crouch(crouch: false);
            if (PlayerScript.playerBodyAnimator != null)
            {
                PlayerScript.playerBodyAnimator.SetBool("Limp", value: false);
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
        }

        if (GameNetworkManager.Instance.localPlayerController == PlayerScript)
        {
            PlayerScript.bleedingHeavily = false;
            PlayerScript.criticallyInjured = false;
            PlayerScript.health = 5;
            HUDManager.Instance.UpdateHealthUI(5, hurtPlayer: true);
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

        if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
        {
            HUDManager.Instance.UpdateBoxesSpectateUI();
            HUDManager.Instance.UpdateSpectateBoxSpeakerIcons();
        }      
    }
}