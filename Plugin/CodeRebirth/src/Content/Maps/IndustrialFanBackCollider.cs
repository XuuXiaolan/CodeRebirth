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
            playersInRange.Add(player);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
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
                Vector3 direction = (targetPosition - player.transform.position).normalized;
                float step = industrialFan.suctionForce * Time.fixedDeltaTime;

                if (Vector3.Distance(player.transform.position, targetPosition) > step)
                {
                    player.transform.position += direction * step;
                }
                else
                {
                    player.transform.position = targetPosition;
                }
            }
        }
    }
}