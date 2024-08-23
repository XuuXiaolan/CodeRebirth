using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class RedwoodFeetCollision : MonoBehaviour
{
    public RedwoodTitanAI mainscript;
    private List<PlayerControllerB> playersBeingKicked = new List<PlayerControllerB>();
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playersBeingKicked.Contains(other.GetComponent<PlayerControllerB>()) && mainscript.kickingOut)
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            playersBeingKicked.Add(player);
            StartCoroutine(KickingPlayer(player));
            player.DamagePlayer(20, true, true, CauseOfDeath.Bludgeoning, 0, false);
            Plugin.ExtendedLogging("Kicked player...");
        }
    }

    private IEnumerator KickingPlayer(PlayerControllerB player)
    {
        float duration = 2f;
        Vector3 direction = (player.transform.position - this.gameObject.transform.position).normalized;
        while (duration > 0) {
            duration -= Time.fixedDeltaTime;
            player.externalForces = direction * 20f;
            yield return new WaitForFixedUpdate();
        }
        playersBeingKicked.Remove(player);
    }
}