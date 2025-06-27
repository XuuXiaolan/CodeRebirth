using CodeRebirth.src.Content.Maps;

namespace CodeRebirth.src.MiscScripts;
public class ACUnitBounds : BoundsDefiner
{
    public void OnEnable()
    {
        AirControlUnit.safeBounds.Add(this);
    }

    public void OnDisable()
    {
        AirControlUnit.safeBounds.Remove(this);
    }
}