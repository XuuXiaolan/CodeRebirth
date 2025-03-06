using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class KillAndShredPlayer : MonoBehaviour
{
    public ShreddingSarah sarah = null!;

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerB player) && player.IsOwner)
        {
            player.KillPlayer(player.transform.position, false, CauseOfDeath.Crushing, 0, default);
            sarah.TryFeedItemServerRpc(true, 10);
        }
    }
}