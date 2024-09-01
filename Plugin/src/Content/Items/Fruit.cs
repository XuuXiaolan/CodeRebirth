using UnityEngine;
using static CodeRebirth.src.Content.Unlockables.PlantPot;

namespace CodeRebirth.src.Content.Items;
public class Fruit : GrabbableObject {
    public FruitType fruitType = FruitType.None;
    public Renderer[] renderers;
    public override void Start() {
        base.Start();

        renderers[(int)fruitType].enabled = true;
    }
}