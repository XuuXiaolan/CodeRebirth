using static CodeRebirth.src.Content.Unlockables.PlantPot;

namespace CodeRebirth.src.Content.Items;
public class WoodenSeed : GrabbableObject
{
    public FruitType fruitType = FruitType.None;
    public override void Start()
    {
        base.Start();
        System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed);

        fruitType = (FruitType)random.Next(1, 3);
    }
}