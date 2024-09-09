using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class KnockPlayersAway : MonoBehaviour
{
    public PjonkGooseAI mainscript = null!;
    private List<PlayerControllerB> playersBeingSmashed = new List<PlayerControllerB>();
    private void OnTriggerEnter(Collider other)
    {
        if (mainscript.pushingAway && other.CompareTag("Player") && !playersBeingSmashed.Contains(other.GetComponent<PlayerControllerB>()))
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            playersBeingSmashed.Add(player);
            StartCoroutine(SmashPlayer(player));
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (mainscript.pushingAway && other.CompareTag("Player") && !playersBeingSmashed.Contains(other.GetComponent<PlayerControllerB>()))
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            playersBeingSmashed.Add(player);
            StartCoroutine(SmashPlayer(player));
        }
    }

    private IEnumerator SmashPlayer(PlayerControllerB player)
    {
        float duration = 0.2f;
        Vector3 direction = (player.transform.position - this.gameObject.GetComponent<Collider>().bounds.center).normalized;
        while (duration > 0) {
            duration -= Time.fixedDeltaTime;
            player.externalForces = direction * 10f;
            yield return new WaitForFixedUpdate();
        }
        playersBeingSmashed.Remove(player);
    }
}