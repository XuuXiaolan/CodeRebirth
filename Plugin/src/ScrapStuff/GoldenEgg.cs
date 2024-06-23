using CodeRebirth.Misc;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class GoldenEgg : GrabbableObject {
    public override void Start() {
        base.Start();
        GetComponent<ScrapValueSyncer>().SetScrapValue(scrapValue);
    }
}