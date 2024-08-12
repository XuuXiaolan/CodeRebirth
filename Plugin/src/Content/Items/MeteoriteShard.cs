using CodeRebirth.src.MiscScripts;

namespace CodeRebirth.src.Content.Items;
public class MeteoriteShard : GrabbableObject {
    public override void Start() {
        base.Start();
        GetComponent<ScrapValueSyncer>().SetScrapValue(scrapValue);
    }
}