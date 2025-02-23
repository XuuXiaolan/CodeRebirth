using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class ShipUnlocker : GrabbableObject
{
    public int creditValue = 0;

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        playerHeldBy.inSpecialInteractAnimation = true;
        Terminal terminal = GameObject.FindFirstObjectByType<Terminal>();
        List<UnlockableItem> unlockableItems = new();
        foreach (var item in StartOfRound.Instance.unlockablesList.unlockables)
        {
            if (item.hasBeenUnlockedByPlayer) continue;
            unlockableItems.Add(item);
        }
        if (unlockableItems.Count <= 0) return;
        UnlockableItem randomItem = unlockableItems[Random.Range(0, unlockableItems.Count)];
        StartOfRound.Instance.BuyShipUnlockableServerRpc(StartOfRound.Instance.unlockablesList.unlockables.IndexOf(randomItem), terminal.groupCredits);
        StartCoroutine(WaitForEndOfFrame());
    }

    private IEnumerator WaitForEndOfFrame()
    {
        yield return new WaitForSeconds(0.2f);
        playerHeldBy.inSpecialInteractAnimation = false;
        playerHeldBy.DespawnHeldObject();
    }
}