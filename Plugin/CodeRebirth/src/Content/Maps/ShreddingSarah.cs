using System.Collections;
using CodeRebirth.src.Content.Enemies;
using CodeRebirth.src.Util;
using Dawn;
using Dusk;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class ShreddingSarah : NetworkSingleton<ShreddingSarah>
{
    public float launchSpeed = 30f;
    public float landingRadius = 8f;
    public float landingRaycastUp = 50f;
    public float landingRaycastDown = 100f;

    public Transform shreddingPoint = null!;
    public InteractTrigger cannonTrigger = null!;
    public Transform shootPoint = null!;
    public Transform targetTransform = null!;
    public AudioSource loadAudioSource = null!;
    public AudioClip loadSFX = null!;
    public AudioSource shootAudioSource = null!;
    public AudioClip shootSFX = null!;

    public void Start()
    {
        cannonTrigger.hoverTip = $"Shred item : [{(StartOfRound.Instance.localPlayerUsingController ? "R-trigger" : "LMB")}]";
        cannonTrigger.disabledHoverTip = "Hold item to Shred";
    }

    public void Update()
    {
        cannonTrigger.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null;
    }

    public void TryFeedItem(PlayerControllerB player)
    {
        if (!player.IsLocalPlayer() || player.currentlyHeldObjectServer == null)
            return;

        int value = 0;
        bool isDeadBody = false;
        if (player.currentlyHeldObjectServer is SnailCatPhysicsProp)
        {
            DuskModContent.Achievements.TryTriggerAchievement(CodeRebirthAchievementKeys.YouMonster);
        }

        if (player.currentlyHeldObjectServer is RagdollGrabbableObject)
        {
            isDeadBody = true;
            value = 10;
        }
        else if (player.currentlyHeldObjectServer.itemProperties.itemName.Contains("Shredded Scraps"))
        {
            value = -10;
        }
        value += player.currentlyHeldObjectServer.scrapValue;
        TryFeedItemServerRpc(isDeadBody, value);
        player.DespawnHeldObject();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryFeedItemServerRpc(bool playerDeath, int valueOfItem)
    {
        NetworkObjectReference netObjRef;
        if (playerDeath)
        {
            netObjRef = CodeRebirthUtils.Instance.SpawnScrap(LethalContent.Items[CodeRebirthItemKeys.BloodyShreddedScraps].Item, shootPoint.position, false, true, valueOfItem);
        }
        else
        {
            netObjRef = CodeRebirthUtils.Instance.SpawnScrap(LethalContent.Items[CodeRebirthItemKeys.NormalShreddedScraps].Item, shootPoint.position, false, true, valueOfItem);
        }
        ShootItemForwards(netObjRef);
    }

    private void ShootItemForwards(NetworkObjectReference netObjRef)
    {
        Plugin.ExtendedLogging($"Scrap Cannon - Shooting forwards (untargeted)");

        Vector3 shootPointPos = shootPoint.position;

        // Instead of using shootPoint.forward, compute the 2D direction from shootPoint to target.
        // Project the difference on the ground (Y = 0).
        Vector3 randomizedTargetTransformPos = targetTransform.position + new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f));
        Vector3 targetDir2D = new Vector3(randomizedTargetTransformPos.x - shootPointPos.x, 0f, randomizedTargetTransformPos.z - shootPointPos.z).normalized;

        float randOffset = UnityEngine.Random.Range(0, 1) * landingRadius;
        float randAngle = UnityEngine.Random.Range(0, 1) * (360f * Mathf.Deg2Rad);

        // Use the computed target direction to set the distance.
        Vector3 targetPos2D = new(randomizedTargetTransformPos.x, 0f, randomizedTargetTransformPos.z);
        // Use the exact distance from the shootPoint to the target.
        float distance = Vector3.Distance(targetPos2D, new Vector3(shootPointPos.x, 0f, shootPointPos.z));

        // Compute the landing position using the direction from shootPoint to target.
        Vector3 landingPos2D = new Vector3(shootPointPos.x, 0f, shootPointPos.z)
                                + (targetDir2D * distance)
                                + new Vector3(Mathf.Sin(randAngle), 0f, Mathf.Cos(randAngle)) * randOffset;
        Vector3 landingPositionRay = landingPos2D + (transform.position.y + landingRaycastUp) * Vector3.up;

        if (Physics.Raycast(landingPositionRay, -Vector3.up, out RaycastHit raycastHit, landingRaycastUp + landingRaycastDown, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
        {
            // Raycast again in case any mods hook GetItemFloorPosition
            GrabbableObject grabbableObject = ((GameObject)netObjRef).GetComponent<GrabbableObject>();
            Vector3 landingPosition = grabbableObject.GetItemFloorPosition(raycastHit.point + Vector3.up);
            ShootItemClientRPC(netObjRef, landingPosition);
        }
        else
        {
            Vector3 landingPosition = landingPos2D + (transform.position.y * Vector3.up);
            ShootItemClientRPC(netObjRef, landingPosition);
        }
    }

    [ClientRpc]
    public void ShootItemClientRPC(NetworkObjectReference heldObjectRef, Vector3 landingPosition)
    {
        if (!heldObjectRef.TryGet(out NetworkObject heldObjectNetObj))
        {
            Plugin.Logger.LogError($"Scrap Cannon - ShootItemClientRPC - Held object could not be found: {heldObjectRef.NetworkObjectId}");
            return;
        }

        GrabbableObject grabbableObject = heldObjectNetObj.gameObject.GetComponent<GrabbableObject>();
        grabbableObject.fallTime = 1.1f;
        grabbableObject.hasHitGround = true;
        grabbableObject.EnablePhysics(false);
        grabbableObject.EnableItemMeshes(false);

        loadAudioSource.PlayOneShot(loadSFX);
        StartCoroutine(ShootItemRoutine(grabbableObject, landingPosition));
    }

    private IEnumerator ShootItemRoutine(GrabbableObject grabbableObject, Vector3 landingPosition)
    {
        yield return new WaitForSeconds(loadSFX.length + 0.25f);

        shootAudioSource.PlayOneShot(shootSFX);
        grabbableObject.EnableItemMeshes(true);

        float launchTotalDistance = Vector3.Distance(shootPoint.position, landingPosition);
        float launchProgress = 0f;

        Plugin.ExtendedLogging($"Old World position: {grabbableObject.transform.position}");
        Plugin.ExtendedLogging($"Old Local position: {grabbableObject.transform.localPosition}");
        while (launchProgress < launchTotalDistance)
        {
            // Sample along the circular arc (chute) from shootPoint to landing position.
            Vector3 currentPosition = SampleChute(shootPoint.position, landingPosition, launchProgress / launchTotalDistance);
            Vector3 localPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(currentPosition);

            grabbableObject.startFallingPosition = localPosition;
            grabbableObject.targetFloorPosition = localPosition;
            grabbableObject.transform.localPosition = localPosition;

            launchProgress += launchSpeed * Time.deltaTime;
            yield return null;
        }

        Vector3 finalLocalPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(landingPosition);
        grabbableObject.EnablePhysics(true);
        grabbableObject.startFallingPosition = finalLocalPosition;
        grabbableObject.targetFloorPosition = finalLocalPosition;
        grabbableObject.transform.localPosition = finalLocalPosition;
        grabbableObject.hasHitGround = false;
        grabbableObject.fallTime = 0f;
    }

    /// <summary>
    /// Samples a curved chute path from start to end along a vertical circle that has its highest point at 'start'.
    /// The circle’s center is forced directly below the start so that the tangent at the start is horizontal,
    /// and the arc proceeds monotonically downward onto the destination.
    /// </summary>
    private Vector3 SampleChute(Vector3 start, Vector3 end, float t)
    {
        // If start is nearly equal to end (or if start is not above end), just Lerp.
        if (Vector3.Distance(start, end) < 0.001f || start.y <= end.y)
            return Vector3.Lerp(start, end, t);

        // Compute the horizontal distance between start and end.
        Vector3 horizontalDiff = new(end.x - start.x, 0f, end.z - start.z);
        float dxz = horizontalDiff.magnitude;

        // Compute the circle center assuming it lies directly below the start.
        // Derived from requiring |start - center| = |end - center| with center = (start.x, C_y, start.z):
        float centerY = (start.y + end.y - ((dxz * dxz) / (start.y - end.y))) / 2f;
        Vector3 center = new(start.x, centerY, start.z);

        // Offsets from the center.
        Vector3 startOffset = start - center; // This should be purely vertical.
        Vector3 endOffset = end - center;

        // Compute the rotation axis (should be horizontal).
        Vector3 axis = Vector3.Cross(startOffset, endOffset).normalized;
        if (axis == Vector3.zero)
            return Vector3.Lerp(start, end, t);

        // Determine the total rotation angle from startOffset to endOffset.
        float totalAngle = Vector3.SignedAngle(startOffset, endOffset, axis);

        // Rotate startOffset by a fraction of the total angle.
        Vector3 currentOffset = Quaternion.AngleAxis(totalAngle * t, axis) * startOffset;
        return center + currentOffset;
    }
}