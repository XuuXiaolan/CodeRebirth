using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class MoveTransformBasedOnShipModel : MonoBehaviour
{
    public Transform ThingToMove = null!;

    public void MoveTransform()
    {
        Ray ray = new Ray(ThingToMove.transform.position, Vector3.down);
        Physics.Raycast(ray, out RaycastHit hit, 50f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore);
        Plugin.ExtendedLogging($"Raycast hit: {hit.point} with normal: {hit.normal}");
        ThingToMove.transform.position = hit.point;
    }
}