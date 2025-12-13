using Dawn.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFanBackCollider : MonoBehaviour
{
    public IndustrialFan industrialFan = null!;

    private void OnTriggerStay(Collider other)
    {
        PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
        if (!player.IsLocalPlayer())
            return;

        if (industrialFan.IsObstructed(other.transform.position) || Vector3.Distance(other.transform.position, industrialFan.fanTransform.position) > 20f)
        {
            return;
        }

        Vector3 targetPosition = industrialFan.fanTransform.position;
        Vector3 direction = (targetPosition - other.transform.position).normalized;
        float step = industrialFan.suctionForce * Time.fixedDeltaTime;

        if (Vector3.Distance(other.transform.position, targetPosition) > step)
        {
            other.transform.position += direction * step;
        }
        else
        {
            other.transform.position = targetPosition;
        }
    }
}