namespace CodeRebirth.src.Patches;
public static class SpikeTrapPatch
{
    public static void Init()
    {
        On.SpikeRoofTrap.Start += SpikeRoofTrap_Start;
    }

    private static void SpikeRoofTrap_Start(On.SpikeRoofTrap.orig_Start orig, SpikeRoofTrap self)
    {
        orig(self);
        var parent = self.gameObject.transform.parent;
        parent.transform.Find("BaseSupport").gameObject.layer = 21;
        parent.transform.Find("SpikeRoof").gameObject.layer = 21;
        parent.transform.Find("SpikeRoof").Find("MovingBar").gameObject.layer = 21;
    }
}