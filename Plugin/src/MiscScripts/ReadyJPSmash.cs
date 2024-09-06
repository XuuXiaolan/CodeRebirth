using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class ReadyJPSmash : MonoBehaviour
{
    public ReadyJP mainscript;
    private List<PlayerControllerB> playersBeingSmashed = new List<PlayerControllerB>();
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playersBeingSmashed.Contains(other.GetComponent<PlayerControllerB>()))
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            playersBeingSmashed.Add(player);
            StartCoroutine(KickingPlayer(player));
            player.DamagePlayer(20, true, true, CauseOfDeath.Bludgeoning, 0, false);
            Plugin.ExtendedLogging("Smashed player...");
        }
    }

    private IEnumerator KickingPlayer(PlayerControllerB player)
    {
        float duration = 1f;
        Vector3 direction = (player.transform.position - this.mainscript.gameObject.transform.position).normalized;
        while (duration > 0) {
            duration -= Time.fixedDeltaTime;
            player.externalForces = direction * 10f;
            yield return new WaitForFixedUpdate();
        }
        playersBeingSmashed.Remove(player);
    }
}