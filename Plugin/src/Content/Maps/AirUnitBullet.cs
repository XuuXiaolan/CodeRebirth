using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps
{
    public class AirUnitProjectile : NetworkBehaviour
    {
        private float damage;
        public float speed = 20f;
        public float lifetime = 5f;
        public float anglePointingTo = 0f;

        public void Initialize(float damageAmount, float angle)
        {
            damage = damageAmount;
            anglePointingTo = angle; // Assign the angle to use for rotation
            StartCoroutine(DespawnAfterDelay(lifetime));

            // Set the rotation of the projectile based on the angle
            Vector3 currentEulerAngles = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(anglePointingTo, currentEulerAngles.y, currentEulerAngles.z);
        }

        private void Update()
        {
            if (!IsServer) return;

            // Move the projectile in its forward direction in world space
            transform.position += transform.up * speed * Time.deltaTime;
        }

        private IEnumerator DespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (IsServer) this.NetworkObject.Despawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
            {
                Vector3 forceFlung = transform.up * 250f;
                player.DamagePlayer((int)damage, true, false, CauseOfDeath.Blast, 0, false, forceFlung);
                this.GetComponent<Collider>().isTrigger = false;
                if (!player.isPlayerDead) StartCoroutine(PushPlayerFarAway(player, forceFlung));
            }
        }

        private IEnumerator PushPlayerFarAway(PlayerControllerB player, Vector3 force)
        {
            float duration = 1f;
            while (duration > 0)
            {
                duration -= Time.fixedDeltaTime;
                player.externalForces += force;
                yield return new WaitForFixedUpdate();
            }
            yield break;
        }
    }
}