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
    public MeshFilter bulletMesh = null!;

    private Transform oldParent = null!;
    [HideInInspector] public GunslingerGreg gregScript = null!;
    [HideInInspector] public bool ready = false;
    [HideInInspector] public Transform mainTransform = null!;
    private Collider[] cachedColliders = new Collider[8];
    private PlayerControllerB? playerToTarget = null;

    public void Initialize(PlayerControllerB targetPlayer, GunslingerGreg greg)
    {
        playerToTarget = targetPlayer; // Assign the player to target
        gregScript = greg;
        oldParent = transform.parent;
        transform.SetParent(null);
        greg.rockets.Enqueue(this);
    }

    public void FixedUpdate()
    {
        if (playerToTarget == null) return;
        int collidersFound = Physics.OverlapSphereNonAlloc(this.transform.position, 2f, cachedColliders, CodeRebirthUtils.Instance.collidersAndRoomAndPlayersAndEnemiesAndTerrainAndVehicleMask, QueryTriggerInteraction.Ignore);
        if (collidersFound > 0)
        {
            CRUtilities.CreateExplosion(this.transform.position, true, 15, 0, 4, 6, null, null, 20f);
            // playerHitSoundSource.Play();
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
            // windSource.volume = 0f;
            this.gameObject.SetActive(false);
            this.transform.SetParent(oldParent, false);
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
}