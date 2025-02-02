using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFan : CodeRebirthHazard
{
    public Transform fanTransform = null!;
    public AudioSource cutAudioSource = null!;
    public float rotationSpeed = 45f;
    public float pushForce = 15f;
    public float suctionForce = 15f;
    public ParticleSystem redMistEffect = null!;

    public override void Start()
    {
        base.Start();
        Plugin.ExtendedLogging("Industrial fan initialized");
    }

    public void FixedUpdate()
    {
        // Rotate the fan continuously
        fanTransform.Rotate(Vector3.up, rotationSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kill players who touch the back blades
        if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            cutAudioSource.Play();
            player.KillPlayer(player.velocityLastFrame, !Plugin.ModConfig.ConfigHazardsDeleteBodies.Value, CauseOfDeath.Fan, 0, default);
            PlayRedMist();
        }
    }

    public bool IsObstructed(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - fanTransform.position).normalized;
        float distance = Vector3.Distance(fanTransform.position, targetPosition);
        if (Physics.Raycast(fanTransform.position, direction, out RaycastHit hit, distance, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("InteractableObject", "Railing"), QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject.layer != 3)
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
