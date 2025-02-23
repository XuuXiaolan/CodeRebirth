using CodeRebirth.src.MiscScripts;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class TimeSlower : GrabbableObject
{
    public Animator animator = null!;

    private static readonly int activateWatch = Animator.StringToHash("activateWatch");
    private static readonly int watchSpeed = Animator.StringToHash("watchSpeed");

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        animator.SetTrigger(activateWatch);
        animator.SetFloat(watchSpeed, 1/30);
        SlowDownEffect.DoSlowdownEffect(30, 0.2f);
    }
}