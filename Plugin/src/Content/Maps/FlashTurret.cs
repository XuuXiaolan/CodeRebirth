using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class FlashTurret : NetworkBehaviour, INoiseListener
{
    public Transform turretTransform = null!;
    public Light flashLight = null!;
    public AudioSource warningSound = null!;
    public float rotationSpeed = 90f;
    public float detectionRange = 15f;
    public float flashCooldownMin = 3f;
    public float flashCooldownMax = 5f;
    public float flashDuration = 1f;
    public float blindDuration = 5f;
    public float flashIntensity = 10f;
    public float flashFadeSpeed = 5f;
    public int maxFlashes = 3;

    private bool isTriggered = false;
    private float flashTimer = 0f;
    private PlayerControllerB? detectedPlayer = null;
    private bool isFlashing = false;
    private int currentFlashCount = 0;

    private void Update()
    {
        if (!IsServer) return;

        // Rotate turret towards player if triggered
        if (isTriggered && detectedPlayer != null)
        {
            Vector3 directionToPlayer = (detectedPlayer.transform.position - turretTransform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            turretTransform.rotation = Quaternion.RotateTowards(turretTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Handle flashing cooldown
        if (isTriggered)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                TriggerFlash();
                flashTimer = UnityEngine.Random.Range(flashCooldownMin, flashCooldownMax);
                currentFlashCount++;

                // Reset after max flashes
                if (currentFlashCount >= maxFlashes)
                {
                    ResetTurret();
                }
            }
        }

        // Handle flashing light intensity
        if (isFlashing)
        {
            flashLight.intensity = Mathf.Lerp(flashLight.intensity, 0f, flashFadeSpeed * Time.deltaTime);
            if (flashLight.intensity <= 0.1f)
            {
                flashLight.intensity = 0f;
                isFlashing = false;
            }
        }
    }

    public void OnNoiseDetected(Vector3 noisePosition, int noiseID)
    {
        if (!IsServer || detectedPlayer != null) return;

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
                    warningSound.Play();
                    flashTimer = flashDuration; // Start flashing after initial detection
                    currentFlashCount = 0; // Reset flash count on new detection
                }
            }
        }
    }

    private void TriggerFlash()
    {
        if (flashLight != null)
        {
            flashLight.intensity = flashIntensity;
            isFlashing = true;
        }

        if (detectedPlayer != null)
        {
            StunGrenadeItem.StunExplosion(detectedPlayer.transform.position, true, 0.4f, 5f, 1f, false, null, null, blindDuration - 1);
        }
    }

    private void ResetTurret()
    {
        isTriggered = false;
        detectedPlayer = null;
        currentFlashCount = 0;
    }

    public void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot, int noiseID)
    {
        OnNoiseDetected(noisePosition, noiseID);
    }
}