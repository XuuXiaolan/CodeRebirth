using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class CreditPad : GrabbableObject
{
    public int creditValue = 0;

    public override void InteractItem()
    {
        base.InteractItem();
        playerHeldBy.inSpecialInteractAnimation = true;
        IncreaseShipValueServerRpc();
        StartCoroutine(WaitForEndOfFrame());
    }

    private IEnumerator WaitForEndOfFrame()
    {
        yield return new WaitForSeconds(0.2f);
        playerHeldBy.inSpecialInteractAnimation = false;
        playerHeldBy.DespawnHeldObject();
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseShipValueServerRpc()
    {
        Terminal? terminal = GameObject.FindFirstObjectByType<Terminal>();
        if (terminal == null) return;
        int moneyToBe = terminal.groupCredits + creditValue;
        if (moneyToBe < 0) moneyToBe = 0; 
        terminal.SyncGroupCreditsClientRpc(moneyToBe, terminal.numberOfItemsInDropship);
    }
}