using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class PlayerOnlyTriggers : MonoBehaviour
{
    public EnemyAI mainScript = null!;

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerControllerB player) && !player.isPlayerDead)
        {
            mainScript.OnCollideWithPlayer(other);
        }
    }
}