using CodeRebirth.src.Content.Maps;

namespace CodeRebirth.src.MiscScripts;
public class GregBounds : BoundsDefiner
{
    public void OnEnable()
    {
        GunslingerGreg.safeBounds.Add(this);
    }

    public void OnDisable()
    {
        GunslingerGreg.safeBounds.Remove(this);
    }
}