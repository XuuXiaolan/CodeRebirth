using System.Collections;
using CodeRebirth.src.Util;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class ShreddingSarah : NetworkBehaviour
{
    public float arcHeight = 30f;
    public float launchSpeed = 30f;
    public float landingRadius = 8f;
    public float landingRaycastUp = 50f;
    public float landingRaycastDown = 100f;

    public InteractTrigger cannonTrigger = null!;
    public Transform shootPoint = null!;
    public Transform targetTransform = null!;
    public AudioSource audioSource = null!;
    public AudioClip loadSFX = null!;
    public AudioClip shootSFX = null!;

    private System.Random cannonRandom = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Plugin.ExtendedLogging("Scrap Cannon - Network spawned");
        cannonRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
    }

    public void Update()
    {
        cannonTrigger.hoverTip = $"Shred item : [{(StartOfRound.Instance.localPlayerUsingController ? "R-trigger" : "LMB")}]";
        cannonTrigger.disabledHoverTip = "Hold item to Shred";
        cannonTrigger.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null;
    }

    public void TryFeedItem(PlayerControllerB player)
    {
        if (!player.IsOwner || player.currentlyHeldObjectServer == null) return;
        int value = 0;
        if (player.currentlyHeldObjectServer is RagdollGrabbableObject)
        {
            player.DespawnHeldObject();
            TryFeedItemServerRpc(true, 10);
            return;
        }
        else if (player.currentlyHeldObjectServer.itemProperties.itemName.Contains("Shredded Scraps"))
        {
            value = -24;
        }
        value += player.currentlyHeldObjectServer.scrapValue;
        player.DespawnHeldObject();
        TryFeedItemServerRpc(false, value + UnityEngine.Random.Range(10, 23));
    }

    [ServerRpc(RequireOwnership = false)]
    public void TryFeedItemServerRpc(bool playerDeath, int valueOfItem)
    {
        NetworkObjectReference netObjRef;
        if (playerDeath)
        {
            netObjRef = CodeRebirthUtils.Instance.SpawnScrap(MapObjectHandler.Instance.ShredderSarah.DeadPlayerScrap, shootPoint.position, false, true, valueOfItem);
        }
        else
        {
            netObjRef = CodeRebirthUtils.Instance.SpawnScrap(MapObjectHandler.Instance.ShredderSarah.ShreddedScrap, shootPoint.position, false, true, valueOfItem);
        }
        ShootItemForwards(netObjRef);
    }

    private void ShootItemForwards(NetworkObjectReference netObjRef)
    {
        Plugin.ExtendedLogging($"Scrap Cannon - Shooting forwards (untargeted)");

        Vector3 shootPointPos = shootPoint.position;

        // Instead of using shootPoint.forward, compute the 2D direction from shootPoint to target.
        // Project the difference on the ground (Y = 0).
        Vector3 randomizedTargetTransformPos = targetTransform.position + new Vector3(cannonRandom.NextFloat(-2f, 2f), cannonRandom.NextFloat(-2f, 2f), cannonRandom.NextFloat(-2f, 2f));
        Vector3 targetDir2D = new Vector3(randomizedTargetTransformPos.x - shootPointPos.x, 0f, randomizedTargetTransformPos.z - shootPointPos.z).normalized;

        float randOffset = cannonRandom.NextFloat(0, 1) * landingRadius;
        float randAngle = cannonRandom.NextFloat(0, 1) * (360f * Mathf.Deg2Rad);

        // Use the computed target direction to set the distance.
        Vector3 targetPos2D = new Vector3(randomizedTargetTransformPos.x, 0f, randomizedTargetTransformPos.z);
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

        audioSource.PlayOneShot(loadSFX);
        // Don't shoot to same position on every client
        cannonRandom.NextDouble();
        cannonRandom.NextDouble();
        StartCoroutine(ShootItemRoutine(grabbableObject, landingPosition));
    }

    private IEnumerator ShootItemRoutine(GrabbableObject grabbableObject, Vector3 landingPosition)
    {
        Plugin.ExtendedLogging($"Scrap Cannon - Scrap landing at {landingPosition}");
        yield return new WaitForSeconds(0.5f);

        audioSource.PlayOneShot(shootSFX);
        grabbableObject.EnableItemMeshes(true);

        float launchTotalDistance = Vector3.Distance(shootPoint.position, landingPosition);
        float launchProgress = 0f;

        while (launchProgress < launchTotalDistance)
        {
            // Sample along the circular arc (chute) from shootPoint to landing position.
            Vector3 currentPosition = SampleChute(shootPoint.position, landingPosition, arcHeight, launchProgress / launchTotalDistance);
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
    private Vector3 SampleChute(Vector3 start, Vector3 end, float arcHeight, float t)
    {
        // If start is nearly equal to end (or if start is not above end), just Lerp.
        if (Vector3.Distance(start, end) < 0.001f || start.y <= end.y)
            return Vector3.Lerp(start, end, t);

        // Compute the horizontal distance between start and end.
        Vector3 horizontalDiff = new Vector3(end.x - start.x, 0f, end.z - start.z);
        float dxz = horizontalDiff.magnitude;

        // Compute the circle center assuming it lies directly below the start.
        // Derived from requiring |start - center| = |end - center| with center = (start.x, C_y, start.z):
        float centerY = (start.y + end.y - ((dxz * dxz) / (start.y - end.y))) / 2f;
        Vector3 center = new Vector3(start.x, centerY, start.z);
        float R = start.y - centerY; // radius

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