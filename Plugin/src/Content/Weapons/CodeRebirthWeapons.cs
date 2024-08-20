using UnityEngine;

namespace CodeRebirth.src.Content.Weapons;
public class CodeRebirthWeapons : Shovel
{
    public int defaultForce = 0;
    public bool critPossible = true;
    public float critChance = 25;
    public bool canBreakTrees = false;
    public Transform weaponTip = null!;

    public void Awake() {
        defaultForce = shovelHitForce;
        canBreakTrees = Plugin.ModConfig.ConfigCanBreakTrees.Value;
        critChance = Plugin.ModConfig.ConfigCritChance.Value;
    }
}