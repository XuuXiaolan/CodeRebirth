namespace CodeRebirth.src.Content.Items;
public class GoldRigo : GrabbableObject
{
    public SmallRigoManager smallRigoManager = null!;

    public override void Start()
    {
        base.Start();
        // When the GoldRigo is grabbed, and not in orbit and ship isnt leaving and ship isnt landing, spawn the 20 SmallRigo's
        // every half a second Instantiate a SmallRigo until 20 are spawned.
        // Instantly hide the spawned SmallRigo's
        // When the ship is leaving or landing, Hide the spawned SmallRigo's
        // Activate SmallRigo's when a player grabs the GoldRigo
        // When activating the SmallRigo's, warp their agent somewhere nearby.
        // Make them all path to the person holding the GoldRigo
        // Only deactivate the SmallRigo's when the ship is landing, leaving, in orbit, or the GoldRigo is somehow destroyed.
        // When player drops the GoldRigo, the SmallRigo's grab it and take it to where it was spawned, and they rest there until deactivated.
        // Depending on what phase the game is in and what they're doing, either their agent is disabled temporarily, or they're hidden.
    }

    public override void GrabItem()
    {
        base.GrabItem();
        smallRigoManager.Activate();
    }
}