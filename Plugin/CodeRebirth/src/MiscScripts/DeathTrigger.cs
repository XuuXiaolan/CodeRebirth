using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class DeathTrigger : MonoBehaviour
{

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerB player) && player == GameNetworkManager.Instance.localPlayerController)
        {
            player.KillPlayer(player.velocityLastFrame, true, CauseOfDeath.Crushing, 0, default);
        }
    }
}