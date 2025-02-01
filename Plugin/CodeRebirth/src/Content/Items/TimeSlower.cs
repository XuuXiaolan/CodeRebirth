using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Content.Items;
public class TimeSlower : GrabbableObject
{
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        SlowDownEffect.DoSlowdownEffect(30, 0.2f);
    }
}