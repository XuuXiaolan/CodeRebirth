using System;
using System.Collections;
using CodeRebirth.src.Content.Unlockables;
using Dawn;
using Dawn.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodeRebirth.src.Content.DevTools;

public class XuBuck : GrabbableObject
{
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!LethalContent.Unlockables.TryGetValue(CodeRebirthUnlockableItemKeys.DenominationAnalyzer, out DawnUnlockableItemInfo unlockableItemInfo))
        {
            return;
        }

        if (unlockableItemInfo.UnlockableItem.inStorage && (unlockableItemInfo.UnlockableItem.hasBeenUnlockedByPlayer || unlockableItemInfo.UnlockableItem.alreadyUnlocked))
        {
            StartOfRound.Instance.ReturnUnlockableFromStorageServerRpc(StartOfRound.Instance.unlockablesList.unlockables.IndexOf(unlockableItemInfo.UnlockableItem));
            return;
        }
        StartOfRound.Instance.BuyShipUnlockableServerRpc(StartOfRound.Instance.unlockablesList.unlockables.IndexOf(unlockableItemInfo.UnlockableItem), TerminalRefs.Instance.groupCredits);
        StartCoroutine(WaitToMaxCounter());
    }

    public override void Update()
    {
        base.Update();
        if (isPocketed || !isHeld)
        {
            return;
        }

        Keyboard? keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (MoneyCounter.Instance == null)
        {
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            MoneyCounter.Instance.RemoveMoney(MoneyCounter.Instance.MoneyStored());
        }
        else if (keyboard.zKey.wasPressedThisFrame)
        {
            MoneyCounter.Instance.RemoveMoney(MoneyCounter.Instance.MoneyStored() + 1);
        }
    }

    private IEnumerator WaitToMaxCounter()
    {
        yield return new WaitForSeconds(1f);
        int moneyRequired = Math.Clamp(999 - MoneyCounter.Instance.MoneyStored(), 0, 999);
        MoneyCounter.Instance.AddMoney(moneyRequired);
    }
}