using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class AirUnitProjectile : NetworkBehaviour
{
    private float damage;
    public float speed = 20f;
    public float lifetime = 5f;
    private Vector3 directionOfBullet = Vector3.zero;

    public void Initialize(float damageAmount, Vector3 direction)
    {
        damage = damageAmount;
        this.directionOfBullet = direction;
        StartCoroutine(DespawnAfterDelay(lifetime));
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        this.NetworkObject.Despawn();
    }

    private void Update()
    {
        transform.Translate(directionOfBullet * speed * Time.deltaTime); // Follow the turret cannon's forward direction
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