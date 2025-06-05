using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class AutonomousCraneTrigger : MonoBehaviour
{
    public AutonomousCrane mainScript = null!;

    public void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerB player) && !player.isPlayerDead)
        {
            // mainScript.OnCollideWithPlayer(other);
        }
    }
}