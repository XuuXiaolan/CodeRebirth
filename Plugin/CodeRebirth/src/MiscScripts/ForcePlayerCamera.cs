using System;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
/// <summary>
/// Combines camera override (AimTarget) and auto-pull aiming towards a target object.
/// </summary>
public class ForcePlayerCamera : MonoBehaviour
{
    [Header("Auto-Aim Pull Settings")]
    [SerializeField] private float horizontalPullStrength = 2000f;
    [SerializeField] private float verticalPullStrength = 25f;
    [SerializeField] private Vector2 horizontalDeadzone = new Vector2(0.46f, 0.54f);
    [SerializeField] private Vector2 verticalDeadzone = new Vector2(0.35f, 0.6f);
    [SerializeField] private float cameraLerpSpeed = 20f;
    [SerializeField] private bool forceAlign = true;
    [SerializeField] private float forceAlignMinPull = 0.01f;

    [Header("Aim Target Override")]
    [SerializeField] private AnimationCurve AimTargetCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private float AimSpeedMouse = 1f;

    [Header("References")]
    public Camera gameplayCamera;        // The camera for vertical adjustments
    public Transform turnCompass;        // Pivot for horizontal yaw
    public Transform playerBody;         // Player body transform for leaning alignment
    public Animator playerBodyAnimator;  // Animator for pull animations
    [Range(0f, 1f)] public float normalizedStrength = 1f;

    // Internal aim state
    private float aimHorizontal;
    private float aimVertical;
    private Quaternion playerAim = Quaternion.identity;

    // AimTarget override state
    private bool AimTargetActive;
    private Vector3 AimTargetPosition;
    private float AimTargetTimer;
    private float AimTargetSpeed;
    private float AimTargetLerp;
    private int AimTargetPriority = -999;

    // Soft aim state
    private Vector3 AimTargetSoftPosition;
    private float AimTargetSoftTimer;
    private float AimTargetSoftStrength;
    private float AimTargetSoftStrengthNoAim;
    private float AimTargetSoftStrengthCurrent;
    private int AimTargetSoftPriority = -999;

    private float PlayerAimingTimer;
    private float overrideAimStopTimer;
    private bool overrideAimStop;
    private float overrideAimSmooth;
    private float overrideAimSmoothTimer;

    public void SetupPlayer(PlayerControllerB player)
    {
        gameplayCamera = player.gameplayCamera;
        turnCompass = player.turnCompass;
        playerBody = player.thisPlayerBody;
        playerBodyAnimator = player.playerBodyAnimator;
    }

    public void AimTargetSet(Vector3 position, float time, float speed, GameObject obj, int priority)
    {
        if (priority < AimTargetPriority)
            return;

        if (AimTargetLerp != 0f)
            return;

        AimTargetActive = true;
        AimTargetPosition = position;
        AimTargetTimer = time;
        AimTargetSpeed = speed;
        AimTargetPriority = priority;
    }

    public void SetNormalizedStrength(float strength)
    {
        normalizedStrength = strength;
    }

    public void AimTargetSoftSet(Vector3 position, float time, float strength, float strengthNoAim, GameObject obj, int priority)
    {
        if (priority < this.AimTargetSoftPriority)
            return;

        if (AimTargetSoftPosition != Vector3.zero)
            return;

        if (obj != null && AimTargetSoftTimer <= 0f)
            PlayerAimingTimer = 0f;

        AimTargetSoftPosition = position;
        AimTargetSoftTimer = time;
        AimTargetSoftStrength = strength;
        AimTargetSoftStrengthNoAim = strengthNoAim;
        AimTargetSoftPriority = priority;
    }

    public void OverrideAimStop()
    {
        overrideAimStopTimer = 0.2f;
    }

    private void OverrideAimStopTick()
    {
        if (overrideAimStopTimer > 0f)
        {
            overrideAimStop = true;
            overrideAimStopTimer -= Time.deltaTime;
        }
        else
        {
            overrideAimStop = false;
        }
    }

    private void ResetPlayerAim(Quaternion _rotation)
    {
        // Normalize to -180..180 for vertical
        float x = _rotation.eulerAngles.x;
        aimVertical = (x > 180f) ? x - 360f : x;
        aimHorizontal = _rotation.eulerAngles.y;
        playerAim = _rotation;
    }

