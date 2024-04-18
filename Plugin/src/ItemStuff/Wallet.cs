using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class Wallet : GrabbableObject {
    private RaycastHit hit;
    public AudioSource WalletPlayer;

    public override void Start() {
        base.Start();
    }
    public override void Update() {
        base.Update();
        DetectUseKey();
    }

    public override void ItemActivate(bool used, bool buttonDown = true) {
        base.ItemActivate(used, buttonDown);
    }
    
    public void DetectUseKey() {
        if (Plugin.InputActionsInstance.UseWallet.triggered) { // Keybind is in CodeRebirthInputs.cs
            //Logic to detect if thing you pressed key on while hovering over is a specific scrap!
            var interactRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
            if (Physics.Raycast(interactRay, out hit, playerHeldBy.grabDistance, playerHeldBy.interactableObjectsMask) && hit.collider.gameObject.layer != 8) {
                // set coin you're looking at
                Money coin = hit.collider.transform.gameObject.GetComponent<Money>();
                coin.customGrabTooltip = "Yummy"; //todo: fix this tmrw, throws a null exception error at this line for some reason
                
                if (coin == null) return;
                this.scrapValue += coin.scrapValue;
                Destroy(coin);
            }
        }
    }
}