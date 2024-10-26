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
    private AirControlUnit airControlUnit = null!;

    public void Initialize(float damageAmount, AirControlUnit airControlUnit)
    {
        damage = damageAmount;
        this.airControlUnit = airControlUnit;
        StartCoroutine(DespawnAfterDelay(lifetime));
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        this.NetworkObject.Despawn();
    }

    private void Update()
    {
        if (airControlUnit == null) return;
        transform.Translate(airControlUnit.turretCannonTransform.forward * speed * Time.deltaTime); // Follow the turret cannon's forward direction
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