    private void HandleAutoAim()
    {
        Vector3 worldPos = AimTargetPosition;
        float dt = Mathf.Clamp(Time.deltaTime, 0f, 0.1f);

        // --- HORIZONTAL PULL ---
        Vector3 targetScreen = gameplayCamera.WorldToViewportPoint(worldPos);
        float pullPos = targetScreen.x - 0.5f;
        float absPull = Mathf.Abs(pullPos);
        float horizStrength = horizontalPullStrength * normalizedStrength;

        if (targetScreen.x < horizontalDeadzone.x || targetScreen.x > horizontalDeadzone.y)
        {
            float dir = Mathf.Sign(pullPos);
            float rotAmount = dir * horizStrength * dt * absPull;
            turnCompass.Rotate(Vector3.up * rotAmount);
            playerBodyAnimator.SetBool("PullingCameraRight", dir > 0f);
            playerBodyAnimator.SetBool("PullingCameraLeft", dir < 0f);
        }
        else
        {
            playerBodyAnimator.SetBool("PullingCameraLeft", false);
            playerBodyAnimator.SetBool("PullingCameraRight", false);

            if (forceAlign && absPull < forceAlignMinPull)
            {
                Vector3 lookDir = (worldPos - turnCompass.position).normalized;
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                turnCompass.rotation = Quaternion.Slerp(
                    turnCompass.rotation,
                    targetRot,
                    dt * cameraLerpSpeed * normalizedStrength
                );
            }
        }

        // --- VERTICAL PULL ---
        Vector3 vertScreen = gameplayCamera.WorldToViewportPoint(worldPos + Vector3.up * 0.35f);
        float vertOffset = vertScreen.y - 0.5f;
        float absVert = Mathf.Abs(vertOffset);
        float vertStrength = verticalPullStrength * normalizedStrength;
        float newVert = aimVertical;

        if (vertScreen.y > verticalDeadzone.y)
        {
            newVert = Mathf.Clamp(
                Mathf.Lerp(aimVertical, aimVertical - vertStrength, vertStrength * dt * absVert),
                -89f, 89f
            );
        }
        else if (vertScreen.y < verticalDeadzone.x)
        {
            newVert = Mathf.Clamp(
                Mathf.Lerp(aimVertical, aimVertical + vertStrength, vertStrength * dt * absVert),
                -89f, 89f
            );
        }

        aimVertical = newVert;
        Vector3 camEuler = gameplayCamera.transform.localEulerAngles;
        gameplayCamera.transform.localEulerAngles = new Vector3(aimVertical, camEuler.y, camEuler.z);

        // --- BODY ALIGNMENT ---
        Vector3 bodyEuler = Vector3.up * turnCompass.eulerAngles.y;
        playerBody.rotation = Quaternion.Lerp(
            playerBody.rotation,
            Quaternion.Euler(bodyEuler),
            dt * cameraLerpSpeed * (1f - absPull)
        );
    }

    private void Update()
    {
        float cameraSmoothing = 1f;
        AimSpeedMouse = Mathf.Lerp(0.2f, 4f, 20f / 100f);

        // USER INPUT (when no hard override)
        if (AimTargetTimer <= 0f && !overrideAimStop)
        {
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            if (AimTargetSoftTimer > 0f)
            {
                mouseDelta = (mouseDelta.magnitude > 1f) ? mouseDelta.normalized : Vector2.zero;
            }
            else
            {
                mouseDelta *= AimSpeedMouse;
            }

            aimHorizontal += mouseDelta.x;
            aimVertical = Mathf.Clamp(aimVertical - mouseDelta.y, -70f, 80f);
            playerAim = Quaternion.Euler(aimVertical, aimHorizontal, 0f);

            if (cameraSmoothing > 0f)
                playerAim = Quaternion.RotateTowards(
                    transform.localRotation,
                    playerAim,
                    10000f * Time.deltaTime
                );

            if (mouseDelta.magnitude > 0f)
                PlayerAimingTimer = 0.1f;
        }

        // TIMERS
        if (PlayerAimingTimer > 0f)
            PlayerAimingTimer -= Time.deltaTime;

        if (AimTargetTimer > 0f)
        {
            AimTargetTimer -= Time.deltaTime;
            AimTargetLerp = Mathf.Clamp01(AimTargetLerp + Time.deltaTime * AimTargetSpeed);
        }
        else if (AimTargetLerp > 0f)
        {
            ResetPlayerAim(transform.localRotation);
            AimTargetLerp = 0f;
            AimTargetPriority = -999;
            AimTargetActive = false;
        }

        // APPLY OVERRIDE AIM TARGET
        Quaternion finalRot = playerAim;
        if (AimTargetActive)
        {
            finalRot = Quaternion.LerpUnclamped(
                playerAim,
                Quaternion.LookRotation(AimTargetPosition - transform.position),
                AimTargetCurve.Evaluate(AimTargetLerp)
            );
        }

        if (AimTargetSoftTimer > 0f && AimTargetTimer <= 0f)
        {
            float strength = (PlayerAimingTimer <= 0f)
                ? AimTargetSoftStrengthNoAim
                : AimTargetSoftStrength;
            AimTargetSoftStrengthCurrent = Mathf.Lerp(
                AimTargetSoftStrengthCurrent,
                strength,
                10f * Time.deltaTime
            );

            Quaternion softRot = Quaternion.LookRotation(AimTargetSoftPosition - transform.position);
            finalRot = Quaternion.Lerp(finalRot, softRot, AimTargetSoftStrengthCurrent * Time.deltaTime);

            AimTargetSoftTimer -= Time.deltaTime;
            if (AimTargetSoftTimer <= 0f)
                AimTargetSoftPriority = -999;
        }

        // OVERRIDE SMOOTH STOP
        if (overrideAimSmoothTimer > 0f)
        {
            cameraSmoothing = overrideAimSmooth;
            overrideAimSmoothTimer -= Time.deltaTime;
        }

        // DECIDE AUTO-AIM OR DIRECT ROTATION
        if (!AimTargetActive && AimTargetSoftTimer <= 0f)
        {
            HandleAutoAim();
        }
        else
        {
            // DIRECT CAMERA ROTATION
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                finalRot,
                ((cameraSmoothing / 100f) * Time.deltaTime)
            );
            ResetPlayerAim(transform.localRotation);
        }

        OverrideAimStopTick();
    }
}