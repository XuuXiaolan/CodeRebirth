using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFanBackCollider : NetworkBehaviour
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
                // Calculate the new position by interpolating between the player's current position and the fan's position
                Vector3 targetPosition = industrialFan.fanTransform.position;
                player.transform.position = Vector3.Lerp(player.transform.position, targetPosition, industrialFan.suctionForce * Time.fixedDeltaTime);
            }
        }
    }
}