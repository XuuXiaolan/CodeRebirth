using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class RedwoodFeetCollision : MonoBehaviour
{
    public RedwoodTitanAI mainscript;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            PlayerControllerB player = collision.collider.GetComponent<PlayerControllerB>();
            if (player == GameNetworkManager.Instance.localPlayerController && mainscript.startedKick)
                player.DamagePlayer(50, true, true, CauseOfDeath.Bludgeoning, 0, false);
                player.externalForces = this.gameObject.transform.forward * 20f;
        }
    }
}