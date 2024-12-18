using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;
public class SCP999GalAI : NetworkBehaviour, INoiseListener
{
    public Animator animator = null!;
    public NetworkAnimator networkAnimator = null!;
    public InteractTrigger HealTrigger = null!;
    public GameObject particleSystemGameObject = null!;
    public List<Transform> revivePositions = new();

    [NonSerialized] public float boomboxTimer = 0f;
    [NonSerialized] public NetworkVariable<bool> boomboxPlaying = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private System.Random random = new();
    private List<GameObject> particlesSpawned = new();
    private bool currentlyHealing = false;
    private NetworkVariable<float> cooldownTimer = new(5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private static readonly int playerIsNearby = Animator.StringToHash("playerIsNearby"); // bool
    private static readonly int isDancing = Animator.StringToHash("isDancing"); // bool
    private static readonly int doSquishAnimation = Animator.StringToHash("doSquish"); // trigger
    private static readonly int doSucceedAnimation = Animator.StringToHash("doSucceed"); // trigger
    private static readonly int doFailAnimation = Animator.StringToHash("doFail"); // trigger
    private NetworkVariable<int> healChargeCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> reviveChargeCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Dictionary<PlayerControllerB, float> playerMaxHealthDict = new();

    public static List<SCP999GalAI> Instances = new();

    public void Start()
    {
        Instances.Add(this);
        UpdatePlayerHealths();
        QualitySettings.skinWeights = SkinWeights.FourBones;
        random = new System.Random(StartOfRound.Instance.randomMapSeed + 40);
        if (IsServer)
        {
            RechargeGalHealsAndRevivesServerRpc(true, true);
            StartCoroutine(DetectingNearbyPlayer());
            MakeTriggerInteractableServerRpc(false);
        }
        HealTrigger.onInteract.AddListener(HealPlayerInteraction);
    }

    public void Update()
    {
        if (cooldownTimer.Value <= 0f)
        {
            bool moreThan0HealCapacity = healChargeCount.Value > 0;
            bool moreThan0ReviveCapacity = reviveChargeCount.Value > 0;
            if (moreThan0HealCapacity && moreThan0ReviveCapacity)
            {
                HealTrigger.hoverTip =  "Heals Left: " + healChargeCount.Value + "\nRevives Left: " + reviveChargeCount.Value;
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
            HealTrigger.hoverTip = "Healing Cooldown: " + Math.Round(cooldownTimer.Value, 2);
        }

        if (!IsServer) return;
        if (!currentlyHealing)
        {
            // Plugin.ExtendedLogging($"Cooldown timer: {cooldownTimer}");
            cooldownTimer.Value -= Time.deltaTime;
        }
        BoomboxUpdate();
    }

    private void BoomboxUpdate()
    {
        if (!boomboxPlaying.Value) return;

        boomboxTimer += Time.deltaTime;
        if (boomboxTimer >= 2f)
        {
            boomboxTimer = 0f;
            boomboxPlaying.Value = false;
            animator.SetBool(isDancing, false);
        }
    }

    public void UpdatePlayerHealths()
    {
        playerMaxHealthDict.Clear();
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            int maxHealth = GrabMaxHealthForPlayer(player);
            playerMaxHealthDict.Add(player, maxHealth);
        }
    }

    public int GrabMaxHealthForPlayer(PlayerControllerB player)
    {
        return 100;
    }

    private IEnumerator DetectingNearbyPlayer()
    {
        while (true)
        {
            bool foundPlayer = false;
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerDead || !player.isPlayerControlled || !player.isInHangarShipRoom) continue;
                if (Vector3.Distance(transform.position, player.transform.position) <= 10f)
                {
                    foundPlayer = true;
                    break;
                }
            }
            animator.SetBool(playerIsNearby, foundPlayer);
            yield return new WaitForSeconds(1f);
            yield return new WaitForEndOfFrame();
        }
    }

    private void HealPlayerInteraction(PlayerControllerB playerInteracting)
    {
        Plugin.ExtendedLogging($"Healing player: {playerInteracting} | Cooldown timer: {cooldownTimer.Value} | Heal Charge count: {healChargeCount.Value} | Revive Charge count: {reviveChargeCount.Value}");
        if (boomboxPlaying.Value) return;
        if (cooldownTimer.Value > 0f || (healChargeCount.Value <= 0 && reviveChargeCount.Value <= 0))
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
        if (random.NextFloat(0, 100) <= Plugin.ModConfig.Config999GalFailureChance.Value)
        {
            fail = true;
        }
        if (reviveNearbyDeadPlayers && reviveChargeCount.Value > 0)
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                Plugin.ExtendedLogging($"Checking player {player.name} | dead: {player.isPlayerDead} | controlled: {player.isPlayerControlled}");
                if (!player.isPlayerDead) continue;
                float distanceFromGal = Vector3.Distance(transform.position, player.deadBody.transform.position);
                Plugin.ExtendedLogging($"Distance from gal: {distanceFromGal}");
                if (distanceFromGal > 5) continue;
                reviveChargeCount.Value--;
                galDidSomething = true;
                DoALotOfShitToRevivePlayerClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player), fail);
            }
        }
        if (onlyInteractedPlayerHealed)
        {
            if (healChargeCount.Value <= 0 || playerInteracting == null || playerInteracting.isPlayerDead || !playerInteracting.isPlayerControlled || playerInteracting.health >= playerMaxHealthDict[playerInteracting]) return;
            galDidSomething = true;
            HealPlayersClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerInteracting), healAmount, healingSpeed, fail);
        }
        else
        {
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                Plugin.ExtendedLogging($"Checking player {player.name} | dead: {player.isPlayerDead} | controlled: {player.isPlayerControlled}");
                if (healChargeCount.Value <= 0 || player == null || player.isPlayerDead || !player.isPlayerControlled || player.health >= playerMaxHealthDict[player]) continue;
                if (Vector3.Distance(transform.position, player.transform.position) > 5) continue;
                galDidSomething = true;
                HealPlayersClientRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, player), healAmount, healingSpeed, fail);
            }
        }

        if (galDidSomething && !fail)
        {
            networkAnimator.SetTrigger(doSucceedAnimation);
        }
    
        cooldownTimer.Value = Plugin.ModConfig.Config999GalHealCooldown.Value;
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
            if (IsServer) networkAnimator.SetTrigger(doFailAnimation);
            Plugin.ExtendedLogging("Failed to heal player.");
            yield break;
        }
        currentlyHealing = true;
        
        // Instantiate the particle system at the player's position.
        var newParticles = GameObject.Instantiate(particleSystemGameObject, player.transform.position, Quaternion.identity);
        newParticles.SetActive(true);
        particlesSpawned.Add(newParticles);
        
        int totalHealthToHeal = healAmount;
        int healthHealed = 0;
        
        // This variable will track the time passed and calculate health to heal over time.
        float timeElapsed = 0f;

        // While we haven't healed the full amount yet.
        while (healthHealed < totalHealthToHeal && player.health < playerMaxHealthDict[player])
        {
            // Accumulate time.
            timeElapsed += Time.deltaTime;

            // Calculate how much health to heal based on the elapsed time and healingSpeed.
            int healthThisFrame = Mathf.FloorToInt(healAmount * (timeElapsed / healingSpeed)) - healthHealed;

            // Heal the player (ensure we don't exceed the total amount).
            if (healthThisFrame > 0)
            {
                healthHealed += healthThisFrame;
                if (IsServer) healChargeCount.Value -= healthThisFrame;
                player.health += healthThisFrame;
                SetVisualChangesToPlayer(player);
            }

            // Wait for the next frame.
            yield return null;
        }
        
        // Ensure we do not heal more than the target amount.
        if (healthHealed < totalHealthToHeal && player.health < playerMaxHealthDict[player])
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
    private void DoALotOfShitToRevivePlayerClientRpc(int PlayerScriptIndex, bool failed)
    {
        PlayerControllerB PlayerScript = StartOfRound.Instance.allPlayerScripts[PlayerScriptIndex];
        DeadBodyInfo deadBodyInfo = PlayerScript.deadBody;

        if (failed)
        {
            Plugin.ExtendedLogging("Failed to revive player.");
            if (IsServer) networkAnimator.SetTrigger(doFailAnimation);
            return;
        }
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
            Plugin.ExtendedLogging("playerInital is dead, reviving them.");
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

        deadBodyInfo.DeactivateBody(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RechargeGalHealsAndRevivesServerRpc(bool heal, bool revive)
    {
        MakeTriggerInteractableClientRpc(true);
        int ActivePlayerAmount = 0;
        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
        {
            if (player.isPlayerControlled)
            {
                ActivePlayerAmount++;
            }
        }
        Plugin.ExtendedLogging("Active Amount with dict: " + StartOfRound.Instance.ClientPlayerList.Count);
        Plugin.ExtendedLogging($"ActivePlayerAmount: {ActivePlayerAmount} | heal: {heal} | revive: {revive}");
        if (heal)
        {
            healChargeCount.Value = Plugin.ModConfig.Config999GalHealTotalAmount.Value * (Plugin.ModConfig.Config999GalScaleHealAndReviveWithPlayerCount.Value ? ActivePlayerAmount : 1 );
        }
        if (revive)
        {
            reviveChargeCount.Value = Plugin.ModConfig.Config999GalReviveCharges.Value * (Plugin.ModConfig.Config999GalScaleHealAndReviveWithPlayerCount.Value ? ActivePlayerAmount : 1 );
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

    public void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot, int noiseID)
    {
        if (!IsServer) return;
		if (noiseID == 5 && !Physics.Linecast(transform.position, noisePosition, StartOfRound.Instance.collidersAndRoomMask))
		{
            boomboxTimer = 0f;
			boomboxPlaying.Value = true;
            animator.SetBool(isDancing, true);
		}
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Instances.Remove(this);
    }
}