using CodeRebirth.Misc;
using CodeRebirth.Util.Spawning;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class GoldenEgg : GrabbableObject {

    public NetworkVariable<bool> NetworkGrabbable = new NetworkVariable<bool>(true);
    public NetworkVariable<bool> NetworkGrabbableToEnemies = new NetworkVariable<bool>(true);

    public new bool grabbable
    {
        get => NetworkGrabbable.Value;
        set => NetworkGrabbable.Value = value;
    }

    public new bool grabbableToEnemies
    {
        get => NetworkGrabbableToEnemies.Value;
        set => NetworkGrabbableToEnemies.Value = value;
    }

    public override void Start() {
        base.Start();
        NetworkGrabbable.OnValueChanged += (_, value) => {
            grabbable = value;
        };

        CodeRebirthUtils.goldenEggs.Add(this);
    }
}