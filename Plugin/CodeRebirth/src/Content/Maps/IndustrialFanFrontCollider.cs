using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFanFrontCollider : NetworkBehaviour
{
    public IndustrialFan industrialFan = null!;
    private HashSet<PlayerControllerB> playersInRange = new HashSet<PlayerControllerB>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            if (!industrialFan.IsObstructed(other.transform.position) && player == GameNetworkManager.Instance.localPlayerController)
            {
                industrialFan.windAudioSource.volume = 0.8f;
            }
            playersInRange.Add(player);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            industrialFan.windAudioSource.volume = 0.2f;
            playersInRange.Remove(player);
        }
    }

    private void FixedUpdate()
    {
        foreach (PlayerControllerB player in playersInRange)
        {
            if (!industrialFan.IsObstructed(player.transform.position))
            {
                // Calculate the direction to push the player away from the fan
                Vector3 pushDirection = (player.transform.position - industrialFan.fanTransform.position).normalized;

                // Calculate the target position by moving the player in the push direction
                Vector3 targetPosition = player.transform.position + (pushDirection * industrialFan.pushForce);

                // Check if the target position is valid using a raycast or other collision checks
                if (!Physics.Raycast(player.transform.position, pushDirection, industrialFan.pushForce, StartOfRound.Instance.collidersAndRoomMask | LayerMask.GetMask("InteractableObject", "Railing"), QueryTriggerInteraction.Ignore))
                {
                    // Interpolate smoothly between the current position and the target position
                    player.transform.position = Vector3.Lerp(player.transform.position, targetPosition, industrialFan.pushForce * Time.fixedDeltaTime);
                }
                else
                {
                    player.externalForces += pushDirection * industrialFan.pushForce * 0.1f;
                }
            }
        }
    }
}