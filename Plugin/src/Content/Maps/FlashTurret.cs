using System.Linq;
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

    private bool isTriggered = false;
    private float flashTimer = 0f;
    private PlayerControllerB? detectedPlayer = null;
    private bool isFlashing = false;

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
        if (!IsServer) return;

        float distance = Vector3.Distance(turretTransform.position, noisePosition);
        int playerNoiseID = 0;
        if (distance <= detectionRange && noiseID == playerNoiseID)
        {
            detectedPlayer = StartOfRound.Instance.allPlayerScripts.Where(x => !x.isPlayerDead && x.isPlayerControlled && Vector3.Distance(x.transform.position, noisePosition) <= detectionRange).OrderBy(x => Vector3.Distance(x.transform.position, noisePosition)).FirstOrDefault();
            isTriggered = true;
            warningSound.Play();
            flashTimer = flashDuration; // Start flashing after initial detection
        }
    }

    private void TriggerFlash()
    {
        if (flashLight != null)
        {
            flashLight.intensity = flashIntensity;
            isFlashing = true;
        }

        if (detectedPlayer != null && detectedPlayer.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            // ApplyBlindness(blindDuration, player);
        }
    }

    public void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot, int noiseID)
    {
        OnNoiseDetected(noisePosition, noiseID);
    }
}