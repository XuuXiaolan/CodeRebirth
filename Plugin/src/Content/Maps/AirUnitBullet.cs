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
        public float curveStrength = 2f; // Strength of curve adjustment
    
        private float anglePointingTo = 0f;
        private PlayerControllerB playerToTarget = null!;

        public void Initialize(float damageAmount, float angle, PlayerControllerB targetPlayer)
        {
            damage = damageAmount;
            anglePointingTo = angle; // Assign the angle to use for rotation
            playerToTarget = targetPlayer; // Assign the player to target
            StartCoroutine(DespawnAfterDelay(lifetime));

            // Set the initial rotation of the projectile based on the angle
            Vector3 currentEulerAngles = transform.localEulerAngles;
            transform.localEulerAngles = new Vector3(anglePointingTo, currentEulerAngles.y, currentEulerAngles.z);
        }

        private void Update()
        {
            if (!IsServer) return;

            // Move the projectile in its forward direction in world space
            transform.position += transform.up * speed * Time.deltaTime;

            // Curve towards target if the target player is within range
            if (playerToTarget != null && Vector3.Distance(transform.position, playerToTarget.transform.position) <= 30f)
            {
                Vector3 directionToTarget = (playerToTarget.transform.position - transform.position).normalized;
                Vector3 newDirection = Vector3.Lerp(transform.up, directionToTarget, curveStrength * Time.deltaTime).normalized;
                transform.up = newDirection;
            }
        }

        private IEnumerator DespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (IsServer) this.NetworkObject.Despawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == 3 && other.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
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