using System.Collections;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class GunslingerMissile : MonoBehaviour
{
    public float speed = 20f;
    public float curveStrength = 2f; // Strength of curve adjustment
    public AudioSource playerHitSoundSource = null!;
    public AudioSource windSource = null!;
    public MeshFilter bulletMesh = null!;

    private Collider[] cachedColliders = new Collider[8];
    private PlayerControllerB? playerToTarget = null;

    public void Initialize(PlayerControllerB targetPlayer)
    {
        playerToTarget = targetPlayer; // Assign the player to target
        this.transform.SetParent(null);
    }

    public void FixedUpdate()
    {
        if (playerToTarget == null) return;
        int collidersFound = Physics.OverlapSphereNonAlloc(this.transform.position, 2f, cachedColliders, CodeRebirthUtils.Instance.collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask, QueryTriggerInteraction.Ignore);
        if (collidersFound > 0)
        {
            CRUtilities.CreateExplosion(this.transform.position, true, 15, 0, 4, 6, null, null, 20f);
            StartCoroutine(DespawnAfterDelay(5f));
            // playerHitSoundSource.Play();
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            // windSource.volume = 0f;
            bulletMesh.mesh = null;
            playerToTarget = null;
            return;
        }

        // Move the projectile in its forward direction in world space
        transform.position += transform.forward * speed * Time.fixedDeltaTime;

        // Curve towards target if the target player is within range
        Vector3 directionToTarget = (playerToTarget.transform.position - transform.position).normalized;
        Vector3 newDirection = Vector3.Lerp(transform.forward, directionToTarget, curveStrength * Time.fixedDeltaTime).normalized;
        transform.forward = newDirection;
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(this);
    }
}