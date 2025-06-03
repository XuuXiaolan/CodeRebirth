using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFanBackCollider : NetworkBehaviour
{
    public IndustrialFan industrialFan = null!;

    private void OnTriggerStay(Collider collider)
    {
        if (industrialFan.IsObstructed(collider.transform.position)) return;
        // Calculate the new position by interpolating between the collider's current position and the fan's position
        Vector3 targetPosition = industrialFan.fanTransform.position;
        Vector3 direction = (targetPosition - collider.transform.position).normalized;
        float step = industrialFan.suctionForce * Time.fixedDeltaTime;

        if (Vector3.Distance(collider.transform.position, targetPosition) > step)
        {
            collider.transform.position += direction * step;
        }
        else
        {
            collider.transform.position = targetPosition;
        }
    }
}