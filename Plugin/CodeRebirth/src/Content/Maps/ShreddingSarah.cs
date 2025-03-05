using System.Collections;
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
    public float launchDistance = 75f;

    public float landingRadius = 8f;

    public float landingRaycastUp = 50f;
    public float landingRaycastDown = 100f;

    public float targetAngleSnap = 17.5f;
    public float targetAngleSnapMinDistance = 20f;

    public InteractTrigger cannonTrigger;
    public Transform shootPoint;
    public Transform targetTransform;
    public AudioSource audioSource;
    public AudioClip loadSFX;
    public AudioClip shootSFX;
    public LineRenderer[] debugLines;

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
#if DEBUG
        DrawDebugLines();
#endif

        cannonTrigger.hoverTip = $"Shred item : [{(StartOfRound.Instance.localPlayerUsingController ? "R-trigger" : "LMB")}]";
        cannonTrigger.disabledHoverTip = "Hold item to Shred";
        cannonTrigger.interactable = !interactWaiting && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null;
    }

    private void DrawDebugLines()
    {
        DrawDebugLine(debugLines[0], targetTransform);
    }

    private void DrawDebugLine(LineRenderer debugLine, Transform targetPos)
    {
        var cannonPos2D = new Vector3(shootPoint.position.x, 0f, shootPoint.position.z);
        var targetPos2D = new Vector3(targetPos.position.x, 0f, targetPos.position.z);

        var targetDistance = Vector3.Distance(cannonPos2D, targetPos2D);

        if (targetDistance < targetAngleSnapMinDistance)
        {
            debugLine.enabled = false;
            return;
        }
        else
        {
            debugLine.enabled = true;
        }

        var cannonFacing2D = new Vector3(shootPoint.forward.x, 0f, shootPoint.forward.z).normalized;

        var targetAngleFull = Quaternion.FromToRotation(cannonFacing2D, targetPos2D - cannonPos2D);
        var targetAngle = targetAngleFull.eulerAngles.y;

        Plugin.ExtendedLogging($"TARGET ANGLE: {targetAngle}");

        debugLine.SetPosition(0, new Vector3(Mathf.Sin((targetAngle - targetAngleSnap) * Mathf.Deg2Rad) * 3f, 0, Mathf.Cos((targetAngle - targetAngleSnap) * Mathf.Deg2Rad) * 3f));
        debugLine.SetPosition(1, new Vector3(Mathf.Sin((targetAngle + targetAngleSnap) * Mathf.Deg2Rad) * 3f, 0, Mathf.Cos((targetAngle + targetAngleSnap) * Mathf.Deg2Rad) * 3f));
    }

    public void TryFeedItem(PlayerControllerB player)
    {
        if (player != GameNetworkManager.Instance.localPlayerController || player.currentlyHeldObjectServer == null) return;
        ShootItemForwards(player, targetTransform);

        // Launch a scrap into a pit, just like slerp its position.
    }

    private void ShootItemForwards(PlayerControllerB player, Transform? _targetTransform = null)
    {
        Plugin.ExtendedLogging($"Scrap Cannon - Shooting forwards (untargeted)");

        var heldObject = player.currentlyHeldObjectServer;

        var cannonPos2D = new Vector3(shootPoint.position.x, 0f, shootPoint.position.z);
        var cannonFacing2D = new Vector3(shootPoint.forward.x, 0f, shootPoint.forward.z).normalized;

        var randOffset = (float)(cannonRandom.NextDouble() * landingRadius);
        var randAngle = (float)(cannonRandom.NextDouble() * (360f * Mathf.Deg2Rad));

        var distance = launchDistance;

        if (_targetTransform != null)
        {
            var shipPos2D = new Vector3(_targetTransform.position.x, 0f, _targetTransform.position.y);

            var targetAngleFull = Quaternion.FromToRotation(cannonFacing2D, shipPos2D - cannonPos2D);
            var targetAngle = targetAngleFull.eulerAngles.y;

            if (Mathf.DeltaAngle(0f, targetAngle) < 90f)
            {
                distance = Mathf.Max(distance, Vector3.Distance(shipPos2D, cannonPos2D));
            }
        }

        var landingPos2D = cannonPos2D + (cannonFacing2D * distance) + new Vector3(Mathf.Sin(randAngle), 0f, Mathf.Cos(randAngle)) * randOffset;
        var landingPositionRay = landingPos2D + (transform.position.y + landingRaycastUp) * Vector3.up;

        if (Physics.Raycast(landingPositionRay, -Vector3.up, out var raycastHit, landingRaycastUp + landingRaycastDown, 0b10000000000000000100100000001, QueryTriggerInteraction.Ignore))
        {
            // Raycast again incase any mods hook GetItemFloorPosition
            var landingPosition = heldObject.GetItemFloorPosition(raycastHit.point + Vector3.up);

            ShootItem(player, landingPosition);
        }
        else
        {
            var landingPosition = landingPos2D + (transform.position.y * Vector3.up);

            ShootItem(player, landingPosition);
        }
    }

    public void ShootItem(PlayerControllerB player, Vector3 landingPosition)
    {
        var heldObject = player.currentlyHeldObjectServer;

        player.SetSpecialGrabAnimationBool(false, heldObject);
        player.playerBodyAnimator.SetBool("cancelHolding", true);
        player.playerBodyAnimator.SetTrigger("Throw");
        HUDManager.Instance.itemSlotIcons[player.currentItemSlot].enabled = false;
        HUDManager.Instance.holdingTwoHandedItem.enabled = false;

        var localPos = StartOfRound.Instance.propsContainer.InverseTransformPoint(transform.position + 0.75f * Vector3.up);

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
            var localPos = StartOfRound.Instance.propsContainer.InverseTransformPoint(transform.position + 0.75f * Vector3.up);

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

        var launchTotalDistance = Vector3.Distance(shootPoint.position, landingPosition);
        var launchProgress = 0f;

        while (launchProgress < launchTotalDistance)
        {
            var currentPosition = SampleParabola(shootPoint.position, landingPosition, arcHeight, launchProgress / launchTotalDistance);
            var localPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(currentPosition);

            heldObject.startFallingPosition = localPosition;
            heldObject.targetFloorPosition = localPosition;
            heldObject.transform.localPosition = localPosition;

            launchProgress += launchSpeed * Time.deltaTime;

            yield return null;
        }

        var finalLocalPosition = StartOfRound.Instance.propsContainer.InverseTransformPoint(landingPosition);

        heldObject.EnablePhysics(true);
        heldObject.startFallingPosition = finalLocalPosition;
        heldObject.targetFloorPosition = finalLocalPosition;
        heldObject.transform.localPosition = finalLocalPosition;
        heldObject.hasHitGround = false;
        heldObject.fallTime = 0f;
    }

    // https://forum.unity.com/threads/generating-dynamic-parabola.211681/#post-1426169
    private Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
    {
        float parabolicT = t * 2 - 1;
        if (Mathf.Abs(start.y - end.y) < 0.1f)
        {
            //start and end are roughly level, pretend they are - simpler solution with less steps
            Vector3 travelDirection = end - start;
            Vector3 result = start + t * travelDirection;
            result.y += (-parabolicT * parabolicT + 1) * height;
            return result;
        }
        else
        {
            //start and end are not level, gets more complicated
            Vector3 travelDirection = end - start;
            Vector3 levelDirecteion = end - new Vector3(start.x, end.y, start.z);
            Vector3 right = Vector3.Cross(travelDirection, levelDirecteion);
            Vector3 up = Vector3.Cross(right, travelDirection);
            if (end.y > start.y) up = -up;
            Vector3 result = start + t * travelDirection;
            result += ((-parabolicT * parabolicT + 1) * height) * up.normalized;
            return result;
        }
    }
}