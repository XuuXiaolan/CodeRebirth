using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFan : NetworkBehaviour
{
    public Transform fanTransform = null!;
    public float rotationSpeed = 45f;
    public float pushForce = 15f;
    public float suctionForce = 15f;
    public VisualEffect windForwardEffect = null!;
    public VisualEffect windBackwardEffect = null!;
    public ParticleSystem redMistEffect = null!;

    private void Start()
    {
        windForwardEffect.Play();
        windBackwardEffect.Play();
    }

    private void Update()
    {
        // Rotate the fan continuously
        float frontDistance = GetDistanceToWall(fanTransform.forward);
        float backDistance = GetDistanceToWall(-fanTransform.forward);
        fanTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private float GetDistanceToWall(Vector3 direction)
    {
        if (Physics.Raycast(fanTransform.position, direction, out RaycastHit hit, 50f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Collide))
        {
            return hit.distance;
        }
        return 50f;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kill players who touch the back blades
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            player.KillPlayer(default, false, CauseOfDeath.Fan, 0, default);
            if (player.isPlayerDead) PlayRedMist();
        }
    }

    public bool IsObstructed(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - fanTransform.position).normalized;
        float distance = Vector3.Distance(fanTransform.position, targetPosition);
        if (Physics.Raycast(fanTransform.position, direction, out RaycastHit hit, distance, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Collide))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                // There is an obstruction between the fan and the player
                return true;
            }
        }
        return false;
    }

    private void PlayRedMist()
    {
        redMistEffect.Play();
    }
}
