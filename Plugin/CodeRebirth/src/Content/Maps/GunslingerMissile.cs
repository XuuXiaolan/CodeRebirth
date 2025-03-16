using System.Collections;
using CodeRebirth.src.MiscScripts;
using CodeRebirth.src.Util;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class GunslingerMissile : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 5f;
    public float curveStrength = 2f; // Strength of curve adjustment
    public AudioSource playerHitSoundSource = null!;
    public AudioSource windSource = null!;
    public MeshFilter bulletMesh = null!;

    private Collider[] cachedColliders = new Collider[8];
    private PlayerControllerB? playerToTarget = null;

    public void Initialize(PlayerControllerB targetPlayer)
    {
        playerToTarget = targetPlayer; // Assign the player to target
        StartCoroutine(DespawnAfterDelay(lifetime));
    }

    private void FixedUpdate()
    {
        if (playerToTarget == null) return;
        int collidersFound = Physics.OverlapSphereNonAlloc(this.transform.position, 2f, cachedColliders, CodeRebirthUtils.Instance.collidersAndRoomAndRailingAndInteractableMask, QueryTriggerInteraction.Ignore);
        if (bulletMesh.mesh != null && collidersFound > 0)
        {
            CRUtilities.CreateExplosion(this.transform.position, true, 15, 0, 4, 6, null, null, 20f);
            playerHitSoundSource.Play();
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            bulletMesh.mesh = null;
            windSource.volume = 0f;
            playerToTarget = null;
            return;
        }

        // Move the projectile in its forward direction in world space
        transform.position += transform.up * speed * Time.fixedDeltaTime;

        // Curve towards target if the target player is within range
        Vector3 directionToTarget = (playerToTarget.transform.position - transform.position).normalized;
        Vector3 newDirection = Vector3.Lerp(transform.up, directionToTarget, curveStrength * Time.fixedDeltaTime).normalized;
        transform.up = newDirection;
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(this);
    }
}