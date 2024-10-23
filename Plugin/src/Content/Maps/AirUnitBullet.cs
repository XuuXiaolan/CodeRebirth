
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class Projectile : NetworkBehaviour
{
    private float damage;
    public float speed = 20f;
    public float lifetime = 5f;

    public void Initialize(float damageAmount)
    {
        damage = damageAmount;
        StartCoroutine(DespawnAfterDelay(lifetime));
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        this.NetworkObject.Despawn();
    }

    private void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
        {
            player.DamagePlayer((int)damage, true, true, CauseOfDeath.Blast, 0, false, default);
            player.DisableJetpackModeClientRpc();
            this.NetworkObject.Despawn();
        }
    }
}