using Dawn.Utils;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFanFrontCollider : MonoBehaviour
{
    public IndustrialFan industrialFan = null!;

    private void OnTriggerStay(Collider other)
    {
        if (!industrialFan.IsObstructed(other.transform.position))
        {
            Vector3 pushDirection = (other.transform.position - industrialFan.fanTransform.position).normalized;
            Vector3 targetPosition = other.transform.position + (pushDirection * industrialFan.pushForce);
            if (Physics.Linecast(other.transform.position, targetPosition, MoreLayerMasks.CollidersAndRoomAndRailingAndInteractableMask, QueryTriggerInteraction.Ignore))
            {
                other.transform.position = Vector3.Lerp(other.transform.position, targetPosition, industrialFan.pushForce * Time.fixedDeltaTime * 0.1f);
            }
            else
            {
                other.transform.position = Vector3.Lerp(other.transform.position, targetPosition, industrialFan.pushForce * Time.fixedDeltaTime);
            }
        }
    }
}