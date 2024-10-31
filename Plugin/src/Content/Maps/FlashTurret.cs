using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class FlashTurret : NetworkBehaviour, INoiseListener
{
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

    private void Update()
    {
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

    public void OnNoiseDetected(Vector3 noisePosition, int noiseID)
    {
        OnNoiseDetectedClientRpc(noisePosition, noiseID);
    }

    [ClientRpc]
    public void OnNoiseDetectedClientRpc(Vector3 noisePosition, int noiseID)
    {
        if (detectedPlayer != null || cooldownTimer > 0f) return;

        float distance = Vector3.Distance(turretTransform.position, noisePosition);
        int playerNoiseID = 6;
        if (distance <= detectionRange && noiseID == playerNoiseID)
        {
            if (Physics.Raycast(turretTransform.position, (noisePosition - turretTransform.position).normalized, out RaycastHit hit, detectionRange, StartOfRound.Instance.collidersAndRoomMaskAndPlayers, QueryTriggerInteraction.Collide))
            {
                if (hit.collider != null && hit.collider.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
                {
                    detectedPlayer = player;
                    isTriggered = true;
                    cameraAudioSource.PlayOneShot(spotPlayerSound);
                }
            }
        }
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
            StunGrenadeItem.StunExplosion(detectedPlayer.transform.position, true, 0.5f, 5f, 1f, false, null, null, blindDuration - 1);
        }
    }

    private void ResetTurret()
    {
        isTriggered = false;
        detectedPlayer = null;
    }

    public void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot, int noiseID)
    {
        OnNoiseDetected(noisePosition, noiseID);
    }
}