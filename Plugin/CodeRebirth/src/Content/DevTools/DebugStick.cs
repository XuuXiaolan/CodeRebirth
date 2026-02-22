using Dawn;

namespace CodeRebirth.src.Content.DevTools;

public class DebugStick : GrabbableObject
{

    // public Dictionary<DawnMapObjectInfo, HologramCopy> hologramCopies = new Dictionary<DawnMapObjectInfo, HologramCopy>();

    public DawnMapObjectInfo currentlySelectedHazard;
    public void Awake()
    {
        // Q and E to cycle through list of hazards
    }
}