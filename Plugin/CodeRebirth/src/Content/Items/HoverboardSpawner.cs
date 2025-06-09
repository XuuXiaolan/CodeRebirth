namespace CodeRebirth.src.Content.Items;

public class HoverboardSpawner : GrabbableObject
{
    private FuturisticHoverboard? _hoverboard = null;
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (_hoverboard != null)
        {
            // despawn
        }
        // spawn
    }
}