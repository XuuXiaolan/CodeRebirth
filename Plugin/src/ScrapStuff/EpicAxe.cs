using CodeRebirth.Util.Extensions;

namespace CodeRebirth.ScrapStuff;
public class EpicAxe : Shovel { // Added for potential future implementations
    public System.Random random = new();
    public void HitShovel() {
        if (StartOfRound.Instance != null) {
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
        } else {
            random = new System.Random(69);
        }
        int force = this.shovelHitForce;
        this.shovelHitForce = ShovelExtensions.CriticalHit(this.shovelHitForce, random);
        base.HitShovel();
        this.shovelHitForce = force;
    }
}