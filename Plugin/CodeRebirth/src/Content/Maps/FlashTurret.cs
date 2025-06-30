using CodeRebirth.src.MiscScripts;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class FlashTurret : CodeRebirthHazard
{
    public CRNoiseListener _FlashTurretNoiseListener = null!; // todo implement this
    public Transform turretTransform = null!;
    public Light flashLight = null!;
    public float rotationSpeed = 90f;
    public float detectionRange = 15f;
    public float flashDuration = 3f;
    public float blindDuration = 5f;
    public float flashIntensity = 10f;
    public float flashCooldown = 3f;
    public AudioSource cameraAudioSource = null!;
    public AudioClip spotPlayerSound = null!;
    public AudioClip flashPlayerSound = null!;

    private bool isTriggered = false;
    private PlayerControllerB? detectedPlayer = null;
    private bool isFlashing = false;
    private float flashTimer = 0f;
    private float cooldownTimer = 0f;

    public override void Start()
    {
        base.Start();
        _FlashTurretNoiseListener._onNoiseDetected.AddListener(OnNoiseDetected);
        Plugin.ExtendedLogging("Flash Turret initialized");
    }

    private void Update()
    {
        UpdateAudio();
        // Rotate turret towards player if triggered
        if (isTriggered && detectedPlayer != null)
        {
            Vector3 directionToPlayer = (detectedPlayer.transform.position - turretTransform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            turretTransform.rotation = Quaternion.RotateTowards(turretTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Check if turret has finished rotating towards the player
            if (Quaternion.Angle(turretTransform.rotation, targetRotation) < 1f && !isFlashing)
            {
                // Check if the player is looking towards the turret
                Vector3 playerForward = detectedPlayer.gameplayCamera.transform.forward;
                Vector3 directionToTurret = (turretTransform.position - detectedPlayer.transform.position).normalized;
                float dotProduct = Vector3.Dot(playerForward, directionToTurret);
                TriggerFlash(dotProduct);
            }
        }

        // Handle flashing light duration
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            flashLight.intensity = Mathf.Lerp(flashIntensity, 0f, 1 - (flashTimer / 5f));
            if (flashTimer <= 0f)
            {
                flashLight.intensity = 0f;
                isFlashing = false;
                cooldownTimer = flashCooldown;
                ResetTurret();
            }
        }

        // Handle cooldown before detection can occur again
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void UpdateAudio()
    {
        cameraAudioSource.volume = Plugin.ModConfig.ConfigFlashTurretVolume.Value;
    }

    public void OnNoiseDetected(NoiseParams noiseParams)
    {
        Plugin.ExtendedLogging($"Flash turret hearing noiseID: {noiseParams.noiseID}");
        if (noiseParams.noiseID != 6)
            return;

        if (detectedPlayer != null || cooldownTimer > 0f)
            return;

        float distance = Vector3.Distance(turretTransform.position, noiseParams.noisePosition);
        if (distance > detectionRange)
            return;

        Ray ray = new Ray(turretTransform.position, noiseParams.noisePosition - turretTransform.position);
        if (!Physics.Raycast(ray, out RaycastHit hit, distance + 1f, StartOfRound.Instance.collidersAndRoomMaskAndPlayers, QueryTriggerInteraction.Collide))
            return;

        if (hit.collider == null || !hit.collider.TryGetComponent(out PlayerControllerB player))
            return;

        detectedPlayer = player;
        isTriggered = true;
        cameraAudioSource.PlayOneShot(spotPlayerSound);
    }

    private void TriggerFlash(float dotProduct)
    {
        cameraAudioSource.PlayOneShot(flashPlayerSound, 0.5f);
        if (flashLight != null)
        {
            flashLight.intensity = flashIntensity;
            isFlashing = true;
            flashTimer = flashDuration;
        }

        if (detectedPlayer != null && dotProduct > 0.5f)
        {
            StunGrenadeItem.StunExplosion(detectedPlayer.transform.position, true, 0.5f, 5f, 1f, false, null, null, 0);
        }
        RoundManager.Instance.PlayAudibleNoise(this.transform.position, 20, 0.5f, 0, false, 75);
    }

    private void ResetTurret()
    {
        isTriggered = false;
        detectedPlayer = null;
    }
}