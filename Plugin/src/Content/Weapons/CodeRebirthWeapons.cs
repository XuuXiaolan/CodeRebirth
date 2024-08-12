namespace CodeRebirth.src.Content.Weapons;
public class CodeRebirthWeapons : Shovel
{
    public int defaultForce = 0;
    public bool critPossible = true;
    public float critChance = 25;

    public void Awake() {
        defaultForce = shovelHitForce;
        critChance = Plugin.ModConfig.ConfigCritChance.Value;
    }
}