using System.Collections;
using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class ShreddingSarah : NetworkBehaviour
{
    public void OnKillLocalPlayer(PlayerControllerB player)
    {
        if (player != GameNetworkManager.Instance.localPlayerController || player.isPlayerDead || !player.isPlayerControlled) return;
        // spawn a meat cube or smthn
    }

    public float arcHeight = 30f;
    public float launchSpeed = 30f;

    public float landingRadius = 8f;

    public float landingRaycastUp = 50f;
    public float landingRaycastDown = 100f;

    public InteractTrigger cannonTrigger;
    public Transform shootPoint;
    public Transform targetTransform;
    public AudioSource audioSource;
    public AudioClip loadSFX;
    public AudioClip shootSFX;

    private System.Random cannonRandom = new();
    private bool interactWaiting;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Plugin.Logger.LogInfo("Scrap Cannon - Network spawned");
        cannonRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
    }

    public void Update()
    {
        cannonTrigger.hoverTip = $"Shred item : [{(StartOfRound.Instance.localPlayerUsingController ? "R-trigger" : "LMB")}]";
        cannonTrigger.disabledHoverTip = "Hold item to Shred";
        cannonTrigger.interactable = !interactWaiting && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null;
    }

    public void TryFeedItem(PlayerControllerB player)
    {
        if (player != GameNetworkManager.Instance.localPlayerController || player.currentlyHeldObjectServer == null) return;
        ShootItemForwards(player, targetTransform);
    }

    private void ShootItemForwards(PlayerControllerB player, Transform _targetTransform)
    {
        Plugin.ExtendedLogging($"Scrap Cannon - Shooting forwards (untargeted)");

        GrabbableObject heldObject = player.currentlyHeldObjectServer;
        Vector3 shootPointPos = shootPoint.position;

        // Instead of using shootPoint.forward, compute the 2D direction from shootPoint to target.
        // Project the difference on the ground (Y = 0).
        Vector3 targetDir2D = new Vector3(_targetTransform.position.x - shootPointPos.x, 0f, _targetTransform.position.z - shootPointPos.z).normalized;

        float randOffset = cannonRandom.NextFloat(0, 1) * landingRadius;
        float randAngle = cannonRandom.NextFloat(0, 1) * (360f * Mathf.Deg2Rad);

        // Use the computed target direction to set the distance.
        Vector3 targetPos2D = new Vector3(_targetTransform.position.x, 0f, _targetTransform.position.z);
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
            Vector3 landingPosition = heldObject.GetItemFloorPosition(raycastHit.point + Vector3.up);
            ShootItem(player, landingPosition);
        }
        else
        {
            Vector3 landingPosition = landingPos2D + (transform.position.y * Vector3.up);
            ShootItem(player, landingPosition);
        }
    }

    public void ShootItem(PlayerControllerB player, Vector3 landingPosition)
    {
        GrabbableObject heldObject = player.currentlyHeldObjectServer;

        player.SetSpecialGrabAnimationBool(false, heldObject);
        player.playerBodyAnimator.SetBool("cancelHolding", true);
        player.playerBodyAnimator.SetTrigger("Throw");
        HUDManager.Instance.itemSlotIcons[player.currentItemSlot].enabled = false;
        HUDManager.Instance.holdingTwoHandedItem.enabled = false;

        Vector3 localPos = StartOfRound.Instance.propsContainer.InverseTransformPoint(transform.position + 0.75f * Vector3.up);
        player.PlaceGrabbableObject(StartOfRound.Instance.propsContainer, localPos, false, heldObject);
        heldObject.DiscardItemOnClient();

        heldObject.fallTime = 1.1f;
        heldObject.hasHitGround = true;
        heldObject.EnablePhysics(false);
        heldObject.EnableItemMeshes(false);

        cannonTrigger.interactable = false;
        interactWaiting = true;

        if (loadSFX != null)
        {
            audioSource.PlayOneShot(loadSFX);
        }

        ShootItemServerRPC((int)player.playerClientId, heldObject.gameObject.GetComponent<NetworkObject>(), landingPosition);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShootItemServerRPC(int playerId, NetworkObjectReference heldObjectRef, Vector3 landingPosition)
    {
        if (!heldObjectRef.TryGet(out _))
        {
            Plugin.Logger.LogError($"Scrap Cannon - ShootItemServerRPC - Held object could not be found: {heldObjectRef.NetworkObjectId}");
            return;
        }
        ShootItemClientRPC(playerId, heldObjectRef, landingPosition);
    }

    [ClientRpc]
    public void ShootItemClientRPC(int playerId, NetworkObjectReference heldObjectRef, Vector3 landingPosition)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];

        if (player == null)
        {
            Plugin.Logger.LogError($"Scrap Cannon - ShootItemClientRPC - Player could not be found: {playerId}");
            return;
        }
        if (!heldObjectRef.TryGet(out var heldObjectNetwork))
        {
            Plugin.Logger.LogError($"Scrap Cannon - ShootItemClientRPC - Held object could not be found: {heldObjectRef.NetworkObjectId}");
            return;
        }
        var heldObject = heldObjectNetwork.GetComponent<GrabbableObject>();

        if (!player.IsOwner)
        {
            Vector3 localPos = StartOfRound.Instance.propsContainer.InverseTransformPoint(transform.position + 0.75f * Vector3.up);
            player.PlaceGrabbableObject(StartOfRound.Instance.propsContainer, localPos, false, heldObject);
            heldObject.fallTime = 1.1f;
            heldObject.hasHitGround = true;
            heldObject.EnablePhysics(false);
            heldObject.EnableItemMeshes(false);

            if (loadSFX != null)
            {
                audioSource.PlayOneShot(loadSFX);
            }
            // Don't shoot to same position on every client
            cannonRandom.NextDouble();
            cannonRandom.NextDouble();
        }

        if (!heldObject.itemProperties.syncDiscardFunction)
        {
            heldObject.playerHeldBy = null;
        }

        if (player.currentlyHeldObjectServer == heldObject)
        {
            player.currentlyHeldObjectServer = null;
        }
        else
        {
            Plugin.Logger.LogError($"Scrap Cannon - ShootItemClientRPC - Held object mismatch (found {player.currentlyHeldObjectServer?.gameObject.name ?? "null"}) on player {playerId}");
        }

        if (player.IsOwner)
        {
            player.throwingObject = false;
            HUDManager.Instance.itemSlotIcons[player.currentItemSlot].enabled = false;
        }

        interactWaiting = false;
        StartCoroutine(ShootItemRoutine(heldObject, landingPosition));
    }

    private IEnumerator ShootItemRoutine(GrabbableObject heldObject, Vector3 landingPosition)
    {
        Plugin.Logger.LogInfo($"Scrap Cannon - Scrap landing at {landingPosition}");
        yield return new WaitForSeconds(0.5f);

        if (shootSFX != null)
        {
            audioSource.PlayOneShot(shootSFX);
        }
        heldObject.EnableItemMeshes(true);

        float launchTotalDistance = Vector3.Distance(shootPoint.position, landingPosition);
        float launchProgress = 0f;

        while (launchProgress < launchTotalDistance)
        {
            // Sample along a circular arc (chute) from shootPoint to landing position.
            Vector3 currentPosition = SampleChute(shootPoint.position, landingPosition, arcHeight, launchProgress / launchTotalDistance);
            Vector3 localPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(currentPosition);

            heldObject.startFallingPosition = localPosition;
            heldObject.targetFloorPosition = localPosition;
            heldObject.transform.localPosition = localPosition;

            launchProgress += launchSpeed * Time.deltaTime;
            yield return null;
        }

        Vector3 finalLocalPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(landingPosition);
        heldObject.EnablePhysics(true);
        heldObject.startFallingPosition = finalLocalPosition;
        heldObject.targetFloorPosition = finalLocalPosition;
        heldObject.transform.localPosition = finalLocalPosition;
        heldObject.hasHitGround = false;
        heldObject.fallTime = 0f;
    }

    /// <summary>
    /// Samples a curved chute path from start to end using spherical interpolation.
    /// The arcHeight value represents the maximum vertical deviation (sagitta) from the chord.
    /// </summary>
    private Vector3 SampleChute(Vector3 start, Vector3 end, float arcHeight, float t)
    {
        // If no arc is desired or if start/end are nearly identical, simply Lerp.
        if (arcHeight <= 0f || Vector3.Distance(start, end) < 0.001f)
            return Vector3.Lerp(start, end, t);

        // Compute the chord.
        Vector3 chord = end - start;
        float d = chord.magnitude;
        Vector3 mid = (start + end) / 2f;

        // Determine the circle radius R given the sagitta (arcHeight)
        // Formula: R = ((d/2)^2 + arcHeight^2) / (2 * arcHeight)
        float R = (((d * d) / 4f) + (arcHeight * arcHeight)) / (2f * arcHeight);

        // Compute the distance from the midpoint to the circle center along the perpendicular bisector.
        float offsetDistance = R - arcHeight;

        // Determine the vertical plane for the arc by computing a perpendicular direction to the chord in the plane defined by the chord and Vector3.up.
        Vector3 planeNormal = Vector3.Cross(chord, Vector3.up).normalized;
        Vector3 perp = Vector3.Cross(planeNormal, chord).normalized;
        if (perp.y > 0f) perp = -perp;

        // Compute the circle center.
        Vector3 center = mid + perp * offsetDistance;

        // Compute the vectors from the center to start and end.
        Vector3 fromCenterToStart = start - center;
        Vector3 fromCenterToEnd = end - center;

        // Spherical interpolation between the two vectors.
        Vector3 currentDirection = Vector3.Slerp(fromCenterToStart, fromCenterToEnd, t);
        return center + currentDirection;
    }
}