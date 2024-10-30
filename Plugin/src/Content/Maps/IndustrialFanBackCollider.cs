using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFanBackCollider : NetworkBehaviour
{
    public IndustrialFan industrialFan = null!;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            if (!industrialFan.IsObstructed(other.transform.position) && player == GameNetworkManager.Instance.localPlayerController)
            {
                industrialFan.windAudioSource.volume = 0.8f;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            if (!industrialFan.IsObstructed(other.transform.position))
            {
                Vector3 suctionDirection = (industrialFan.fanTransform.position - other.transform.position).normalized;
                Vector3 force = suctionDirection * industrialFan.suctionForce;
                player.externalForces += force;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            industrialFan.windAudioSource.volume = 0.2f;
        }
    }
}