using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

namespace CodeRebirth.src.Content.Maps;
public class LaserTurret : NetworkBehaviour
{
    public Transform turretTransform = null!;
    public Transform laserStartPoint = null!;
    public VisualEffect visualEffect = null!;
    public float rotationSpeed = 45f;
    public float laserRange = 50f;
    public float laserDamage = 3f;
    public float laserThickness = 0.5f;
    public ParticleSystem ashParticle = null!;

    private float originalImpactPositionZ = 1f;
    private float originalParticlesVelocityZ = 1f;
    private float originalDarkBeamScaleY = 1f;
    private float originalElectricBeamScaleY = 1f;
    private float originalBeamCoreScaleY = 1f;
    private bool isFiring = false;
    private float damageTimer = 0f;

    private void Start()
    {
        visualEffect.Play();
        originalImpactPositionZ = visualEffect.GetVector3("ImpactPosition").z;
        originalParticlesVelocityZ = visualEffect.GetVector3("ParticlesVelocity").z;
        originalDarkBeamScaleY = visualEffect.GetVector3("DarkBeamScale").y;
        originalElectricBeamScaleY = visualEffect.GetVector3("ElectricBeamScale").y;
        originalBeamCoreScaleY = visualEffect.GetVector3("BeamCoreScale").y;

        if (IsServer)
        {
            ValidateSpawnPosition();
        }
    }

    private void Update()
    {
        // Rotate the turret
        if (IsServer) turretTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Fire laser continuously
        FireLaser();
    }

    private void FireLaser()
    {
        Vector3 laserDirection = laserStartPoint.forward;
        if (Physics.SphereCast(laserStartPoint.position, laserThickness / 2, laserDirection, out RaycastHit hit, laserRange, StartOfRound.Instance.collidersAndRoomMaskAndPlayers, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("Player") && hit.collider.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
            {
                if (player.isCrouching && player.gameplayCamera.transform.position.y + laserThickness < laserStartPoint.position.y)
                {
                    // Ignore player and continue laser
                    Vector3 newStart = hit.point + laserDirection * 0.01f; // Move start point slightly beyond player
                    if (Physics.SphereCast(newStart, laserThickness / 2, laserDirection, out RaycastHit newHit, laserRange - hit.distance, StartOfRound.Instance.collidersAndRoomMaskAndPlayers, QueryTriggerInteraction.Collide))
                    {
                        hit = newHit;
                    }
                    else
                    {
                        // No further hit, end laser
                        UpdateLaserVisuals(hit.point);
                        return;
                    }
                }
            }

            UpdateLaserVisuals(hit.point);

            if (hit.collider.CompareTag("Player") && hit.collider.TryGetComponent<PlayerControllerB>(out PlayerControllerB targetPlayer))
            {
                damageTimer -= Time.deltaTime;
                if (damageTimer <= 0f)
                {
                    if (targetPlayer.health > laserDamage) targetPlayer.DamagePlayer((int)laserDamage, true, false, CauseOfDeath.Burning, 0, false, default);
                    else targetPlayer.KillPlayer(default, false, CauseOfDeath.Burning, 0, default);
                    damageTimer = 0.1f;

                    if (targetPlayer.isPlayerDead)
                    {
                        SpawnAshParticle(targetPlayer.transform.position);
                    }
                }
            }
        }
        else
        {
            if (isFiring)
            {
                isFiring = false;
                //laserLineRenderer.enabled = false;
            }
        }
    }

    private void UpdateLaserVisuals(Vector3 laserEndPoint)
    {
        float distance = Vector3.Distance(laserStartPoint.position, laserEndPoint) / 5;
        Vector3 beamCoreScale = visualEffect.GetVector3("BeamCoreScale");
        beamCoreScale.y = originalBeamCoreScaleY * distance;
        Vector3 electricBeamScale = visualEffect.GetVector3("ElectricBeamScale");
        electricBeamScale.y = originalElectricBeamScaleY * distance;
        Vector3 darkBeamScale = visualEffect.GetVector3("DarkBeamScale");
        darkBeamScale.y = originalDarkBeamScaleY * distance;
        Vector3 particlesVelocity = visualEffect.GetVector3("ParticlesVelocity");
        particlesVelocity.z = originalParticlesVelocityZ * distance;
        Vector3 impactPosition = visualEffect.GetVector3("ImpactPosition");
        impactPosition.z = originalImpactPositionZ * distance;

        visualEffect.SetVector3("ImpactPosition", impactPosition);
        visualEffect.SetVector3("ParticlesVelocity", particlesVelocity);
        visualEffect.SetVector3("DarkBeamScale", darkBeamScale);
        visualEffect.SetVector3("ElectricBeamScale", electricBeamScale);
        visualEffect.SetVector3("BeamCoreScale", beamCoreScale);

        if (!isFiring)
        {
            isFiring = true;
            //laserLineRenderer.enabled = true;
        }
    }

    private void SpawnAshParticle(Vector3 position)
    {
        Instantiate(ashParticle, position, Quaternion.identity).Play();
    }

    private void ValidateSpawnPosition()
    {
        Vector3 averagePosition = Vector3.zero;
        int validRaycastCount = 0;
        int raycastCount = 16;
        for (int i = 0; i < raycastCount; i++)
        {
            float angle = i * (360 / raycastCount);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            if (Physics.Raycast(turretTransform.position, direction, out RaycastHit hit, laserRange))
            {
                averagePosition += hit.point;
                validRaycastCount++;
            }
        }

        if (validRaycastCount > 0)
        {
            averagePosition /= validRaycastCount;
            averagePosition.y = this.transform.position.y; // Keep the original height
            this.transform.position = Vector3.Lerp(this.transform.position, averagePosition, 0.75f);
            NavMesh.SamplePosition(this.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas);
            this.transform.position = hit.position;
        }
    }
}