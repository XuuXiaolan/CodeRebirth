using System;
using System.Collections;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using CodeRebirthLib.Util;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class AirUnitProjectile : NetworkBehaviour
{
    private float damage;
    public float speed = 20f;
    public float lifetime = 5f;
    public float bulletTrailForce = 50f;
    public float curveStrength = 2f; // Strength of curve adjustment
    public AudioSource playerHitSoundSource = null!;
    public AudioSource windSource = null!;
    public MeshFilter bulletMesh = null!;

    private Collider[] cachedColliders = new Collider[5];
    [NonSerialized] public bool explodedOnTarget = false;
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

    private void FixedUpdate()
    {
        if (playerToTarget == null)
        {
            return;
        }
        int collidersFound = Physics.OverlapSphereNonAlloc(this.transform.position, 2f, cachedColliders, MoreLayerMasks.collidersAndRoomAndRailingAndInteractableMask, QueryTriggerInteraction.Ignore);
        if (!explodedOnTarget && collidersFound != 0 && playerToTarget.playerSteamId != Plugin.GLITCH_STEAM_ID)
        {
            CRUtilities.CreateExplosion(this.transform.position, true, 100, 0, 10, 6, null, null, 5f);
            playerHitSoundSource.Play();
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            explodedOnTarget = true;
            bulletMesh.mesh = null;
            windSource.volume = 0f;
        }
        if (!IsServer) return;

        // Move the projectile in its forward direction in world space
        transform.position += transform.up * speed * Time.fixedDeltaTime;

        // Curve towards target if the target player is within range
        if (!explodedOnTarget && playerToTarget != null && Vector3.Distance(transform.position, playerToTarget.transform.position) <= (30f + (playerToTarget.playerSteamId == Plugin.GLITCH_STEAM_ID ? 100 : 1)))
        {
            Vector3 directionToTarget = (playerToTarget.transform.position - transform.position).normalized;
            Vector3 newDirection = Vector3.Lerp(transform.up, directionToTarget, curveStrength * Time.fixedDeltaTime * (playerToTarget.playerSteamId == Plugin.GLITCH_STEAM_ID ? 100 : 1)).normalized;
            transform.up = newDirection;
        }
        if (playerToTarget != null && explodedOnTarget)
        {
            transform.position = playerToTarget.transform.position;
        }
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (IsServer) this.NetworkObject.Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!explodedOnTarget && other.TryGetComponent(out PlayerControllerB player))
        {
            Vector3 forceFlung = transform.up * Plugin.ModConfig.ConfigAirControlUnitKnockbackPower.Value;
            CRUtilities.CreateExplosion(this.transform.position, true, 0, 0, 0, 6, null, null, 5f);
            player.DamagePlayer((int)damage, true, false, CauseOfDeath.Blast, 0, false, forceFlung);
            playerHitSoundSource.Play();
            if (player == GameNetworkManager.Instance.localPlayerController)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            }
            if (!player.isPlayerDead) StartCoroutine(PushPlayerFarAway(player, forceFlung));
            explodedOnTarget = true;
            bulletMesh.mesh = null;
            windSource.volume = 0f;
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