using CodeRebirth.src.MiscScripts;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TimeSlower : GrabbableObject
{
    public Animator animator = null!;

    private static readonly int activateWatch = Animator.StringToHash("activateWatch");

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        animator.SetTrigger(activateWatch);
        SlowDownEffect.DoSlowdownEffect(30, 0.2f);
    }
}