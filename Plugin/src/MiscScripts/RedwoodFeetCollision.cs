using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class RedwoodFeetCollision : MonoBehaviour
{
    public RedwoodTitanAI mainscript;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            if (player == GameNetworkManager.Instance.localPlayerController && mainscript.kicking)
                Plugin.ExtendedLogging("Kicked player...");
                player.DamagePlayer(20, true, true, CauseOfDeath.Bludgeoning, 0, false);
                player.externalForces = this.gameObject.transform.forward * 500f;
        }
    }
}