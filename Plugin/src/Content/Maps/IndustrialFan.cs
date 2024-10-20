using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFan : NetworkBehaviour
{
    public Transform fanTransform = null!;
    public AudioSource cutAudioSource = null!;
    public float rotationSpeed = 45f;
    public float pushForce = 15f;
    public float suctionForce = 15f;
    public ParticleSystem redMistEffect = null!;

    private void Update()
    {
        if (!IsServer) return;
        // Rotate the fan continuously
        fanTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kill players who touch the back blades
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            cutAudioSource.Play();
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
