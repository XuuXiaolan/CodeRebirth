using CodeRebirth.src.Util;
using GameNetcodeStuff;
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
        if (other.TryGetComponent(out PlayerControllerB player))
        {
            cutAudioSource.Play();
            player.KillPlayer(player.velocityLastFrame, !Plugin.ModConfig.ConfigHazardsDeleteBodies.Value, CauseOfDeath.Fan, 9, default);
            PlayRedMist();
        }
    }

    public bool IsObstructed(Vector3 targetPosition)
    {
        return Physics.Linecast(fanTransform.position, targetPosition, out _, CodeRebirthUtils.Instance.collidersAndRoomAndRailingAndInteractableMask, QueryTriggerInteraction.Ignore);
    }

    private void PlayRedMist()
    {
        redMistEffect.Play();
    }
}
