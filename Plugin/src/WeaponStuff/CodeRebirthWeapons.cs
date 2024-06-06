namespace CodeRebirth.WeaponStuff;
public class CodeRebirthWeapons : Shovel
{
    public int defaultForce = 0;
    public bool critPossible = true;
    public int critChance = 25;

    public void Awake() {
        defaultForce = shovelHitForce;
    }
}