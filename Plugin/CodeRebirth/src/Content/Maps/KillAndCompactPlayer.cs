using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class KillAndCompactPlayer : MonoBehaviour
{
    public CompactorToby toby = null!;

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerB player) && player.IsOwner)
        {
            player.KillPlayer(player.transform.position, false, CauseOfDeath.Crushing, 0, default);
            toby.TryCompactItemServerRpc(10, true);
        }
    }
}