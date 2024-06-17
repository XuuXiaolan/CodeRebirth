using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.Collisions;

public class ColliderIdentifier : MonoBehaviour
{
    [SerializeField]
    private float damageDuration = 16.0f; // Duration for which the damage should be active
    private float damageInterval = 0.25f;
    private float timeSinceLastDamage = 0.0f;
    private List<PlayerControllerB> playersInTrigger = new List<PlayerControllerB>();

    private void OnEnable()
    {
        StartCoroutine(ManageDamageTimer());
    }

    private void Update()
    {
        if (timeSinceLastDamage >= damageInterval)
        {
            ApplyDamageToAllPlayers();
            timeSinceLastDamage = 0f;
        }
        else
        {
            timeSinceLastDamage += Time.deltaTime;
        }
    }

    private IEnumerator ManageDamageTimer()
    {
        yield return new WaitForSeconds(damageDuration);
        playersInTrigger.Clear(); // Clear all players from the list
        gameObject.SetActive(false); // Disable this component or GameObject after the damage duration
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            if (player != null && !playersInTrigger.Contains(player))
            {
                playersInTrigger.Add(player);
            }
        }
    }
    private void OnParticleCollisionEvent(ParticleCollisionEvent particleCollisionEvent) {
        // Maybe use this at some point?
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            if (player != null)
            {
                playersInTrigger.Remove(player);
            }
        }
    }

    private void ApplyDamageToAllPlayers()
    {
        foreach (var player in playersInTrigger)
        {
            if (player != null && !player.isPlayerDead)
            {
                player.DamagePlayer(5); // Assuming a method DamagePlayer exists on the player controller
                Debug.Log("Damage applied to player.");
            }
        }
    }
}