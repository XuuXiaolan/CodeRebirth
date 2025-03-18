namespace CodeRebirth.src.Content.Items;
public class GoldRigo : GrabbableObject
{
    public SmallRigoManager smallRigoManager = null!;

    public override void Start()
    {
        base.Start();
        // Activate SmallRigo's when a player grabs the GoldRigo
        // When activating the SmallRigo's, warp their agent somewhere nearby.
        // When player drops the GoldRigo, the SmallRigo's grab it and take it to where it was spawned, and they rest there until deactivated.
        // Depending on what phase the game is in and what they're doing, either their agent is disabled temporarily, or they're hidden.
    }

    public override void GrabItem()
    {
        base.GrabItem();
        smallRigoManager.Activate();
    }
}