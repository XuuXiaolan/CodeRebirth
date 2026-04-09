using Dawn.Utils;
using UnityEngine;

namespace CodeRebirth.src.Content.Maps;

public class UnityWheelColliderProxy : BearTrapWheelProxy
{
    public override void SetupWheel()
    {
        WheelCollider vehicleTyre = GetComponent<WheelCollider>();
        SphereCollider sphereCollider = vehicleTyre.gameObject.AddComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = vehicleTyre.radius;
        sphereCollider.includeLayers = MoreLayerMasks.HazardMask;
    }

    public override void PunctureWheel()
    {
        base.PunctureWheel();
        WheelCollider vehicleTyre = GetComponent<WheelCollider>();
        vehicleTyre.radius *= 0.9f;

        WheelFrictionCurve forwardFriction = vehicleTyre.forwardFriction;
        forwardFriction.stiffness *= 0.7f;
        vehicleTyre.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = vehicleTyre.sidewaysFriction;
        sidewaysFriction.stiffness *= 0.4f;
        vehicleTyre.sidewaysFriction = sidewaysFriction;
    }
}