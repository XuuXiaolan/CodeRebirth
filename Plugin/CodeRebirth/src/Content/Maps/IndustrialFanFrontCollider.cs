using Dawn.Utils;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;
public class IndustrialFanFrontCollider : MonoBehaviour
{
    public IndustrialFan industrialFan = null!;

    private void OnTriggerStay(Collider other)
    {
        PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();
        if (!player.IsLocalPlayer())
            return;

        if (industrialFan.IsObstructed(other.transform.position))
        {
            return;
        }

        float distance = Vector3.Distance(other.transform.position, industrialFan.fanTransform.position);
        if (distance > 20f)
        {
            return;
        }

        Vector3 pushDirection = (other.transform.position - industrialFan.fanTransform.position).normalized;
        Vector3 targetPosition = other.transform.position + (pushDirection * industrialFan.pushForce);

        float pushForceMultiplier = 1 - (distance / 20f);
        other.transform.position = Vector3.Lerp(other.transform.position, targetPosition, industrialFan.pushForce * pushForceMultiplier * Time.fixedDeltaTime);
    }
}