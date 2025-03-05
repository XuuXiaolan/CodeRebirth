using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class LaunchableItem : GrabbableObject
{
    public bool isLaunching { get; private set; } = false;
    public override void FallWithCurve()
    {
        base.FallWithCurve();
    }
}