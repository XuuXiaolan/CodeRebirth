using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CodeRebirth.src.Content.Maps;
public class LaserTurret : NetworkBehaviour
{
    public Transform turretTransform = null!;
    public Transform laserStartPoint = null!;
    public LineRenderer laserLineRenderer = null!;
    public float rotationSpeed = 45f;
    public float laserRange = 50f;
    public float laserDamage = 1f;
    public float laserThickness = 0.3f;
    public ParticleSystem ashParticle = null!;

    private bool isFiring = false;
    private float damageTimer = 0f;

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
            Vector3 laserEndPoint = hit.point;
            laserLineRenderer.SetPosition(0, laserStartPoint.position);
            laserLineRenderer.SetPosition(1, laserEndPoint);

            if (!isFiring)
            {
                isFiring = true;
                laserLineRenderer.enabled = true;
            }

            if (hit.collider.CompareTag("Player") && hit.collider.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
            {
                damageTimer -= Time.deltaTime;
                if (damageTimer <= 0f)
                {
                    if (player.health > laserDamage) player.DamagePlayer((int)laserDamage, true, false, CauseOfDeath.Burning, 0, false, default);
                    else player.KillPlayer(default, false, CauseOfDeath.Burning, 0, default);
                    damageTimer = 0.1f;

                    if (player.isPlayerDead)
                    {
                        SpawnAshParticle(player.transform.position);
                    }
                }
            }
        }
        else
        {
            if (isFiring)
            {
                isFiring = false;
                laserLineRenderer.enabled = false;
            }
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
            float angle = i * (360/raycastCount);
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
            this.transform.position = Vector3.Lerp(this.transform.position, averagePosition, 0.5f);
            NavMesh.SamplePosition(this.transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas);
            this.transform.position = hit.position;
        }
    }

    private void Start()
    {
        if (IsServer)
        {
            ValidateSpawnPosition();
        }
    }
}