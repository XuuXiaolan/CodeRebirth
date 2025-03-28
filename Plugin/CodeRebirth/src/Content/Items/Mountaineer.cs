namespace CodeRebirth.src.Content.Items;
public class Mountaineer : GrabbableObject
{
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        // Hold type mechanic
        // Enable specialanimation so they cant drop.
        // Also maybe instead override fall curve so that it doesnt fall down if you drop it and just sticks
        // Constantly resets player gravity on update.
        // When let go launches the player a bit up
    }
}